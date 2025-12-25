# Architecture Guide

This document explains the architectural decisions and patterns used in the CleanPro solution.

## Table of Contents

1. [Clean Architecture Overview](#clean-architecture-overview)
2. [Layer Responsibilities](#layer-responsibilities)
3. [Dependency Flow](#dependency-flow)
4. [Project Dependencies](#project-dependencies)
5. [Design Patterns Used](#design-patterns-used)

---

## Clean Architecture Overview

Clean Architecture, introduced by Robert C. Martin (Uncle Bob), emphasizes separation of concerns through concentric layers with dependencies pointing inward.

```
                    ┌───────────────────────────────────┐
                    │           Presentation            │
                    │    (WebAPI, Controllers, Views)   │
                    └───────────────┬───────────────────┘
                                    │
                    ┌───────────────▼───────────────────┐
                    │           Application             │
                    │   (Use Cases, Commands, Queries)  │
                    └───────────────┬───────────────────┘
                                    │
                    ┌───────────────▼───────────────────┐
                    │             Domain                │
                    │   (Entities, Value Objects)       │
                    └───────────────────────────────────┘
                                    ▲
                    ┌───────────────┴───────────────────┐
                    │          Infrastructure           │
                    │    (Database, External Services)  │
                    └───────────────────────────────────┘
```

### Core Principles

1. **Independence of Frameworks** - Architecture doesn't depend on libraries
2. **Testability** - Business rules can be tested without UI, database, or external services
3. **Independence of UI** - UI can change without changing business rules
4. **Independence of Database** - Business rules aren't bound to a specific database
5. **Independence of External Agencies** - Business rules don't know about the outside world

---

## Layer Responsibilities

### Domain Layer (`CleanPro.Domain`)

The innermost layer containing enterprise business rules.

```
CleanPro.Domain/
├── Common/
│   ├── BaseEntity.cs           # Base class with ID and domain events
│   ├── AuditableEntity.cs      # Adds audit fields (CreatedAt, ModifiedBy, etc.)
│   ├── Results/
│   │   ├── Result.cs           # Result pattern implementation
│   │   ├── Error.cs            # Structured error type
│   │   └── Unit.cs             # Void replacement for commands
│   └── ValueObjects/
│       ├── ValueObject.cs      # Base class for value objects
│       └── Money.cs            # Example value object
├── Entities/
│   └── Product.cs              # Domain entity with behavior
├── Events/
│   ├── IDomainEvent.cs         # Domain event interface
│   ├── DomainEvent.cs          # Base domain event
│   └── ProductCreatedEvent.cs  # Specific domain event
├── Exceptions/
│   └── DomainException.cs      # Domain-specific exceptions
└── Interfaces/
    ├── IProductRepository.cs   # Repository interface
    └── IUnitOfWork.cs          # Unit of work interface
```

**Key Characteristics:**
- Zero external dependencies (no NuGet packages)
- Contains business logic and rules
- Defines interfaces that outer layers implement
- Rich domain models with behavior (not anemic)

**Example - Product Entity:**

```csharp
public sealed class Product : AuditableEntity
{
    // Private setters - state changes through methods
    public string Name { get; private set; }
    public Money Price { get; private set; }

    // Factory method - ensures valid creation
    public static Product Create(string name, decimal price, int stock)
    {
        var product = new Product(Guid.NewGuid(), name, Money.Create(price), stock);
        product.RaiseDomainEvent(new ProductCreatedEvent(product.Id));
        return product;
    }

    // Behavior method - encapsulates business logic
    public void Update(string name, decimal price, int stock)
    {
        Name = name;
        Price = Money.Create(price);
        RaiseDomainEvent(new ProductUpdatedEvent(Id));
    }
}
```

---

### Application Layer (`CleanPro.Application`)

Contains application-specific business rules and orchestrates data flow.

```
CleanPro.Application/
├── Common/
│   ├── Interfaces/
│   │   ├── ICommand.cs         # Command marker interface
│   │   ├── IQuery.cs           # Query marker interface
│   │   ├── ICommandHandler.cs  # Command handler interface
│   │   ├── IQueryHandler.cs    # Query handler interface
│   │   └── IDispatcher.cs      # Dispatcher interface
│   ├── Dispatcher.cs           # CQRS dispatcher implementation
│   ├── Behaviors/
│   │   ├── ValidationBehavior.cs   # Validation pipeline
│   │   └── LoggingBehavior.cs      # Logging pipeline
│   └── Extensions/
│       └── ServiceCollectionExtensions.cs  # DI registration
└── Features/
    └── Products/
        ├── ProductDto.cs       # Data transfer object
        ├── ProductMapper.cs    # Manual mapping
        ├── Commands/
        │   ├── CreateProduct/
        │   │   ├── CreateProductCommand.cs
        │   │   ├── CreateProductCommandHandler.cs
        │   │   └── CreateProductCommandValidator.cs
        │   ├── UpdateProduct/
        │   └── DeleteProduct/
        └── Queries/
            ├── GetProduct/
            └── GetProducts/
```

**Key Characteristics:**
- Implements use cases (commands and queries)
- Defines DTOs for data transfer
- Contains validation logic
- Orchestrates domain operations
- No knowledge of infrastructure details

**Example - Command Handler:**

```csharp
public class CreateProductCommandHandler
    : ICommandHandler<CreateProductCommand, Result<Guid>>
{
    private readonly IProductRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Result<Guid>> HandleAsync(
        CreateProductCommand command,
        CancellationToken ct)
    {
        // Business rule: Name must be unique
        if (await _repository.NameExistsAsync(command.Name, ct))
            return Error.Conflict("Product.NameExists", "Name already exists");

        // Create domain entity
        var product = Product.Create(
            command.Name,
            command.Price,
            command.StockQuantity);

        // Persist
        _repository.Add(product);
        await _unitOfWork.SaveChangesAsync(ct);

        return product.Id;
    }
}
```

---

### Infrastructure Layer (`CleanPro.Infrastructure`)

Implements interfaces defined in inner layers.

```
CleanPro.Infrastructure/
├── Persistence/
│   ├── ApplicationDbContext.cs         # EF Core DbContext
│   ├── Configurations/
│   │   └── ProductConfiguration.cs     # Entity configuration
│   └── Repositories/
│       └── ProductRepository.cs        # Repository implementation
└── DependencyInjection/
    └── ServiceCollectionExtensions.cs  # Infrastructure DI
```

**Key Characteristics:**
- Implements repository interfaces
- Contains database configuration
- Handles external service integration
- Depends on Application and Domain layers

**Example - Repository Implementation:**

```csharp
public class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _context;

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public void Add(Product product)
    {
        _context.Products.Add(product);
    }
}
```

---

### Presentation Layer (`CleanPro.WebAPI`)

Handles HTTP requests and responses.

```
CleanPro.WebAPI/
├── Controllers/
│   └── ProductsController.cs   # API endpoints
├── Middleware/
│   └── ExceptionHandlingMiddleware.cs  # Global error handling
├── Extensions/
│   └── ResultExtensions.cs     # Result to IActionResult conversion
├── Program.cs                  # Application entry point
└── appsettings.json           # Configuration
```

**Key Characteristics:**
- Thin controllers - delegate to Application layer
- Handles HTTP concerns (status codes, content negotiation)
- Configures middleware pipeline
- No business logic

---

## Dependency Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│   WebAPI ──────────────────┐                                    │
│     │                      │                                    │
│     │ depends on           │ depends on                         │
│     ▼                      ▼                                    │
│   Application ◄─────── Infrastructure                          │
│     │                      │                                    │
│     │ depends on           │ depends on                         │
│     ▼                      │                                    │
│   Domain ◄─────────────────┘                                    │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

**Dependency Rule Enforcement:**
- Domain has no project references
- Application references only Domain
- Infrastructure references Application (and transitively Domain)
- WebAPI references Infrastructure (and transitively all others)

---

## Project Dependencies

### CleanPro.Domain.csproj
```xml
<!-- No dependencies - pure C# -->
<PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
</PropertyGroup>
```

### CleanPro.Application.csproj
```xml
<ItemGroup>
    <PackageReference Include="FluentValidation" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
</ItemGroup>
<ItemGroup>
    <ProjectReference Include="..\CleanPro.Domain\CleanPro.Domain.csproj" />
</ItemGroup>
```

### CleanPro.Infrastructure.csproj
```xml
<ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" />
</ItemGroup>
<ItemGroup>
    <ProjectReference Include="..\CleanPro.Application\CleanPro.Application.csproj" />
</ItemGroup>
```

### CleanPro.WebAPI.csproj
```xml
<ItemGroup>
    <PackageReference Include="Swashbuckle.AspNetCore" />
</ItemGroup>
<ItemGroup>
    <ProjectReference Include="..\CleanPro.Infrastructure\CleanPro.Infrastructure.csproj" />
</ItemGroup>
```

---

## Design Patterns Used

| Pattern | Location | Purpose |
|---------|----------|---------|
| **CQRS** | Application | Separate read/write operations |
| **Repository** | Domain/Infrastructure | Abstract data access |
| **Unit of Work** | Domain/Infrastructure | Manage transactions |
| **Result Pattern** | Domain | Handle errors without exceptions |
| **Value Object** | Domain | Immutable domain concepts |
| **Domain Events** | Domain | Decouple domain side effects |
| **Factory Method** | Domain Entities | Encapsulate creation logic |
| **Decorator** | Application Behaviors | Add cross-cutting concerns |

---

## Benefits of This Architecture

1. **Testability** - Each layer can be tested in isolation
2. **Maintainability** - Changes in one layer don't ripple through
3. **Flexibility** - Swap implementations without changing business logic
4. **Scalability** - Clear boundaries for team ownership
5. **Understandability** - Predictable code organization
