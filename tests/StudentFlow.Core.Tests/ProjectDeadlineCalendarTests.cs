using System.Globalization;
using StudentFlow.Core.Projects;

namespace StudentFlow.Core.Tests;

/// <summary>
/// Ověřuje, že hranice dne projektu odpovídá času v Praze v zimě i v létě.
/// </summary>
public class ProjectDeadlineCalendarTests
{
    [Theory]
    [InlineData("2026-01-15T22:59:00+00:00", "2026-01-15")]
    [InlineData("2026-01-15T23:01:00+00:00", "2026-01-16")]
    [InlineData("2026-07-15T21:59:00+00:00", "2026-07-15")]
    [InlineData("2026-07-15T22:01:00+00:00", "2026-07-16")]
    public void Today_UsesPragueCalendarDay(string isoInstant, string expectedDate)
    {
        var instant = DateTimeOffset.Parse(isoInstant, CultureInfo.InvariantCulture);
        var expected = DateOnly.ParseExact(expectedDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);

        Assert.Equal(expected, ProjectDeadlineCalendar.Today(instant));
    }
}
