using EchoEye.Core.Constants;
using EchoEye.Core.Exceptions;
using EchoEye.Core.Interfaces;
using EchoEye.Core.Models;
using EchoEye.Core.Utilities;
using EchoEye.Installer.Validators;
using Microsoft.Extensions.Logging;

namespace EchoEye.Installer.Services;

/// <summary>
/// Main installer service for EchoEye X
/// </summary>
public class InstallerService
{
    private readonly ILogger _logger;
    private readonly PrerequisiteValidator _prerequisiteValidator;
    private readonly DirectoryManager _directoryManager;
    private readonly IdentityGenerator _identityGenerator;
    private readonly DatabaseInitializer _databaseInitializer;
    private readonly ServiceInstaller _serviceInstaller;
    private readonly ConfigurationInitializer _configInitializer;

    public InstallerService(ILogger logger)
    {
        _logger = logger;
        _prerequisiteValidator = new PrerequisiteValidator(logger);
        _directoryManager = new DirectoryManager(logger);
        _identityGenerator = new IdentityGenerator(logger);
        _databaseInitializer = new DatabaseInitializer(logger);
        _serviceInstaller = new ServiceInstaller(logger);
        _configInitializer = new ConfigurationInitializer(logger);
    }

    /// <summary>
    /// Perform complete installation
    /// </summary>
    public async Task<InstallationResult> InstallAsync()
    {
        var result = new InstallationResult();

        try
        {
            // Step 1: Validate prerequisites
            _logger.LogInformation("Step 1: Validating system prerequisites...");
            var prereqResult = _prerequisiteValidator.Validate();
            if (!prereqResult.IsValid)
            {
                result.ErrorMessage = string.Join(", ", prereqResult.Errors);
                return result;
            }
            _logger.LogInformation("Prerequisites validated successfully");

            // Step 2: Create directory structure
            _logger.LogInformation("Step 2: Creating directory structure...");
            _directoryManager.CreateDirectories();
            _logger.LogInformation("Directory structure created");

            // Step 3: Generate device identity
            _logger.LogInformation("Step 3: Generating device identity...");
            var identity = await _identityGenerator.GenerateIdentityAsync();
            result.DeviceId = identity.DeviceId;
            _logger.LogInformation("Device identity generated: {DeviceId}", identity.DeviceId);

            // Step 4: Initialize database
            _logger.LogInformation("Step 4: Initializing database...");
            await _databaseInitializer.InitializeAsync(identity);
            _logger.LogInformation("Database initialized");

            // Step 5: Initialize configuration
            _logger.LogInformation("Step 5: Initializing configuration...");
            await _configInitializer.InitializeAsync();
            _logger.LogInformation("Configuration initialized");

            // Step 6: Register Windows Service
            _logger.LogInformation("Step 6: Registering Windows Service...");
            _serviceInstaller.RegisterService();
            _logger.LogInformation("Windows Service registered");

            // Step 7: Generate installation report
            _logger.LogInformation("Step 7: Generating installation report...");
            result.ReportPath = await GenerateReportAsync(identity, prereqResult);
            _logger.LogInformation("Installation report generated: {ReportPath}", result.ReportPath);

            result.Success = true;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Installation failed");
            result.ErrorMessage = ex.Message;
            return result;
        }
    }

    /// <summary>
    /// Generate installation report
    /// </summary>
    private async Task<string> GenerateReportAsync(DeviceIdentity identity, PrerequisiteValidationResult prereqResult)
    {
        var report = new
        {
            InstallationDate = DateTime.UtcNow,
            Version = "1.0.0",
            DeviceId = identity.DeviceId,
            MachineName = identity.MachineName,
            OsVersion = identity.OsVersion,
            Fingerprint = identity.Fingerprint,
            Prerequisites = new
            {
                IsValid = prereqResult.IsValid,
                Errors = prereqResult.Errors,
                Warnings = prereqResult.Warnings
            },
            Directories = new
            {
                Base = Paths.BaseDirectory,
                Data = Paths.DataDirectory,
                Logs = Paths.LogsDirectory,
                Config = Paths.ConfigDirectory,
                Recovery = Paths.RecoveryDirectory,
                Forensics = Paths.ForensicsDirectory,
                Decoys = Paths.DecoysDirectory,
                Intelligence = Paths.IntelligenceDirectory
            },
            Services = new[]
            {
                new
                {
                    Name = "EchoEyeProtection",
                    DisplayName = "EchoEye X Protection Service",
                    Status = "Registered"
                }
            }
        };

        var reportPath = Path.Combine(Paths.LogsDirectory, $"installation_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json");
        var reportJson = JsonHelper.Serialize(report);
        await File.WriteAllTextAsync(reportPath, reportJson);

        return reportPath;
    }
}

/// <summary>
/// Result of installation operation
/// </summary>
public class InstallationResult
{
    public bool Success { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string ReportPath { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}
