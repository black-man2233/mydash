namespace MyDash.Domain.Entities;

public class AuditEntry
{
    public Guid Id { get; private set; }
    public DateTimeOffset At { get; private set; }
    public string Actor { get; set; } = "system";
    public AuditAction Action { get; set; }
    public string Target { get; set; } = string.Empty;
    public string Ip { get; set; } = string.Empty;
    public string Outcome { get; set; } = "ok";
    public string? Metadata { get; set; }

    private AuditEntry() { }

    public static AuditEntry Create(AuditAction action, string target, string ip, string outcome = "ok", string actor = "system")
    {
        return new AuditEntry
        {
            Id = Guid.NewGuid(),
            At = DateTimeOffset.UtcNow,
            Actor = actor,
            Action = action,
            Target = target,
            Ip = ip,
            Outcome = outcome,
        };
    }
}

public enum AuditAction
{
    PinRequested,
    PinVerified,
    PinFailed,
    ServiceOpened,
    ServiceAdded,
    ServiceRemoved,
    ScanStarted,
    ScanCompleted,
    AgentEnrolled,
    AgentRevoked,
    SettingsChanged,
}
