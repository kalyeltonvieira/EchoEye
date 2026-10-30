using System.Security.Cryptography;
using System.Text;

namespace EchoEye.Core.Utilities;

/// <summary>
/// Cryptographic utility functions
/// </summary>
public static class CryptoHelper
{
    /// <summary>
    /// Compute SHA-256 hash of data
    /// </summary>
    public static string ComputeSha256Hash(byte[] data)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(data);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Compute SHA-256 hash of file
    /// </summary>
    public static async Task<string> ComputeFileSha256Async(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var bytes = await sha256.ComputeHashAsync(stream);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Compute SHA-256 hash of string
    /// </summary>
    public static string ComputeStringHash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        return ComputeSha256Hash(bytes);
    }

    /// <summary>
    /// Generate a random UUID
    /// </summary>
    public static string GenerateUuid()
    {
        return Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Generate cryptographically secure random bytes
    /// </summary>
    public static byte[] GenerateRandomBytes(int length)
    {
        var bytes = new byte[length];
        RandomNumberGenerator.Fill(bytes);
        return bytes;
    }

    /// <summary>
    /// Calculate entropy of data (0-8 scale)
    /// </summary>
    public static double CalculateEntropy(byte[] data)
    {
        if (data.Length == 0) return 0;

        var frequency = new int[256];
        foreach (var b in data)
        {
            frequency[b]++;
        }

        double entropy = 0;
        foreach (var count in frequency)
        {
            if (count == 0) continue;
            var probability = (double)count / data.Length;
            entropy -= probability * Math.Log2(probability);
        }

        return entropy;
    }

    /// <summary>
    /// Calculate entropy of file
    /// </summary>
    public static async Task<double> CalculateFileEntropyAsync(string filePath, int maxBytes = 1024 * 1024)
    {
        var bufferSize = Math.Max(1, maxBytes);
        var data = new byte[bufferSize];
        int bytesRead;

        await using (var stream = File.OpenRead(filePath))
        {
            bytesRead = await stream.ReadAsync(data.AsMemory(0, bufferSize));
        }

        if (bytesRead != data.Length)
        {
            Array.Resize(ref data, bytesRead);
        }

        return CalculateEntropy(data);
    }
}
