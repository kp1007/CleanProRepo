# CleanPro - .NET 8 Clean Architecture Solution

A production-ready .NET 8 solution implementing **Clean Architecture** with a custom **zero-reflection CQRS** pattern. This project demonstrates how to build scalable, maintainable applications without relying on heavy libraries like MediatR or AutoMapper.

## Key Features

- **Zero-Reflection CQRS** - Custom dispatcher using self-referencing generics
- **No MediatR** - Lightweight, transparent command/query handling
- **No AutoMapper** - Explicit, debuggable mapping
- **Result Pattern** - Clean error handling without exceptions
- **FluentValidation** - Robust input validation
- **EF Core 8** - Modern data access with SQL Server

## Quick Start

```bash
# Clone and navigate
cd CleanProRepo

# Restore and run
dotnet restore
dotnet run --project src/CleanPro.WebAPI

# Open Swagger UI
# https://localhost:5001/swagger
```

## Solution Structure

```
CleanPro.sln
│
├── src/
│   ├── CleanPro.Domain/           # Enterprise business rules
│   │   ├── Common/                # Base entities, value objects, results
│   │   ├── Entities/              # Domain entities (Product)
│   │   ├── Events/                # Domain events
│   │   ├── Exceptions/            # Domain exceptions
│   │   └── Interfaces/            # Repository interfaces
│   │
│   ├── CleanPro.Application/      # Application business rules
│   │   ├── Common/                # CQRS infrastructure
│   │   │   ├── Behaviors/         # Pipeline behaviors
│   │   │   ├── Extensions/        # DI extensions
│   │   │   └── Interfaces/        # Command/Query interfaces
│   │   └── Features/              # Feature modules
│   │       └── Products/          # Product CRUD feature
│   │
│   ├── CleanPro.Infrastructure/   # External concerns
│   │   ├── Persistence/           # EF Core DbContext & repos
│   │   └── DependencyInjection/   # Service registration
│   │
│   └── CleanPro.WebAPI/           # Presentation layer
│       ├── Controllers/           # API controllers
│       ├── Middleware/            # Exception handling
│       └── Extensions/            # Result extensions
│
└── docs/                          # Documentation
    ├── architecture.md            # Architecture overview
    ├── cqrs-guide.md              # CQRS implementation details
    ├── request-flow.md            # Request lifecycle
    └── getting-started.md         # Setup guide
```

## Architecture Overview

This solution follows **Clean Architecture** principles with clear separation of concerns:

```
┌─────────────────────────────────────────────────────────────┐
│                        WebAPI                                │
│                   (Controllers, Middleware)                  │
├─────────────────────────────────────────────────────────────┤
│                      Application                             │
│              (Commands, Queries, Handlers)                   │
├─────────────────────────────────────────────────────────────┤
│                        Domain                                │
│            (Entities, Value Objects, Events)                 │
├─────────────────────────────────────────────────────────────┤
│                     Infrastructure                           │
│               (EF Core, Repositories)                        │
└─────────────────────────────────────────────────────────────┘
```

**Dependency Rule**: Dependencies point inward. Domain has no dependencies; Infrastructure depends on Application and Domain.

## CQRS Pattern

Our custom CQRS implementation uses **self-referencing generics** for compile-time safety:

```csharp
// Command definition
public record CreateProductCommand(string Name, decimal Price)
    : ICommand<CreateProductCommand, Result<Guid>>;

// Handler
public class CreateProductHandler
    : ICommandHandler<CreateProductCommand, Result<Guid>>
{
    public Task<Result<Guid>> HandleAsync(CreateProductCommand cmd, CancellationToken ct)
    {
        // Implementation
    }
}

// Usage in controller
var result = await _dispatcher.SendAsync<CreateProductCommand, Result<Guid>>(command, ct);
```

**Why zero-reflection?** The dispatcher resolves handlers via `GetRequiredService<T>()` with compile-time known types - no `MakeGenericType()` or reflection.

## Documentation

| Document | Description |
|----------|-------------|
| [Architecture Guide](docs/architecture.md) | Detailed architecture explanation |
| [CQRS Guide](docs/cqrs-guide.md) | How our CQRS implementation works |
| [Request Flow](docs/request-flow.md) | Complete request lifecycle |
| [Getting Started](docs/getting-started.md) | Setup and development guide |

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/products` | List all products |
| GET | `/api/products/{id}` | Get product by ID |
| POST | `/api/products` | Create new product |
| PUT | `/api/products/{id}` | Update product |
| DELETE | `/api/products/{id}` | Delete product |

## Technology Stack

| Technology | Version | Purpose |
|------------|---------|---------|
| .NET | 8.0 | Runtime & SDK |
| ASP.NET Core | 8.0 | Web framework |
| Entity Framework Core | 8.0.10 | ORM |
| FluentValidation | 11.9.0 | Input validation |
| SQL Server | - | Database |
| Swagger/OpenAPI | - | API documentation |

## Design Decisions

### Why No MediatR?

1. **Transparency** - Direct handler resolution, no magic
2. **Performance** - No reflection overhead
3. **Debugging** - Clear call stack
4. **Simplicity** - Fewer abstractions

### Why No AutoMapper?

1. **Explicitness** - See exactly what maps to what
2. **Compile-time safety** - No runtime mapping failures
3. **Performance** - No reflection or expression compilation
4. **Debugging** - Step through mapping code

### Why Result Pattern?

1. **No exceptions for control flow** - Cleaner, faster
2. **Explicit error handling** - Caller must handle failures
3. **Rich error information** - Structured error codes and messages
4. **Composable** - Chain operations with Match/Map

## License

MIT License - See [LICENSE](LICENSE) for details.
