using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyDash.Application.Repositories;
using MyDash.Domain.Entities;

namespace MyDash.Infrastructure.BackgroundServices;

public class AgentHeartbeatWatchdog : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<AgentHeartbeatWatchdog> _logger;

    public AgentHeartbeatWatchdog(IServiceProvider sp, ILogger<AgentHeartbeatWatchdog> logger)
    {
        _sp = sp;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            try
            {
                using var scope = _sp.CreateScope();
                var servers = scope.ServiceProvider.GetRequiredService<IServerRepository>();
                var all = await servers.ListAllAsync(stoppingToken);
                var now = DateTimeOffset.UtcNow;

                foreach (var s in all)
                {
                    if (s.LastHeartbeat is null) continue;
                    var age = now - s.LastHeartbeat.Value;
                    var newStatus = age.TotalSeconds switch
                    {
                        > 300 => ServerStatus.Down,
                        > 60 => ServerStatus.Degraded,
                        _ => ServerStatus.Up,
                    };
                    if (s.Status != newStatus)
                    {
                        s.Status = newStatus;
                        await servers.UpdateAsync(s, stoppingToken);
                        _logger.LogInformation("Server {Name} status changed to {Status}", s.Name, newStatus);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Watchdog error");
            }
        }
    }
}
