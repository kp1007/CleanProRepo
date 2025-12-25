# CQRS Implementation Guide

This document explains our custom zero-reflection CQRS (Command Query Responsibility Segregation) implementation in detail.

## Table of Contents

1. [What is CQRS?](#what-is-cqrs)
2. [Why Custom Implementation?](#why-custom-implementation)
3. [Core Interfaces](#core-interfaces)
4. [The Dispatcher](#the-dispatcher)
5. [Creating Commands](#creating-commands)
6. [Creating Queries](#creating-queries)
7. [Handler Registration](#handler-registration)
8. [Pipeline Behaviors](#pipeline-behaviors)
9. [Comparison with MediatR](#comparison-with-mediatr)

---

## What is CQRS?

CQRS separates read operations (Queries) from write operations (Commands):

```
┌─────────────────────────────────────────────────────────────┐
│                        Client                                │
└─────────────────────────┬───────────────────────────────────┘
                          │
            ┌─────────────┴─────────────┐
            │                           │
            ▼                           ▼
    ┌───────────────┐           ┌───────────────┐
    │   Commands    │           │    Queries    │
    │  (Write Ops)  │           │  (Read Ops)   │
    └───────┬───────┘           └───────┬───────┘
            │                           │
            ▼                           ▼
    ┌───────────────┐           ┌───────────────┐
    │    Command    │           │     Query     │
    │   Handlers    │           │   Handlers    │
    └───────┬───────┘           └───────┬───────┘
            │                           │
            ▼                           ▼
    ┌───────────────┐           ┌───────────────┐
    │  Write Model  │           │  Read Model   │
    │  (Domain)     │           │  (Optimized)  │
    └───────────────┘           └───────────────┘
```

**Benefits:**
- Clear separation of concerns
- Optimized read/write paths
- Easier to scale independently
- Simplified testing

---

## Why Custom Implementation?

### MediatR Approach (Reflection-Based)

```csharp
// MediatR uses reflection internally
public interface IRequest<TResponse> { }

// Handler resolution involves:
// 1. typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType)
// 2. ServiceProvider.GetService(handlerType)
// 3. MethodInfo.Invoke() or compiled expressions
```

### Our Approach (Zero-Reflection)

```csharp
// Self-referencing generic constraint
public interface ICommand<TCommand, TResult>
    where TCommand : ICommand<TCommand, TResult> { }

// Dispatcher knows exact types at compile time
public Task<TResult> SendAsync<TCommand, TResult>(TCommand command, ...)
    where TCommand : ICommand<TCommand, TResult>
{
    // Direct generic resolution - NO reflection!
    var handler = _sp.GetRequiredService<ICommandHandler<TCommand, TResult>>();
    return handler.HandleAsync(command, ct);
}
```

### Comparison

| Aspect | MediatR | Our Implementation |
|--------|---------|-------------------|
| Handler Resolution | Reflection | Direct DI |
| Type Safety | Runtime | Compile-time |
| Performance | Good | Excellent |
| Debugging | Complex stack | Clear stack |
| Dependencies | NuGet package | None |
| Learning Curve | Medium | Low |

---

## Core Interfaces

### Command Interface

```csharp
// Location: CleanPro.Application/Common/Interfaces/ICommand.cs

namespace CleanPro.Application.Common.Interfaces;

/// <summary>
/// Marker interface for commands with self-referencing generic constraint.
/// The constraint ensures type safety at compile time.
/// </summary>
/// <typeparam name="TCommand">The command type itself</typeparam>
/// <typeparam name="TResult">The result type</typeparam>
public interface ICommand<TCommand, TResult>
    where TCommand : ICommand<TCommand, TResult>
{
}
```

**Why Self-Referencing Generic?**

```csharp
// This constraint prevents invalid implementations:

// VALID - CreateProductCommand references itself
public record CreateProductCommand(string Name)
    : ICommand<CreateProductCommand, Result<Guid>>;

// INVALID - Compiler error! UpdateProductCommand doesn't match
public record BadCommand(string Name)
    : ICommand<CreateProductCommand, Result<Guid>>; // Won't compile!
```

### Query Interface

```csharp
// Location: CleanPro.Application/Common/Interfaces/IQuery.cs

namespace CleanPro.Application.Common.Interfaces;

/// <summary>
/// Marker interface for queries. Same pattern as commands.
/// </summary>
public interface IQuery<TQuery, TResult>
    where TQuery : IQuery<TQuery, TResult>
{
}
```

### Handler Interfaces

```csharp
// Location: CleanPro.Application/Common/Interfaces/ICommandHandler.cs

public interface ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TCommand, TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

// Location: CleanPro.Application/Common/Interfaces/IQueryHandler.cs

public interface IQueryHandler<TQuery, TResult>
    where TQuery : IQuery<TQuery, TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
```

### Dispatcher Interface

```csharp
// Location: CleanPro.Application/Common/Interfaces/IDispatcher.cs

public interface IDispatcher
{
    Task<TResult> SendAsync<TCommand, TResult>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand<TCommand, TResult>;

    Task<TResult> QueryAsync<TQuery, TResult>(
        TQuery query,
        CancellationToken cancellationToken = default)
        where TQuery : IQuery<TQuery, TResult>;
}
```

---

## The Dispatcher

The dispatcher is the heart of our CQRS implementation:

```csharp
// Location: CleanPro.Application/Common/Dispatcher.cs

public sealed class Dispatcher : IDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public Dispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<TResult> SendAsync<TCommand, TResult>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand<TCommand, TResult>
    {
        // ZERO REFLECTION!
        // The generic type parameters are known at compile time.
        // DI container directly resolves ICommandHandler<TCommand, TResult>
        var handler = _serviceProvider
            .GetRequiredService<ICommandHandler<TCommand, TResult>>();

        return handler.HandleAsync(command, cancellationToken);
    }

    public Task<TResult> QueryAsync<TQuery, TResult>(
        TQuery query,
        CancellationToken cancellationToken = default)
        where TQuery : IQuery<TQuery, TResult>
    {
        var handler = _serviceProvider
            .GetRequiredService<IQueryHandler<TQuery, TResult>>();

        return handler.HandleAsync(query, cancellationToken);
    }
}
```

### How It Works

```
1. Controller calls:
   dispatcher.SendAsync<CreateProductCommand, Result<Guid>>(command, ct)

2. Dispatcher receives call with compile-time known types:
   TCommand = CreateProductCommand
   TResult = Result<Guid>

3. DI container resolves:
   ICommandHandler<CreateProductCommand, Result<Guid>>
   → CreateProductCommandHandler

4. Handler executes and returns Result<Guid>
```

**No reflection needed because:**
- Generic type parameters are resolved at compile time
- DI container has explicit registration for the handler
- Method call is direct, not via MethodInfo.Invoke()

---

## Creating Commands

### Command Definition

Commands represent intentions to change state:

```csharp
// Location: CleanPro.Application/Features/Products/Commands/CreateProduct/

// 1. Define the command as a record (immutable)
public sealed record CreateProductCommand(
    string Name,
    string Description,
    decimal Price,
    int StockQuantity
) : ICommand<CreateProductCommand, Result<Guid>>;
```

### Command Handler

```csharp
// 2. Implement the handler
public sealed class CreateProductCommandHandler
    : ICommandHandler<CreateProductCommand, Result<Guid>>
{
    private readonly IProductRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductCommandHandler(
        IProductRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> HandleAsync(
        CreateProductCommand command,
        CancellationToken ct = default)
    {
        // Validate business rules
        if (await _repository.NameExistsAsync(command.Name, ct: ct))
        {
            return Error.Conflict(
                "Product.NameExists",
                $"Product '{command.Name}' already exists");
        }

        // Create domain entity
        var product = Product.Create(
            command.Name,
            command.Description,
            command.Price,
            command.StockQuantity);

        // Persist
        _repository.Add(product);
        await _unitOfWork.SaveChangesAsync(ct);

        // Return success with created ID
        return product.Id;
    }
}
```

### Command Validator

```csharp
// 3. Add validation with FluentValidation
public sealed class CreateProductCommandValidator
    : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name too long");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be positive");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Stock cannot be negative");
    }
}
```

### Commands Without Return Value

Use the `Unit` type for commands that don't return data:

```csharp
// Command that just performs an action
public sealed record DeleteProductCommand(Guid Id)
    : ICommand<DeleteProductCommand, Result<Unit>>;

// Handler
public sealed class DeleteProductCommandHandler
    : ICommandHandler<DeleteProductCommand, Result<Unit>>
{
    public async Task<Result<Unit>> HandleAsync(
        DeleteProductCommand command,
        CancellationToken ct)
    {
        var product = await _repository.GetByIdAsync(command.Id, ct);
        if (product is null)
            return Error.NotFound("Product", command.Id);

        _repository.Remove(product);
        await _unitOfWork.SaveChangesAsync(ct);

        return Unit.Value;  // Success with no data
    }
}
```

---

## Creating Queries

### Query Definition

Queries represent requests for data (read-only):

```csharp
// Location: CleanPro.Application/Features/Products/Queries/GetProducts/

public sealed record GetProductsQuery(bool ActiveOnly = false)
    : IQuery<GetProductsQuery, IReadOnlyList<ProductDto>>;
```

### Query Handler

```csharp
public sealed class GetProductsQueryHandler
    : IQueryHandler<GetProductsQuery, IReadOnlyList<ProductDto>>
{
    private readonly IProductRepository _repository;

    public GetProductsQueryHandler(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<ProductDto>> HandleAsync(
        GetProductsQuery query,
        CancellationToken ct)
    {
        var products = query.ActiveOnly
            ? await _repository.GetActiveProductsAsync(ct)
            : await _repository.GetAllAsync(ct);

        // Manual mapping - no AutoMapper
        return products.ToDto();
    }
}
```

### Query with Result Pattern

```csharp
// For queries that might not find data
public sealed record GetProductQuery(Guid Id)
    : IQuery<GetProductQuery, Result<ProductDto>>;

public sealed class GetProductQueryHandler
    : IQueryHandler<GetProductQuery, Result<ProductDto>>
{
    public async Task<Result<ProductDto>> HandleAsync(
        GetProductQuery query,
        CancellationToken ct)
    {
        var product = await _repository.GetByIdAsync(query.Id, ct);

        if (product is null)
            return Error.NotFound("Product", query.Id);

        return product.ToDto();
    }
}
```

---

## Handler Registration

Handlers are auto-registered by scanning the assembly:

```csharp
// Location: CleanPro.Application/Common/Extensions/ServiceCollectionExtensions.cs

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Register dispatcher
        services.AddScoped<IDispatcher, Dispatcher>();

        // Auto-register all handlers
        services.AddHandlersFromAssembly(assembly);

        // Register all validators
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }

    public static IServiceCollection AddHandlersFromAssembly(
        this IServiceCollection services,
        Assembly assembly)
    {
        // Find all handler implementations
        var handlerTypes = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType &&
                    (i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>) ||
                     i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>)))
                .Select(i => new { Implementation = t, Interface = i }))
            .ToList();

        // Register each handler
        foreach (var handler in handlerTypes)
        {
            services.AddScoped(handler.Interface, handler.Implementation);
        }

        return services;
    }
}
```

**Registration Result:**

```
ICommandHandler<CreateProductCommand, Result<Guid>> → CreateProductCommandHandler
ICommandHandler<UpdateProductCommand, Result<Unit>> → UpdateProductCommandHandler
ICommandHandler<DeleteProductCommand, Result<Unit>> → DeleteProductCommandHandler
IQueryHandler<GetProductQuery, Result<ProductDto>>  → GetProductQueryHandler
IQueryHandler<GetProductsQuery, IReadOnlyList<ProductDto>> → GetProductsQueryHandler
```

---

## Pipeline Behaviors

Behaviors add cross-cutting concerns (validation, logging) without modifying handlers.

### Validation Behavior

```csharp
// Location: CleanPro.Application/Common/Behaviors/ValidationBehavior.cs

public sealed class ValidationBehavior<TCommand, TResult>
    : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TCommand, TResult>
{
    private readonly ICommandHandler<TCommand, TResult> _next;
    private readonly IEnumerable<IValidator<TCommand>> _validators;

    public ValidationBehavior(
        ICommandHandler<TCommand, TResult> next,
        IEnumerable<IValidator<TCommand>> validators)
    {
        _next = next;
        _validators = validators;
    }

    public async Task<TResult> HandleAsync(
        TCommand command,
        CancellationToken ct = default)
    {
        // Skip if no validators
        if (!_validators.Any())
            return await _next.HandleAsync(command, ct);

        // Run all validators
        var context = new ValidationContext<TCommand>(command);
        var results = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, ct)));

        var failures = results
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        // Return validation errors as Result
        if (failures.Count > 0)
        {
            var errors = failures
                .Select(f => Error.Validation(f.PropertyName, f.ErrorMessage))
                .ToArray();

            // Handle Result<T> return types
            return CreateValidationResult<TResult>(errors);
        }

        // Continue to actual handler
        return await _next.HandleAsync(command, ct);
    }
}
```

### Logging Behavior

```csharp
// Location: CleanPro.Application/Common/Behaviors/LoggingBehavior.cs

public sealed class LoggingBehavior<TCommand, TResult>
    : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TCommand, TResult>
{
    private readonly ICommandHandler<TCommand, TResult> _next;
    private readonly ILogger<LoggingBehavior<TCommand, TResult>> _logger;

    public async Task<TResult> HandleAsync(
        TCommand command,
        CancellationToken ct = default)
    {
        var commandName = typeof(TCommand).Name;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Handling {Command}", commandName);

        try
        {
            var result = await _next.HandleAsync(command, ct);

            _logger.LogInformation(
                "Handled {Command} in {ElapsedMs}ms",
                commandName,
                stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed {Command} after {ElapsedMs}ms",
                commandName,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
```

---

## Comparison with MediatR

### Code Comparison

**MediatR:**
```csharp
// Command
public class CreateProductCommand : IRequest<Result<Guid>>
{
    public string Name { get; set; }
}

// Handler
public class CreateProductHandler
    : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    public Task<Result<Guid>> Handle(
        CreateProductCommand request,
        CancellationToken ct) { }
}

// Usage
await _mediator.Send(command);  // Type inferred
```

**Our Implementation:**
```csharp
// Command
public record CreateProductCommand(string Name)
    : ICommand<CreateProductCommand, Result<Guid>>;

// Handler
public class CreateProductHandler
    : ICommandHandler<CreateProductCommand, Result<Guid>>
{
    public Task<Result<Guid>> HandleAsync(
        CreateProductCommand command,
        CancellationToken ct) { }
}

// Usage - explicit types required
await _dispatcher.SendAsync<CreateProductCommand, Result<Guid>>(command, ct);
```

### Trade-offs

| Aspect | MediatR | Our Implementation |
|--------|---------|-------------------|
| **Syntax** | Shorter (`Send(cmd)`) | Longer (explicit generics) |
| **Type Safety** | Runtime errors possible | Compile-time guaranteed |
| **Performance** | Slight overhead | Direct invocation |
| **Debugging** | More complex | Straightforward |
| **Features** | Notifications, Streams | Commands/Queries only |
| **Ecosystem** | Large, many behaviors | Build what you need |

### When to Choose Each

**Choose MediatR when:**
- You want minimal boilerplate
- You need advanced features (notifications, streams)
- You're okay with some reflection overhead
- Team is familiar with it

**Choose Our Implementation when:**
- You want full control and understanding
- Performance is critical
- You prefer explicit over implicit
- You want minimal dependencies
- You're building a new project from scratch
