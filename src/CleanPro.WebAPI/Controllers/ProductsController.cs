using CleanPro.Application.Common.Interfaces;
using CleanPro.Application.Features.Products;
using CleanPro.Application.Features.Products.Commands.CreateProduct;
using CleanPro.Application.Features.Products.Commands.DeleteProduct;
using CleanPro.Application.Features.Products.Commands.UpdateProduct;
using CleanPro.Application.Features.Products.Queries.GetProduct;
using CleanPro.Application.Features.Products.Queries.GetProducts;
using CleanPro.Domain.Common.Results;
using CleanPro.WebAPI.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace CleanPro.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public ProductsController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts(
        [FromQuery] bool activeOnly = false,
        CancellationToken cancellationToken = default)
    {
        var query = new GetProductsQuery(activeOnly);
        var products = await _dispatcher.QueryAsync<GetProductsQuery, IReadOnlyList<ProductDto>>(
            query,
            cancellationToken);

        return Ok(products);
    }

    [HttpGet("{id:guid}", Name = "GetProduct")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProduct(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetProductQuery(id);
        var result = await _dispatcher.QueryAsync<GetProductQuery, Result<ProductDto>>(
            query,
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateProduct(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new CreateProductCommand(
            request.Name,
            request.Description,
            request.Price,
            request.StockQuantity);

        var result = await _dispatcher.SendAsync<CreateProductCommand, Result<Guid>>(
            command,
            cancellationToken);

        return result.ToCreatedResult("GetProduct", new { id = result.IsSuccess ? result.Value : Guid.Empty });
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateProduct(
        Guid id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateProductCommand(
            id,
            request.Name,
            request.Description,
            request.Price,
            request.StockQuantity);

        var result = await _dispatcher.SendAsync<UpdateProductCommand, Result<Unit>>(
            command,
            cancellationToken);

        if (result.IsSuccess)
        {
            return NoContent();
        }

        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var command = new DeleteProductCommand(id);
        var result = await _dispatcher.SendAsync<DeleteProductCommand, Result<Unit>>(
            command,
            cancellationToken);

        if (result.IsSuccess)
        {
            return NoContent();
        }

        return result.ToActionResult();
    }
}

public record CreateProductRequest(
    string Name,
    string Description,
    decimal Price,
    int StockQuantity);

public record UpdateProductRequest(
    string Name,
    string Description,
    decimal Price,
    int StockQuantity);
