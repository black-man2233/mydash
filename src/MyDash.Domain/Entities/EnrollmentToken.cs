namespace MyDash.Domain.Entities;

public class EnrollmentToken
{
    public Guid Id { get; private set; }
    public string ServerName { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }

    public bool IsConsumed => ConsumedAt.HasValue;
    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsExpired => DateTimeOffset.UtcNow > ExpiresAt;
    public bool IsValid => !IsConsumed && !IsRevoked && !IsExpired;

    private EnrollmentToken() { }

    public static EnrollmentToken Create(string serverName, string tokenHash, List<string> tags, TimeSpan ttl)
    {
        return new EnrollmentToken
        {
            Id = Guid.NewGuid(),
            ServerName = serverName,
            TokenHash = tokenHash,
            Tags = tags,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.Add(ttl),
        };
    }

    public void Consume() => ConsumedAt = DateTimeOffset.UtcNow;
    public void Revoke() => RevokedAt = DateTimeOffset.UtcNow;
}
