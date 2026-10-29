namespace PriceForge.BuildingBlocks.Exceptions;

public sealed class DomainException(string message) : Exception(message);
