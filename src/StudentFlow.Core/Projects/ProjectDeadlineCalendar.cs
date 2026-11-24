namespace StudentFlow.Core.Projects;

/// <summary>
/// Určuje dnešní datum pro termíny projektů v jednotném českém časovém pásmu.
/// </summary>
public static class ProjectDeadlineCalendar
{
    private static readonly TimeZoneInfo PragueTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Prague");

    /// <summary>
    /// Převede konkrétní okamžik na kalendářní datum v Praze včetně letního času.
    /// </summary>
    public static DateOnly Today(DateTimeOffset instant)
    {
        var pragueTime = TimeZoneInfo.ConvertTime(instant, PragueTimeZone);
        return DateOnly.FromDateTime(pragueTime.DateTime);
    }
}
