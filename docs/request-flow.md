# Request Flow Guide

This document traces a complete HTTP request through all layers of the application, explaining each step in detail.

## Table of Contents

1. [Overview](#overview)
2. [Complete Request Flow Diagram](#complete-request-flow-diagram)
3. [Step-by-Step: Create Product](#step-by-step-create-product)
4. [Step-by-Step: Get Product](#step-by-step-get-product)
5. [Error Handling Flow](#error-handling-flow)
6. [Sequence Diagrams](#sequence-diagrams)

---

## Overview

Every request flows through these layers:

```
HTTP Request
    │
    ▼
┌─────────────────────────────────────┐
│           ASP.NET Core              │
│         Middleware Pipeline         │
│  (Exception, Auth, Routing, etc.)   │
└─────────────────┬───────────────────┘
                  │
                  ▼
┌─────────────────────────────────────┐
│            Controller               │
│     (Receives, Validates Shape)     │
└─────────────────┬───────────────────┘
                  │
                  ▼
┌─────────────────────────────────────┐
│            Dispatcher               │
│     (Routes to Handler)             │
└─────────────────┬───────────────────┘
                  │
                  ▼
┌─────────────────────────────────────┐
│          Command/Query              │
│            Handler                  │
│    (Executes Business Logic)        │
└─────────────────┬───────────────────┘
                  │
                  ▼
┌─────────────────────────────────────┐
│           Repository                │
│      (Data Access)                  │
└─────────────────┬───────────────────┘
                  │
                  ▼
┌─────────────────────────────────────┐
│       Entity Framework Core         │
│          (Database)                 │
└─────────────────────────────────────┘
```

---

## Complete Request Flow Diagram

```
┌──────────────────────────────────────────────────────────────────────────┐
│                              HTTP Request                                 │
│                     POST /api/products                                    │
│                     { "name": "Widget", "price": 29.99 }                  │
└──────────────────────────────────┬───────────────────────────────────────┘
                                   │
                                   ▼
┌──────────────────────────────────────────────────────────────────────────┐
│  1. MIDDLEWARE PIPELINE                                                   │
│  ┌────────────────────────────────────────────────────────────────────┐  │
│  │  ExceptionHandlingMiddleware                                        │  │
│  │  - Wraps entire request in try/catch                               │  │
│  │  - Converts exceptions to ProblemDetails                           │  │
│  └────────────────────────────────────────────────────────────────────┘  │
│                                   │                                       │
│  ┌────────────────────────────────▼───────────────────────────────────┐  │
│  │  Routing Middleware                                                 │  │
│  │  - Matches route: POST /api/products → ProductsController.Create   │  │
│  └────────────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────┬───────────────────────────────────────┘
                                   │
                                   ▼
┌──────────────────────────────────────────────────────────────────────────┐
│  2. CONTROLLER (ProductsController.cs)                                    │
│  ┌────────────────────────────────────────────────────────────────────┐  │
│  │  [HttpPost]                                                         │  │
│  │  public async Task<IActionResult> CreateProduct(                    │  │
│  │      CreateProductRequest request, CancellationToken ct)            │  │
│  │  {                                                                  │  │
│  │      // Map request DTO to command                                  │  │
│  │      var command = new CreateProductCommand(                        │  │
│  │          request.Name, request.Description,                         │  │
│  │          request.Price, request.StockQuantity);                     │  │
│  │                                                                     │  │
│  │      // Dispatch command                                            │  │
│  │      var result = await _dispatcher                                 │  │
│  │          .SendAsync<CreateProductCommand, Result<Guid>>(            │  │
│  │              command, ct);                                          │  │
│  │                                                                     │  │
│  │      // Convert Result to HTTP response                             │  │
│  │      return result.ToCreatedResult("GetProduct", new { id = ... }); │  │
│  │  }                                                                  │  │
│  └────────────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────┬───────────────────────────────────────┘
                                   │
                                   ▼
┌──────────────────────────────────────────────────────────────────────────┐
│  3. DISPATCHER (Dispatcher.cs)                                            │
│  ┌────────────────────────────────────────────────────────────────────┐  │
│  │  public Task<TResult> SendAsync<TCommand, TResult>(...)             │  │
│  │  {                                                                  │  │
│  │      // Resolve handler from DI (NO REFLECTION!)                    │  │
│  │      var handler = _serviceProvider                                 │  │
│  │          .GetRequiredService<ICommandHandler<TCommand, TResult>>(); │  │
│  │                                                                     │  │
│  │      // Execute handler                                             │  │
│  │      return handler.HandleAsync(command, ct);                       │  │
│  │  }                                                                  │  │
│  └────────────────────────────────────────────────────────────────────┘  │
│                                                                          │
│  DI Resolution:                                                          │
│  ICommandHandler<CreateProductCommand, Result<Guid>>                     │
│      → CreateProductCommandHandler                                       │
└──────────────────────────────────┬───────────────────────────────────────┘
                                   │
                                   ▼
┌──────────────────────────────────────────────────────────────────────────┐
│  4. COMMAND HANDLER (CreateProductCommandHandler.cs)                      │
│  ┌────────────────────────────────────────────────────────────────────┐  │
│  │  public async Task<Result<Guid>> HandleAsync(                       │  │
│  │      CreateProductCommand command, CancellationToken ct)            │  │
│  │  {                                                                  │  │
│  │      // Business rule: Check name uniqueness                        │  │
│  │      if (await _repository.NameExistsAsync(command.Name, ct: ct))   │  │
│  │          return Error.Conflict("Product.NameExists", "...");        │  │
│  │                                                                     │  │
│  │      // Create domain entity                                        │  │
│  │      var product = Product.Create(                                  │  │
│  │          command.Name, command.Description,                         │  │
│  │          command.Price, command.StockQuantity);                     │  │
│  │                                                                     │  │
│  │      // Add to repository                                           │  │
│  │      _repository.Add(product);                                      │  │
│  │                                                                     │  │
│  │      // Save changes (Unit of Work)                                 │  │
│  │      await _unitOfWork.SaveChangesAsync(ct);                        │  │
│  │                                                                     │  │
│  │      // Return success with ID                                      │  │
│  │      return product.Id;                                             │  │
│  │  }                                                                  │  │
│  └────────────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────┬───────────────────────────────────────┘
                                   │
                                   ▼
┌──────────────────────────────────────────────────────────────────────────┐
│  5. DOMAIN ENTITY (Product.cs)                                           │
│  ┌────────────────────────────────────────────────────────────────────┐  │
│  │  public static Product Create(...)                                  │  │
│  │  {                                                                  │  │
│  │      var product = new Product(                                     │  │
│  │          Guid.NewGuid(),                                            │  │
│  │          name,                                                      │  │
│  │          description,                                               │  │
│  │          Money.Create(price),                                       │  │
│  │          stockQuantity);                                            │  │
│  │                                                                     │  │
│  │      // Raise domain event                                          │  │
│  │      product.RaiseDomainEvent(                                      │  │
│  │          new ProductCreatedEvent(product.Id, name, price));         │  │
│  │                                                                     │  │
│  │      return product;                                                │  │
│  │  }                                                                  │  │
│  └────────────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────┬───────────────────────────────────────┘
                                   │
                                   ▼
┌──────────────────────────────────────────────────────────────────────────┐
│  6. REPOSITORY (ProductRepository.cs)                                     │
│  ┌────────────────────────────────────────────────────────────────────┐  │
│  │  public void Add(Product product)                                   │  │
│  │  {                                                                  │  │
│  │      _context.Products.Add(product);                                │  │
│  │  }                                                                  │  │
│  └────────────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────┬───────────────────────────────────────┘
                                   │
                                   ▼
┌──────────────────────────────────────────────────────────────────────────┐
│  7. UNIT OF WORK / DB CONTEXT (ApplicationDbContext.cs)                   │
│  ┌────────────────────────────────────────────────────────────────────┐  │
│  │  public override async Task<int> SaveChangesAsync(...)              │  │
│  │  {                                                                  │  │
│  │      // Update audit fields                                         │  │
│  │      UpdateAuditableEntities();                                     │  │
│  │                                                                     │  │
│  │      // Persist to database                                         │  │
│  │      return await base.SaveChangesAsync(ct);                        │  │
│  │  }                                                                  │  │
│  └────────────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────┬───────────────────────────────────────┘
                                   │
                                   ▼
┌──────────────────────────────────────────────────────────────────────────┐
│  8. DATABASE                                                              │
│  ┌────────────────────────────────────────────────────────────────────┐  │
│  │  INSERT INTO Products (Id, Name, Description, Price, ...)           │  │
│  │  VALUES ('abc-123', 'Widget', 'A great widget', 29.99, ...)         │  │
│  └────────────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────┬───────────────────────────────────────┘
                                   │
                                   ▼
┌──────────────────────────────────────────────────────────────────────────┐
│  9. RESPONSE TRANSFORMATION                                               │
│  ┌────────────────────────────────────────────────────────────────────┐  │
│  │  Result<Guid>.Success("abc-123")                                    │  │
│  │      ↓                                                              │  │
│  │  result.ToCreatedResult(...)                                        │  │
│  │      ↓                                                              │  │
│  │  HTTP 201 Created                                                   │  │
│  │  Location: /api/products/abc-123                                    │  │
│  │  Body: "abc-123"                                                    │  │
│  └────────────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────────┘
```

---

## Step-by-Step: Create Product

### 1. HTTP Request Arrives

```http
POST /api/products HTTP/1.1
Content-Type: application/json

{
    "name": "Premium Widget",
    "description": "A high-quality widget",
    "price": 49.99,
    "stockQuantity": 100
}
```

### 2. Middleware Pipeline

```csharp
// Program.cs - Middleware order matters!
app.UseMiddleware<ExceptionHandlingMiddleware>();  // Catches all errors
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();  // Routes to controller
```

### 3. Controller Receives Request

```csharp
// ProductsController.cs
[HttpPost]
public async Task<IActionResult> CreateProduct(
    [FromBody] CreateProductRequest request,
    CancellationToken ct)
{
    // ASP.NET Core automatically deserializes JSON → CreateProductRequest
    // Model binding validates required fields

    // Create command from request
    var command = new CreateProductCommand(
        request.Name,
        request.Description,
        request.Price,
        request.StockQuantity);

    // Dispatch to handler
    var result = await _dispatcher
        .SendAsync<CreateProductCommand, Result<Guid>>(command, ct);

    // Transform result to HTTP response
    return result.ToCreatedResult(
        "GetProduct",
        new { id = result.IsSuccess ? result.Value : Guid.Empty });
}
```

### 4. Dispatcher Routes to Handler

```csharp
// Dispatcher.cs
public Task<TResult> SendAsync<TCommand, TResult>(
    TCommand command,
    CancellationToken ct)
    where TCommand : ICommand<TCommand, TResult>
{
    // DI resolves: ICommandHandler<CreateProductCommand, Result<Guid>>
    //           → CreateProductCommandHandler
    var handler = _serviceProvider
        .GetRequiredService<ICommandHandler<TCommand, TResult>>();

    return handler.HandleAsync(command, ct);
}
```

### 5. Handler Executes Business Logic

```csharp
// CreateProductCommandHandler.cs
public async Task<Result<Guid>> HandleAsync(
    CreateProductCommand command,
    CancellationToken ct)
{
    // STEP 1: Validate business rules
    var nameExists = await _productRepository
        .NameExistsAsync(command.Name, cancellationToken: ct);

    if (nameExists)
    {
        return Error.Conflict(
            "Product.NameExists",
            $"Product with name '{command.Name}' already exists.");
    }

    // STEP 2: Create domain entity
    var product = Product.Create(
        command.Name,
        command.Description,
        command.Price,
        command.StockQuantity);

    // STEP 3: Persist via repository
    _productRepository.Add(product);

    // STEP 4: Commit transaction
    await _unitOfWork.SaveChangesAsync(ct);

    // STEP 5: Return success
    return product.Id;  // Implicit conversion to Result<Guid>
}
```

### 6. Domain Entity Created

```csharp
// Product.cs
public static Product Create(
    string name,
    string description,
    decimal price,
    int stockQuantity)
{
    var product = new Product(
        id: Guid.NewGuid(),
        name: name,
        description: description,
        price: Money.Create(price),
        stockQuantity: stockQuantity);

    product.IsActive = true;

    // Domain event for side effects
    product.RaiseDomainEvent(
        new ProductCreatedEvent(product.Id, name, price));

    return product;
}
```

### 7. Repository Adds to Context

```csharp
// ProductRepository.cs
public void Add(Product product)
{
    _context.Products.Add(product);
    // Entity is now tracked by EF Core in "Added" state
}
```

### 8. DbContext Saves Changes

```csharp
// ApplicationDbContext.cs
public override async Task<int> SaveChangesAsync(CancellationToken ct)
{
    // Auto-populate audit fields
    foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
    {
        if (entry.State == EntityState.Added)
        {
            entry.Entity.CreatedAt = DateTimeOffset.UtcNow;
        }
    }

    // Generate SQL and execute
    return await base.SaveChangesAsync(ct);
}
```

**Generated SQL:**
```sql
INSERT INTO [Products] ([Id], [Name], [Description], [Price], [Currency],
    [StockQuantity], [IsActive], [CreatedAt], [CreatedBy])
VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8);
```

### 9. Response Returned

```csharp
// Result flows back up the chain:
// Handler → Dispatcher → Controller

// Controller transforms to HTTP:
return result.ToCreatedResult("GetProduct", new { id = result.Value });

// ResultExtensions.cs
public static IActionResult ToCreatedResult<T>(
    this Result<T> result,
    string routeName,
    object routeValues)
{
    if (result.IsSuccess)
    {
        return new CreatedAtRouteResult(routeName, routeValues, result.Value);
    }
    return ToProblemDetails(result.Error);
}
```

**HTTP Response:**
```http
HTTP/1.1 201 Created
Location: /api/products/3fa85f64-5717-4562-b3fc-2c963f66afa6
Content-Type: application/json

"3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

---

## Step-by-Step: Get Product

### Request

```http
GET /api/products/3fa85f64-5717-4562-b3fc-2c963f66afa6 HTTP/1.1
```

### Controller

```csharp
[HttpGet("{id:guid}", Name = "GetProduct")]
public async Task<IActionResult> GetProduct(Guid id, CancellationToken ct)
{
    var query = new GetProductQuery(id);

    var result = await _dispatcher
        .QueryAsync<GetProductQuery, Result<ProductDto>>(query, ct);

    return result.ToActionResult();
}
```

### Query Handler

```csharp
public async Task<Result<ProductDto>> HandleAsync(
    GetProductQuery query,
    CancellationToken ct)
{
    var product = await _productRepository.GetByIdAsync(query.Id, ct);

    if (product is null)
    {
        return Error.NotFound("Product", query.Id);
    }

    return product.ToDto();  // Manual mapping
}
```

### Manual Mapping

```csharp
// ProductMapper.cs
public static ProductDto ToDto(this Product product)
{
    return new ProductDto(
        product.Id,
        product.Name,
        product.Description,
        product.Price.Amount,
        product.Price.Currency,
        product.StockQuantity,
        product.IsActive,
        product.CreatedAt);
}
```

### Response

```http
HTTP/1.1 200 OK
Content-Type: application/json

{
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Premium Widget",
    "description": "A high-quality widget",
    "price": 49.99,
    "currency": "USD",
    "stockQuantity": 100,
    "isActive": true,
    "createdAt": "2024-01-15T10:30:00Z"
}
```

---

## Error Handling Flow

### Validation Error

```
Request with invalid data
    │
    ▼
FluentValidation runs
    │
    ▼
Validation fails: "Price must be greater than 0"
    │
    ▼
ValidationBehavior catches errors
    │
    ▼
Returns Result<T>.Failure with errors
    │
    ▼
Controller: result.ToActionResult()
    │
    ▼
ResultExtensions converts to ProblemDetails
    │
    ▼
HTTP 400 Bad Request
{
    "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
    "title": "Bad Request",
    "status": 400,
    "detail": "Price must be greater than 0",
    "errorCode": "Price"
}
```

### Not Found Error

```
GET /api/products/non-existent-id
    │
    ▼
Handler: product = null
    │
    ▼
Handler returns: Error.NotFound("Product", id)
    │
    ▼
Controller: result.ToActionResult()
    │
    ▼
HTTP 404 Not Found
{
    "type": "...",
    "title": "Not Found",
    "status": 404,
    "detail": "Product with ID 'non-existent-id' was not found.",
    "errorCode": "Product.NotFound"
}
```

### Unhandled Exception

```
Any unhandled exception
    │
    ▼
ExceptionHandlingMiddleware catches
    │
    ▼
Logs error
    │
    ▼
Converts to ProblemDetails
    │
    ▼
HTTP 500 Internal Server Error
{
    "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
    "title": "Internal Server Error",
    "status": 500,
    "detail": "An unexpected error occurred."
}
```

---

## Sequence Diagrams

### Create Product Sequence

```
Client          Controller      Dispatcher      Handler         Repository      Database
  │                 │               │              │                │              │
  │ POST /products  │               │              │                │              │
  │────────────────>│               │              │                │              │
  │                 │               │              │                │              │
  │                 │ SendAsync()   │              │                │              │
  │                 │──────────────>│              │                │              │
  │                 │               │              │                │              │
  │                 │               │ GetService() │                │              │
  │                 │               │──────────────│                │              │
  │                 │               │              │                │              │
  │                 │               │ HandleAsync()│                │              │
  │                 │               │─────────────>│                │              │
  │                 │               │              │                │              │
  │                 │               │              │ NameExists?    │              │
  │                 │               │              │───────────────>│              │
  │                 │               │              │                │ SELECT       │
  │                 │               │              │                │─────────────>│
  │                 │               │              │                │<─────────────│
  │                 │               │              │<───────────────│              │
  │                 │               │              │                │              │
  │                 │               │              │ Product.Create │              │
  │                 │               │              │────────┐       │              │
  │                 │               │              │<───────┘       │              │
  │                 │               │              │                │              │
  │                 │               │              │ Add(product)   │              │
  │                 │               │              │───────────────>│              │
  │                 │               │              │                │              │
  │                 │               │              │ SaveChanges()  │              │
  │                 │               │              │───────────────>│              │
  │                 │               │              │                │ INSERT       │
  │                 │               │              │                │─────────────>│
  │                 │               │              │                │<─────────────│
  │                 │               │              │<───────────────│              │
  │                 │               │              │                │              │
  │                 │               │ Result<Guid> │                │              │
  │                 │               │<─────────────│                │              │
  │                 │               │              │                │              │
  │                 │ Result<Guid>  │              │                │              │
  │                 │<──────────────│              │                │              │
  │                 │               │              │                │              │
  │ 201 Created     │               │              │                │              │
  │<────────────────│               │              │                │              │
```

### Get Product Sequence

```
Client          Controller      Dispatcher      Handler         Repository      Database
  │                 │               │              │                │              │
  │ GET /products/1 │               │              │                │              │
  │────────────────>│               │              │                │              │
  │                 │               │              │                │              │
  │                 │ QueryAsync()  │              │                │              │
  │                 │──────────────>│              │                │              │
  │                 │               │              │                │              │
  │                 │               │ GetService() │                │              │
  │                 │               │──────────────│                │              │
  │                 │               │              │                │              │
  │                 │               │ HandleAsync()│                │              │
  │                 │               │─────────────>│                │              │
  │                 │               │              │                │              │
  │                 │               │              │ GetByIdAsync() │              │
  │                 │               │              │───────────────>│              │
  │                 │               │              │                │ SELECT       │
  │                 │               │              │                │─────────────>│
  │                 │               │              │                │<─────────────│
  │                 │               │              │<───────────────│              │
  │                 │               │              │                │              │
  │                 │               │              │ product.ToDto()│              │
  │                 │               │              │────────┐       │              │
  │                 │               │              │<───────┘       │              │
  │                 │               │              │                │              │
  │                 │               │ Result<Dto>  │                │              │
  │                 │               │<─────────────│                │              │
  │                 │               │              │                │              │
  │                 │ Result<Dto>   │              │                │              │
  │                 │<──────────────│              │                │              │
  │                 │               │              │                │              │
  │ 200 OK + JSON   │               │              │                │              │
  │<────────────────│               │              │                │              │
```
