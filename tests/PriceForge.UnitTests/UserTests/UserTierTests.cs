using PriceForge.Domain.Pricing;
using PriceForge.Domain.Users;

namespace PriceForge.UnitTests.UserTests;

[Trait("Category", "Unit")]
public class UserTierTests
{
    private static User CreateUser() => new("tier@priceforge.test", "hash", "User");

    [Fact]
    public void NewUser_IsRegular()
    {
        User user = CreateUser();

        Assert.Equal(CustomerTier.Regular, user.Tier);
    }

    [Fact]
    public void ChangeTier_WithValidTier_UpdatesTier()
    {
        User user = CreateUser();

        user.ChangeTier(CustomerTier.Vip);

        Assert.Equal(CustomerTier.Vip, user.Tier);
    }

    [Theory]
    [InlineData(CustomerTier.None)]
    [InlineData((CustomerTier)99)]
    public void ChangeTier_WithInvalidTier_Throws(CustomerTier tier)
    {
        User user = CreateUser();

        Assert.Throws<ArgumentException>(() => user.ChangeTier(tier));
        Assert.Equal(CustomerTier.Regular, user.Tier);
    }
}
