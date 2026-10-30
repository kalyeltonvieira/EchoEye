using EchoEye.Core.Data;
using EchoEye.Core.Exceptions;
using EchoEye.Core.Models;
using EchoEye.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace EchoEye.Recovery;

public interface IRestorationEngine
{
    Task<RestorationResult> RestoreFileAsync(string filePath, string recoveryPointId);
    Task<RestorationResult> RestoreFilesAsync(List<string> filePaths, string recoveryPointId);
    Task<RestorationResult> FullRestoreAsync(string recoveryPointId);
    Task<RestorationProgress> GetProgressAsync(string operationId);
    Task CancelAsync(string operationId);
}

/// <summary>
/// Performs file restoration operations
/// </summary>
public class RestorationEngine : IRestorationEngine
{
    private readonly Database _database;
    private readonly ILogger _logger;
    private readonly Dictionary<string, RestorationProgress> _activeOperations;

    public RestorationEngine(Database database, ILogger logger)
    {
        _database = database;
        _logger = logger;
        _activeOperations = new Dictionary<string, RestorationProgress>();
    }

    public async Task<RestorationResult> RestoreFileAsync(string filePath, string recoveryPointId)
    {
        var operationId = CryptoHelper.GenerateUuid();
        var progress = new RestorationProgress
        {
            OperationId = operationId,
            Status = RecoveryStatus.InProgress,
            TotalFiles = 1,
            CompletedFiles = 0,
            ElapsedTime = TimeSpan.Zero
        };

        _activeOperations[operationId] = progress;

        try
        {
            _logger.LogInformation("Starting file restoration: {File} from recovery point {Id}", filePath, recoveryPointId);

            var recoveryPoint = await GetRecoveryPointAsync(recoveryPointId);
            if (recoveryPoint == null)
            {
                throw new RecoveryException($"Recovery point not found: {recoveryPointId}");
            }

            // Find file in recovery point
            var file = await FindFileInRecoveryPointAsync(recoveryPointId, filePath);
            if (file == null)
            {
                throw new RecoveryException($"File not found in recovery point: {filePath}");
            }

            // Restore file
            await RestoreSingleFileAsync(file);

            progress.CompletedFiles = 1;
            progress.ProgressPercentage = 100.0;
            progress.Status = RecoveryStatus.Completed;

            var result = new RestorationResult
            {
                OperationId = operationId,
                Status = RecoveryStatus.Completed,
                FilesAttempted = 1,
                FilesSucceeded = 1,
                FilesFailed = 0,
                BytesRestored = file.Size,
                Summary = $"Successfully restored {filePath}"
            };

            await SaveRestorationOperationAsync(result, recoveryPointId);

            _logger.LogInformation("File restoration completed: {File}", filePath);
            return result;
        }
        catch (Exception ex)
        {
            progress.Status = RecoveryStatus.Failed;
            _logger.LogError(ex, "File restoration failed: {File}", filePath);

            return new RestorationResult
            {
                OperationId = operationId,
                Status = RecoveryStatus.Failed,
                FilesAttempted = 1,
                FilesSucceeded = 0,
                FilesFailed = 1,
                ErrorDetails = ex.Message,
                Summary = $"Failed to restore {filePath}"
            };
        }
        finally
        {
            _activeOperations.Remove(operationId);
        }
    }

    public async Task<RestorationResult> RestoreFilesAsync(List<string> filePaths, string recoveryPointId)
    {
        var operationId = CryptoHelper.GenerateUuid();
        var progress = new RestorationProgress
        {
            OperationId = operationId,
            Status = RecoveryStatus.InProgress,
            TotalFiles = filePaths.Count,
            CompletedFiles = 0,
            ElapsedTime = TimeSpan.Zero
        };

        _activeOperations[operationId] = progress;
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Starting batch restoration of {Count} files from recovery point {Id}",
                filePaths.Count, recoveryPointId);

            var recoveryPoint = await GetRecoveryPointAsync(recoveryPointId);
            if (recoveryPoint == null)
            {
                throw new RecoveryException($"Recovery point not found: {recoveryPointId}");
            }

            var succeeded = 0;
            var failed = 0;
            var bytesRestored = 0L;
            var failedFiles = new List<string>();

            foreach (var filePath in filePaths)
            {
                try
                {
                    var file = await FindFileInRecoveryPointAsync(recoveryPointId, filePath);
                    if (file != null)
                    {
                        await RestoreSingleFileAsync(file);
                        succeeded++;
                        bytesRestored += file.Size;
                    }
                    else
                    {
                        failed++;
                        failedFiles.Add(filePath);
                    }

                    progress.CompletedFiles++;
                    progress.ProgressPercentage = (double)progress.CompletedFiles / progress.TotalFiles * 100;
                }
                catch (Exception ex)
                {
                    failed++;
                    failedFiles.Add(filePath);
                    _logger.LogWarning(ex, "Failed to restore file: {File}", filePath);
                }
            }

            progress.Status = failed == 0 ? RecoveryStatus.Completed : RecoveryStatus.PartialSuccess;

            var result = new RestorationResult
            {
                OperationId = operationId,
                Status = progress.Status,
                FilesAttempted = filePaths.Count,
                FilesSucceeded = succeeded,
                FilesFailed = failed,
                BytesRestored = bytesRestored,
                FailedFiles = failedFiles,
                Summary = $"Restored {succeeded} of {filePaths.Count} files"
            };

            await SaveRestorationOperationAsync(result, recoveryPointId);

            _logger.LogInformation("Batch restoration completed: {Succeeded} succeeded, {Failed} failed",
                succeeded, failed);

            return result;
        }
        catch (Exception ex)
        {
            progress.Status = RecoveryStatus.Failed;
            _logger.LogError(ex, "Batch restoration failed");

            return new RestorationResult
            {
                OperationId = operationId,
                Status = RecoveryStatus.Failed,
                FilesAttempted = filePaths.Count,
                FilesSucceeded = 0,
                FilesFailed = filePaths.Count,
                ErrorDetails = ex.Message,
                Summary = "Batch restoration failed"
            };
        }
        finally
        {
            progress.ElapsedTime = DateTime.UtcNow - startTime;
            _activeOperations.Remove(operationId);
        }
    }

    public async Task<RestorationResult> FullRestoreAsync(string recoveryPointId)
    {
        var operationId = CryptoHelper.GenerateUuid();
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Starting full restore from recovery point: {Id}", recoveryPointId);

            var recoveryPoint = await GetRecoveryPointAsync(recoveryPointId);
            if (recoveryPoint == null)
            {
                throw new RecoveryException($"Recovery point not found: {recoveryPointId}");
            }

            // Get all files in recovery point
            var files = await GetAllFilesInRecoveryPointAsync(recoveryPointId);
            var filePaths = files.Select(f => f.OriginalPath).ToList();

            return await RestoreFilesAsync(filePaths, recoveryPointId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Full restore failed");

            return new RestorationResult
            {
                OperationId = operationId,
                Status = RecoveryStatus.Failed,
                FilesAttempted = 0,
                FilesSucceeded = 0,
                FilesFailed = 0,
                ErrorDetails = ex.Message,
                Summary = "Full restore failed"
            };
        }
    }

    public async Task<RestorationProgress> GetProgressAsync(string operationId)
    {
        if (_activeOperations.TryGetValue(operationId, out var progress))
        {
            return progress;
        }

        // Try to load from database
        var sql = @"
            SELECT * FROM RecoveryOperation
            WHERE OperationId = @OperationId";

        var parameters = new[] { new SqliteParameter("@OperationId", operationId) };

        var results = _database.ExecuteQuery(sql, reader =>
        {
            return new RestorationProgress
            {
                OperationId = reader.GetString("OperationId"),
                Status = (RecoveryStatus)reader.GetInt32("Status"),
                TotalFiles = reader.GetInt32("FilesAttempted"),
                CompletedFiles = reader.GetInt32("FilesSucceeded"),
                ProgressPercentage = reader.GetInt32("FilesAttempted") > 0
                    ? (double)reader.GetInt32("FilesSucceeded") / reader.GetInt32("FilesAttempted") * 100
                    : 0,
                ElapsedTime = reader.IsDBNull("CompletedAt")
                    ? DateTime.UtcNow - DateTime.Parse(reader.GetString("StartedAt"))
                    : DateTime.Parse(reader.GetString("CompletedAt")) - DateTime.Parse(reader.GetString("StartedAt"))
            };
        }, parameters);

        return results.FirstOrDefault() ?? new RestorationProgress
        {
            OperationId = operationId,
            Status = RecoveryStatus.Pending,
            TotalFiles = 0,
            CompletedFiles = 0,
            ProgressPercentage = 0,
            ElapsedTime = TimeSpan.Zero
        };
    }

    public async Task CancelAsync(string operationId)
    {
        if (_activeOperations.TryGetValue(operationId, out var progress))
        {
            progress.Status = RecoveryStatus.Cancelled;
            _logger.LogInformation("Restoration operation cancelled: {OperationId}", operationId);
        }
    }

    private async Task<RecoveryPoint?> GetRecoveryPointAsync(string recoveryPointId)
    {
        var sql = "SELECT * FROM RecoveryPoint WHERE RecoveryPointId = @RecoveryPointId";
        var parameters = new[] { new SqliteParameter("@RecoveryPointId", recoveryPointId) };

        var results = _database.ExecuteQuery(sql, reader =>
        {
            return new RecoveryPoint
            {
                RecoveryPointId = reader.GetString("RecoveryPointId"),
                DeviceId = reader.GetString("DeviceId"),
                Name = reader.GetString("Name"),
                Type = (RecoveryPointType)reader.GetInt32("Type"),
                CreatedAt = DateTime.Parse(reader.GetString("CreatedAt")),
                FileCount = reader.GetInt32("FileCount"),
                TotalSizeBytes = reader.GetInt64("TotalSizeBytes"),
                Checksum = reader.GetString("Checksum"),
                StoragePath = reader.GetString("StoragePath"),
                IsValid = reader.GetBoolean("IsValid")
            };
        }, parameters);

        return results.FirstOrDefault();
    }

    private async Task<RecoveryPointFile?> FindFileInRecoveryPointAsync(string recoveryPointId, string filePath)
    {
        var sql = @"
            SELECT * FROM RecoveryPointFile
            WHERE RecoveryPointId = @RecoveryPointId AND OriginalPath = @OriginalPath";

        var parameters = new[]
        {
            new SqliteParameter("@RecoveryPointId", recoveryPointId),
            new SqliteParameter("@OriginalPath", filePath)
        };

        var results = _database.ExecuteQuery(sql, reader =>
        {
            return new RecoveryPointFile
            {
                FileId = reader.GetString("FileId"),
                RecoveryPointId = reader.GetString("RecoveryPointId"),
                OriginalPath = reader.GetString("OriginalPath"),
                BackupPath = reader.GetString("BackupPath"),
                Size = reader.GetInt64("Size"),
                OriginalModifiedTime = DateTime.Parse(reader.GetString("OriginalModifiedTime")),
                Hash = reader.GetString("Hash"),
                IsEncrypted = reader.GetBoolean("IsEncrypted")
            };
        }, parameters);

        return results.FirstOrDefault();
    }

    private async Task<List<RecoveryPointFile>> GetAllFilesInRecoveryPointAsync(string recoveryPointId)
    {
        var sql = "SELECT * FROM RecoveryPointFile WHERE RecoveryPointId = @RecoveryPointId";
        var parameters = new[] { new SqliteParameter("@RecoveryPointId", recoveryPointId) };

        return _database.ExecuteQuery(sql, reader =>
        {
            return new RecoveryPointFile
            {
                FileId = reader.GetString("FileId"),
                RecoveryPointId = reader.GetString("RecoveryPointId"),
                OriginalPath = reader.GetString("OriginalPath"),
                BackupPath = reader.GetString("BackupPath"),
                Size = reader.GetInt64("Size"),
                OriginalModifiedTime = DateTime.Parse(reader.GetString("OriginalModifiedTime")),
                Hash = reader.GetString("Hash"),
                IsEncrypted = reader.GetBoolean("IsEncrypted")
            };
        }, parameters);
    }

    private async Task RestoreSingleFileAsync(RecoveryPointFile file)
    {
        if (!File.Exists(file.BackupPath))
        {
            throw new RecoveryException($"Backup file not found: {file.BackupPath}");
        }

        var backupHash = await CryptoHelper.ComputeFileSha256Async(file.BackupPath);
        if (!string.Equals(backupHash, file.Hash, StringComparison.OrdinalIgnoreCase))
        {
            throw new RecoveryException($"Backup hash mismatch for {file.OriginalPath}");
        }

        // Ensure target directory exists
        var targetDir = Path.GetDirectoryName(file.OriginalPath);
        if (!string.IsNullOrEmpty(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        var tempPath = Path.Combine(
            targetDir ?? Path.GetTempPath(),
            $".echoeye-restore-{Guid.NewGuid():N}.tmp");

        File.Copy(file.BackupPath, tempPath, true);
        File.Move(tempPath, file.OriginalPath, overwrite: true);

        // Restore original modification time
        File.SetLastWriteTimeUtc(file.OriginalPath, file.OriginalModifiedTime);
    }

    private async Task SaveRestorationOperationAsync(RestorationResult result, string recoveryPointId)
    {
        var sql = @"
            INSERT INTO RecoveryOperation (
                OperationId, RecoveryPointId, Type, StartedAt, CompletedAt, Status,
                FilesAttempted, FilesSucceeded, FilesFailed, BytesRestored, Summary
            ) VALUES (
                @OperationId, @RecoveryPointId, @Type, @StartedAt, @CompletedAt, @Status,
                @FilesAttempted, @FilesSucceeded, @FilesFailed, @BytesRestored, @Summary
            )";

        var parameters = new[]
        {
            new SqliteParameter("@OperationId", result.OperationId),
            new SqliteParameter("@RecoveryPointId", recoveryPointId),
            new SqliteParameter("@Type", (int)RecoveryOperationType.SelectiveRestore),
            new SqliteParameter("@StartedAt", DateTime.UtcNow.ToString("o")),
            new SqliteParameter("@CompletedAt", DateTime.UtcNow.ToString("o")),
            new SqliteParameter("@Status", (int)result.Status),
            new SqliteParameter("@FilesAttempted", result.FilesAttempted),
            new SqliteParameter("@FilesSucceeded", result.FilesSucceeded),
            new SqliteParameter("@FilesFailed", result.FilesFailed),
            new SqliteParameter("@BytesRestored", result.BytesRestored),
            new SqliteParameter("@Summary", result.Summary ?? (object)DBNull.Value)
        };

        _database.ExecuteNonQuery(sql, parameters);
    }
}

public class RestorationProgress
{
    public string OperationId { get; set; } = string.Empty;
    public RecoveryStatus Status { get; set; }
    public int TotalFiles { get; set; }
    public int CompletedFiles { get; set; }
    public double ProgressPercentage { get; set; }
    public TimeSpan ElapsedTime { get; set; }
    public TimeSpan? EstimatedTimeRemaining { get; set; }
}

public class RestorationResult
{
    public string OperationId { get; set; } = string.Empty;
    public RecoveryStatus Status { get; set; }
    public int FilesAttempted { get; set; }
    public int FilesSucceeded { get; set; }
    public int FilesFailed { get; set; }
    public long BytesRestored { get; set; }
    public List<string> FailedFiles { get; set; } = new();
    public string? ErrorDetails { get; set; }
    public string? Summary { get; set; }
}
