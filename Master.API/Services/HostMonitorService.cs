using Master.API.Data;
using Microsoft.EntityFrameworkCore;

namespace Master.API.Services;

public class HostMonitorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HostMonitorService> _logger;

    public HostMonitorService(IServiceProvider serviceProvider, ILogger<HostMonitorService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Host Monitor Service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Mark hosts offline if no heartbeat for 1 minute
                var cutoff = DateTime.Now.AddMinutes(-1);
                var offlineHosts = await db.Hosts
                    .Where(h => h.Status == "Online" && h.LastHeartbeat < cutoff)
                    .ToListAsync(stoppingToken);

                foreach (var host in offlineHosts)
                {
                    host.Status = "Offline";
                    _logger.LogWarning("Host {Hostname} marked as Offline.", host.Hostname);
                }

                if (offlineHosts.Any())
                    await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Host Monitor Service.");
            }

            // Check every 30 seconds
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}