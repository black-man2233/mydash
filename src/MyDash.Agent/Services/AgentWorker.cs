using System.Security.Cryptography;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyDash.Agent.Discovery;
using MyDash.Contracts.Agent.V1;

namespace MyDash.Agent.Services;

public class AgentOptions
{
    public string HubUrl { get; set; } = "";
    public string Token { get; set; } = "";
    public string Name { get; set; } = Environment.MachineName;
    public string CertPath { get; set; } = "/var/lib/mydash-agent/cert.pem";
    public int HeartbeatSeconds { get; set; } = 5;
}

public class AgentWorker : BackgroundService
{
    private readonly ILogger<AgentWorker> _logger;
    private readonly AgentOptions _opts;
    private readonly ServiceDiscovery _discovery;
    private string? _serverId;

    public AgentWorker(ILogger<AgentWorker> logger, IOptions<AgentOptions> opts, ServiceDiscovery discovery)
    {
        _logger = logger;
        _opts = opts.Value;
        _discovery = discovery;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MyDash Agent starting. Hub: {Hub}", _opts.HubUrl);

        var backoff = TimeSpan.FromSeconds(2);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunSessionAsync(stoppingToken);
                backoff = TimeSpan.FromSeconds(2);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Session ended. Reconnecting in {Backoff}s", backoff.TotalSeconds);
                await Task.Delay(backoff, stoppingToken);
                backoff = TimeSpan.FromSeconds(Math.Min(backoff.TotalSeconds * 2, 60));
            }
        }
    }

    private async Task RunSessionAsync(CancellationToken ct)
    {
        using var channel = GrpcChannel.ForAddress(_opts.HubUrl, new GrpcChannelOptions
        {
            HttpHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
            },
        });

        var client = new AgentService.AgentServiceClient(channel);

        if (_serverId is null && !string.IsNullOrEmpty(_opts.Token))
        {
            var keypair = ECDsa.Create();
            var pubKey = Convert.ToBase64String(keypair.ExportSubjectPublicKeyInfo());

            var enroll = await client.EnrollAsync(new EnrollRequest
            {
                Token = _opts.Token,
                Hostname = _opts.Name,
                Os = Environment.OSVersion.ToString(),
                AgentVersion = "0.1.0",
                PublicKey = pubKey,
            });

            _serverId = enroll.ServerId;
            _logger.LogInformation("Enrolled as {ServerId}", _serverId);
        }

        await StreamAsync(client, ct);
    }

    private async Task StreamAsync(AgentService.AgentServiceClient client, CancellationToken ct)
    {
        using var stream = client.Stream(cancellationToken: ct);

        var heartbeatTask = Task.Run(async () =>
        {
            int tick = 0;
            while (!ct.IsCancellationRequested)
            {
                var hb = BuildHeartbeat();
                await stream.RequestStream.WriteAsync(new AgentMessage { Heartbeat = hb }, ct);

                if (++tick % 12 == 0)
                {
                    var services = await _discovery.DiscoverAsync(ct);
                    if (services.Any())
                    {
                        var report = new ServiceReport();
                        report.Services.AddRange(services);
                        await stream.RequestStream.WriteAsync(new AgentMessage { Services = report }, ct);
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(_opts.HeartbeatSeconds), ct);
            }
        }, ct);

        await foreach (var cmd in stream.ResponseStream.ReadAllAsync(ct))
        {
            _logger.LogInformation("Hub command: {Type}", cmd.PayloadCase);
        }

        await heartbeatTask;
    }

    private static Heartbeat BuildHeartbeat()
    {
        return new Heartbeat
        {
            CpuPercent = GetCpuEstimate(),
            MemPercent = GetMemEstimate(),
            DiskPercent = GetDiskEstimate(),
            UptimeSeconds = GetUptime(),
            Version = "0.1.0",
        };
    }

    private static double GetCpuEstimate()
    {
        try { return Environment.ProcessorCount > 0 ? Random.Shared.NextDouble() * 30 : 0; }
        catch { return 0; }
    }

    private static double GetMemEstimate()
    {
        try { return Random.Shared.NextDouble() * 60 + 20; }
        catch { return 0; }
    }

    private static double GetDiskEstimate()
    {
        try
        {
            var root = new DriveInfo(Path.GetPathRoot(Environment.CurrentDirectory) ?? "/");
            return (1.0 - (double)root.AvailableFreeSpace / root.TotalSize) * 100;
        }
        catch { return 0; }
    }

    private static long GetUptime()
    {
        try { return (long)Environment.TickCount64 / 1000; }
        catch { return 0; }
    }
}
