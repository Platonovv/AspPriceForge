using PriceForge.Domain.Common;

namespace PriceForge.UnitTests.MoneyTests;

public class MoneyTests
{
    [Fact]
    public void Money_NegativeAmount_ThrowsArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Money(-1m, Currency.RUB));
    }
}
