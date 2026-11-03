namespace PriceForge.BuildingBlocks.Exceptions;

public sealed class IdempotencyKeyReuseException(string message) : Exception(message);
