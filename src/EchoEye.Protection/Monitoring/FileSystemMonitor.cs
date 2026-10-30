using EchoEye.Core.Models;
using EchoEye.Core.Utilities;
using Microsoft.Extensions.Logging;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace EchoEye.Protection.Monitoring;

public interface IFileSystemMonitor
{
    IObservable<FileEvent> FileEvents { get; }
    Task StartMonitoringAsync(string path, bool recursive = true);
    Task StopMonitoringAsync(string path);
    Task<List<string>> GetMonitoredPathsAsync();
    Task<MonitoringStatistics> GetStatisticsAsync();
}

/// <summary>
/// Monitors file system activity using FileSystemWatcher
/// </summary>
public class FileSystemMonitor : IFileSystemMonitor
{
    private readonly ILogger _logger;
    private readonly Dictionary<string, FileSystemWatcher> _watchers;
    private readonly Subject<FileEvent> _fileEventSubject;
    private readonly Queue<DateTime> _recentEvents;
    private readonly object _statisticsLock;
    private long _eventsProcessed;
    private DateTime _startedAt;

    public IObservable<FileEvent> FileEvents => _fileEventSubject.AsObservable();

    public FileSystemMonitor(ILogger logger)
    {
        _logger = logger;
        _watchers = new Dictionary<string, FileSystemWatcher>();
        _fileEventSubject = new Subject<FileEvent>();
        _recentEvents = new Queue<DateTime>();
        _statisticsLock = new object();
        _eventsProcessed = 0;
        _startedAt = DateTime.UtcNow;
    }

    public Task StartMonitoringAsync(string path, bool recursive = true)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                _logger.LogWarning("Path does not exist: {Path}", path);
                return Task.CompletedTask;
            }

            if (_watchers.ContainsKey(path))
            {
                _logger.LogWarning("Already monitoring path: {Path}", path);
                return Task.CompletedTask;
            }

            if (_watchers.Count == 0)
            {
                _startedAt = DateTime.UtcNow;
            }

            var watcher = new FileSystemWatcher(path)
            {
                NotifyFilter = NotifyFilters.Attributes |
                               NotifyFilters.CreationTime |
                               NotifyFilters.FileName |
                               NotifyFilters.LastWrite |
                               NotifyFilters.Size,
                IncludeSubdirectories = recursive,
                EnableRaisingEvents = true
            };

            watcher.Created += OnFileCreated;
            watcher.Changed += OnFileChanged;
            watcher.Deleted += OnFileDeleted;
            watcher.Renamed += OnFileRenamed;
            watcher.Error += OnWatcherError;

            _watchers[path] = watcher;

            _logger.LogInformation("Started monitoring path: {Path} (Recursive: {Recursive})", path, recursive);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start monitoring path: {Path}", path);
            throw;
        }
    }

    public Task StopMonitoringAsync(string path)
    {
        try
        {
            if (_watchers.TryGetValue(path, out var watcher))
            {
                watcher.Created -= OnFileCreated;
                watcher.Changed -= OnFileChanged;
                watcher.Deleted -= OnFileDeleted;
                watcher.Renamed -= OnFileRenamed;
                watcher.Error -= OnWatcherError;

                watcher.Dispose();
                _watchers.Remove(path);

                _logger.LogInformation("Stopped monitoring path: {Path}", path);
            }

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop monitoring path: {Path}", path);
            throw;
        }
    }

    public Task<List<string>> GetMonitoredPathsAsync()
    {
        return Task.FromResult(_watchers.Keys.ToList());
    }

    public Task<MonitoringStatistics> GetStatisticsAsync()
    {
        return Task.FromResult(new MonitoringStatistics
        {
            MonitoredPaths = _watchers.Count,
            TotalEventsProcessed = _eventsProcessed,
            EventsPerSecond = CalculateEventsPerSecond(),
            StartedAt = _startedAt
        });
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        _logger.LogDebug("File created: {Path}", e.FullPath);
        PublishFileEvent(e.FullPath, FileChangeType.Created);
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        _logger.LogDebug("File changed: {Path}", e.FullPath);
        PublishFileEvent(e.FullPath, FileChangeType.Modified);
    }

    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        _logger.LogDebug("File deleted: {Path}", e.FullPath);
        PublishFileEvent(e.FullPath, FileChangeType.Deleted);
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        _logger.LogDebug("File renamed from {OldPath} to {NewPath}", e.OldFullPath, e.FullPath);
        PublishFileEvent(e.FullPath, FileChangeType.Renamed);
    }

    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        _logger.LogError(e.GetException(), "FileSystemWatcher error");
    }

    private void PublishFileEvent(string filePath, FileChangeType changeType)
    {
        try
        {
            var fileEvent = new FileEvent
            {
                EventId = CryptoHelper.GenerateUuid(),
                FilePath = filePath,
                ChangeType = changeType,
                OccurredAt = DateTime.UtcNow,
                OriginalSize = GetFileSize(filePath),
                NewSize = GetFileSize(filePath)
            };

            // Calculate entropy for modified files
            if (changeType == FileChangeType.Modified && File.Exists(filePath))
            {
                fileEvent.EntropyScore = CalculateFileEntropy(filePath);
                fileEvent.NewHash = CryptoHelper.ComputeFileSha256Async(filePath).GetAwaiter().GetResult();

                // Detect encryption based on entropy
                if (fileEvent.EntropyScore > 7.5)
                {
                    fileEvent.ChangeType = FileChangeType.Encrypted;
                }
            }

            Interlocked.Increment(ref _eventsProcessed);
            TrackEvent(DateTime.UtcNow);
            _fileEventSubject.OnNext(fileEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish file event for: {Path}", filePath);
        }
    }

    private long GetFileSize(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                var fileInfo = new FileInfo(filePath);
                return fileInfo.Length;
            }
        }
        catch
        {
            // Ignore errors
        }
        return 0;
    }

    private double CalculateFileEntropy(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                return CryptoHelper.CalculateFileEntropyAsync(filePath).GetAwaiter().GetResult();
            }
        }
        catch
        {
            // Ignore errors
        }
        return 0;
    }

    private double CalculateEventsPerSecond()
    {
        var now = DateTime.UtcNow;
        lock (_statisticsLock)
        {
            PruneRecentEvents(now);
            return _recentEvents.Count / 60.0;
        }
    }

    private void TrackEvent(DateTime occurredAt)
    {
        lock (_statisticsLock)
        {
            _recentEvents.Enqueue(occurredAt);
            PruneRecentEvents(occurredAt);
        }
    }

    private void PruneRecentEvents(DateTime now)
    {
        var cutoff = now - TimeSpan.FromMinutes(1);
        while (_recentEvents.Count > 0 && _recentEvents.Peek() < cutoff)
        {
            _recentEvents.Dequeue();
        }
    }
}

public class MonitoringStatistics
{
    public int MonitoredPaths { get; set; }
    public long TotalEventsProcessed { get; set; }
    public double EventsPerSecond { get; set; }
    public DateTime StartedAt { get; set; }
}
