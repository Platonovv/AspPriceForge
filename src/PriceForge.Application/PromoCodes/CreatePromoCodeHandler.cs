using MediatR;
using PriceForge.BuildingBlocks.Exceptions;
using PriceForge.Domain.PromoCodes;

namespace PriceForge.Application.PromoCodes;

public sealed class CreatePromoCodeHandler : IRequestHandler<CreatePromoCodeCommand, Guid>
{
    private readonly IPromoCodeRepository _promoCodeRepository;

    public CreatePromoCodeHandler(IPromoCodeRepository promoCodeRepository)
    {
        ArgumentNullException.ThrowIfNull(promoCodeRepository);
        _promoCodeRepository = promoCodeRepository;
    }

    public async Task<Guid> Handle(CreatePromoCodeCommand request, CancellationToken cancellationToken)
    {
        PromoCode? existPromoCode = await _promoCodeRepository.GetByCodeAsync(request.Code, cancellationToken);

        if (existPromoCode != null)
        {
            throw new ConflictException("Code already exists");
        }

        var newPromoCode = new PromoCode(new Code(request.Code),
                                         request.DiscountPercent,
                                         request.ValidFrom,
                                         request.ValidTo,
                                         request.UsedLimit,
                                         request.MinOrderAmount);

        await _promoCodeRepository.AddAsync(newPromoCode, cancellationToken);
        await _promoCodeRepository.SaveChangesAsync(cancellationToken);

        return newPromoCode.Id;
    }
}
