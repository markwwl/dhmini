namespace DietPlan.Domain;

/// <summary>饮食方案（顶层聚合根）</summary>
public class Plan
{
    /// <summary>主键</summary>
    public int Id { get; set; }

    /// <summary>方案名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>方案简介</summary>
    public string Intro { get; set; } = string.Empty;

    /// <summary>状态（草稿/上线/下线）</summary>
    public PlanStatus Status { get; set; } = PlanStatus.Draft;

    /// <summary>排序号（小在前）</summary>
    public int Sort { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>下属阶段集合</summary>
    public List<Stage> Stages { get; set; } = new();
}

/// <summary>方案阶段（如：第1阶段 均衡启动）</summary>
public class Stage
{
    /// <summary>主键</summary>
    public int Id { get; set; }

    /// <summary>所属方案 Id</summary>
    public int PlanId { get; set; }

    /// <summary>阶段名</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>阶段主题（如：均衡启动）</summary>
    public string Theme { get; set; } = string.Empty;

    /// <summary>排序号</summary>
    public int Sort { get; set; }

    /// <summary>方案详情富文本（图文 HTML）</summary>
    public string DetailRichText { get; set; } = string.Empty;

    /// <summary>下属周集合</summary>
    public List<Week> Weeks { get; set; } = new();
}

/// <summary>阶段内的周（如：第1周）</summary>
public class Week
{
    /// <summary>主键</summary>
    public int Id { get; set; }

    /// <summary>所属阶段 Id</summary>
    public int StageId { get; set; }

    /// <summary>周标签（第 N 周）</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>周主题</summary>
    public string Theme { get; set; } = string.Empty;

    /// <summary>周卡片背景图 URL</summary>
    public string CoverImage { get; set; } = string.Empty;

    /// <summary>排序号</summary>
    public int Sort { get; set; }

    /// <summary>所属阶段（导航，可空避免 API 模型验证误判必填）</summary>
    public Stage? Stage { get; set; }

    /// <summary>下属天集合</summary>
    public List<PlanDay> Days { get; set; } = new();
}

/// <summary>方案天（周内第 N 天）</summary>
public class PlanDay
{
    /// <summary>主键</summary>
    public int Id { get; set; }

    /// <summary>所属周 Id</summary>
    public int WeekId { get; set; }

    /// <summary>天序号（周内，从 1 开始）</summary>
    public int DayNo { get; set; }

    /// <summary>所属周（导航，可空避免 API 模型验证误判必填）</summary>
    public Week? Week { get; set; }

    /// <summary>当日餐次槽位集合</summary>
    public List<MealSlot> MealSlots { get; set; } = new();
}

/// <summary>餐次槽位（某天的早餐/午餐/晚餐）</summary>
public class MealSlot
{
    /// <summary>主键</summary>
    public int Id { get; set; }

    /// <summary>所属天 Id</summary>
    public int PlanDayId { get; set; }

    /// <summary>餐次类型</summary>
    public MealType MealType { get; set; }

    /// <summary>排序号</summary>
    public int Sort { get; set; }

    /// <summary>菜品槽位集合</summary>
    public List<DishSlotItem> DishItems { get; set; } = new();
}

/// <summary>菜品槽位（餐次内的具体菜品，含可替换候选组引用）</summary>
public class DishSlotItem
{
    /// <summary>主键</summary>
    public int Id { get; set; }

    /// <summary>所属餐次槽位 Id</summary>
    public int MealSlotId { get; set; }

    /// <summary>当前菜品 Id</summary>
    public int DishId { get; set; }

    /// <summary>可替换候选组 Id（空表示不可更换）</summary>
    public int? CandidateGroupId { get; set; }

    /// <summary>排序号</summary>
    public int Sort { get; set; }
}
