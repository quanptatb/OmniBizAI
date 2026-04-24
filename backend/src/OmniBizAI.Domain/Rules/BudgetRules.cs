namespace OmniBizAI.Domain.Rules;

public static class BudgetRules
{
    public static decimal Remaining(decimal allocated, decimal spent, decimal committed = 0)
    {
        return allocated - spent - committed;
    }

    public static decimal UtilizationPercent(decimal allocated, decimal spent)
    {
        if (allocated <= 0)
        {
            return 0;
        }

        return Math.Round(spent / allocated * 100, 2, MidpointRounding.AwayFromZero);
    }

    public static string WarningLevel(decimal allocated, decimal spent, decimal warningThreshold = 80)
    {
        var utilization = UtilizationPercent(allocated, spent);
        if (utilization >= 100)
        {
            return "Red";
        }

        return utilization >= warningThreshold ? "Yellow" : "Green";
    }
}
