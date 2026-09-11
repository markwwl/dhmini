namespace DietPlan.Application.DTOs;

/// <summary>方案列表项</summary>
public record PlanSummaryDto(int Id, string Name, string Intro, int StageCount);

/// <summary>方案树（阶段 → 周）</summary>
public class PlanTreeDto
{
    /// <summary>方案 Id</summary>
    public int Id { get; set; }

    /// <summary>方案名</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>简介</summary>
    public string Intro { get; set; } = string.Empty;

    /// <summary>阶段列表</summary>
    public List<StageDto> Stages { get; set; } = new();
}

/// <summary>阶段节点</summary>
public class StageDto
{
    /// <summary>阶段 Id</summary>
    public int Id { get; set; }

    /// <summary>阶段名（第 1 阶段）</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>主题（均衡启动）</summary>
    public string Theme { get; set; } = string.Empty;

    /// <summary>方案详情富文本</summary>
    public string DetailRichText { get; set; } = string.Empty;

    /// <summary>周列表</summary>
    public List<WeekDto> Weeks { get; set; } = new();
}

/// <summary>周节点</summary>
public class WeekDto
{
    /// <summary>周 Id</summary>
    public int Id { get; set; }

    /// <summary>周标签（第1周）</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>周主题</summary>
    public string Theme { get; set; } = string.Empty;

    /// <summary>背景图</summary>
    public string CoverImage { get; set; } = string.Empty;

    /// <summary>天数</summary>
    public int DayCount { get; set; }
}

/// <summary>某天三餐食谱视图</summary>
public class DayMealsDto
{
    /// <summary>周 Id</summary>
    public int WeekId { get; set; }

    /// <summary>天序号</summary>
    public int DayNo { get; set; }

    /// <summary>方案 Id</summary>
    public int PlanId { get; set; }

    /// <summary>阶段 Id</summary>
    public int StageId { get; set; }

    /// <summary>餐次列表</summary>
    public List<MealDto> Meals { get; set; } = new();
}

/// <summary>餐次（含菜品槽位）</summary>
public class MealDto
{
    /// <summary>餐次类型值（1早 2午 3晚）</summary>
    public int MealType { get; set; }

    /// <summary>餐次名（早餐/午餐/晚餐）</summary>
    public string MealName { get; set; } = string.Empty;

    /// <summary>菜品槽位列表</summary>
    public List<DishSlotDto> Slots { get; set; } = new();
}

/// <summary>菜品槽位（L2 卡片）</summary>
public class DishSlotDto
{
    /// <summary>槽位 Id</summary>
    public int SlotId { get; set; }

    /// <summary>菜品 Id</summary>
    public int DishId { get; set; }

    /// <summary>菜名</summary>
    public string DishName { get; set; } = string.Empty;

    /// <summary>副标题</summary>
    public string Subtitle { get; set; } = string.Empty;

    /// <summary>主图（首图）</summary>
    public string Image { get; set; } = string.Empty;

    /// <summary>食材摘要（如"有机黑米 20g、粳米 20g"）</summary>
    public string IngredientsSummary { get; set; } = string.Empty;

    /// <summary>候选组 Id（空=不可更换）</summary>
    public int? CandidateGroupId { get; set; }

    /// <summary>当日是否已打卡</summary>
    public bool Checked { get; set; }
}

/// <summary>菜品制作解析（L3）</summary>
public class DishDetailDto
{
    /// <summary>菜品 Id</summary>
    public int Id { get; set; }

    /// <summary>菜名</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>副标题</summary>
    public string Subtitle { get; set; } = string.Empty;

    /// <summary>分类</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>图片列表</summary>
    public List<string> Images { get; set; } = new();

    /// <summary>食材清单</summary>
    public List<IngredientItemDto> Ingredients { get; set; } = new();

    /// <summary>制作步骤</summary>
    public List<StepDto> Steps { get; set; } = new();
}

/// <summary>食材项</summary>
public class IngredientItemDto
{
    /// <summary>食材名</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>用量</summary>
    public string Amount { get; set; } = string.Empty;

    /// <summary>缩略图</summary>
    public string Image { get; set; } = string.Empty;
}

/// <summary>制作步骤项</summary>
public class StepDto
{
    /// <summary>步骤说明</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>步骤图（可选）</summary>
    public string Image { get; set; } = string.Empty;
}

/// <summary>候选菜品项（L4）</summary>
public class CandidateDto
{
    /// <summary>菜品 Id</summary>
    public int DishId { get; set; }

    /// <summary>菜名</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>副标题</summary>
    public string Subtitle { get; set; } = string.Empty;

    /// <summary>主图</summary>
    public string Image { get; set; } = string.Empty;

    /// <summary>食材摘要</summary>
    public string IngredientsSummary { get; set; } = string.Empty;

    /// <summary>是否为当前槽位菜品</summary>
    public bool IsCurrent { get; set; }
}

/// <summary>单菜打卡请求</summary>
public record CheckinRequest(int WeekId, int DayNo, int SlotId);

/// <summary>整日打卡请求（当日全部餐次）</summary>
public record DayCheckinRequest(int WeekId, int DayNo);

/// <summary>打卡统计汇总（我的页）</summary>
public class CheckinSummaryDto
{
    /// <summary>累计打卡天数</summary>
    public int TotalDays { get; set; }

    /// <summary>连续打卡天数</summary>
    public int StreakDays { get; set; }

    /// <summary>近期日历（月视图：日期 → 当日打卡菜品数）</summary>
    public List<CheckinDayDto> Calendar { get; set; } = new();
}

/// <summary>打卡日历项</summary>
public record CheckinDayDto(DateOnly Date, int Count);

/// <summary>更换菜品请求</summary>
public record ReplaceDishRequest(int SlotId, int DishId);
