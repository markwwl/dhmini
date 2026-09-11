namespace DietPlan.Domain;

/// <summary>菜品主数据（内容全部由后台维护）</summary>
public class Dish
{
    /// <summary>主键</summary>
    public int Id { get; set; }

    /// <summary>菜名</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>副标题</summary>
    public string Subtitle { get; set; } = string.Empty;

    /// <summary>分类（主食/荤菜/素菜/汤品…）</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>图片列表 JSON（数组字符串）</summary>
    public string ImagesJson { get; set; } = "[]";

    /// <summary>食材及克重 JSON（[{name,amount,unit,image}]）</summary>
    public string IngredientsJson { get; set; } = "[]";

    /// <summary>制作步骤 JSON（[{text,image}]）</summary>
    public string StepsJson { get; set; } = "[]";

    /// <summary>状态（草稿/已发布/已下架）</summary>
    public DishStatus Status { get; set; } = DishStatus.Draft;

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>食材主数据</summary>
public class Ingredient
{
    /// <summary>主键</summary>
    public int Id { get; set; }

    /// <summary>食材名</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>计量单位（g/ml/个…）</summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>食材图 URL</summary>
    public string Image { get; set; } = string.Empty;
}

/// <summary>可替换候选组（同类型菜品互换，保证营养结构）</summary>
public class DishCandidateGroup
{
    /// <summary>主键</summary>
    public int Id { get; set; }

    /// <summary>组名</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>组内菜品集合</summary>
    public List<CandidateGroupItem> Items { get; set; } = new();
}

/// <summary>候选组成员</summary>
public class CandidateGroupItem
{
    /// <summary>主键</summary>
    public int Id { get; set; }

    /// <summary>所属候选组 Id</summary>
    public int GroupId { get; set; }

    /// <summary>菜品 Id</summary>
    public int DishId { get; set; }
}
