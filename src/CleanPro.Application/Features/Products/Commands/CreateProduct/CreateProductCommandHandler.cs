using CleanPro.Application.Common.Interfaces;
using CleanPro.Domain.Common.Results;
using CleanPro.Domain.Entities;
using CleanPro.Domain.Interfaces;

namespace CleanPro.Application.Features.Products.Commands.CreateProduct;

public sealed class CreateProductCommandHandler
    : ICommandHandler<CreateProductCommand, Result<Guid>>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductCommandHandler(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> HandleAsync(
        CreateProductCommand command,
        CancellationToken cancellationToken = default)
    {
        var nameExists = await _productRepository.NameExistsAsync(
            command.Name,
            cancellationToken: cancellationToken);

        if (nameExists)
        {
            return Error.Conflict(
                "Product.NameExists",
                $"A product with name '{command.Name}' already exists.");
        }

        var product = Product.Create(
            command.Name,
            command.Description,
            command.Price,
            command.StockQuantity);

        _productRepository.Add(product);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return product.Id;
    }
}
