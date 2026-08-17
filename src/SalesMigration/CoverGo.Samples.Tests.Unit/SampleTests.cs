using Xunit;

namespace CoverGo.Samples.Tests.Unit;

/// <summary>
/// Placeholder proving the unit-test project builds and runs. Replace it with real tests
/// as each sample lands; Moq is already referenced for mocking the source and target
/// interfaces.
/// </summary>
public class SampleTests
{
    [Fact]
    public void Sample_test_runs()
    {
        // Arrange
        bool harnessWorks = true;

        // Act
        bool result = harnessWorks;

        // Assert
        Assert.True(result);
    }
}
