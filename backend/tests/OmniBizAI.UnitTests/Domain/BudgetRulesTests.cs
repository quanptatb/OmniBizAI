using OmniBizAI.Domain.Rules;

namespace OmniBizAI.UnitTests.Domain;

public sealed class BudgetRulesTests
{
    [Fact]
    public void Remaining_SubtractsSpentAndCommitted()
    {
        var remaining = BudgetRules.Remaining(100_000_000, 60_000_000, 10_000_000);

        Assert.Equal(30_000_000, remaining);
    }

    [Theory]
    [InlineData(100, 85, "Yellow")]
    [InlineData(100, 100, "Red")]
    [InlineData(100, 30, "Green")]
    public void WarningLevel_ReturnsExpectedThreshold(decimal allocated, decimal spent, string expected)
    {
        Assert.Equal(expected, BudgetRules.WarningLevel(allocated, spent));
    }
}
