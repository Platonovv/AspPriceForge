namespace PriceForge.BuildingBlocks.Exceptions;

public sealed class TooManyRequestsException(string message) : Exception(message);
