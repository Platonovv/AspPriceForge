namespace PriceForge.IntegrationTests.Factory;

public sealed class JwtApiFactory : ApiFactory
{
    protected override bool UseTestAuth => false;
}
