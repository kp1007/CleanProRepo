using CleanPro.Application.Common.Interfaces;
using CleanPro.Domain.Common.Results;
using CleanPro.Domain.Interfaces;

namespace CleanPro.Application.Features.Products.Queries.GetProduct;

public sealed class GetProductQueryHandler
    : IQueryHandler<GetProductQuery, Result<ProductDto>>
{
    private readonly IProductRepository _productRepository;

    public GetProductQueryHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Result<ProductDto>> HandleAsync(
        GetProductQuery query,
        CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(query.Id, cancellationToken);

        if (product is null)
        {
            return Error.NotFound("Product", query.Id);
        }

        return product.ToDto();
    }
}
