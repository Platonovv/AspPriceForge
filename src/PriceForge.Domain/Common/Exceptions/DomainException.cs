namespace PriceForge.Application.Common.Exceptions;

public sealed class DomainException(string message) : Exception(message);
