namespace PriceForge.BuildingBlocks.Exceptions;

public sealed class ForbiddenException(string message) : Exception(message);
