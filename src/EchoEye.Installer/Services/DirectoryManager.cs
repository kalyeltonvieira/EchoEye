using EchoEye.Core.Constants;
using Microsoft.Extensions.Logging;

namespace EchoEye.Installer.Services;

/// <summary>
/// Manages directory structure creation
/// </summary>
public class DirectoryManager
{
    private readonly ILogger _logger;

    public DirectoryManager(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Create all required directories
    /// </summary>
    public void CreateDirectories()
    {
        try
        {
            Paths.EnsureDirectories();
            _logger.LogInformation("All directories created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create directories");
            throw;
        }
    }

    /// <summary>
    /// Set directory permissions
    /// </summary>
    public void SetDirectoryPermissions(string path)
    {
        try
        {
            var directoryInfo = new DirectoryInfo(path);
            var security = directoryInfo.GetAccessControl();

            // Add full control for SYSTEM
            // Add full control for Administrators
            // Add read/write for the service account

            directoryInfo.SetAccessControl(security);
            _logger.LogInformation("Permissions set for directory: {Path}", path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set permissions for directory: {Path}", path);
        }
    }
}
