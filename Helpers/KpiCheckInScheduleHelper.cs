namespace OmniBizAI.Helpers;

/// <summary>Calculates KPI check-in deadlines and schedules based on frequency settings.</summary>
public static class KpiCheckInScheduleHelper
{
    /// <summary>Get next check-in deadline from the last check-in date.</summary>
    public static DateOnly GetNextDeadline(DateOnly lastCheckIn, int frequencyDays)
        => lastCheckIn.AddDays(frequencyDays);

    /// <summary>Get next check-in deadline with time component.</summary>
    public static DateTimeOffset GetNextDeadlineWithTime(DateOnly lastCheckIn, int frequencyDays, TimeOnly? deadlineTime, TimeZoneInfo? tz = null)
    {
        var nextDate = lastCheckIn.AddDays(frequencyDays);
        var time = deadlineTime ?? new TimeOnly(17, 0);
        var dateTime = nextDate.ToDateTime(time);
        tz ??= TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        return new DateTimeOffset(dateTime, tz.GetUtcOffset(dateTime));
    }

    /// <summary>Check if a check-in is overdue.</summary>
    public static bool IsOverdue(DateTimeOffset deadline) => DateTimeOffset.UtcNow > deadline;

    /// <summary>Check if a check-in deadline is approaching (within N hours).</summary>
    public static bool IsApproaching(DateTimeOffset deadline, int hoursBeforeDeadline = 24)
    {
        var diff = deadline - DateTimeOffset.UtcNow;
        return diff.TotalHours > 0 && diff.TotalHours <= hoursBeforeDeadline;
    }

    /// <summary>Get all scheduled check-in dates between start and end.</summary>
    public static List<DateOnly> GetScheduledDates(DateOnly start, DateOnly end, int frequencyDays)
    {
        var dates = new List<DateOnly>();
        if (frequencyDays <= 0) return dates;

        var current = start.AddDays(frequencyDays);
        while (current <= end)
        {
            dates.Add(current);
            current = current.AddDays(frequencyDays);
        }
        return dates;
    }

    /// <summary>Count missed check-ins: scheduled dates with no check-in recorded.</summary>
    public static int CountMissedCheckIns(DateOnly start, DateOnly today, int frequencyDays, int completedCheckIns)
    {
        var scheduled = GetScheduledDates(start, today, frequencyDays);
        return Math.Max(0, scheduled.Count - completedCheckIns);
    }
}
