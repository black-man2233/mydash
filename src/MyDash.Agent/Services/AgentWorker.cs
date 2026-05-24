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

    // Tracks previous /proc/stat snapshot for delta CPU calculation
    private (long total, long idle) _prevCpu = ReadCpuStats();

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

        if (_serverId is null)
        {
            if (string.IsNullOrEmpty(_opts.Token))
            {
                _logger.LogWarning("No enrollment token configured — skipping session");
                await Task.Delay(TimeSpan.FromMinutes(1), ct);
                return;
            }
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
                var hb = BuildHeartbeat(_serverId!);
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

    private Heartbeat BuildHeartbeat(string serverId)
    {
        return new Heartbeat
        {
            ServerId = serverId,
            CpuPercent = GetCpuPercent(),
            MemPercent = GetMemPercent(),
            DiskPercent = GetDiskPercent(),
            UptimeSeconds = GetUptime(),
            Version = "0.1.0",
        };
    }

    // ─── System metrics ───────────────────────────────────────────────────────

    private double GetCpuPercent()
    {
        try
        {
            var current = ReadCpuStats();
            var dTotal = current.total - _prevCpu.total;
            var dIdle  = current.idle  - _prevCpu.idle;
            _prevCpu = current;
            return dTotal > 0 ? Math.Round((1.0 - (double)dIdle / dTotal) * 100.0, 1) : 0.0;
        }
        catch { return 0.0; }
    }

    private static (long total, long idle) ReadCpuStats()
    {
        // /proc/stat first line: "cpu user nice system idle iowait irq softirq steal ..."
        var line  = File.ReadLines("/proc/stat").First();
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1).Select(long.Parse).ToArray();
        var idle  = parts[3] + parts[4]; // idle + iowait
        var total = parts.Sum();
        return (total, idle);
    }

    private static double GetMemPercent()
    {
        try
        {
            long memTotal = 0, memAvailable = 0;
            foreach (var line in File.ReadLines("/proc/meminfo"))
            {
                if (line.StartsWith("MemTotal:"))
                    memTotal = ParseKbLine(line);
                else if (line.StartsWith("MemAvailable:"))
                    memAvailable = ParseKbLine(line);
                if (memTotal > 0 && memAvailable > 0) break;
            }
            return memTotal > 0 ? Math.Round((1.0 - (double)memAvailable / memTotal) * 100.0, 1) : 0.0;
        }
        catch { return 0.0; }
    }

    private static long ParseKbLine(string line)
    {
        var value = line.Split(':')[1].Trim().Split(' ')[0];
        return long.TryParse(value, out var v) ? v : 0;
    }

    private static double GetDiskPercent()
    {
        try
        {
            var root = new DriveInfo(Path.GetPathRoot(Environment.CurrentDirectory) ?? "/");
            return Math.Round((1.0 - (double)root.AvailableFreeSpace / root.TotalSize) * 100.0, 1);
        }
        catch { return 0.0; }
    }

    private static long GetUptime()
    {
        try
        {
            // /proc/uptime: "seconds_up idle_seconds"
            var line = File.ReadLines("/proc/uptime").First();
            var secs = line.Split(' ')[0];
            return double.TryParse(secs, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var d) ? (long)d : 0;
        }
        catch { return (long)(Environment.TickCount64 / 1000); }
    }
}
