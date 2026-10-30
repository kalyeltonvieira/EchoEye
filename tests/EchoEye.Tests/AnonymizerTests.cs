using EchoEye.Intelligence;
using Microsoft.Extensions.Logging.Abstractions;

namespace EchoEye.Tests;

public class AnonymizerTests
{
    [Fact]
    public void AnonymizeProcess_ReturnsStableHashPrefix()
    {
        var anonymizer = new Anonymizer(NullLogger.Instance);

        var first = anonymizer.AnonymizeProcess("suspicious.exe");
        var second = anonymizer.AnonymizeProcess("suspicious.exe");

        Assert.Equal(first, second);
        Assert.Equal(12, first.Length);
        Assert.DoesNotContain("suspicious", first, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HashIdentifier_ReturnsStableSixteenCharacterValue()
    {
        var anonymizer = new Anonymizer(NullLogger.Instance);

        var hash = anonymizer.HashIdentifier("device-123");

        Assert.Equal(16, hash.Length);
        Assert.Equal(hash, anonymizer.HashIdentifier("device-123"));
    }
}
