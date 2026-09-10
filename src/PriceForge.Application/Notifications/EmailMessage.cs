namespace PriceForge.Application.Notifications;

public sealed record EmailMessage(string To, string Subject, string Body, string? HtmlBody = null);
