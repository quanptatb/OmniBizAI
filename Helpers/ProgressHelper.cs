namespace OmniBizAI.Helpers;

/// <summary>Calculates progress percentages for KPI and OKR entities.</summary>
public static class ProgressHelper
{
    /// <summary>Calculate progress % = actual / target * 100, clamped to 0-100.</summary>
    public static decimal CalculatePercent(decimal actual, decimal target, bool isInverse = false)
    {
        if (target == 0) return 0;
        decimal raw = isInverse
            ? (target - actual) / target * 100
            : actual / target * 100;
        return Math.Clamp(Math.Round(raw, 2), 0, 999);
    }

    /// <summary>Determine expected value at the current date given start, end, and target.</summary>
    public static decimal ExpectedValueAtDate(DateOnly start, DateOnly end, DateOnly current, decimal target)
    {
        if (current >= end) return target;
        if (current <= start) return 0;

        var totalDays = (double)(end.DayNumber - start.DayNumber);
        var elapsedDays = (double)(current.DayNumber - start.DayNumber);

        if (totalDays <= 0) return target;
        return Math.Round(target * (decimal)(elapsedDays / totalDays), 2);
    }

    /// <summary>Returns CSS class based on progress percentage.</summary>
    public static string GetProgressColorClass(decimal percent) => percent switch
    {
        >= 100 => "bg-success",
        >= 80 => "bg-info",
        >= 50 => "bg-warning",
        _ => "bg-danger"
    };

    /// <summary>Schedule progress: how much of the time period has elapsed (0-100%).</summary>
    public static decimal ScheduleProgress(DateOnly start, DateOnly end)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        if (today >= end) return 100;
        if (today <= start) return 0;

        var totalDays = (double)(end.DayNumber - start.DayNumber);
        var elapsedDays = (double)(today.DayNumber - start.DayNumber);
        return totalDays > 0 ? Math.Round((decimal)(elapsedDays / totalDays * 100), 1) : 0;
    }
}
