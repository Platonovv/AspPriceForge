using MediatR;
using PriceForge.Application.Cache;
using PriceForge.Application.Categories;
using PriceForge.BuildingBlocks.Exceptions;
using PriceForge.Application.Transactions;
using PriceForge.Domain.Categories;

namespace PriceForge.Application.Products;

public sealed class AssignProductToCategoryHandler : IRequestHandler<AssignProductToCategoryCommand>
{
    private readonly ICacheVersion _cacheVersion;
    private readonly ITransactionRunner _runner;
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly PublicPriceRecalculator _priceRecalculator;

    public AssignProductToCategoryHandler(ICacheVersion cacheVersion,
                                          IProductRepository productRepository,
                                          ICategoryRepository categoryRepository,
                                          ITransactionRunner runner,
                                          PublicPriceRecalculator priceRecalculator)
    {
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(cacheVersion);
        ArgumentNullException.ThrowIfNull(priceRecalculator);
        ArgumentNullException.ThrowIfNull(productRepository);
        ArgumentNullException.ThrowIfNull(categoryRepository);

        _runner = runner;
        _cacheVersion = cacheVersion;
        _priceRecalculator = priceRecalculator;
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

        await _runner.RunAsync(async () =>
                               {
                                   await _categoryRepository.AddBindingAsync(newProductCategory, cancellationToken);
                                   await _categoryRepository.SaveChangesAsync(cancellationToken);
                                   await _priceRecalculator.RecalculateAsync([request.ProductId], cancellationToken);
                                   await _categoryRepository.SaveChangesAsync(cancellationToken);
                               },
                               cancellationToken);

        await _cacheVersion.BumpAsync(cancellationToken);
    }
}
