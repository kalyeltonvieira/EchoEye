using EchoEye.Core.Models;
using EchoEye.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace EchoEye.Intelligence;

public interface IAnonymizer
{
    string AnonymizePath(string filePath);
    string AnonymizeProcess(string processName);
    Task<ThreatReport> AnonymizeReportAsync(ThreatReport report);
    string HashIdentifier(string identifier);
}

/// <summary>
/// Anonymizes sensitive data before sharing
/// </summary>
public class Anonymizer : IAnonymizer
{
    private readonly ILogger _logger;

    public Anonymizer(ILogger logger)
    {
        _logger = logger;
    }

    public string AnonymizePath(string filePath)
    {
        try
        {
            // Remove user-specific parts of the path
            var path = filePath;

            // Replace user profile path
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (path.StartsWith(userProfile, StringComparison.OrdinalIgnoreCase))
            {
                path = "%USERPROFILE%" + path.Substring(userProfile.Length);
            }

            // Hash the filename to preserve uniqueness while removing content
            var fileName = Path.GetFileName(path);
            var directory = Path.GetDirectoryName(path);
            var hashedFileName = CryptoHelper.ComputeStringHash(fileName).Substring(0, 8);

            return Path.Combine(directory ?? string.Empty, hashedFileName + Path.GetExtension(fileName));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to anonymize path: {Path}", filePath);
            return "[ANONYMIZED]";
        }
    }

    public string AnonymizeProcess(string processName)
    {
        try
        {
            // Hash process name to preserve identity without revealing actual name
            return CryptoHelper.ComputeStringHash(processName).Substring(0, 12);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to anonymize process: {Process}", processName);
            return "[ANONYMIZED]";
        }
    }

    public async Task<ThreatReport> AnonymizeReportAsync(ThreatReport report)
    {
        var anonymized = new ThreatReport
        {
            ReportId = report.ReportId,
            ThreatFamilyId = report.ThreatFamilyId,
            DeviceId = CryptoHelper.ComputeStringHash(report.DeviceId).Substring(0, 16),
            GeneratedAt = report.GeneratedAt,
            FirstSeen = report.FirstSeen,
            LastSeen = report.LastSeen,
            BehavioralFingerprint = report.BehavioralFingerprint,
            GeographicRegion = report.GeographicRegion,
            DetectionCount = report.DetectionCount,
            RecoverySuccessCount = report.RecoverySuccessCount,
            RecoveryFailureCount = report.RecoveryFailureCount,
            AverageConfidenceScore = report.AverageConfidenceScore,
            IsShared = true,
            SharedAt = DateTime.UtcNow
        };

        // Anonymize file hashes if present (already hashes, but ensure no path data)
        if (!string.IsNullOrEmpty(report.FileHashes))
        {
            // File hashes are already SHA-256, so they're safe
            anonymized.FileHashes = report.FileHashes;
        }

        // Anonymize process indicators
        if (!string.IsNullOrEmpty(report.ProcessIndicators))
        {
            var processes = JsonHelper.Deserialize<List<string>>(report.ProcessIndicators);
            if (processes != null)
            {
                var anonymizedProcesses = processes.Select(AnonymizeProcess).ToList();
                anonymized.ProcessIndicators = JsonHelper.Serialize(anonymizedProcesses);
            }
        }

        // Remove any metadata that might contain sensitive information
        anonymized.Metadata = null;

        _logger.LogInformation("Threat report anonymized: {ReportId}", report.ReportId);
        return anonymized;
    }

    public string HashIdentifier(string identifier)
    {
        return CryptoHelper.ComputeStringHash(identifier).Substring(0, 16);
    }
}
