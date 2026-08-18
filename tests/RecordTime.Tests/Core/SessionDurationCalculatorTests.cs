using RecordTime.Core.Services;
using Xunit;

namespace RecordTime.Tests.Core;

public class SessionDurationCalculatorTests
{
    private static readonly DateTime DayStart = new(2026, 8, 18, 0, 0, 0);
    private static readonly DateTime DayEnd = DayStart.AddDays(1);

    [Fact]
    public void GetDurationSeconds_ActiveSession_UsesNow()
    {
        var start = DayStart.AddHours(10);
        var now = DayStart.AddHours(12).AddMinutes(30);

        var seconds = SessionDurationCalculator.GetDurationSeconds(start, null, DayStart, DayEnd, now);

        Assert.Equal(9000, seconds);
    }

    [Fact]
    public void GetDurationSeconds_ActiveSession_ClampsToRangeEnd()
    {
        var previousDay = DayStart.AddDays(-1);
        var start = previousDay.AddHours(23);
        var now = DayStart.AddHours(12);

        var seconds = SessionDurationCalculator.GetDurationSeconds(
            start, null, previousDay, DayStart, now);

        Assert.Equal(3600, seconds);
    }

    [Fact]
    public void GetDurationSeconds_CompletedSession_ClampsAtDayBoundary()
    {
        var start = DayStart.AddHours(23);
        var end = DayEnd.AddHours(1);

        var yesterdaySeconds = SessionDurationCalculator.GetDurationSeconds(
            start, end, DayStart, DayEnd, DayEnd.AddHours(2));
        var todaySeconds = SessionDurationCalculator.GetDurationSeconds(
            start, end, DayEnd, DayEnd.AddDays(1), DayEnd.AddHours(2));

        Assert.Equal(3600, yesterdaySeconds);
        Assert.Equal(3600, todaySeconds);
    }

    [Fact]
    public void GetClampedEndTime_ActiveSession_UsesNow()
    {
        var now = DayStart.AddHours(12);

        var result = SessionDurationCalculator.GetClampedEndTime(null, DayEnd, now);

        Assert.Equal(now, result);
    }
}
