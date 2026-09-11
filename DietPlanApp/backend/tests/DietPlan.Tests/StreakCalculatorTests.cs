using DietPlan.Application;
using Xunit;

namespace DietPlan.Tests;

/// <summary>连续打卡计算器单测</summary>
public class StreakCalculatorTests
{
    [Fact]
    public void Empty_ReturnsZero()
    {
        var (total, streak) = StreakCalculator.Calculate([], new DateOnly(2026, 9, 11));
        Assert.Equal(0, total);
        Assert.Equal(0, streak);
    }

    [Fact]
    public void ConsecutiveDays_CountsStreak()
    {
        var today = new DateOnly(2026, 9, 11);
        var dates = new[] { today, today.AddDays(-1), today.AddDays(-2) };
        var (total, streak) = StreakCalculator.Calculate(dates, today);
        Assert.Equal(3, total);
        Assert.Equal(3, streak);
    }

    [Fact]
    public void Gap_BreaksStreak()
    {
        var today = new DateOnly(2026, 9, 11);
        var dates = new[] { today, today.AddDays(-1), today.AddDays(-3), today.AddDays(-4) };
        var (total, streak) = StreakCalculator.Calculate(dates, today);
        Assert.Equal(4, total);
        Assert.Equal(2, streak);
    }

    [Fact]
    public void NotCheckedToday_StillCountsHistoricalStreak()
    {
        var today = new DateOnly(2026, 9, 11);
        var dates = new[] { today.AddDays(-1), today.AddDays(-2) };
        var (total, streak) = StreakCalculator.Calculate(dates, today);
        Assert.Equal(2, total);
        Assert.Equal(2, streak);
    }

    [Fact]
    public void DuplicateDates_Deduplicated()
    {
        var today = new DateOnly(2026, 9, 11);
        var dates = new[] { today, today, today };
        var (total, streak) = StreakCalculator.Calculate(dates, today);
        Assert.Equal(1, total);
        Assert.Equal(1, streak);
    }
}
