namespace PriceForge.Domain.Common;

public interface ISpecification<T>
{
    public bool IsSatisfiedBy(T candidate);
}
