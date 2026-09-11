using DietPlan.Application;
using DietPlan.Application.DTOs;
using DietPlan.Domain;
using Microsoft.EntityFrameworkCore;

namespace DietPlan.Infrastructure.Services;

/// <summary>打卡服务：单菜打卡 / 整日打卡 / 统计汇总（仅限当日，防重复）</summary>
public class CheckinService
{
    private readonly AppDbContext _db;

    /// <summary>构造</summary>
    public CheckinService(AppDbContext db) => _db = db;

    /// <summary>单菜打卡（按槽位）</summary>
    public async Task CheckinAsync(int userId, int weekId, int dayNo, int slotId)
    {
        var slot = await _db.DishSlotItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == slotId)
            ?? throw new BizException("菜品槽位不存在", 404);

        var mealSlot = await _db.MealSlots.AsNoTracking().FirstOrDefaultAsync(m => m.Id == slot.MealSlotId)
            ?? throw new BizException("餐次槽位不存在", 404);

        var day = await _db.PlanDays.AsNoTracking()
            .Include(d => d.Week).ThenInclude(w => w.Stage)
            .FirstOrDefaultAsync(d => d.Id == mealSlot.PlanDayId)
            ?? throw new BizException("天不存在", 404);

        if (day.WeekId != weekId || day.DayNo != dayNo)
        {
            throw new BizException("槽位与所选周/天不匹配");
        }

        await InsertCheckinAsync(userId, day, mealSlot.MealType, slot.DishId);
    }

    /// <summary>整日打卡（当日全部餐次全部菜品）</summary>
    public async Task<int> CheckinDayAsync(int userId, int weekId, int dayNo)
    {
        var day = await _db.PlanDays.AsNoTracking()
            .Include(d => d.Week).ThenInclude(w => w.Stage)
            .Include(d => d.MealSlots).ThenInclude(m => m.DishItems)
            .FirstOrDefaultAsync(d => d.WeekId == weekId && d.DayNo == dayNo)
            ?? throw new BizException("该天不存在", 404);

        var count = 0;
        foreach (var meal in day.MealSlots.OrderBy(m => m.Sort))
        foreach (var item in meal.DishItems.OrderBy(i => i.Sort))
        {
            if (await InsertCheckinAsync(userId, day, meal.MealType, item.DishId))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>插入打卡记录；重复打卡静默跳过（唯一索引兜底）。返回是否新插入。</summary>
    private async Task<bool> InsertCheckinAsync(int userId, PlanDay day, MealType mealType, int dishId)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var exists = await _db.CheckInRecords.AnyAsync(c =>
            c.UserId == userId && c.Date == today && c.DishId == dishId);
        if (exists) return false;

        _db.CheckInRecords.Add(new CheckInRecord
        {
            UserId = userId,
            Date = today,
            PlanId = day.Week!.Stage!.PlanId,
            StageId = day.Week.StageId,
            WeekId = day.WeekId,
            DayNo = day.DayNo,
            MealType = mealType,
            DishId = dishId,
            CreatedAt = DateTime.Now
        });

        try
        {
            await _db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            // 并发下撞唯一索引(UserId,Date,DishId)：视为已打卡，静默忽略
            var added = _db.CheckInRecords.Local.FirstOrDefault();
            if (added != null) _db.Entry(added).State = EntityState.Detached;
            return false;
        }
    }

    /// <summary>我的打卡统计（累计/连续/近 30 天日历）</summary>
    public async Task<CheckinSummaryDto> GetSummaryAsync(int userId)
    {
        var records = await _db.CheckInRecords.AsNoTracking()
            .Where(c => c.UserId == userId)
            .Select(c => new { c.Date })
            .ToListAsync();

        var today = DateOnly.FromDateTime(DateTime.Today);
        var (total, streak) = StreakCalculator.Calculate(records.Select(r => r.Date), today);

        var from = today.AddDays(-29);
        var calendar = records.Where(r => r.Date >= from)
            .GroupBy(r => r.Date)
            .Select(g => new CheckinDayDto(g.Key, g.Count()))
            .OrderBy(x => x.Date)
            .ToList();

        return new CheckinSummaryDto { TotalDays = total, StreakDays = streak, Calendar = calendar };
    }
}
