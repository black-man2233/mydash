namespace MyDash.Domain.Entities;

public class Server
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string TailscaleHost { get; set; } = string.Empty;
    public string Color { get; set; } = "#594AE2";
    public string InitialChar { get; set; } = "?";
    public List<string> Tags { get; set; } = new();
    public string OS { get; set; } = string.Empty;
    public ServerStatus Status { get; set; } = ServerStatus.Unknown;
    public string AgentVersion { get; set; } = string.Empty;
    public string AgentFingerprint { get; set; } = string.Empty;
    public DateTimeOffset EnrolledAt { get; private set; }
    public DateTimeOffset? LastHeartbeat { get; set; }

    public double CpuPercent { get; set; }
    public double MemPercent { get; set; }
    public double DiskPercent { get; set; }
    public long UptimeSeconds { get; set; }

    public List<Service> Services { get; set; } = new();

    private Server() { }

    public static Server NewEnrolled(string name, string fullName, string tailscaleHost, string os)
    {
        var s = new Server
        {
            Id = Guid.NewGuid(),
            Name = name,
            FullName = fullName,
            TailscaleHost = tailscaleHost,
            OS = os,
            Status = ServerStatus.Unknown,
            EnrolledAt = DateTimeOffset.UtcNow,
            InitialChar = name.Length > 0 ? name[0].ToString().ToUpperInvariant() : "?",
            Color = ColorFromName(name),
        };
        return s;
    }

    private static string ColorFromName(string name)
    {
        var lc = name.ToLowerInvariant();
        return lc switch
        {
            "hp" or "hpserver" => "#0096D6",
            "huawei" => "#C7000B",
            "cool" => "#3DB2FF",
            _ => "#594AE2",
        };
    }
}

public enum ServerStatus { Up, Down, Degraded, Unknown }
