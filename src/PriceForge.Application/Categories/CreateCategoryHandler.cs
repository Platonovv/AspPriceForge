using MediatR;
using PriceForge.Application.Common.Exceptions;
using PriceForge.Domain.Categories;

namespace PriceForge.Application.Categories;

public sealed class CreateCategoryHandler : IRequestHandler<CreateCategoryCommand, Guid>
{
    private readonly ICategoryRepository _categoryRepository;

    public CreateCategoryHandler(ICategoryRepository categoryRepository)
    {
        ArgumentNullException.ThrowIfNull(categoryRepository);
        _categoryRepository = categoryRepository;
    }

    public async Task<Guid> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        if (await _categoryRepository.ExistsAsync(request.Name, cancellationToken))
        {
            throw new ConflictException("Category already exists");
        }

        var newCategory = new Category(request.Name);

        await _categoryRepository.AddAsync(newCategory, cancellationToken);
        await _categoryRepository.SaveChangesAsync(cancellationToken);

        return newCategory.Id;
    }
}
