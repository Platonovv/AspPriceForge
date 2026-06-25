namespace PriceForge.Application.Brokers;

public sealed class OutboxMessage
{
    public Guid Id { get; }
    public string Type { get; } = null!;
    public string Content { get; } = null!;
    public DateTime OccurredAt { get; }
    public DateTime? PublishedAt { get; private set; }

    public OutboxMessage(string type, string content)
    {
        Id = Guid.NewGuid();
        Type = type;
        Content = content;
        OccurredAt = DateTime.UtcNow;
    }

    private OutboxMessage()
    {
    }

    public void MarkPublished() => PublishedAt = DateTime.UtcNow;
}
