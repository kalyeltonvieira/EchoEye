namespace EchoEye.Core.Constants;

/// <summary>
/// Constant paths used throughout the application
/// </summary>
public static class Paths
{
    /// <summary>
    /// Base installation directory
    /// </summary>
    public static readonly string BaseDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "EchoEye");

    /// <summary>
    /// Data directory
    /// </summary>
    public static readonly string DataDirectory = Path.Combine(BaseDirectory, "Data");

    /// <summary>
    /// Database file path
    /// </summary>
    public static readonly string DatabasePath = Path.Combine(DataDirectory, "echoeye.db");

    /// <summary>
    /// Logs directory
    /// </summary>
    public static readonly string LogsDirectory = Path.Combine(BaseDirectory, "Logs");

    /// <summary>
    /// Configuration directory
    /// </summary>
    public static readonly string ConfigDirectory = Path.Combine(BaseDirectory, "Config");

    /// <summary>
    /// Recovery points directory
    /// </summary>
    public static readonly string RecoveryDirectory = Path.Combine(BaseDirectory, "Recovery");

    /// <summary>
    /// Forensic evidence directory
    /// </summary>
    public static readonly string ForensicsDirectory = Path.Combine(BaseDirectory, "Forensics");

    /// <summary>
    /// Decoys directory
    /// </summary>
    public static readonly string DecoysDirectory = Path.Combine(BaseDirectory, "Decoys");

    /// <summary>
    /// Intelligence directory
    /// </summary>
    public static readonly string IntelligenceDirectory = Path.Combine(BaseDirectory, "Intelligence");

    /// <summary>
    /// Temp directory
    /// </summary>
    public static readonly string TempDirectory = Path.Combine(BaseDirectory, "Temp");

    /// <summary>
    /// Ensure all directories exist
    /// </summary>
    public static void EnsureDirectories()
    {
        var directories = new[]
        {
            BaseDirectory,
            DataDirectory,
            LogsDirectory,
            ConfigDirectory,
            RecoveryDirectory,
            ForensicsDirectory,
            DecoysDirectory,
            IntelligenceDirectory,
            TempDirectory
        };

        foreach (var dir in directories)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }
    }
}
