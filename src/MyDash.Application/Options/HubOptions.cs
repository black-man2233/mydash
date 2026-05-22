namespace MyDash.Application.Options;

public class HubOptions
{
    public int AgentGrpcPort { get; set; } = 7777;
    public string CaCertPath { get; set; } = "/var/lib/mydash/ca.pfx";
    public string HubPublicUrl { get; set; } = "https://mydash.local";
}
