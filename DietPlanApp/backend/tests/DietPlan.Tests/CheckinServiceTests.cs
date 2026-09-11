using DietPlan.Application;
using DietPlan.Domain;
using DietPlan.Infrastructure;
using DietPlan.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DietPlan.Tests;

/// <summary>打卡服务单测（InMemory）</summary>
public class CheckinServiceTests : TestBase
{
    [Fact]
    public async Task Checkin_SingleDish_CreatesRecord()
    {
        await using var db = CreateDb();
        await SeedStandardPlanAsync(db);
        var svc = new CheckinService(db);

        await svc.CheckinAsync(userId: 9, weekId: 1, dayNo: 1, slotId: 101);

        Assert.Single(db.CheckInRecords);
        var rec = await db.CheckInRecords.SingleAsync();
        Assert.Equal(9, rec.UserId);
        Assert.Equal(1, rec.DishId);
        Assert.Equal(MealType.Breakfast, rec.MealType);
    }

    [Fact]
    public async Task Checkin_SameDishTwice_IsRejected()
    {
        await using var db = CreateDb();
        await SeedStandardPlanAsync(db);
        var svc = new CheckinService(db);

        await svc.CheckinAsync(9, 1, 1, 101);
        // 同一菜品同日重复打卡：静默跳过（ dinner 菜 3 也可以独立打卡）
        await svc.CheckinAsync(9, 1, 1, 103);

        Assert.Equal(2, await db.CheckInRecords.CountAsync());
    }

    [Fact]
    public async Task Checkin_SlotNotInSelectedDay_Throws()
    {
        await using var db = CreateDb();
        await SeedStandardPlanAsync(db);
        var svc = new CheckinService(db);

        // 槽位 101 属于第 1 天，谎报第 2 天 → 拒绝
        await Assert.ThrowsAsync<BizException>(() => svc.CheckinAsync(9, 1, 2, 101));
    }

    [Fact]
    public async Task CheckinDay_CoversAllSlots_Once()
    {
        await using var db = CreateDb();
        await SeedStandardPlanAsync(db);
        var svc = new CheckinService(db);

        var count = await svc.CheckinDayAsync(9, 1, 1);

        Assert.Equal(3, count); // 早/午/晚 各 1 菜
        Assert.Equal(3, await db.CheckInRecords.CountAsync());

        // 再来一次全部幂等
        var count2 = await svc.CheckinDayAsync(9, 1, 1);
        Assert.Equal(0, count2);
        Assert.Equal(3, await db.CheckInRecords.CountAsync());
    }

    [Fact]
    public async Task Summary_AggregatesTotalAndStreak()
    {
        await using var db = CreateDb();
        await SeedStandardPlanAsync(db);
        var svc = new CheckinService(db);

        var today = DateOnly.FromDateTime(DateTime.Today);
        db.CheckInRecords.Add(new CheckInRecord { UserId = 9, Date = today, DishId = 1, MealType = MealType.Breakfast });
        db.CheckInRecords.Add(new CheckInRecord { UserId = 9, Date = today, DishId = 2, MealType = MealType.Lunch });
        db.CheckInRecords.Add(new CheckInRecord { UserId = 9, Date = today.AddDays(-1), DishId = 1, MealType = MealType.Breakfast });
        await db.SaveChangesAsync();

        var summary = await svc.GetSummaryAsync(9);
        Assert.Equal(2, summary.TotalDays);
        Assert.Equal(2, summary.StreakDays);
        Assert.Contains(summary.Calendar, c => c.Date == today && c.Count == 2);
    }
}
