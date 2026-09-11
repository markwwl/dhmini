using System.Text.Json;
using DietPlan.Application;
using DietPlan.Application.DTOs;
using DietPlan.Domain;
using Microsoft.EntityFrameworkCore;

namespace DietPlan.Infrastructure.Services;

/// <summary>C 端内容查询服务：方案树 / 每日食谱 / 菜品解析 / 候选菜品</summary>
public class ContentService
{
    private readonly AppDbContext _db;

    // JSON 解析统一大小写不敏感（后台可能存小写键）
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    /// <summary>构造</summary>
    public ContentService(AppDbContext db) => _db = db;

    /// <summary>在线方案列表</summary>
    public async Task<List<PlanSummaryDto>> GetOnlinePlansAsync()
    {
        return await _db.Plans.AsNoTracking()
            .Where(p => p.Status == PlanStatus.Online)
            .OrderBy(p => p.Sort)
            .Select(p => new PlanSummaryDto(
                p.Id, p.Name, p.Intro,
                _db.Stages.Count(s => s.PlanId == p.Id)))
            .ToListAsync();
    }

    /// <summary>方案树（阶段 → 周），仅在线方案</summary>
    public async Task<PlanTreeDto> GetPlanTreeAsync(int planId)
    {
        var plan = await _db.Plans.AsNoTracking()
            .Include(p => p.Stages.OrderBy(s => s.Sort))
                .ThenInclude(s => s.Weeks.OrderBy(w => w.Sort))
            .FirstOrDefaultAsync(p => p.Id == planId && p.Status == PlanStatus.Online)
            ?? throw new BizException("方案不存在或未上线", 404);

        var dayCounts = await _db.PlanDays.AsNoTracking()
            .GroupBy(d => d.WeekId)
            .Select(g => new { WeekId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.WeekId, x => x.Count);

        return new PlanTreeDto
        {
            Id = plan.Id,
            Name = plan.Name,
            Intro = plan.Intro,
            Stages = plan.Stages.Select(s => new StageDto
            {
                Id = s.Id,
                Name = s.Name,
                Theme = s.Theme,
                DetailRichText = s.DetailRichText,
                Weeks = s.Weeks.Select(w => new WeekDto
                {
                    Id = w.Id,
                    Label = w.Label,
                    Theme = w.Theme,
                    CoverImage = w.CoverImage,
                    DayCount = dayCounts.GetValueOrDefault(w.Id)
                }).ToList()
            }).ToList()
        };
    }

    /// <summary>某天三餐食谱（含打卡状态与候选组标识）</summary>
    public async Task<DayMealsDto> GetDayMealsAsync(int weekId, int dayNo, int? userId)
    {
        var day = await _db.PlanDays.AsNoTracking()
            .Include(d => d.MealSlots.OrderBy(m => m.Sort))
                .ThenInclude(m => m.DishItems.OrderBy(i => i.Sort))
            .FirstOrDefaultAsync(d => d.WeekId == weekId && d.DayNo == dayNo)
            ?? throw new BizException("该天不存在", 404);

        var week = await _db.Weeks.AsNoTracking()
            .Include(w => w.Stage)
            .FirstAsync(w => w.Id == weekId);

        var dishIds = day.MealSlots.SelectMany(m => m.DishItems).Select(i => i.DishId).Distinct().ToList();
        var dishes = await _db.Dishes.AsNoTracking()
            .Where(d => dishIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id);

        // 当日打卡集合（真实自然日）
        var today = DateOnly.FromDateTime(DateTime.Today);
        var checkedDishIds = userId.HasValue
            ? await _db.CheckInRecords.AsNoTracking()
                .Where(c => c.UserId == userId.Value && c.Date == today)
                .Select(c => c.DishId)
                .ToListAsync()
            : new List<int>();
        var checkedSet = checkedDishIds.ToHashSet();

        return new DayMealsDto
        {
            WeekId = weekId,
            DayNo = dayNo,
            PlanId = week.Stage.PlanId,
            StageId = week.StageId,
            Meals = day.MealSlots.Select(m => new MealDto
            {
                MealType = (int)m.MealType,
                MealName = MealName(m.MealType),
                Slots = m.DishItems.Select(i =>
                {
                    var dish = dishes.GetValueOrDefault(i.DishId);
                    return new DishSlotDto
                    {
                        SlotId = i.Id,
                        DishId = i.DishId,
                        DishName = dish?.Name ?? string.Empty,
                        Subtitle = dish?.Subtitle ?? string.Empty,
                        Image = FirstImage(dish?.ImagesJson),
                        IngredientsSummary = BuildIngredientsSummary(dish?.IngredientsJson),
                        CandidateGroupId = i.CandidateGroupId,
                        Checked = checkedSet.Contains(i.DishId)
                    };
                }).ToList()
            }).ToList()
        };
    }

    /// <summary>菜品制作解析（L3）</summary>
    public async Task<DishDetailDto> GetDishDetailAsync(int dishId)
    {
        var dish = await _db.Dishes.AsNoTracking().FirstOrDefaultAsync(d => d.Id == dishId)
            ?? throw new BizException("菜品不存在", 404);

        return new DishDetailDto
        {
            Id = dish.Id,
            Name = dish.Name,
            Subtitle = dish.Subtitle,
            Category = dish.Category,
            Images = ParseList(dish.ImagesJson),
            Ingredients = ParseIngredients(dish.IngredientsJson),
            Steps = ParseSteps(dish.StepsJson)
        };
    }

    /// <summary>槽位候选菜品（L4），仅已发布菜品，支持按菜名/食材关键字过滤</summary>
    public async Task<List<CandidateDto>> GetCandidatesAsync(int slotId, string? keyword)
    {
        var slot = await _db.DishSlotItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == slotId)
            ?? throw new BizException("菜品槽位不存在", 404);

        var query = from item in _db.CandidateGroupItems.AsNoTracking()
                    join dish in _db.Dishes.AsNoTracking() on item.DishId equals dish.Id
                    where item.GroupId == slot.CandidateGroupId && dish.Status == DishStatus.Published
                    select dish;

        var list = await query.ToListAsync();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            list = list.Where(d =>
                    d.Name.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                    d.IngredientsJson.Contains(kw, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return list.Select(d => new CandidateDto
        {
            DishId = d.Id,
            Name = d.Name,
            Subtitle = d.Subtitle,
            Image = FirstImage(d.ImagesJson),
            IngredientsSummary = BuildIngredientsSummary(d.IngredientsJson),
            IsCurrent = d.Id == slot.DishId
        }).ToList();
    }

    private static string MealName(MealType type) => type switch
    {
        MealType.Breakfast => "早餐",
        MealType.Lunch => "午餐",
        MealType.Dinner => "晚餐",
        _ => "餐食"
    };

    private static string FirstImage(string? imagesJson)
    {
        var list = ParseList(imagesJson);
        return list.Count > 0 ? list[0] : string.Empty;
    }

    private static List<string> ParseList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<string>();
        try { return JsonSerializer.Deserialize<List<string>>(json, JsonOpts) ?? new List<string>(); }
        catch (JsonException) { return new List<string>(); }
    }

    private static string BuildIngredientsSummary(string? ingredientsJson)
    {
        var items = ParseIngredients(ingredientsJson);
        return string.Join("、", items.Select(i => $"{i.Name} {i.Amount}"));
    }

    private static List<IngredientItemDto> ParseIngredients(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<IngredientItemDto>();
        try
        {
            return JsonSerializer.Deserialize<List<IngredientItemDto>>(json, JsonOpts) ?? new List<IngredientItemDto>();
        }
        catch (JsonException) { return new List<IngredientItemDto>(); }
    }

    private static List<StepDto> ParseSteps(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<StepDto>();
        try
        {
            return JsonSerializer.Deserialize<List<StepDto>>(json, JsonOpts) ?? new List<StepDto>();
        }
        catch (JsonException) { return new List<StepDto>(); }
    }
}
