using MediatR;
using PriceForge.Application.Cache;
using PriceForge.Application.Common.Exceptions;
using PriceForge.Domain.Categories;

namespace PriceForge.Application.Categories;

public sealed class CreateCategoryPromotionHandler : IRequestHandler<CreateCategoryPromotionCommand, Guid>
{
    private readonly ICacheVersion _cacheVersion;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ICategoryPromotionRepository _categoryPromotionRepository;

    public CreateCategoryPromotionHandler(ICacheVersion cacheVersion,
                                          ICategoryRepository categoryRepository,
                                          ICategoryPromotionRepository categoryPromotionRepository)
    {
        ArgumentNullException.ThrowIfNull(cacheVersion);
        ArgumentNullException.ThrowIfNull(categoryRepository);
        ArgumentNullException.ThrowIfNull(categoryPromotionRepository);

        _cacheVersion = cacheVersion;
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

        await _categoryPromotionRepository.AddAsync(newCategoryPromotion, cancellationToken);
        await _categoryPromotionRepository.SaveChangesAsync(cancellationToken);
        await _cacheVersion.BumpAsync(cancellationToken);

        return newCategoryPromotion.Id;
    }
}
