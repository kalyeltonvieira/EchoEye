using EchoEye.Core.Models;
using EchoEye.Core.Utilities;
using Microsoft.Extensions.Logging;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace EchoEye.Decoy.Generation;

public interface IDecoyGenerator
{
    Task<DecoyAsset> GenerateAsync(DecoyType type, string targetPath, DecoyOptions? options = null);
    Task<List<DecoyAsset>> GenerateBatchAsync(DecoyType type, string targetPath, int count, DecoyOptions? options = null);
    Task<List<DecoyTemplate>> GetTemplatesAsync(DecoyType type);
}

/// <summary>
/// Generates realistic decoy assets
/// </summary>
public class DecoyGenerator : IDecoyGenerator
{
    private readonly ILogger _logger;
    private readonly Random _random;

    public DecoyGenerator(ILogger logger)
    {
        _logger = logger;
        _random = new Random();
    }

    public async Task<DecoyAsset> GenerateAsync(DecoyType type, string targetPath, DecoyOptions? options = null)
    {
        options ??= new DecoyOptions();

        var fileName = options.FileName ?? GenerateFileName(type);
        var fullPath = Path.Combine(targetPath, fileName);
        var fileSize = options.FileSize > 0 ? options.FileSize : GenerateFileSize(type);

        byte[] content;
        string contentHash;

        switch (type)
        {
            case DecoyType.Document:
                content = await GenerateDocumentContentAsync(fullPath, options);
                break;
            case DecoyType.Image:
                content = await GenerateImageContentAsync(fileSize);
                break;
            case DecoyType.Database:
                content = await GenerateDatabaseContentAsync(fileSize);
                break;
            case DecoyType.Archive:
                content = await GenerateArchiveContentAsync(fileSize);
                break;
            case DecoyType.Configuration:
                content = await GenerateConfigurationContentAsync();
                break;
            default:
                throw new ArgumentException($"Unsupported decoy type: {type}");
        }

        contentHash = CryptoHelper.ComputeSha256Hash(content);

        var decoy = new DecoyAsset
        {
            DecoyId = CryptoHelper.GenerateUuid(),
            FilePath = fullPath,
            Type = type,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            ContentHash = contentHash,
            FileSize = content.Length,
            Metadata = JsonHelper.Serialize(new
            {
                GeneratedBy = "EchoEye",
                Options = options
            })
        };

        _logger.LogInformation("Generated decoy: {Type} at {Path}", type, fullPath);
        return decoy;
    }

    public async Task<List<DecoyAsset>> GenerateBatchAsync(DecoyType type, string targetPath, int count, DecoyOptions? options = null)
    {
        var decoys = new List<DecoyAsset>();

        for (int i = 0; i < count; i++)
        {
            var decoy = await GenerateAsync(type, targetPath, options);
            decoys.Add(decoy);
        }

        return decoys;
    }

    public Task<List<DecoyTemplate>> GetTemplatesAsync(DecoyType type)
    {
        var templates = new List<DecoyTemplate>();

        switch (type)
        {
            case DecoyType.Document:
                templates.Add(new DecoyTemplate
                {
                    TemplateId = "doc-financial",
                    Name = "Financial Document",
                    Type = DecoyType.Document,
                    Description = "Realistic financial statement decoy"
                });
                templates.Add(new DecoyTemplate
                {
                    TemplateId = "doc-contract",
                    Name = "Contract Document",
                    Type = DecoyType.Document,
                    Description = "Legal contract decoy"
                });
                break;

            case DecoyType.Image:
                templates.Add(new DecoyTemplate
                {
                    TemplateId = "img-photo",
                    Name = "Personal Photo",
                    Type = DecoyType.Image,
                    Description = "Personal photograph decoy"
                });
                break;

            case DecoyType.Database:
                templates.Add(new DecoyTemplate
                {
                    TemplateId = "db-customer",
                    Name = "Customer Database",
                    Type = DecoyType.Database,
                    Description = "Customer records database decoy"
                });
                break;
        }

        return Task.FromResult(templates);
    }

    private string GenerateFileName(DecoyType type)
    {
        var prefixes = new Dictionary<DecoyType, string[]>
        {
            [DecoyType.Document] = new[] { "document", "report", "statement", "contract", "proposal" },
            [DecoyType.Image] = new[] { "photo", "image", "picture", "screenshot" },
            [DecoyType.Database] = new[] { "database", "data", "records", "backup" },
            [DecoyType.Archive] = new[] { "archive", "backup", "files", "data" },
            [DecoyType.Configuration] = new[] { "config", "settings", "preferences" }
        };

        var extensions = new Dictionary<DecoyType, string[]>
        {
            [DecoyType.Document] = new[] { ".docx", ".xlsx", ".pdf" },
            [DecoyType.Image] = new[] { ".jpg", ".png" },
            [DecoyType.Database] = new[] { ".db", ".sqlite" },
            [DecoyType.Archive] = new[] { ".zip", ".rar" },
            [DecoyType.Configuration] = new[] { ".json", ".xml", ".ini" }
        };

        var prefix = prefixes[type][_random.Next(prefixes[type].Length)];
        var extension = extensions[type][_random.Next(extensions[type].Length)];
        var number = _random.Next(1000, 9999);

        return $"{prefix}_{number}{extension}";
    }

    private long GenerateFileSize(DecoyType type)
    {
        return type switch
        {
            DecoyType.Document => _random.Next(50 * 1024, 500 * 1024), // 50-500 KB
            DecoyType.Image => _random.Next(100 * 1024, 2 * 1024 * 1024), // 100 KB - 2 MB
            DecoyType.Database => _random.Next(1 * 1024 * 1024, 10 * 1024 * 1024), // 1-10 MB
            DecoyType.Archive => _random.Next(500 * 1024, 5 * 1024 * 1024), // 500 KB - 5 MB
            DecoyType.Configuration => _random.Next(1 * 1024, 50 * 1024), // 1-50 KB
            _ => 100 * 1024
        };
    }

    private async Task<byte[]> GenerateDocumentContentAsync(string filePath, DecoyOptions options)
    {
        try
        {
            // Create a simple Word document
            using var wordDocument = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document);
            var mainPart = wordDocument.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());

            // Add realistic content
            var paragraph = body.AppendChild(new Paragraph());
            var run = paragraph.AppendChild(new Run());
            run.AppendChild(new Text("This is a decoy document created by EchoEye X for ransomware detection purposes."));

            wordDocument.Close();
            return await File.ReadAllBytesAsync(filePath);
        }
        catch
        {
            // Fallback to simple text file
            var content = "This is a decoy document created by EchoEye X for ransomware detection purposes.";
            return System.Text.Encoding.UTF8.GetBytes(content);
        }
    }

    private async Task<byte[]> GenerateImageContentAsync(long fileSize)
    {
        // Generate a simple bitmap-like image
        var width = 800;
        var height = 600;
        var headerSize = 54; // BMP header
        var dataSize = width * height * 3; // RGB
        var totalSize = headerSize + dataSize;

        var data = new byte[Math.Min(totalSize, fileSize)];

        // Simple BMP header
        data[0] = 0x42; // 'B'
        data[1] = 0x4D; // 'M'
        // Write file size
        BitConverter.GetBytes((int)data.Length).CopyTo(data, 2);
        // Offset to pixel data
        BitConverter.GetBytes(headerSize).CopyTo(data, 10);
        // Header size
        BitConverter.GetBytes(40).CopyTo(data, 14);
        // Width and height
        BitConverter.GetBytes(width).CopyTo(data, 18);
        BitConverter.GetBytes(height).CopyTo(data, 22);
        // Planes (1)
        BitConverter.GetBytes((short)1).CopyTo(data, 26);
        // Bits per pixel (24)
        BitConverter.GetBytes((short)24).CopyTo(data, 28);

        // Fill with random pixel data
        for (int i = headerSize; i < data.Length; i++)
        {
            data[i] = (byte)_random.Next(256);
        }

        return data;
    }

    private async Task<byte[]> GenerateDatabaseContentAsync(long fileSize)
    {
        // Generate a simple SQLite database structure
        var sql = @"
            CREATE TABLE decoy_data (
                id INTEGER PRIMARY KEY,
                name TEXT,
                value TEXT,
                created_at TEXT
            );
            INSERT INTO decoy_data (name, value, created_at) VALUES
                ('record1', 'decoy_value_1', datetime('now')),
                ('record2', 'decoy_value_2', datetime('now')),
                ('record3', 'decoy_value_3', datetime('now'));
        ";

        return System.Text.Encoding.UTF8.GetBytes(sql);
    }

    private async Task<byte[]> GenerateArchiveContentAsync(long fileSize)
    {
        // Generate a simple ZIP-like structure
        var content = "This is a decoy archive file created by EchoEye X.";
        return System.Text.Encoding.UTF8.GetBytes(content);
    }

    private async Task<byte[]> GenerateConfigurationContentAsync()
    {
        var config = new
        {
            settings = new
            {
                enabled = true,
                version = "1.0.0",
                decoy = true
            },
            metadata = new
            {
                created = DateTime.UtcNow,
                author = "EchoEye X"
            }
        };

        return System.Text.Encoding.UTF8.GetBytes(JsonHelper.Serialize(config));
    }
}

public class DecoyOptions
{
    public string? FileName { get; set; }
    public long FileSize { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
    public bool IncludeRealisticContent { get; set; } = true;
}

public class DecoyTemplate
{
    public string TemplateId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DecoyType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object>? Parameters { get; set; }
}
