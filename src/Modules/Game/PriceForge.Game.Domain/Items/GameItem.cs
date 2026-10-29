namespace PriceForge.Game.Domain.Items;

public sealed class GameItem
{
    public Guid ProductId { get; }
    public long CoinPrice { get; private set; }

    public GameItem(Guid productId, long coinPrice)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException("ProductId cannot be empty", nameof(productId));
        }

        if (coinPrice <= 0)
        {
            throw new ArgumentException("CoinPrice must be positive", nameof(coinPrice));
        }

        ProductId = productId;
        CoinPrice = coinPrice;
    }

    private GameItem()
    {
    }
}
