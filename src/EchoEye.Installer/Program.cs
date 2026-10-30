using EchoEye.Core.Constants;
using EchoEye.Core.Exceptions;
using EchoEye.Core.Utilities;
using EchoEye.Installer.Services;
using Microsoft.Extensions.Logging;

namespace EchoEye.Installer;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // Setup logging
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        var logger = loggerFactory.CreateLogger<Program>();

        try
        {
            logger.LogInformation("EchoEye X Installer");
            logger.LogInformation("====================");

            var installer = new InstallerService(logger);
            var result = await installer.InstallAsync();

            if (result.Success)
            {
                logger.LogInformation("Installation completed successfully!");
                logger.LogInformation("Device ID: {DeviceId}", result.DeviceId);
                logger.LogInformation("Installation Report: {ReportPath}", result.ReportPath);
                return 0;
            }
            else
            {
                logger.LogError("Installation failed: {Error}", result.ErrorMessage);
                return 1;
            }
        }
        catch (EchoEyeException ex)
        {
            logger.LogError("Installation error [{ErrorCode}]: {Message}", ex.ErrorCode, ex.Message);
            return 1;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during installation");
            return 1;
        }
    }
}
