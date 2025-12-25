using CleanPro.Domain.Common;
using CleanPro.Domain.Common.ValueObjects;
using CleanPro.Domain.Events;

namespace CleanPro.Domain.Entities;

public sealed class Product : AuditableEntity
{
    private Product() { }

    private Product(Guid id, string name, string description, Money price, int stockQuantity)
    {
        Id = id;
        Name = name;
        Description = description;
        Price = price;
        StockQuantity = stockQuantity;
        IsActive = true;

        RaiseDomainEvent(new ProductCreatedEvent(Id, Name, Price.Amount));
    }

    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Money Price { get; private set; } = Money.Zero();
    public int StockQuantity { get; private set; }
    public bool IsActive { get; private set; }

    public static Product Create(string name, string description, decimal price, int stockQuantity)
    {
        return new Product(
            Guid.NewGuid(),
            name,
            description,
            Money.Create(price),
            stockQuantity);
    }

    public void Update(string name, string description, decimal price, int stockQuantity)
    {
        Name = name;
        Description = description;
        Price = Money.Create(price);
        StockQuantity = stockQuantity;

        RaiseDomainEvent(new ProductUpdatedEvent(Id, Name, Price.Amount));
    }

    public void UpdateStock(int quantity)
    {
        if (StockQuantity + quantity < 0)
            throw new InvalidOperationException("Stock quantity cannot be negative.");

        StockQuantity += quantity;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
