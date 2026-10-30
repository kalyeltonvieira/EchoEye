using EchoEye.Core.Constants;
using EchoEye.Core.Data;
using EchoEye.Core.Interfaces;
using EchoEye.Core.Logging;
using EchoEye.Core.Models;
using EchoEye.Management.Alerting;
using EchoEye.Management.Configuration;
using EchoEye.Management.Incident;
using EchoEye.Management.Status;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EchoEye.Dashboard.Services;

/// <summary>
/// Dashboard service for web UI
/// </summary>
public class DashboardService : BackgroundService
{
    private readonly ILogger<DashboardService> _logger;
    private readonly Database _database;
    private readonly IConfigurationProvider _configProvider;
    private readonly IHealthReporter _healthReporter;
    private readonly IStatusReporter _statusReporter;
    private readonly IAlertManager _alertManager;
    private readonly IIncidentManager _incidentManager;

    public DashboardService(ILogger<DashboardService> logger)
    {
        _logger = logger;
        _database = new Database(Paths.DatabasePath);

        var logAction = new Action<string, LogLevel, string?>((category, level, message) =>
        {
            _logger.Log(level, "[{Category}] {Message}", category, message);
        });

        var loggerProvider = new EchoEyeLoggerProvider(logAction);
        var moduleLogger = loggerProvider.CreateLogger("Dashboard");

        _configProvider = new ConfigurationProvider(_database, moduleLogger);
        _healthReporter = new Protection.Services.HealthReporter(_database, moduleLogger);
        _statusReporter = new StatusReporter(_database, moduleLogger);
        _alertManager = new AlertManager(_database, moduleLogger);
        _incidentManager = new IncidentManager(_database, moduleLogger);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EchoEye X Dashboard Service starting...");

        try
        {
            // Report initial health
            await _healthReporter.ReportHealthAsync("Dashboard", HealthStatus.Healthy, "Dashboard service starting");

            // In a full implementation, this would start a web server (e.g., ASP.NET Core)
            // For now, we'll just run periodic status checks
            while (!stoppingToken.IsCancellationRequested)
            {
                await PerformPeriodicChecks();
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dashboard service error");
            await _healthReporter.ReportHealthAsync("Dashboard", HealthStatus.Unhealthy, ex.Message);
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("EchoEye X Dashboard Service stopping...");
        await _healthReporter.ReportHealthAsync("Dashboard", HealthStatus.Healthy, "Dashboard service stopped");
        await base.StopAsync(cancellationToken);
    }

    private async Task PerformPeriodicChecks()
    {
        try
        {
            // Get system status
            var status = await _statusReporter.GetSystemStatusAsync();
            _logger.LogInformation("System Status: {Status}", status.OverallHealth);

            // Get recent alerts
            var alerts = await _alertManager.ListAlertsAsync(new AlertFilter { Limit = 10 });
            _logger.LogInformation("Recent Alerts: {Count}", alerts.Count);

            // Get active incidents
            var incidents = await _incidentManager.ListIncidentsAsync(new IncidentFilter { Status = IncidentStatus.Detected });
            _logger.LogInformation("Active Incidents: {Count}", incidents.Count());

            // Report health
            await _healthReporter.ReportHealthAsync("Dashboard", status.OverallHealth);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during periodic checks");
        }
    }
}
