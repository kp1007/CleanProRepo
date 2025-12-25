using CleanPro.Application.Common.Interfaces;
using CleanPro.Domain.Interfaces;

namespace CleanPro.Application.Features.Products.Queries.GetProducts;

public sealed class GetProductsQueryHandler
    : IQueryHandler<GetProductsQuery, IReadOnlyList<ProductDto>>
{
    private readonly IProductRepository _productRepository;

    public GetProductsQueryHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<IReadOnlyList<ProductDto>> HandleAsync(
        GetProductsQuery query,
        CancellationToken cancellationToken = default)
    {
        var products = query.ActiveOnly
            ? await _productRepository.GetActiveProductsAsync(cancellationToken)
            : await _productRepository.GetAllAsync(cancellationToken);

        return products.ToDto();
    }
}
