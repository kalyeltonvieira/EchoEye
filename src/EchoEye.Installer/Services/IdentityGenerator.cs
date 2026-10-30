using EchoEye.Core.Models;
using EchoEye.Core.Utilities;
using Microsoft.Extensions.Logging;
using System.Management;

namespace EchoEye.Installer.Services;

/// <summary>
/// Generates device identity
/// </summary>
public class IdentityGenerator
{
    private readonly ILogger _logger;

    public IdentityGenerator(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generate device identity
    /// </summary>
    public async Task<DeviceIdentity> GenerateIdentityAsync()
    {
        var identity = new DeviceIdentity
        {
            DeviceId = CryptoHelper.GenerateUuid(),
            MachineName = Environment.MachineName,
            OsVersion = Environment.OSVersion.VersionString,
            FirstRegistered = DateTime.UtcNow,
            LastSeen = DateTime.UtcNow
        };

        // Generate hardware fingerprint
        identity.Fingerprint = await GenerateHardwareFingerprintAsync();

        // Generate registration key
        identity.RegistrationKey = CryptoHelper.GenerateRandomBytes(32);

        _logger.LogInformation("Device identity generated: {DeviceId}", identity.DeviceId);
        return identity;
    }

    /// <summary>
    /// Generate hardware fingerprint from system identifiers
    /// </summary>
    private async Task<string> GenerateHardwareFingerprintAsync()
    {
        try
        {
            var identifiers = new List<string>();

            // Get CPU ID
            identifiers.Add(await GetCpuIdAsync());

            // Get motherboard serial
            identifiers.Add(await GetMotherboardSerialAsync());

            // Get MAC address of primary network adapter
            identifiers.Add(GetPrimaryMacAddress());

            // Combine and hash
            var combined = string.Join("|", identifiers.Where(i => !string.IsNullOrEmpty(i)));
            return CryptoHelper.ComputeStringHash(combined);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate hardware fingerprint, using fallback");
            // Fallback to machine name + username
            var fallback = $"{Environment.MachineName}|{Environment.UserName}";
            return CryptoHelper.ComputeStringHash(fallback);
        }
    }

    /// <summary>
    /// Get CPU identifier
    /// </summary>
    private async Task<string> GetCpuIdAsync()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor");
            var objects = await Task.Run(() => searcher.Get());

            foreach (ManagementObject obj in objects)
            {
                return obj["ProcessorId"]?.ToString() ?? string.Empty;
            }
        }
        catch
        {
            // Ignore errors
        }
        return string.Empty;
    }

    /// <summary>
    /// Get motherboard serial number
    /// </summary>
    private async Task<string> GetMotherboardSerialAsync()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard");
            var objects = await Task.Run(() => searcher.Get());

            foreach (ManagementObject obj in objects)
            {
                return obj["SerialNumber"]?.ToString() ?? string.Empty;
            }
        }
        catch
        {
            // Ignore errors
        }
        return string.Empty;
    }

    /// <summary>
    /// Get primary network adapter MAC address
    /// </summary>
    private string GetPrimaryMacAddress()
    {
        try
        {
            var nics = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
            var primary = nics.FirstOrDefault(n =>
                n.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up &&
                n.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback);

            return primary?.GetPhysicalAddress().ToString() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
