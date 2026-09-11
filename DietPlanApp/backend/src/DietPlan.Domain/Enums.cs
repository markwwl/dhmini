namespace DietPlan.Domain;

/// <summary>餐次类型</summary>
public enum MealType
{
    /// <summary>早餐</summary>
    Breakfast = 1,

    /// <summary>午餐</summary>
    Lunch = 2,

    /// <summary>晚餐</summary>
    Dinner = 3
}

/// <summary>方案状态</summary>
public enum PlanStatus
{
    /// <summary>草稿</summary>
    Draft = 0,

    /// <summary>上线</summary>
    Online = 1,

    /// <summary>下线</summary>
    Offline = 2
}

/// <summary>菜品状态</summary>
public enum DishStatus
{
    /// <summary>草稿</summary>
    Draft = 0,

    /// <summary>已发布</summary>
    Published = 1,

    /// <summary>已下架</summary>
    Offline = 2
}
