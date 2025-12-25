using CleanPro.Domain.Entities;

namespace CleanPro.Application.Features.Products;

public static class ProductMapper
{
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

    public static IReadOnlyList<ProductDto> ToDto(this IEnumerable<Product> products)
    {
        return products.Select(p => p.ToDto()).ToList();
    }
}
