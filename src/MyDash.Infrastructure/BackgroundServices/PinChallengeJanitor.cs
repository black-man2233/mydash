using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyDash.Application.Repositories;

namespace MyDash.Infrastructure.BackgroundServices;

public class PinChallengeJanitor : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<PinChallengeJanitor> _logger;

    public PinChallengeJanitor(IServiceProvider sp, ILogger<PinChallengeJanitor> logger)
    {
        _sp = sp;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            try
            {
                using var scope = _sp.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IPinChallengeRepository>();
                await repo.PurgeExpiredAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PIN janitor error");
            }
        }
    }
}
