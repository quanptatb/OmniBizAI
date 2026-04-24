using OmniBizAI.Domain.Enums;
using OmniBizAI.Domain.Rules;

namespace OmniBizAI.UnitTests.Domain;

public sealed class PerformanceRulesTests
{
    [Fact]
    public void CalculateProgress_ReturnsZeroWhenTargetEqualsStart()
    {
        var progress = PerformanceRules.CalculateProgress(50, 50, 70);

        Assert.Equal(0, progress);
    }

    [Fact]
    public void CalculateProgress_SupportsDecreaseDirection()
    {
        var progress = PerformanceRules.CalculateProgress(100, 50, 60, ProgressDirection.Decrease);

        Assert.Equal(80, progress);
    }

    [Theory]
    [InlineData(92, "A")]
    [InlineData(89, "B")]
    [InlineData(60, "C")]
    [InlineData(40, "D")]
    [InlineData(20, "E")]
    public void Rating_ReturnsExpectedBand(decimal score, string expected)
    {
        Assert.Equal(expected, PerformanceRules.Rating(score));
    }
}
