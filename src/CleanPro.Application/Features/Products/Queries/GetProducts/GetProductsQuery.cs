using CleanPro.Application.Common.Interfaces;

namespace CleanPro.Application.Features.Products.Queries.GetProducts;

public sealed record GetProductsQuery(bool ActiveOnly = false)
    : IQuery<GetProductsQuery, IReadOnlyList<ProductDto>>;
