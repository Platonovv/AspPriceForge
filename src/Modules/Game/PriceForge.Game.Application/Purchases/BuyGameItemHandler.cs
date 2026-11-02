using MediatR;
using Microsoft.Extensions.Logging;
using PriceForge.BuildingBlocks.Exceptions;
using PriceForge.Catalog.Contracts;
using PriceForge.Game.Application.Items;
using PriceForge.Game.Application.Transactions;
using PriceForge.Game.Application.Wallets;
using PriceForge.Game.Domain.Items;
using PriceForge.Game.Domain.Purchases;
using PriceForge.Game.Domain.Wallets;

namespace PriceForge.Game.Application.Purchases;

public sealed class BuyGameItemHandler : IRequestHandler<BuyGameItemCommand, CoinPurchaseDto>
{
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<BuyGameItemHandler> _logger;
    private readonly ICatalogModuleApi _catalogModuleApi;
    private readonly IWalletRepository _walletRepository;
    private readonly IGameItemRepository _gameItemRepository;
    private readonly IGameTransactionRunner _gameTransactionRunner;
    private readonly ICoinPurchaseRepository _coinPurchaseRepository;

    public BuyGameItemHandler(ILogger<BuyGameItemHandler> logger,
                              TimeProvider timeProvider,
                              ICatalogModuleApi catalogModuleApi,
                              IWalletRepository walletRepository,
                              IGameItemRepository gameItemRepository,
                              IGameTransactionRunner gameTransactionRunner,
                              ICoinPurchaseRepository coinPurchaseRepository)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(catalogModuleApi);
        ArgumentNullException.ThrowIfNull(walletRepository);
        ArgumentNullException.ThrowIfNull(gameItemRepository);
        ArgumentNullException.ThrowIfNull(gameTransactionRunner);
        ArgumentNullException.ThrowIfNull(coinPurchaseRepository);

        _logger = logger;
        _timeProvider = timeProvider;
        _catalogModuleApi = catalogModuleApi;
        _walletRepository = walletRepository;
        _gameItemRepository = gameItemRepository;
        _gameTransactionRunner = gameTransactionRunner;
        _coinPurchaseRepository = coinPurchaseRepository;
    }

    public async Task<CoinPurchaseDto> Handle(BuyGameItemCommand request, CancellationToken cancellationToken)
    {
        CatalogProductDto? product = await _catalogModuleApi.GetProductAsync(request.ProductId, cancellationToken);

        if (product == null)
        {
            throw new NotFoundException("Item not found");
        }

        GameItem? gameItem = await _gameItemRepository.GetByProductIdAsync(request.ProductId, cancellationToken);

        if (gameItem == null)
        {
            throw new NotFoundException("Game item not found");
        }

        DateTime buyTime = _timeProvider.GetUtcNow().UtcDateTime;

        return await _gameTransactionRunner.RunAsync(
                   () => SpendTransactionAsync(request, gameItem, buyTime, cancellationToken),
                   cancellationToken);
    }

    private async Task<CoinPurchaseDto> SpendTransactionAsync(BuyGameItemCommand request,
                                                               GameItem gameItem,
                                                               DateTime buyTime,
                                                               CancellationToken cancellationToken)
    {
        Wallet? wallet = await _walletRepository.GetByUserIdForUpdateAsync(request.UserId, cancellationToken);

        if (wallet == null)
        {
            throw new DomainException("Insufficient funds");
        }

        CoinPurchase? coinPurchase
            = await _coinPurchaseRepository.GetByIdempotencyKeyAsync(request.UserId,
                                                                     request.IdempotencyKey,
                                                                     cancellationToken);

        if (coinPurchase == null)
        {
            coinPurchase = new CoinPurchase(request.UserId,
                                            request.ProductId,
                                            gameItem.CoinPrice,
                                            request.IdempotencyKey,
                                            buyTime);

            wallet.Spend(coinPurchase.CoinPrice, buyTime);

            await _coinPurchaseRepository.AddAsync(coinPurchase, cancellationToken);
        }
        else
        {
            _logger.LogInformation("Repeat at IdempotencyKey - {IdempotencyKey}", request.IdempotencyKey);
        }

        await _walletRepository.SaveChangesAsync(cancellationToken);

        return new CoinPurchaseDto(coinPurchase.Id,
                                   coinPurchase.ProductId,
                                   coinPurchase.CoinPrice,
                                   coinPurchase.CreatedAt);
    }
}
