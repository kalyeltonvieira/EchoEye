namespace EchoEye.Core.Utilities;

/// <summary>
/// Path and file system utility functions
/// </summary>
public static class PathHelper
{
    /// <summary>
    /// Normalize a file path
    /// </summary>
    public static string NormalizePath(string path)
    {
        return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar);
    }

    /// <summary>
    /// Check if a path is within another path
    /// </summary>
    public static bool IsPathWithin(string path, string parentPath)
    {
        var normalizedPath = NormalizePath(path);
        var normalizedParent = NormalizePath(parentPath);
        return normalizedPath.StartsWith(normalizedParent, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Get relative path from parent to child
    /// </summary>
    public static string GetRelativePath(string parentPath, string childPath)
    {
        return Path.GetRelativePath(parentPath, childPath);
    }

    /// <summary>
    /// Ensure directory exists
    /// </summary>
    public static void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    /// <summary>
    /// Safe delete with retry
    /// </summary>
    public static void SafeDelete(string path, int maxRetries = 3, int delayMs = 100)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                else if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
                return;
            }
            catch (IOException) when (i < maxRetries - 1)
            {
                Thread.Sleep(delayMs);
            }
        }
    }

    /// <summary>
    /// Get file extension without dot
    /// </summary>
    public static string GetExtensionWithoutDot(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        return extension.StartsWith('.') ? extension.Substring(1) : extension;
    }

    /// <summary>
    /// Check if a file is likely encrypted based on extension
    /// </summary>
    public static bool IsLikelyEncryptedFile(string filePath)
    {
        var encryptedExtensions = new[] { "encrypted", "locked", "crypto", "enc" };
        var extension = GetExtensionWithoutDot(filePath).ToLowerInvariant();
        return encryptedExtensions.Contains(extension);
    }
}
