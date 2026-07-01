using BenchmarkDotNet.Attributes;
using PriceForge.Domain.Common;
using PriceForge.Domain.Pricing;
using PriceForge.Domain.Pricing.Strategies;

namespace PriceForge.Benchmarks;

[MemoryDiagnoser]
public class PriceBenchmarks
{
    private PricingContext _context = null!;
    private PriceCalculator _calculator = null!;
    private IEnumerable<IPricingStrategy> _strategies = null!;

    [GlobalSetup]
    public void Setup()
    {
        _strategies = new List<IPricingStrategy> {new VipDiscountStrategy(), new VolumeStrategy()};
        _context = new PricingContext(new Money(1000m, Currency.RUB), 10, CustomerTier.Vip, Region.RU);
        _calculator = new PriceCalculator(_strategies);
    }

    [Benchmark]
    public Money Calculate() => _calculator.Calculate(_context);
}
