using EchoEye.Core.Models;
using EchoEye.Core.Utilities;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace EchoEye.Protection.Monitoring;

public interface IProcessTracker
{
    IObservable<ProcessEvent> ProcessEvents { get; }
    Task StartTrackingAsync();
    Task StopTrackingAsync();
    Task<ProcessInfo?> GetProcessInfoAsync(string processId);
    Task<List<ProcessInfo>> GetActiveProcessesAsync();
    Task UpdateReputationAsync(string processId, ProcessReputation reputation);
    Task<List<ProcessInfo>> GetSuspiciousProcessesAsync();
}

/// <summary>
/// Tracks process behavior and reputation
/// </summary>
public class ProcessTracker : IProcessTracker
{
    private readonly ILogger _logger;
    private readonly Dictionary<string, ProcessInfo> _processes;
    private readonly Subject<ProcessEvent> _processEventSubject;

    public IObservable<ProcessEvent> ProcessEvents => _processEventSubject.AsObservable();

    public ProcessTracker(ILogger logger)
    {
        _logger = logger;
        _processes = new Dictionary<string, ProcessInfo>();
        _processEventSubject = new Subject<ProcessEvent>();
    }

    public Task StartTrackingAsync()
    {
        _logger.LogInformation("Starting process tracking");

        // Get initial list of processes
        foreach (var process in Process.GetProcesses())
        {
            try
            {
                TrackProcess(process);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to track process: {ProcessId}", process.Id);
            }
        }

        // In production, would use WMI event notifications for real-time tracking
        return Task.CompletedTask;
    }

    public Task StopTrackingAsync()
    {
        _logger.LogInformation("Stopping process tracking");
        _processes.Clear();
        return Task.CompletedTask;
    }

    public Task<ProcessInfo?> GetProcessInfoAsync(string processId)
    {
        _processes.TryGetValue(processId, out var processInfo);
        return Task.FromResult(processInfo);
    }

    public Task<List<ProcessInfo>> GetActiveProcessesAsync()
    {
        return Task.FromResult(_processes.Values.Where(p => p.EndedAt == null).ToList());
    }

    public Task UpdateReputationAsync(string processId, ProcessReputation reputation)
    {
        if (_processes.TryGetValue(processId, out var processInfo))
        {
            processInfo.Reputation = reputation;
            _processEventSubject.OnNext(new ProcessEvent
            {
                ProcessId = processId,
                ProcessName = processInfo.ProcessName,
                Type = ProcessEventType.ReputationChanged,
                Timestamp = DateTime.UtcNow,
                Details = new { NewReputation = reputation }
            });
        }
        return Task.CompletedTask;
    }

    public Task<List<ProcessInfo>> GetSuspiciousProcessesAsync()
    {
        return Task.FromResult(_processes.Values
            .Where(p => p.Reputation == ProcessReputation.Suspicious || p.Reputation == ProcessReputation.Malicious)
            .ToList());
    }

    private void TrackProcess(Process process)
    {
        try
        {
            var processId = process.Id.ToString();
            var processInfo = new ProcessInfo
            {
                ProcessId = processId,
                ProcessName = process.ProcessName,
                ExecutablePath = process.MainModule?.FileName ?? string.Empty,
                StartedAt = process.StartTime,
                Reputation = ProcessReputation.Unknown,
                FileModificationCount = 0,
                DecoyInteractionCount = 0
            };

            _processes[processId] = processInfo;

            _logger.LogDebug("Tracked process: {ProcessName} ({ProcessId})", process.ProcessName, process.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get process info for: {ProcessId}", process.Id);
        }
    }
}
