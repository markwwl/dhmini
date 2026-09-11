namespace DietPlan.Domain;

/// <summary>小程序用户（微信 openid 静默登录）</summary>
public class User
{
    /// <summary>主键</summary>
    public int Id { get; set; }

    /// <summary>微信 openid（唯一）</summary>
    public string OpenId { get; set; } = string.Empty;

    /// <summary>昵称</summary>
    public string Nickname { get; set; } = string.Empty;

    /// <summary>头像 URL</summary>
    public string Avatar { get; set; } = string.Empty;

    /// <summary>注册时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>最近活跃时间</summary>
    public DateTime? LastActiveAt { get; set; }
}

/// <summary>餐食打卡明细（菜品粒度）</summary>
public class CheckInRecord
{
    /// <summary>主键</summary>
    public int Id { get; set; }

    /// <summary>用户 Id</summary>
    public int UserId { get; set; }

    /// <summary>打卡日期（自然日）</summary>
    public DateOnly Date { get; set; }

    /// <summary>方案 Id</summary>
    public int PlanId { get; set; }

    /// <summary>阶段 Id</summary>
    public int StageId { get; set; }

    /// <summary>周 Id</summary>
    public int WeekId { get; set; }

    /// <summary>方案内天序号</summary>
    public int DayNo { get; set; }

    /// <summary>餐次类型</summary>
    public MealType MealType { get; set; }

    /// <summary>菜品 Id</summary>
    public int DishId { get; set; }

    /// <summary>打卡时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>首页运营内容块（banner/资讯/视频）</summary>
public class ContentBlock
{
    /// <summary>主键</summary>
    public int Id { get; set; }

    /// <summary>类型（banner/article/video）</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>标题</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>封面图 URL</summary>
    public string Image { get; set; } = string.Empty;

    /// <summary>跳转链接 / 视频地址</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>排序号</summary>
    public int Sort { get; set; }

    /// <summary>是否启用</summary>
    public bool Enabled { get; set; } = true;
}

/// <summary>管理后台账号</summary>
public class AdminUser
{
    /// <summary>主键</summary>
    public int Id { get; set; }

    /// <summary>用户名（唯一）</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>密码哈希（SHA256(salt+password) 十六进制）</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>盐值</summary>
    public string Salt { get; set; } = string.Empty;

    /// <summary>显示名</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
