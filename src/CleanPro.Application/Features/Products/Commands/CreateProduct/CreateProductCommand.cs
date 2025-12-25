using CleanPro.Application.Common.Interfaces;
using CleanPro.Domain.Common.Results;

namespace CleanPro.Application.Features.Products.Commands.CreateProduct;

public sealed record CreateProductCommand(
    string Name,
    string Description,
    decimal Price,
    int StockQuantity) : ICommand<CreateProductCommand, Result<Guid>>;
