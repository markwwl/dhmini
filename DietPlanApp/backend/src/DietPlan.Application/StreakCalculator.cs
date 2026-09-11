namespace DietPlan.Application;

/// <summary>连续打卡天数计算器（纯函数，可单测）</summary>
public static class StreakCalculator
{
    /// <summary>
    /// 计算累计打卡天数与截至基准日（含）的连续打卡天数。
    /// 连续定义：基准日往前逐日连续有打卡；若基准日当天未打卡，则允许从基准日前一天起算（当日未打不影响历史连续）。
    /// </summary>
    /// <param name="dates">用户全部打卡日期（可重复、乱序）</param>
    /// <param name="today">基准日（通常为今天）</param>
    /// <returns>(累计天数, 连续天数)</returns>
    public static (int TotalDays, int StreakDays) Calculate(IEnumerable<DateOnly> dates, DateOnly today)
    {
        var set = dates.Distinct().ToHashSet();
        var total = set.Count;

        var streak = 0;
        var cursor = set.Contains(today) ? today : today.AddDays(-1);
        while (set.Contains(cursor))
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }

        return (total, streak);
    }
}
