using MediatR;
using PriceForge.Application.Cache;
using PriceForge.Application.Common.Exceptions;
using PriceForge.Application.Products;
using PriceForge.Application.Transactions;
using PriceForge.Domain.Categories;

namespace PriceForge.Application.Categories;

public sealed class CreateCategoryPromotionHandler : IRequestHandler<CreateCategoryPromotionCommand, Guid>
{
    private readonly ICacheVersion _cacheVersion;
    private readonly ITransactionRunner _runner;
    private readonly ICategoryRepository _categoryRepository;
    private readonly PublicPriceRecalculator _priceRecalculator;
    private readonly ICategoryPromotionRepository _categoryPromotionRepository;

    public CreateCategoryPromotionHandler(ICacheVersion cacheVersion,
                                          ICategoryRepository categoryRepository,
                                          ICategoryPromotionRepository categoryPromotionRepository,
                                          ITransactionRunner runner,
                                          PublicPriceRecalculator priceRecalculator)
    {
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(cacheVersion);
        ArgumentNullException.ThrowIfNull(priceRecalculator);
        ArgumentNullException.ThrowIfNull(categoryRepository);
        ArgumentNullException.ThrowIfNull(categoryPromotionRepository);

        _runner = runner;
        _cacheVersion = cacheVersion;
        _priceRecalculator = priceRecalculator;
        _categoryRepository = categoryRepository;
        _categoryPromotionRepository = categoryPromotionRepository;
    }

    public async Task<Guid> Handle(CreateCategoryPromotionCommand request, CancellationToken cancellationToken)
    {
        Category? category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);

        if (category == null)
        {
            throw new NotFoundException("Category not found");
        }

        if (await _categoryPromotionRepository.HasOverlappingAsync(request.CategoryId,
                                                                   request.StartAt,
                                                                   request.EndAt,
                                                                   cancellationToken))
        {
            throw new ConflictException("Category overlapping promotion");
        }

        var newCategoryPromotion
            = new CategoryPromotion(request.CategoryId, request.Percent, request.StartAt, request.EndAt);

        await _runner.RunAsync(async () =>
                               {
                                   await _categoryPromotionRepository.AddAsync(newCategoryPromotion, cancellationToken);
                                   await _categoryPromotionRepository.SaveChangesAsync(cancellationToken);

                                   List<Guid> productIdsAsync
                                       = await _categoryRepository.GetProductIdsAsync(newCategoryPromotion.CategoryId,
                                             cancellationToken);

                                   await _priceRecalculator.RecalculateAsync(productIdsAsync, cancellationToken);
                                   await _categoryPromotionRepository.SaveChangesAsync(cancellationToken);
                               },
                               cancellationToken);

        await _cacheVersion.BumpAsync(cancellationToken);

        return newCategoryPromotion.Id;
    }
}
