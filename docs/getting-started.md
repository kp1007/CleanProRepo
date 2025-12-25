# Getting Started Guide

This guide will help you set up the CleanPro solution for development and understand how to add new features.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Installation](#installation)
3. [Running the Application](#running-the-application)
4. [Project Configuration](#project-configuration)
5. [Adding a New Feature](#adding-a-new-feature)
6. [Testing](#testing)
7. [Common Tasks](#common-tasks)

---

## Prerequisites

### Required Software

| Software | Version | Download |
|----------|---------|----------|
| .NET SDK | 8.0+ | [Download](https://dotnet.microsoft.com/download/dotnet/8.0) |
| SQL Server | 2019+ | [Download](https://www.microsoft.com/sql-server/sql-server-downloads) |
| IDE | Any | VS Code, Visual Studio, Rider |

### Optional Tools

| Tool | Purpose |
|------|---------|
| Docker | Run SQL Server in container |
| Azure Data Studio | Database management |
| Postman/Insomnia | API testing |

### Verify Installation

```bash
# Check .NET SDK
dotnet --version
# Expected: 8.0.x

# Check available SDKs
dotnet --list-sdks
```

---

## Installation

### 1. Clone the Repository

```bash
git clone <repository-url>
cd CleanProRepo
```

### 2. Restore Dependencies

```bash
dotnet restore
```

### 3. Configure Database

**Option A: SQL Server LocalDB (Windows)**

The default connection string uses LocalDB:
```json
// src/CleanPro.WebAPI/appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CleanProDb;Trusted_Connection=True"
  }
}
```

**Option B: SQL Server (Docker)**

```bash
# Start SQL Server container
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Passw0rd" \
    -p 1433:1433 --name sqlserver -d mcr.microsoft.com/mssql/server:2022-latest

# Update connection string in appsettings.json
# "Server=localhost,1433;Database=CleanProDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True"
```

**Option C: In-Memory Database (Development)**

Modify `Program.cs` to use in-memory database:
```csharp
// Instead of:
builder.Services.AddInfrastructure(builder.Configuration);

// Use:
builder.Services.AddApplication();
builder.Services.AddInfrastructureWithInMemoryDb();
```

### 4. Apply Database Migrations (if using SQL Server)

```bash
# Install EF Core tools (if not installed)
dotnet tool install --global dotnet-ef

# Create migration (from solution root)
dotnet ef migrations add InitialCreate \
    --project src/CleanPro.Infrastructure \
    --startup-project src/CleanPro.WebAPI

# Apply migration
dotnet ef database update \
    --project src/CleanPro.Infrastructure \
    --startup-project src/CleanPro.WebAPI
```

---

## Running the Application

### Using .NET CLI

```bash
# Run the API
dotnet run --project src/CleanPro.WebAPI

# Run with hot reload
dotnet watch run --project src/CleanPro.WebAPI
```

### Access Points

| URL | Purpose |
|-----|---------|
| https://localhost:5001/swagger | Swagger UI |
| https://localhost:5001/api/products | Products API |
| http://localhost:5000 | HTTP (redirects to HTTPS) |

### Test the API

```bash
# List products
curl https://localhost:5001/api/products -k

# Create a product
curl -X POST https://localhost:5001/api/products \
    -H "Content-Type: application/json" \
    -d '{"name":"Widget","description":"A widget","price":29.99,"stockQuantity":100}' \
    -k

# Get a product
curl https://localhost:5001/api/products/{id} -k
```

---

## Project Configuration

### Solution Structure

```
CleanPro.sln
├── src/
│   ├── CleanPro.Domain/           # No dependencies
│   ├── CleanPro.Application/      # → Domain
│   ├── CleanPro.Infrastructure/   # → Application → Domain
│   └── CleanPro.WebAPI/           # → Infrastructure → ...
└── docs/
```

### Key Configuration Files

| File | Purpose |
|------|---------|
| `global.json` | SDK version constraints |
| `.editorconfig` | Code style rules |
| `appsettings.json` | Application settings |
| `launchSettings.json` | Debug/run profiles |

### Environment Variables

```bash
# Override connection string
export ConnectionStrings__DefaultConnection="Server=..."

# Set environment
export ASPNETCORE_ENVIRONMENT=Development
```

---

## Adding a New Feature

Let's add a complete `Category` feature step by step.

### Step 1: Create Domain Entity

```csharp
// src/CleanPro.Domain/Entities/Category.cs

using CleanPro.Domain.Common;

namespace CleanPro.Domain.Entities;

public sealed class Category : AuditableEntity
{
    private Category() { }

    private Category(Guid id, string name, string description)
    {
        Id = id;
        Name = name;
        Description = description;
    }

    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    public static Category Create(string name, string description)
    {
        return new Category(Guid.NewGuid(), name, description);
    }

    public void Update(string name, string description)
    {
        Name = name;
        Description = description;
    }
}
```

### Step 2: Create Repository Interface

```csharp
// src/CleanPro.Domain/Interfaces/ICategoryRepository.cs

using CleanPro.Domain.Entities;

namespace CleanPro.Domain.Interfaces;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default);
    void Add(Category category);
    void Update(Category category);
    void Remove(Category category);
}
```

### Step 3: Create Application DTOs and Mapper

```csharp
// src/CleanPro.Application/Features/Categories/CategoryDto.cs

namespace CleanPro.Application.Features.Categories;

public sealed record CategoryDto(
    Guid Id,
    string Name,
    string Description,
    DateTimeOffset CreatedAt);
```

```csharp
// src/CleanPro.Application/Features/Categories/CategoryMapper.cs

using CleanPro.Domain.Entities;

namespace CleanPro.Application.Features.Categories;

public static class CategoryMapper
{
    public static CategoryDto ToDto(this Category category)
    {
        return new CategoryDto(
            category.Id,
            category.Name,
            category.Description,
            category.CreatedAt);
    }

    public static IReadOnlyList<CategoryDto> ToDto(this IEnumerable<Category> categories)
    {
        return categories.Select(c => c.ToDto()).ToList();
    }
}
```

### Step 4: Create Command

```csharp
// src/CleanPro.Application/Features/Categories/Commands/CreateCategory/CreateCategoryCommand.cs

using CleanPro.Application.Common.Interfaces;
using CleanPro.Domain.Common.Results;

namespace CleanPro.Application.Features.Categories.Commands.CreateCategory;

public sealed record CreateCategoryCommand(
    string Name,
    string Description
) : ICommand<CreateCategoryCommand, Result<Guid>>;
```

### Step 5: Create Command Validator

```csharp
// src/CleanPro.Application/Features/Categories/Commands/CreateCategory/CreateCategoryCommandValidator.cs

using FluentValidation;

namespace CleanPro.Application.Features.Categories.Commands.CreateCategory;

public sealed class CreateCategoryCommandValidator
    : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");
    }
}
```

### Step 6: Create Command Handler

```csharp
// src/CleanPro.Application/Features/Categories/Commands/CreateCategory/CreateCategoryCommandHandler.cs

using CleanPro.Application.Common.Interfaces;
using CleanPro.Domain.Common.Results;
using CleanPro.Domain.Entities;
using CleanPro.Domain.Interfaces;

namespace CleanPro.Application.Features.Categories.Commands.CreateCategory;

public sealed class CreateCategoryCommandHandler
    : ICommandHandler<CreateCategoryCommand, Result<Guid>>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCategoryCommandHandler(
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> HandleAsync(
        CreateCategoryCommand command,
        CancellationToken ct = default)
    {
        var category = Category.Create(command.Name, command.Description);

        _categoryRepository.Add(category);
        await _unitOfWork.SaveChangesAsync(ct);

        return category.Id;
    }
}
```

### Step 7: Create Query

```csharp
// src/CleanPro.Application/Features/Categories/Queries/GetCategories/GetCategoriesQuery.cs

using CleanPro.Application.Common.Interfaces;

namespace CleanPro.Application.Features.Categories.Queries.GetCategories;

public sealed record GetCategoriesQuery
    : IQuery<GetCategoriesQuery, IReadOnlyList<CategoryDto>>;
```

### Step 8: Create Query Handler

```csharp
// src/CleanPro.Application/Features/Categories/Queries/GetCategories/GetCategoriesQueryHandler.cs

using CleanPro.Application.Common.Interfaces;
using CleanPro.Domain.Interfaces;

namespace CleanPro.Application.Features.Categories.Queries.GetCategories;

public sealed class GetCategoriesQueryHandler
    : IQueryHandler<GetCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    private readonly ICategoryRepository _categoryRepository;

    public GetCategoriesQueryHandler(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<IReadOnlyList<CategoryDto>> HandleAsync(
        GetCategoriesQuery query,
        CancellationToken ct = default)
    {
        var categories = await _categoryRepository.GetAllAsync(ct);
        return categories.ToDto();
    }
}
```

### Step 9: Create Repository Implementation

```csharp
// src/CleanPro.Infrastructure/Persistence/Repositories/CategoryRepository.cs

using CleanPro.Domain.Entities;
using CleanPro.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CleanPro.Infrastructure.Persistence.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly ApplicationDbContext _context;

    public CategoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Categories.OrderBy(c => c.Name).ToListAsync(ct);
    }

    public void Add(Category category) => _context.Categories.Add(category);
    public void Update(Category category) => _context.Categories.Update(category);
    public void Remove(Category category) => _context.Categories.Remove(category);
}
```

### Step 10: Update DbContext

```csharp
// src/CleanPro.Infrastructure/Persistence/ApplicationDbContext.cs

public DbSet<Category> Categories => Set<Category>();
```

### Step 11: Create EF Configuration

```csharp
// src/CleanPro.Infrastructure/Persistence/Configurations/CategoryConfiguration.cs

using CleanPro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanPro.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Description)
            .HasMaxLength(500);

        builder.HasIndex(c => c.Name).IsUnique();
    }
}
```

### Step 12: Register Repository

```csharp
// src/CleanPro.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs

// Add to AddInfrastructure method:
services.AddScoped<ICategoryRepository, CategoryRepository>();
```

### Step 13: Create Controller

```csharp
// src/CleanPro.WebAPI/Controllers/CategoriesController.cs

using CleanPro.Application.Common.Interfaces;
using CleanPro.Application.Features.Categories;
using CleanPro.Application.Features.Categories.Commands.CreateCategory;
using CleanPro.Application.Features.Categories.Queries.GetCategories;
using CleanPro.Domain.Common.Results;
using CleanPro.WebAPI.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace CleanPro.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public CategoriesController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpGet]
    public async Task<IActionResult> GetCategories(CancellationToken ct)
    {
        var query = new GetCategoriesQuery();
        var categories = await _dispatcher
            .QueryAsync<GetCategoriesQuery, IReadOnlyList<CategoryDto>>(query, ct);
        return Ok(categories);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCategory(
        CreateCategoryRequest request,
        CancellationToken ct)
    {
        var command = new CreateCategoryCommand(request.Name, request.Description);
        var result = await _dispatcher
            .SendAsync<CreateCategoryCommand, Result<Guid>>(command, ct);
        return result.ToCreatedResult("GetCategory", new { id = result.Value });
    }
}

public record CreateCategoryRequest(string Name, string Description);
```

### Step 14: Add Migration and Test

```bash
# Create migration
dotnet ef migrations add AddCategories \
    --project src/CleanPro.Infrastructure \
    --startup-project src/CleanPro.WebAPI

# Update database
dotnet ef database update \
    --project src/CleanPro.Infrastructure \
    --startup-project src/CleanPro.WebAPI

# Run and test
dotnet run --project src/CleanPro.WebAPI
```

---

## Testing

### Unit Test Structure (Recommended)

```
tests/
├── CleanPro.Domain.Tests/
│   └── Entities/
│       └── ProductTests.cs
├── CleanPro.Application.Tests/
│   └── Features/Products/
│       └── CreateProductCommandHandlerTests.cs
└── CleanPro.WebAPI.Tests/
    └── Controllers/
        └── ProductsControllerTests.cs
```

### Example Unit Test

```csharp
public class CreateProductCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithValidCommand_ReturnsSuccessWithId()
    {
        // Arrange
        var mockRepo = new Mock<IProductRepository>();
        mockRepo.Setup(r => r.NameExistsAsync(It.IsAny<string>(), null, default))
            .ReturnsAsync(false);

        var mockUoW = new Mock<IUnitOfWork>();
        mockUoW.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var handler = new CreateProductCommandHandler(mockRepo.Object, mockUoW.Object);
        var command = new CreateProductCommand("Test", "Description", 10.00m, 5);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        mockRepo.Verify(r => r.Add(It.IsAny<Product>()), Times.Once);
    }
}
```

---

## Common Tasks

### Add a New NuGet Package

```bash
# Add to specific project
dotnet add src/CleanPro.Application/CleanPro.Application.csproj \
    package PackageName --version X.X.X
```

### Run Code Analysis

```bash
dotnet build /p:TreatWarningsAsErrors=true
```

### Format Code

```bash
dotnet format
```

### Generate EF Migration

```bash
dotnet ef migrations add MigrationName \
    --project src/CleanPro.Infrastructure \
    --startup-project src/CleanPro.WebAPI
```

### Check for Outdated Packages

```bash
dotnet list package --outdated
```
