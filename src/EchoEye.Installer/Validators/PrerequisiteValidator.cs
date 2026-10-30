using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Management;

namespace EchoEye.Installer.Validators;

/// <summary>
/// Validates system prerequisites before installation
/// </summary>
public class PrerequisiteValidator
{
    private readonly ILogger _logger;

    public PrerequisiteValidator(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validate all prerequisites
    /// </summary>
    public PrerequisiteValidationResult Validate()
    {
        var result = new PrerequisiteValidationResult();

        // Check Windows version
        ValidateWindowsVersion(result);

        // Check .NET runtime
        ValidateDotNetRuntime(result);

        // Check disk space
        ValidateDiskSpace(result);

        // Check administrator privileges
        ValidateAdministratorPrivileges(result);

        // Check Windows Service availability
        ValidateWindowsService(result);

        result.IsValid = result.Errors.Count == 0;
        return result;
    }

    /// <summary>
    /// Validate Windows version
    /// </summary>
    private void ValidateWindowsVersion(PrerequisiteValidationResult result)
    {
        try
        {
            var os = Environment.OSVersion;
            var version = os.Version;

            _logger.LogInformation("Windows Version: {Version}", os.VersionString);

            // Require Windows 10 or later (build 10240)
            if (version.Major < 10 || (version.Major == 10 && version.Build < 10240))
            {
                result.Errors.Add("Windows 10 or later is required");
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to validate Windows version: {ex.Message}");
        }
    }

    /// <summary>
    /// Validate .NET runtime
    /// </summary>
    private void ValidateDotNetRuntime(PrerequisiteValidationResult result)
    {
        try
        {
            // Check for .NET 8.0 runtime
            var runtimePath = Environment.GetEnvironmentVariable("DOTNET_ROOT");
            if (string.IsNullOrEmpty(runtimePath))
            {
                result.Warnings.Add(".NET runtime not found in environment variables");
            }

            _logger.LogInformation(".NET Runtime check completed");
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to validate .NET runtime: {ex.Message}");
        }
    }

    /// <summary>
    /// Validate available disk space
    /// </summary>
    private void ValidateDiskSpace(PrerequisiteValidationResult result)
    {
        try
        {
            var drive = new DriveInfo(Environment.SystemDirectory);
            var availableSpaceGB = drive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);

            _logger.LogInformation("Available disk space: {SpaceGB} GB", availableSpaceGB.ToString("F2"));

            // Require at least 5 GB free space
            if (availableSpaceGB < 5)
            {
                result.Errors.Add($"Insufficient disk space. At least 5 GB required, {availableSpaceGB:F2} GB available");
            }
            else if (availableSpaceGB < 10)
            {
                result.Warnings.Add($"Low disk space: {availableSpaceGB:F2} GB available. 10 GB recommended.");
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to validate disk space: {ex.Message}");
        }
    }

    /// <summary>
    /// Validate administrator privileges
    /// </summary>
    private void ValidateAdministratorPrivileges(PrerequisiteValidationResult result)
    {
        try
        {
            using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            var isAdmin = principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);

            if (!isAdmin)
            {
                result.Errors.Add("Administrator privileges are required for installation");
            }

            _logger.LogInformation("Administrator privileges: {IsAdmin}", isAdmin);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to validate administrator privileges: {ex.Message}");
        }
    }

    /// <summary>
    /// Validate Windows Service availability
    /// </summary>
    private void ValidateWindowsService(PrerequisiteValidationResult result)
    {
        try
        {
            // Check if Service Control Manager is accessible
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Service");
            var services = searcher.Get();

            _logger.LogInformation("Windows Service check completed. {Count} services found", services.Count);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to validate Windows Service availability: {ex.Message}");
        }
    }
}

/// <summary>
/// Result of prerequisite validation
/// </summary>
public class PrerequisiteValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
