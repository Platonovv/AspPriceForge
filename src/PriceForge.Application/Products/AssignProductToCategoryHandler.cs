using MediatR;
using PriceForge.Application.Categories;
using PriceForge.Application.Common.Exceptions;
using PriceForge.Domain.Categories;

namespace PriceForge.Application.Products;

public sealed class AssignProductToCategoryHandler : IRequestHandler<AssignProductToCategoryCommand>
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;

    public AssignProductToCategoryHandler(IProductRepository productRepository, ICategoryRepository categoryRepository)
    {
        ArgumentNullException.ThrowIfNull(productRepository);
        ArgumentNullException.ThrowIfNull(categoryRepository);

        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task Handle(AssignProductToCategoryCommand request, CancellationToken cancellationToken)
    {
        if (!await _productRepository.ExistsAsync(request.ProductId, cancellationToken))
        {
            throw new NotFoundException("Product not found");
        }

        Category? category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);

        if (category == null)
        {
            throw new NotFoundException("Category not found");
        }

        if (await _categoryRepository.ExistsBindingAsync(request.ProductId, request.CategoryId, cancellationToken))
        {
            return;
        }

        var newProductCategory = new ProductCategory(request.ProductId, request.CategoryId);

        await _categoryRepository.AddBindingAsync(newProductCategory, cancellationToken);
        await _categoryRepository.SaveChangesAsync(cancellationToken);
    }
}
