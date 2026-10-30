using EchoEye.Core.Constants;
using Microsoft.Extensions.Logging;
using System.ServiceProcess;

namespace EchoEye.Installer.Services;

/// <summary>
/// Manages Windows Service registration
/// </summary>
public class ServiceInstaller
{
    private readonly ILogger _logger;

    public ServiceInstaller(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Register EchoEye Protection Service
    /// </summary>
    public void RegisterService()
    {
        try
        {
            var serviceName = "EchoEyeProtection";
            var displayName = "EchoEye X Protection Service";
            var description = "Monitors file system activity and detects ransomware behavior";

            _logger.LogInformation("Registering Windows Service: {ServiceName}", serviceName);

            // Check if service already exists
            if (ServiceExists(serviceName))
            {
                _logger.LogWarning("Service already exists: {ServiceName}", serviceName);
                return;
            }

            // Create service using sc.exe
            var servicePath = GetServiceExecutablePath();
            var command = $"create {serviceName} binPath= \"{servicePath}\" start= auto DisplayName= \"{displayName}\"";

            ExecuteScCommand(command);

            // Set service description
            var descriptionCommand = $"description {serviceName} \"{description}\"";
            ExecuteScCommand(descriptionCommand);

            _logger.LogInformation("Windows Service registered successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register Windows Service");
            throw;
        }
    }

    /// <summary>
    /// Unregister Windows Service
    /// </summary>
    public void UnregisterService()
    {
        try
        {
            var serviceName = "EchoEyeProtection";

            _logger.LogInformation("Unregistering Windows Service: {ServiceName}", serviceName);

            if (!ServiceExists(serviceName))
            {
                _logger.LogWarning("Service does not exist: {ServiceName}", serviceName);
                return;
            }

            // Stop service if running
            StopService(serviceName);

            // Delete service
            var command = $"delete {serviceName}";
            ExecuteScCommand(command);

            _logger.LogInformation("Windows Service unregistered successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unregister Windows Service");
            throw;
        }
    }

    /// <summary>
    /// Check if service exists
    /// </summary>
    private bool ServiceExists(string serviceName)
    {
        try
        {
            using var controller = new ServiceController(serviceName);
            var status = controller.Status;
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Stop service if running
    /// </summary>
    private void StopService(string serviceName)
    {
        try
        {
            using var controller = new ServiceController(serviceName);
            if (controller.Status == ServiceControllerStatus.Running)
            {
                _logger.LogInformation("Stopping service: {ServiceName}", serviceName);
                controller.Stop();
                controller.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                _logger.LogInformation("Service stopped successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to stop service: {ServiceName}", serviceName);
        }
    }

    /// <summary>
    /// Get path to service executable
    /// </summary>
    private string GetServiceExecutablePath()
    {
        var configuredPath = Environment.GetEnvironmentVariable("ECHOEYE_PROTECTION_EXE");
        if (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath))
        {
            return configuredPath;
        }

        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "EchoEye.Protection.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "EchoEye X", "EchoEye.Protection.exe"),
            Path.Combine(Paths.BaseDirectory, "bin", "EchoEye.Protection.exe")
        };

        var servicePath = candidates.FirstOrDefault(File.Exists);
        if (servicePath == null)
        {
            throw new FileNotFoundException(
                "EchoEye.Protection.exe was not found. Publish the protection service first or set ECHOEYE_PROTECTION_EXE.",
                candidates[0]);
        }

        return servicePath;
    }

    /// <summary>
    /// Execute sc.exe command
    /// </summary>
    private void ExecuteScCommand(string command)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "sc.exe",
            Arguments = command,
            Verb = "runas", // Run as administrator
            UseShellExecute = true,
            WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
        };

        using var process = System.Diagnostics.Process.Start(startInfo);
        process?.WaitForExit();

        if (process?.ExitCode != 0)
        {
            throw new InvalidOperationException($"sc.exe command failed with exit code {process.ExitCode}");
        }
    }
}
