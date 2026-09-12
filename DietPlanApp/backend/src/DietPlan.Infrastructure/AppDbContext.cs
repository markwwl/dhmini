using DietPlan.Domain;
using Microsoft.EntityFrameworkCore;

namespace DietPlan.Infrastructure;

/// <summary>应用数据库上下文（SQL Server；单元测试换 InMemory 提供程序）</summary>
public class AppDbContext : DbContext
{
    /// <summary>构造上下文</summary>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    /// <summary>方案</summary>
    public DbSet<Plan> Plans => Set<Plan>();

    /// <summary>阶段</summary>
    public DbSet<Stage> Stages => Set<Stage>();

    /// <summary>周</summary>
    public DbSet<Week> Weeks => Set<Week>();

    /// <summary>天</summary>
    public DbSet<PlanDay> PlanDays => Set<PlanDay>();

    /// <summary>餐次槽位</summary>
    public DbSet<MealSlot> MealSlots => Set<MealSlot>();

    /// <summary>菜品槽位</summary>
    public DbSet<DishSlotItem> DishSlotItems => Set<DishSlotItem>();

    /// <summary>菜品</summary>
    public DbSet<Dish> Dishes => Set<Dish>();

    /// <summary>食材</summary>
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();

    /// <summary>候选组</summary>
    public DbSet<DishCandidateGroup> CandidateGroups => Set<DishCandidateGroup>();

    /// <summary>候选组成员</summary>
    public DbSet<CandidateGroupItem> CandidateGroupItems => Set<CandidateGroupItem>();

    /// <summary>用户</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>打卡记录</summary>
    public DbSet<CheckInRecord> CheckInRecords => Set<CheckInRecord>();

    /// <summary>运营内容块</summary>
    public DbSet<ContentBlock> ContentBlocks => Set<ContentBlock>();

    /// <summary>后台账号</summary>
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // openid 唯一，防重复注册
        modelBuilder.Entity<User>().HasIndex(u => u.OpenId).IsUnique();

        // 同用户同一计划天同一菜品只允许一条打卡（允许补打卡：身份与真实自然日解耦，
        // 避免「同一天补打两个含相同菜品的计划天」被唯一约束误删）
        modelBuilder.Entity<CheckInRecord>()
            .HasIndex(c => new { c.UserId, c.WeekId, c.DayNo, c.DishId })
            .IsUnique();

        // 后台用户名唯一
        modelBuilder.Entity<AdminUser>().HasIndex(a => a.Username).IsUnique();

        // 周内天序号唯一
        modelBuilder.Entity<PlanDay>().HasIndex(d => new { d.WeekId, d.DayNo }).IsUnique();
    }
}
