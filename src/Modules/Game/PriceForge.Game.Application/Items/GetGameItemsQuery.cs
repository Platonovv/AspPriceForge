using MediatR;

namespace PriceForge.Game.Application.Items;

public sealed record GetGameItemsQuery : IRequest<List<GameItemDto>>;
