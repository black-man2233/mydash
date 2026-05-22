using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyDash.Application.Repositories;

namespace MyDash.Infrastructure.BackgroundServices;

public class EnrollmentTokenJanitor : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<EnrollmentTokenJanitor> _logger;

    public EnrollmentTokenJanitor(IServiceProvider sp, ILogger<EnrollmentTokenJanitor> logger)
    {
        _sp = sp;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            try
            {
                using var scope = _sp.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IEnrollmentTokenRepository>();
                await repo.PurgeExpiredAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token janitor error");
            }
        }
    }
}
