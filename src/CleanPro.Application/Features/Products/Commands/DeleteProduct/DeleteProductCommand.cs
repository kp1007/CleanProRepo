using CleanPro.Application.Common.Interfaces;
using CleanPro.Domain.Common.Results;

namespace CleanPro.Application.Features.Products.Commands.DeleteProduct;

public sealed record DeleteProductCommand(Guid Id)
    : ICommand<DeleteProductCommand, Result<Unit>>;
