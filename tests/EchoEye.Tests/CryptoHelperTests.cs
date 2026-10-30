using EchoEye.Core.Utilities;

namespace EchoEye.Tests;

public class CryptoHelperTests
{
    [Fact]
    public void CalculateEntropy_ReturnsZeroForRepeatedBytes()
    {
        var entropy = CryptoHelper.CalculateEntropy(new byte[128]);

        Assert.Equal(0, entropy);
    }

    [Fact]
    public async Task CalculateFileEntropyAsync_SamplesFileWithoutChangingResultShape()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.bin");
        await File.WriteAllBytesAsync(path, Enumerable.Repeat((byte)7, 4096).ToArray());

        try
        {
            var entropy = await CryptoHelper.CalculateFileEntropyAsync(path, maxBytes: 1024);

            Assert.Equal(0, entropy);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
