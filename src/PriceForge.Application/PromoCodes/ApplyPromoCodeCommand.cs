using MediatR;

namespace PriceForge.Application.PromoCodes;

public record ApplyPromoCodeCommand(string Code, decimal OrderMoney) : IRequest<decimal>;
