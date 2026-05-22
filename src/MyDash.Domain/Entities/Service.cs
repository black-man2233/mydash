namespace MyDash.Domain.Entities;

public class Service
{
    public Guid Id { get; private set; }
    public Guid ServerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Port { get; set; }
    public ServiceType Type { get; set; } = ServiceType.Native;
    public string? DockerImage { get; set; }
    public string? DockerContainerId { get; set; }
    public List<string> Tags { get; set; } = new();
    public string Description { get; set; } = string.Empty;
    public string? HealthEndpoint { get; set; }
    public string IconColor { get; set; } = "#594AE2";
    public string IconGlyph { get; set; } = "router";
    public DateTimeOffset? LastCheck { get; set; }
    public ServerStatus Status { get; set; } = ServerStatus.Unknown;
    public bool IsPinned { get; set; }

    public Server? Server { get; set; }

    private Service() { }

    public static Service Create(Guid serverId, string name, int port, ServiceType type)
    {
        return new Service
        {
            Id = Guid.NewGuid(),
            ServerId = serverId,
            Name = name,
            Port = port,
            Type = type,
        };
    }
}

public enum ServiceType { Docker, Native }
