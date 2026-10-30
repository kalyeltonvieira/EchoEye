using EchoEye.Core.Constants;
using EchoEye.Core.Data;
using EchoEye.Core.Exceptions;
using EchoEye.Core.Models;
using EchoEye.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace EchoEye.Recovery;

public interface IRecoveryPointManager
{
    Task<RecoveryPoint> CreateAsync(RecoveryPointOptions options);
    Task<RecoveryPoint> CreateAutomaticAsync();
    Task<RecoveryPoint?> GetAsync(string recoveryPointId);
    Task<List<RecoveryPoint>> ListAsync(RecoveryPointFilter? filter = null);
    Task DeleteAsync(string recoveryPointId);
    Task<ValidationResult> ValidateAsync(string recoveryPointId);
    Task<RecoveryPointStatistics> GetStatisticsAsync();
}

/// <summary>
/// Manages recovery point creation and lifecycle
/// </summary>
public class RecoveryPointManager : IRecoveryPointManager
{
    private readonly Database _database;
    private readonly ILogger _logger;

    public RecoveryPointManager(Database database, ILogger logger)
    {
        _database = database;
        _logger = logger;
    }

    public async Task<RecoveryPoint> CreateAsync(RecoveryPointOptions options)
    {
        _logger.LogInformation("Creating recovery point: {Name}", options.Name ?? "Manual");

        var recoveryPoint = new RecoveryPoint
        {
            RecoveryPointId = CryptoHelper.GenerateUuid(),
            DeviceId = await GetDeviceIdAsync(),
            Name = options.Name ?? $"Manual-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
            Type = options.Type,
            CreatedAt = DateTime.UtcNow,
            IsValid = true,
            ValidatedAt = DateTime.UtcNow,
            IsAutomatic = options.IsAutomatic,
            RelatedIncidentId = options.RelatedIncidentId
        };

        // Create storage directory
        var storagePath = Path.Combine(Paths.RecoveryDirectory, recoveryPoint.RecoveryPointId);
        Directory.CreateDirectory(storagePath);
        recoveryPoint.StoragePath = storagePath;

        // Backup files
        var filesBacked = await BackupFilesAsync(recoveryPoint, options);
        recoveryPoint.FileCount = filesBacked.Count;
        recoveryPoint.TotalSizeBytes = filesBacked.Sum(f => f.Size);

        // Calculate checksum
        recoveryPoint.Checksum = await CalculateChecksumAsync(recoveryPoint);

        // Save to database
        await SaveRecoveryPointAsync(recoveryPoint);

        // Save file index
        await SaveFileIndexAsync(recoveryPoint, filesBacked);

        _logger.LogInformation("Recovery point created: {Id} with {Count} files ({Size} bytes)",
            recoveryPoint.RecoveryPointId, recoveryPoint.FileCount, recoveryPoint.TotalSizeBytes);

        return recoveryPoint;
    }

    public async Task<RecoveryPoint> CreateAutomaticAsync()
    {
        var options = new RecoveryPointOptions
        {
            Name = $"Automatic-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
            Type = RecoveryPointType.Incremental,
            IsAutomatic = true,
            IncludePaths = new List<string>
            {
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
            }
        };

        return await CreateAsync(options);
    }

    public async Task<RecoveryPoint?> GetAsync(string recoveryPointId)
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
                IsValid = reader.GetBoolean("IsValid"),
                ValidatedAt = reader.IsDBNull("ValidatedAt") ? null : DateTime.Parse(reader.GetString("ValidatedAt")),
                ValidationErrors = reader.IsDBNull("ValidationErrors") ? null : reader.GetString("ValidationErrors"),
                IsAutomatic = reader.GetBoolean("IsAutomatic"),
                RelatedIncidentId = reader.IsDBNull("RelatedIncidentId") ? null : reader.GetString("RelatedIncidentId"),
                ExpiresAt = reader.IsDBNull("ExpiresAt") ? null : DateTime.Parse(reader.GetString("ExpiresAt"))
            };
        }, parameters);

        return results.FirstOrDefault();
    }

    public async Task<List<RecoveryPoint>> ListAsync(RecoveryPointFilter? filter = null)
    {
        var sql = "SELECT * FROM RecoveryPoint WHERE 1=1";
        var parameters = new List<SqliteParameter>();

        if (filter != null)
        {
            if (filter.StartDate.HasValue)
            {
                sql += " AND CreatedAt >= @StartDate";
                parameters.Add(new SqliteParameter("@StartDate", filter.StartDate.Value.ToString("o")));
            }

            if (filter.EndDate.HasValue)
            {
                sql += " AND CreatedAt <= @EndDate";
                parameters.Add(new SqliteParameter("@EndDate", filter.EndDate.Value.ToString("o")));
            }

            if (filter.Type.HasValue)
            {
                sql += " AND Type = @Type";
                parameters.Add(new SqliteParameter("@Type", (int)filter.Type.Value));
            }

            if (filter.IsValid.HasValue)
            {
                sql += " AND IsValid = @IsValid";
                parameters.Add(new SqliteParameter("@IsValid", filter.IsValid.Value));
            }

            if (filter.IsAutomatic.HasValue)
            {
                sql += " AND IsAutomatic = @IsAutomatic";
                parameters.Add(new SqliteParameter("@IsAutomatic", filter.IsAutomatic.Value));
            }
        }

        sql += " ORDER BY CreatedAt DESC";

        return _database.ExecuteQuery(sql, reader =>
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
                IsValid = reader.GetBoolean("IsValid"),
                ValidatedAt = reader.IsDBNull("ValidatedAt") ? null : DateTime.Parse(reader.GetString("ValidatedAt")),
                ValidationErrors = reader.IsDBNull("ValidationErrors") ? null : reader.GetString("ValidationErrors"),
                IsAutomatic = reader.GetBoolean("IsAutomatic"),
                RelatedIncidentId = reader.IsDBNull("RelatedIncidentId") ? null : reader.GetString("RelatedIncidentId"),
                ExpiresAt = reader.IsDBNull("ExpiresAt") ? null : DateTime.Parse(reader.GetString("ExpiresAt"))
            };
        }, parameters.ToArray());
    }

    public async Task DeleteAsync(string recoveryPointId)
    {
        var recoveryPoint = await GetAsync(recoveryPointId);
        if (recoveryPoint == null)
        {
            throw new RecoveryException($"Recovery point not found: {recoveryPointId}");
        }

        // Delete files from storage
        if (Directory.Exists(recoveryPoint.StoragePath))
        {
            EnsureRecoveryPath(recoveryPoint.StoragePath);
            Directory.Delete(recoveryPoint.StoragePath, true);
        }

        // Delete from database
        var sql = "DELETE FROM RecoveryPoint WHERE RecoveryPointId = @RecoveryPointId";
        var parameters = new[] { new SqliteParameter("@RecoveryPointId", recoveryPointId) };
        _database.ExecuteNonQuery(sql, parameters);

        _logger.LogInformation("Recovery point deleted: {Id}", recoveryPointId);
    }

    public async Task<ValidationResult> ValidateAsync(string recoveryPointId)
    {
        var recoveryPoint = await GetAsync(recoveryPointId);
        if (recoveryPoint == null)
        {
            throw new RecoveryException($"Recovery point not found: {recoveryPointId}");
        }

        var result = new ValidationResult { IsValid = true, Errors = new List<string>(), Warnings = new List<string>() };

        try
        {
            // Check storage directory exists
            if (!Directory.Exists(recoveryPoint.StoragePath))
            {
                result.IsValid = false;
                result.Errors.Add("Storage directory not found");
                return result;
            }

            // Verify checksum
            var currentChecksum = await CalculateChecksumAsync(recoveryPoint);
            if (currentChecksum != recoveryPoint.Checksum)
            {
                result.IsValid = false;
                result.Errors.Add("Checksum mismatch - data may be corrupted");
            }

            // Validate file index
            var fileCount = await ValidateFileIndexAsync(recoveryPoint);
            if (fileCount != recoveryPoint.FileCount)
            {
                result.Warnings.Add($"File count mismatch: expected {recoveryPoint.FileCount}, found {fileCount}");
            }

            // Update validation status
            recoveryPoint.IsValid = result.IsValid;
            recoveryPoint.ValidatedAt = DateTime.UtcNow;
            recoveryPoint.ValidationErrors = result.Errors.Any() ? string.Join("; ", result.Errors) : null;

            await UpdateRecoveryPointAsync(recoveryPoint);

            _logger.LogInformation("Recovery point validated: {Id} - Valid: {IsValid}", recoveryPointId, result.IsValid);
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Errors.Add($"Validation error: {ex.Message}");
        }

        return result;
    }

    public async Task<RecoveryPointStatistics> GetStatisticsAsync()
    {
        var recoveryPoints = await ListAsync();

        var stats = new RecoveryPointStatistics
        {
            TotalRecoveryPoints = recoveryPoints.Count,
            TotalStorageUsed = recoveryPoints.Sum(rp => rp.TotalSizeBytes),
            ValidPoints = recoveryPoints.Count(rp => rp.IsValid),
            InvalidPoints = recoveryPoints.Count(rp => !rp.IsValid),
            OldestPoint = recoveryPoints.MinBy(rp => rp.CreatedAt)?.CreatedAt ?? DateTime.UtcNow,
            NewestPoint = recoveryPoints.MaxBy(rp => rp.CreatedAt)?.CreatedAt ?? DateTime.UtcNow
        };

        return stats;
    }

    private async Task<List<RecoveryPointFile>> BackupFilesAsync(RecoveryPoint recoveryPoint, RecoveryPointOptions options)
    {
        var files = new List<RecoveryPointFile>();
        var includePaths = options.IncludePaths ?? new List<string>
        {
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };
        var excludePaths = options.ExcludePaths ?? new List<string>();

        foreach (var path in includePaths)
        {
            if (!Directory.Exists(path))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                // Check if file should be excluded
                if (excludePaths.Any(exclude => PathHelper.IsPathWithin(file, exclude)))
                {
                    continue;
                }

                try
                {
                    var fileInfo = new FileInfo(file);
                    var backupPath = GetBackupPath(recoveryPoint.StoragePath, path, file);

                    // Copy file to backup location
                    Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
                    File.Copy(file, backupPath, true);

                    var hash = await CryptoHelper.ComputeFileSha256Async(file);

                    files.Add(new RecoveryPointFile
                    {
                        FileId = CryptoHelper.GenerateUuid(),
                        RecoveryPointId = recoveryPoint.RecoveryPointId,
                        OriginalPath = file,
                        BackupPath = backupPath,
                        Size = fileInfo.Length,
                        OriginalModifiedTime = fileInfo.LastWriteTimeUtc,
                        Hash = hash,
                        IsEncrypted = false
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to backup file: {File}", file);
                }
            }
        }

        return files;
    }

    private async Task<string> CalculateChecksumAsync(RecoveryPoint recoveryPoint)
    {
        // Calculate checksum of all files in the recovery point
        var hashes = new List<string>();

        foreach (var file in Directory.EnumerateFiles(recoveryPoint.StoragePath, "*", SearchOption.AllDirectories))
        {
            var hash = await CryptoHelper.ComputeFileSha256Async(file);
            hashes.Add(hash);
        }

        hashes.Sort();
        var combined = string.Join("|", hashes);
        return CryptoHelper.ComputeStringHash(combined);
    }

    private static string GetBackupPath(string storagePath, string rootPath, string filePath)
    {
        var relativePath = Path.GetRelativePath(rootPath, filePath);
        if (Path.IsPathRooted(relativePath) || relativePath.StartsWith("..", StringComparison.Ordinal))
        {
            throw new RecoveryException($"File path escapes protected backup root: {filePath}");
        }

        var safeRelativePath = relativePath
            .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Aggregate(storagePath, Path.Combine);

        var fullBackupPath = Path.GetFullPath(safeRelativePath);
        EnsureRecoveryPath(fullBackupPath);
        return fullBackupPath;
    }

    private static void EnsureRecoveryPath(string path)
    {
        var recoveryRoot = Path.GetFullPath(Paths.RecoveryDirectory);
        var candidate = Path.GetFullPath(path);

        if (!candidate.StartsWith(recoveryRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new RecoveryException($"Unsafe recovery path: {path}");
        }
    }

    private async Task<int> ValidateFileIndexAsync(RecoveryPoint recoveryPoint)
    {
        var sql = "SELECT COUNT(*) FROM RecoveryPointFile WHERE RecoveryPointId = @RecoveryPointId";
        var parameters = new[] { new SqliteParameter("@RecoveryPointId", recoveryPoint.RecoveryPointId) };
        var result = _database.ExecuteScalar(sql, parameters);
        return Convert.ToInt32(result);
    }

    private async Task SaveRecoveryPointAsync(RecoveryPoint recoveryPoint)
    {
        var sql = @"
            INSERT INTO RecoveryPoint (
                RecoveryPointId, DeviceId, Name, Type, CreatedAt, FileCount, TotalSizeBytes,
                Checksum, StoragePath, IsValid, ValidatedAt, IsAutomatic, RelatedIncidentId
            ) VALUES (
                @RecoveryPointId, @DeviceId, @Name, @Type, @CreatedAt, @FileCount, @TotalSizeBytes,
                @Checksum, @StoragePath, @IsValid, @ValidatedAt, @IsAutomatic, @RelatedIncidentId
            )";

        var parameters = new[]
        {
            new SqliteParameter("@RecoveryPointId", recoveryPoint.RecoveryPointId),
            new SqliteParameter("@DeviceId", recoveryPoint.DeviceId),
            new SqliteParameter("@Name", recoveryPoint.Name),
            new SqliteParameter("@Type", (int)recoveryPoint.Type),
            new SqliteParameter("@CreatedAt", recoveryPoint.CreatedAt.ToString("o")),
            new SqliteParameter("@FileCount", recoveryPoint.FileCount),
            new SqliteParameter("@TotalSizeBytes", recoveryPoint.TotalSizeBytes),
            new SqliteParameter("@Checksum", recoveryPoint.Checksum),
            new SqliteParameter("@StoragePath", recoveryPoint.StoragePath),
            new SqliteParameter("@IsValid", recoveryPoint.IsValid),
            new SqliteParameter("@ValidatedAt", recoveryPoint.ValidatedAt?.ToString("o") ?? (object)DBNull.Value),
            new SqliteParameter("@IsAutomatic", recoveryPoint.IsAutomatic),
            new SqliteParameter("@RelatedIncidentId", recoveryPoint.RelatedIncidentId ?? (object)DBNull.Value)
        };

        _database.ExecuteNonQuery(sql, parameters);
    }

    private async Task SaveFileIndexAsync(RecoveryPoint recoveryPoint, List<RecoveryPointFile> files)
    {
        foreach (var file in files)
        {
            var sql = @"
                INSERT INTO RecoveryPointFile (
                    FileId, RecoveryPointId, OriginalPath, BackupPath, Size,
                    OriginalModifiedTime, Hash, IsEncrypted
                ) VALUES (
                    @FileId, @RecoveryPointId, @OriginalPath, @BackupPath, @Size,
                    @OriginalModifiedTime, @Hash, @IsEncrypted
                )";

            var parameters = new[]
            {
                new SqliteParameter("@FileId", file.FileId),
                new SqliteParameter("@RecoveryPointId", file.RecoveryPointId),
                new SqliteParameter("@OriginalPath", file.OriginalPath),
                new SqliteParameter("@BackupPath", file.BackupPath),
                new SqliteParameter("@Size", file.Size),
                new SqliteParameter("@OriginalModifiedTime", file.OriginalModifiedTime.ToString("o")),
                new SqliteParameter("@Hash", file.Hash),
                new SqliteParameter("@IsEncrypted", file.IsEncrypted)
            };

            _database.ExecuteNonQuery(sql, parameters);
        }
    }

    private async Task UpdateRecoveryPointAsync(RecoveryPoint recoveryPoint)
    {
        var sql = @"
            UPDATE RecoveryPoint SET
                IsValid = @IsValid,
                ValidatedAt = @ValidatedAt,
                ValidationErrors = @ValidationErrors
            WHERE RecoveryPointId = @RecoveryPointId";

        var parameters = new[]
        {
            new SqliteParameter("@IsValid", recoveryPoint.IsValid),
            new SqliteParameter("@ValidatedAt", recoveryPoint.ValidatedAt?.ToString("o") ?? (object)DBNull.Value),
            new SqliteParameter("@ValidationErrors", recoveryPoint.ValidationErrors ?? (object)DBNull.Value),
            new SqliteParameter("@RecoveryPointId", recoveryPoint.RecoveryPointId)
        };

        _database.ExecuteNonQuery(sql, parameters);
    }

    private async Task<string> GetDeviceIdAsync()
    {
        var sql = "SELECT DeviceId FROM DeviceIdentity LIMIT 1";
        var result = _database.ExecuteScalar(sql);
        return result?.ToString() ?? string.Empty;
    }
}

public class RecoveryPointOptions
{
    public string? Name { get; set; }
    public RecoveryPointType Type { get; set; }
    public List<string>? IncludePaths { get; set; }
    public List<string>? ExcludePaths { get; set; }
    public bool IsAutomatic { get; set; }
    public string? RelatedIncidentId { get; set; }
}

public class RecoveryPointFilter
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public RecoveryPointType? Type { get; set; }
    public bool? IsValid { get; set; }
    public bool? IsAutomatic { get; set; }
}

public class RecoveryPointStatistics
{
    public int TotalRecoveryPoints { get; set; }
    public long TotalStorageUsed { get; set; }
    public int ValidPoints { get; set; }
    public int InvalidPoints { get; set; }
    public DateTime OldestPoint { get; set; }
    public DateTime NewestPoint { get; set; }
}
