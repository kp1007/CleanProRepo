using CleanPro.Application.Common.Interfaces;
using CleanPro.Domain.Common.Results;

namespace CleanPro.Application.Features.Products.Queries.GetProduct;

public sealed record GetProductQuery(Guid Id)
    : IQuery<GetProductQuery, Result<ProductDto>>;
