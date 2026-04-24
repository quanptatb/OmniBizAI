using OmniBizAI.Domain.Enums;

namespace OmniBizAI.Domain.Rules;

public static class PerformanceRules
{
    public static decimal CalculateProgress(decimal startValue, decimal targetValue, decimal currentValue, ProgressDirection direction = ProgressDirection.Increase)
    {
        if (targetValue == startValue)
        {
            return 0;
        }

        var progress = direction == ProgressDirection.Decrease
            ? (startValue - currentValue) / (startValue - targetValue) * 100
            : (currentValue - startValue) / (targetValue - startValue) * 100;

        return Math.Clamp(Math.Round(progress, 2, MidpointRounding.AwayFromZero), 0, 100);
    }

    public static decimal WeightedAverage(IEnumerable<(decimal Score, decimal Weight)> scores)
    {
        var materialized = scores.ToList();
        var totalWeight = materialized.Sum(x => x.Weight);
        if (totalWeight <= 0)
        {
            return 0;
        }

        return Math.Round(materialized.Sum(x => x.Score * x.Weight) / totalWeight, 2, MidpointRounding.AwayFromZero);
    }

    public static string Rating(decimal score)
    {
        return score switch
        {
            >= 90 => "A",
            >= 70 => "B",
            >= 50 => "C",
            >= 30 => "D",
            _ => "E"
        };
    }
}
