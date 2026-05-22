namespace MyDash.Domain.Entities;

public class PinChallenge
{
    public Guid Id { get; private set; }
    public string PhoneE164 { get; set; } = string.Empty;
    public string CodeHash { get; set; } = string.Empty;
    public DateTimeOffset IssuedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
    public int AttemptCount { get; set; }
    public int FailedAttempts { get; set; }

    public bool IsExpired => DateTimeOffset.UtcNow > ExpiresAt;
    public bool IsConsumed => ConsumedAt.HasValue;
    public bool IsValid => !IsExpired && !IsConsumed;

    private PinChallenge() { }

    public static PinChallenge Create(string phoneE164, string codeHash, TimeSpan ttl)
    {
        return new PinChallenge
        {
            Id = Guid.NewGuid(),
            PhoneE164 = phoneE164,
            CodeHash = codeHash,
            IssuedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.Add(ttl),
        };
    }

    public void Consume() => ConsumedAt = DateTimeOffset.UtcNow;
}
