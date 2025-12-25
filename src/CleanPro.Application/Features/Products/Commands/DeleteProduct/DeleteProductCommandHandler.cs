using CleanPro.Application.Common.Interfaces;
using CleanPro.Domain.Common.Results;
using CleanPro.Domain.Interfaces;

namespace CleanPro.Application.Features.Products.Commands.DeleteProduct;

public sealed class DeleteProductCommandHandler
    : ICommandHandler<DeleteProductCommand, Result<Unit>>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProductCommandHandler(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Unit>> HandleAsync(
        DeleteProductCommand command,
        CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(command.Id, cancellationToken);

        if (product is null)
        {
            return Error.NotFound("Product", command.Id);
        }

        _productRepository.Remove(product);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
