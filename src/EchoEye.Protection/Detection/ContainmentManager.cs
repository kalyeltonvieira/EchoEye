using EchoEye.Core.Exceptions;
using EchoEye.Core.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace EchoEye.Protection.Detection;

public interface IContainmentManager
{
    Task<ContainmentResult> ContainProcessAsync(string processId, ContainmentAction action);
    Task<ContainmentResult> ContainFileAsync(string filePath, ContainmentAction action);
    Task<ContainmentResult> IsolateDeviceAsync();
    Task<ContainmentResult> LiftContainmentAsync(string containmentId);
    Task<List<Containment>> GetActiveContainmentsAsync();
}

/// <summary>
/// Manages threat containment actions
/// </summary>
public class ContainmentManager : IContainmentManager
{
    private readonly ILogger _logger;
    private readonly Dictionary<string, Containment> _activeContainments;

    public ContainmentManager(ILogger logger)
    {
        _logger = logger;
        _activeContainments = new Dictionary<string, Containment>();
    }

    public Task<ContainmentResult> ContainProcessAsync(string processId, ContainmentAction action)
    {
        try
        {
            _logger.LogWarning("Containing process {ProcessId} with action {Action}", processId, action);

            var result = new ContainmentResult
            {
                ContainmentId = CryptoHelper.GenerateUuid(),
                ContainedAt = DateTime.UtcNow
            };

            switch (action)
            {
                case ContainmentAction.SuspendProcess:
                    SuspendProcess(processId);
                    result.Success = true;
                    result.Message = $"Process {processId} suspended";
                    break;

                case ContainmentAction.TerminateProcess:
                    TerminateProcess(processId);
                    result.Success = true;
                    result.Message = $"Process {processId} terminated";
                    break;

                case ContainmentAction.BlockNetworkAccess:
                    BlockProcessNetwork(processId);
                    result.Success = true;
                    result.Message = $"Network access blocked for process {processId}";
                    break;

                default:
                    result.Success = false;
                    result.Message = $"Unsupported containment action: {action}";
                    break;
            }

            if (result.Success)
            {
                _activeContainments[result.ContainmentId] = new Containment
                {
                    ContainmentId = result.ContainmentId,
                    TargetType = "Process",
                    TargetId = processId,
                    Action = action,
                    ContainedAt = result.ContainedAt
                };
            }

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to contain process {ProcessId}", processId);
            throw new ContainmentException($"Failed to contain process: {ex.Message}", ex);
        }
    }

    public Task<ContainmentResult> ContainFileAsync(string filePath, ContainmentAction action)
    {
        try
        {
            _logger.LogWarning("Containing file {FilePath} with action {Action}", filePath, action);

            var result = new ContainmentResult
            {
                ContainmentId = CryptoHelper.GenerateUuid(),
                ContainedAt = DateTime.UtcNow
            };

            switch (action)
            {
                case ContainmentAction.QuarantineFile:
                    QuarantineFile(filePath);
                    result.Success = true;
                    result.Message = $"File {filePath} quarantined";
                    break;

                case ContainmentAction.DeleteFile:
                    DeleteFile(filePath);
                    result.Success = true;
                    result.Message = $"File {filePath} deleted";
                    break;

                default:
                    result.Success = false;
                    result.Message = $"Unsupported containment action: {action}";
                    break;
            }

            if (result.Success)
            {
                _activeContainments[result.ContainmentId] = new Containment
                {
                    ContainmentId = result.ContainmentId,
                    TargetType = "File",
                    TargetId = filePath,
                    Action = action,
                    ContainedAt = result.ContainedAt
                };
            }

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to contain file {FilePath}", filePath);
            throw new ContainmentException($"Failed to contain file: {ex.Message}", ex);
        }
    }

    public Task<ContainmentResult> IsolateDeviceAsync()
    {
        _logger.LogWarning("Device isolation requested");

        AddDeviceIsolation();

        var containmentId = CryptoHelper.GenerateUuid();
        var containedAt = DateTime.UtcNow;
        _activeContainments[containmentId] = new Containment
        {
            ContainmentId = containmentId,
            TargetType = "Device",
            TargetId = Environment.MachineName,
            Action = ContainmentAction.IsolateDevice,
            ContainedAt = containedAt
        };

        return Task.FromResult(new ContainmentResult
        {
            ContainmentId = containmentId,
            Success = true,
            Message = "Device network isolation enabled",
            ContainedAt = containedAt
        });
    }

    public Task<ContainmentResult> LiftContainmentAsync(string containmentId)
    {
        try
        {
            if (_activeContainments.TryGetValue(containmentId, out var containment))
            {
                if (containment.Action == ContainmentAction.BlockNetworkAccess)
                {
                    RemoveNetworkBlock(containment.TargetId);
                }
                else if (containment.Action == ContainmentAction.IsolateDevice)
                {
                    RemoveDeviceIsolation();
                }

                _activeContainments.Remove(containmentId);
                _logger.LogInformation("Lifted containment {ContainmentId}", containmentId);

                return Task.FromResult(new ContainmentResult
                {
                    Success = true,
                    Message = $"Containment {containmentId} lifted"
                });
            }

            return Task.FromResult(new ContainmentResult
            {
                Success = false,
                Message = $"Containment {containmentId} not found"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to lift containment {ContainmentId}", containmentId);
            throw new ContainmentException($"Failed to lift containment: {ex.Message}", ex);
        }
    }

    public Task<List<Containment>> GetActiveContainmentsAsync()
    {
        return Task.FromResult(_activeContainments.Values.ToList());
    }

    private void SuspendProcess(string processId)
    {
        try
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException("Process suspension is only supported on Windows.");
            }

            var process = Process.GetProcessById(int.Parse(processId));
            foreach (ProcessThread thread in process.Threads)
            {
                var handle = OpenThread(ThreadAccess.SuspendResume, false, (uint)thread.Id);
                if (handle == IntPtr.Zero)
                {
                    continue;
                }

                try
                {
                    SuspendThread(handle);
                }
                finally
                {
                    CloseHandle(handle);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to suspend process {ProcessId}", processId);
            throw;
        }
    }

    private void TerminateProcess(string processId)
    {
        try
        {
            var process = Process.GetProcessById(int.Parse(processId));
            process.Kill();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to terminate process {ProcessId}", processId);
            throw;
        }
    }

    private void BlockProcessNetwork(string processId)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Windows Firewall containment is only supported on Windows.");
        }

        var process = Process.GetProcessById(int.Parse(processId));
        var executablePath = process.MainModule?.FileName;
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            throw new ContainmentException($"Cannot resolve executable path for process {processId}");
        }

        var ruleName = GetFirewallRuleName(processId);
        ExecuteNetsh($"advfirewall firewall add rule name=\"{ruleName}\" dir=out action=block program=\"{executablePath}\" enable=yes");
        _logger.LogInformation("Blocked outbound network access for process {ProcessId} ({Path})", processId, executablePath);
    }

    private void RemoveNetworkBlock(string processId)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        ExecuteNetsh($"advfirewall firewall delete rule name=\"{GetFirewallRuleName(processId)}\"");
    }

    private static string GetFirewallRuleName(string processId)
    {
        return $"EchoEye-Containment-{processId}";
    }

    private static void AddDeviceIsolation()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Device isolation is only supported on Windows.");
        }

        ExecuteNetsh("advfirewall firewall delete rule name=\"EchoEye-Isolation-Out\"", throwOnFailure: false);
        ExecuteNetsh("advfirewall firewall delete rule name=\"EchoEye-Isolation-In\"", throwOnFailure: false);
        ExecuteNetsh("advfirewall firewall add rule name=\"EchoEye-Isolation-Out\" dir=out action=block enable=yes");
        ExecuteNetsh("advfirewall firewall add rule name=\"EchoEye-Isolation-In\" dir=in action=block enable=yes");
    }

    private static void RemoveDeviceIsolation()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        ExecuteNetsh("advfirewall firewall delete rule name=\"EchoEye-Isolation-Out\"", throwOnFailure: false);
        ExecuteNetsh("advfirewall firewall delete rule name=\"EchoEye-Isolation-In\"", throwOnFailure: false);
    }

    private static void ExecuteNetsh(string arguments, bool throwOnFailure = true)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "netsh.exe",
            Arguments = arguments,
            CreateNoWindow = true,
            UseShellExecute = false
        });

        if (process == null)
        {
            throw new ContainmentException("Failed to start netsh.exe");
        }

        if (!process.WaitForExit(5000))
        {
            process.Kill();
            throw new ContainmentException("Timed out while updating Windows Firewall rule");
        }

        if (process.ExitCode != 0)
        {
            if (!throwOnFailure)
            {
                return;
            }

            throw new ContainmentException($"netsh.exe exited with code {process.ExitCode}");
        }
    }

    private void QuarantineFile(string filePath)
    {
        // Move file to quarantine directory
        var quarantineDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "EchoEye", "Quarantine");

        if (!Directory.Exists(quarantineDir))
        {
            Directory.CreateDirectory(quarantineDir);
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File to quarantine was not found.", filePath);
        }

        var quarantineName = $"{Path.GetFileName(filePath)}.{DateTime.UtcNow:yyyyMMddHHmmssfff}.quarantined";
        var quarantinePath = Path.Combine(quarantineDir, quarantineName);
        File.Move(filePath, quarantinePath);
    }

    private void DeleteFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return;
        }

        File.Delete(filePath);
    }

    [Flags]
    private enum ThreadAccess
    {
        SuspendResume = 0x0002
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenThread(ThreadAccess desiredAccess, bool inheritHandle, uint threadId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint SuspendThread(IntPtr threadHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);
}

public class Containment
{
    public string ContainmentId { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public ContainmentAction Action { get; set; }
    public DateTime ContainedAt { get; set; }
}

public class ContainmentResult
{
    public string ContainmentId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime ContainedAt { get; set; }
}

public enum ContainmentAction
{
    SuspendProcess,
    TerminateProcess,
    QuarantineFile,
    DeleteFile,
    BlockNetworkAccess,
    IsolateDevice
}
