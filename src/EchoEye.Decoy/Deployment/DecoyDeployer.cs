using EchoEye.Core.Constants;
using EchoEye.Core.Models;
using EchoEye.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace EchoEye.Decoy.Deployment;

public interface IDecoyDeployer
{
    Task<DecoyAsset> DeployAsync(DecoyAsset decoy, string targetPath);
    Task<List<DecoyAsset>> DeployToDefaultLocationsAsync(List<DecoyAsset> decoys);
    Task<DeploymentStrategy> GetStrategyAsync();
    Task UpdateStrategyAsync(DeploymentStrategy strategy);
}

/// <summary>
/// Deploys decoys to strategic locations
/// </summary>
public class DecoyDeployer : IDecoyDeployer
{
    private readonly ILogger _logger;
    private DeploymentStrategy _strategy;

    public DecoyDeployer(ILogger logger)
    {
        _logger = logger;
        _strategy = new DeploymentStrategy
        {
            TargetPaths = new List<string>
            {
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
            },
            DecoysPerPath = 10,
            RandomizeNames = true,
            RandomizeLocations = false,
            AllowedTypes = new List<DecoyType>
            {
                DecoyType.Document,
                DecoyType.Image,
                DecoyType.Database
            }
        };
    }

    public async Task<DecoyAsset> DeployAsync(DecoyAsset decoy, string targetPath)
    {
        try
        {
            // Ensure target directory exists
            PathHelper.EnsureDirectoryExists(targetPath);

            var fileName = Path.GetFileName(decoy.FilePath);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = $"{decoy.Type.ToString().ToLowerInvariant()}-{decoy.DecoyId}.decoy";
            }

            var fullPath = Path.Combine(targetPath, fileName);
            
            if (!File.Exists(fullPath))
            {
                var content = CreateDecoyContent(decoy);
                await File.WriteAllBytesAsync(fullPath, content);
            }

            decoy.FilePath = fullPath;
            decoy.FileSize = new FileInfo(fullPath).Length;
            decoy.ContentHash = await CryptoHelper.ComputeFileSha256Async(fullPath);
            decoy.IsActive = true;
            _logger.LogInformation("Deployed decoy to: {Path}", fullPath);

            return decoy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deploy decoy to {Path}", targetPath);
            throw;
        }
    }

    public async Task<List<DecoyAsset>> DeployToDefaultLocationsAsync(List<DecoyAsset> decoys)
    {
        var deployed = new List<DecoyAsset>();

        foreach (var path in _strategy.TargetPaths)
        {
            if (!Directory.Exists(path))
            {
                _logger.LogWarning("Target path does not exist: {Path}", path);
                continue;
            }

            var decoysForPath = decoys
                .Skip(deployed.Count)
                .Take(_strategy.DecoysPerPath)
                .ToList();

            foreach (var decoy in decoysForPath)
            {
                var deployedDecoy = await DeployAsync(decoy, path);
                deployed.Add(deployedDecoy);
            }
        }

        _logger.LogInformation("Deployed {Count} decoys to default locations", deployed.Count);
        return deployed;
    }

    public Task<DeploymentStrategy> GetStrategyAsync()
    {
        return Task.FromResult(_strategy);
    }

    public Task UpdateStrategyAsync(DeploymentStrategy strategy)
    {
        _strategy = strategy;
        _logger.LogInformation("Deployment strategy updated");
        return Task.CompletedTask;
    }

    private static byte[] CreateDecoyContent(DecoyAsset decoy)
    {
        var canary = decoy.DecoyId;
        var createdAt = DateTime.UtcNow.ToString("o");

        var content = decoy.Type switch
        {
            DecoyType.Database => $"""
                SQLite format 3
                -- EchoEye X decoy database marker
                -- decoy_id={canary}
                -- created_at={createdAt}
                """,
            DecoyType.Configuration => $$"""
                {
                  "name": "EchoEye monitored configuration",
                  "decoyId": "{{canary}}",
                  "createdAt": "{{createdAt}}",
                  "classification": "internal"
                }
                """,
            DecoyType.Archive => $"EchoEye X monitored archive decoy\r\ndecoy_id={canary}\r\ncreated_at={createdAt}\r\n",
            DecoyType.Image => $"EchoEye X monitored image placeholder\r\ndecoy_id={canary}\r\ncreated_at={createdAt}\r\n",
            _ => $"EchoEye X monitored document decoy\r\nDecoy ID: {canary}\r\nCreated: {createdAt}\r\nClassification: internal\r\n"
        };

        return System.Text.Encoding.UTF8.GetBytes(content);
    }
}

public class DeploymentStrategy
{
    public List<string> TargetPaths { get; set; } = new();
    public int DecoysPerPath { get; set; }
    public bool RandomizeNames { get; set; }
    public bool RandomizeLocations { get; set; }
    public List<DecoyType> AllowedTypes { get; set; } = new();
}
