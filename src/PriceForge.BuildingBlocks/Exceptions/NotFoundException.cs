namespace PriceForge.BuildingBlocks.Exceptions;

public sealed class NotFoundException(string message) : Exception(message);
