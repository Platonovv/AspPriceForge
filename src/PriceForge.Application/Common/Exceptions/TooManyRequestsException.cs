namespace PriceForge.Application.Common.Exceptions;

public sealed class TooManyRequestsException(string message) : Exception(message);
