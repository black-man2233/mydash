using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using MyDash.Contracts.Agent.V1;

namespace MyDash.Agent.Discovery;

public class ServiceDiscovery
{
    private readonly ILogger<ServiceDiscovery> _logger;
    private static readonly List<KnownService> _known = LoadKnown();

    public ServiceDiscovery(ILogger<ServiceDiscovery> logger) => _logger = logger;

    public async Task<List<DiscoveredService>> DiscoverAsync(CancellationToken ct)
    {
        var results = new List<DiscoveredService>();
        results.AddRange(await DiscoverDockerAsync(ct));
        results.AddRange(await DiscoverNativePortsAsync(ct));
        return results;
    }

    private async Task<List<DiscoveredService>> DiscoverDockerAsync(CancellationToken ct)
    {
        var results = new List<DiscoveredService>();
        try
        {
            using var client = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock")).CreateClient();
            var containers = await client.Containers.ListContainersAsync(new ContainersListParameters { All = false }, ct);

            foreach (var c in containers)
            {
                foreach (var port in c.Ports.Where(p => p.PublicPort > 0))
                {
                    var image = c.Image.Split(':')[0].ToLower();
                    var known = _known.FirstOrDefault(k => image.Contains(k.Image));
                    results.Add(new DiscoveredService
                    {
                        Port = (int)port.PublicPort,
                        ServiceType = "docker",
                        DockerImage = c.Image,
                        DockerContainerId = c.ID[..12],
                        Name = known?.Name ?? c.Names.FirstOrDefault()?.TrimStart('/') ?? image,
                        Status = "Up",
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Docker discovery skipped (Docker not available)");
        }
        return results;
    }

    private async Task<List<DiscoveredService>> DiscoverNativePortsAsync(CancellationToken ct)
    {
        var results = new List<DiscoveredService>();
        var commonPorts = new[] { 22, 80, 443, 3000, 5000, 8080, 8443, 9000 };

        foreach (var port in commonPorts)
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                using var tcp = new TcpClient();
                await tcp.ConnectAsync(IPAddress.Loopback, port, ct);
                results.Add(new DiscoveredService
                {
                    Port = port,
                    ServiceType = "native",
                    Name = GuessName(port),
                    Status = "Up",
                });
            }
            catch { }
        }
        return results;
    }

    private static string GuessName(int port) => port switch
    {
        22 => "SSH", 80 => "HTTP", 443 => "HTTPS",
        3000 => "Web App", 5000 => "Web App", 8080 => "Web",
        8443 => "HTTPS", 9000 => "Portainer",
        _ => $"Port {port}",
    };

    private static List<KnownService> LoadKnown()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Discovery", "KnownServices.json");
        if (!File.Exists(path)) return new();
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<KnownService>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
    }

    private record KnownService(string Image, string Name, string Glyph, string Color, string[] Tags);
}
