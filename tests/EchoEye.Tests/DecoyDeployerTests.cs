using EchoEye.Core.Models;
using EchoEye.Decoy.Deployment;
using Microsoft.Extensions.Logging.Abstractions;

namespace EchoEye.Tests;

public class DecoyDeployerTests
{
    [Fact]
    public async Task DeployAsync_WritesDecoyAndUpdatesHashAndSize()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"echoeye-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        try
        {
            var deployer = new DecoyDeployer(NullLogger.Instance);
            var decoy = new DecoyAsset
            {
                DecoyId = Guid.NewGuid().ToString(),
                FilePath = "finance-report.txt",
                Type = DecoyType.Document
            };

            var deployed = await deployer.DeployAsync(decoy, directory);

            Assert.True(File.Exists(deployed.FilePath));
            Assert.True(deployed.FileSize > 0);
            Assert.False(string.IsNullOrWhiteSpace(deployed.ContentHash));
            Assert.True(deployed.IsActive);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
