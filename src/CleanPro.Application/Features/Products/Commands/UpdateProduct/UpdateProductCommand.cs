using CleanPro.Application.Common.Interfaces;
using CleanPro.Domain.Common.Results;

namespace CleanPro.Application.Features.Products.Commands.UpdateProduct;

public sealed record UpdateProductCommand(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    int StockQuantity) : ICommand<UpdateProductCommand, Result<Unit>>;
