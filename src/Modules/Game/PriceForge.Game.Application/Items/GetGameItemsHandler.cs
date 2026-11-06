using MediatR;
using PriceForge.Catalog.Contracts;
using PriceForge.Game.Domain.Items;

namespace PriceForge.Game.Application.Items;

public sealed class GetGameItemsHandler : IRequestHandler<GetGameItemsQuery, List<GameItemDto>>
{
    private readonly ICatalogModuleApi _catalogModuleApi;
    private readonly IGameItemRepository _gameItemRepository;

    public GetGameItemsHandler(ICatalogModuleApi catalogModuleApi, IGameItemRepository gameItemRepository)
    {
        ArgumentNullException.ThrowIfNull(catalogModuleApi);
        ArgumentNullException.ThrowIfNull(gameItemRepository);

        _catalogModuleApi = catalogModuleApi;
        _gameItemRepository = gameItemRepository;
    }

    public async Task<List<GameItemDto>> Handle(GetGameItemsQuery request, CancellationToken cancellationToken)
    {
        List<GameItem> allItems = await _gameItemRepository.GetAllAsync(cancellationToken);

        List<CatalogProductDto> allProducts
            = await _catalogModuleApi.GetProductsAsync([.. allItems.Select(x => x.ProductId)], cancellationToken);

        Dictionary<Guid, CatalogProductDto> allProductsDictionary = allProducts.ToDictionary(x => x.Id);

        return allItems.Where(x => allProductsDictionary.ContainsKey(x.ProductId))
                       .Select(x => new GameItemDto(x.ProductId,
                                                    allProductsDictionary[x.ProductId].Name,
                                                    x.CoinPrice))
                       .ToList();
    }
}
