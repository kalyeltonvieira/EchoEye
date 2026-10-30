namespace EchoEye.Core.Constants;

/// <summary>
/// Default configuration values
/// </summary>
public static class Defaults
{
    // Protection Service Defaults
    public static class Protection
    {
        public const bool Enabled = true;
        public const int ConfidenceThreshold = 75;
        public const int FileVelocityThreshold = 100; // files per minute
        public const double EntropyThreshold = 7.5;
        public const int MaxFileSizeBytes = 100 * 1024 * 1024; // 100 MB
        public static readonly string[] DefaultWatchedPaths = new[]
        {
            "%USERPROFILE%\\Documents",
            "%USERPROFILE%\\Desktop",
            "%USERPROFILE%\\Pictures",
            "%USERPROFILE%\\Downloads"
        };
        public static readonly string[] DefaultExcludedPaths = new[]
        {
            "%LOCALAPPDATA%",
            "%TEMP%",
            "%WINDIR%",
            "%PROGRAMFILES%"
        };
    }

    // Decoy Engine Defaults
    public static class Decoy
    {
        public const bool Enabled = true;
        public const int DecoyCount = 50;
        public const int MaxDecoysPerPath = 10;
        public static readonly string[] DefaultDeploymentPaths = new[]
        {
            "%USERPROFILE%\\Documents",
            "%USERPROFILE%\\Desktop"
        };
    }

    // Recovery Engine Defaults
    public static class Recovery
    {
        public const bool Enabled = true;
        public const int RetentionDays = 90;
        public const int MaxStorageGB = 100;
        public const int AutomaticBackupIntervalHours = 6;
        public const double MaxStorageUsagePercentage = 0.8;
    }

    // Intelligence Node Defaults
    public static class Intelligence
    {
        public const bool Enabled = true;
        public const bool SharingEnabled = true;
        public const bool PeerDiscoveryEnabled = true;
        public const int SyncIntervalMinutes = 60;
        public const int DefaultPeerPort = 8443;
    }

    // Logging Defaults
    public static class Logging
    {
        public const int RetentionDays = 90;
        public const int MaxLogFileSizeMB = 10;
        public const string DefaultLogLevel = "Information";
    }

    // Database Defaults
    public static class Database
    {
        public const int ConnectionTimeoutSeconds = 30;
        public const int CommandTimeoutSeconds = 60;
        public const bool EnableWALMode = true;
    }
}
