using CleanPro.Application.Common.Interfaces;
using CleanPro.Domain.Common.Results;
using CleanPro.Domain.Interfaces;

namespace CleanPro.Application.Features.Products.Commands.UpdateProduct;

public sealed class UpdateProductCommandHandler
    : ICommandHandler<UpdateProductCommand, Result<Unit>>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductCommandHandler(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Unit>> HandleAsync(
        UpdateProductCommand command,
        CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(command.Id, cancellationToken);

        if (product is null)
        {
            return Error.NotFound("Product", command.Id);
        }

        var nameExists = await _productRepository.NameExistsAsync(
            command.Name,
            command.Id,
            cancellationToken);

        if (nameExists)
        {
            return Error.Conflict(
                "Product.NameExists",
                $"A product with name '{command.Name}' already exists.");
        }

        product.Update(
            command.Name,
            command.Description,
            command.Price,
            command.StockQuantity);

        _productRepository.Update(product);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
