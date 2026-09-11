using DietPlan.Domain;
using DietPlan.Infrastructure;
using DietPlan.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DietPlan.Tests;

/// <summary>测试基类：InMemory 上下文 + 标准方案数据工厂</summary>
public abstract class TestBase
{
    /// <summary>创建独立的 InMemory 上下文</summary>
    protected static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"test_{Guid.NewGuid():N}")
            .Options;
        return new AppDbContext(options);
    }

    /// <summary>
    /// 标准数据：1 个在线方案 → 1 阶段 → 1 周（2 天）→ 每天三餐；
    /// 早餐槽位 1 个菜品（Dish1，挂候选组），午/晚餐各 1 个菜品（Dish2/Dish3，无候选组）。
    /// </summary>
    protected static async Task<Plan> SeedStandardPlanAsync(AppDbContext db)
    {
        var dish1 = new Dish { Id = 1, Name = "黑米粥", Subtitle = "粗粮主食", Status = DishStatus.Published, IngredientsJson = "[{\"name\":\"有机黑米\",\"amount\":\"20g\"},{\"name\":\"粳米\",\"amount\":\"20g\"}]" };
        var dish2 = new Dish { Id = 2, Name = "清蒸鲈鱼", Status = DishStatus.Published, IngredientsJson = "[{\"name\":\"鲈鱼\",\"amount\":\"150g\"}]" };
        var dish3 = new Dish { Id = 3, Name = "蒜蓉西兰花", Status = DishStatus.Published, IngredientsJson = "[{\"name\":\"西兰花\",\"amount\":\"200g\"}]" };
        var dish4 = new Dish { Id = 4, Name = "小米粥", Status = DishStatus.Published, IngredientsJson = "[{\"name\":\"小米\",\"amount\":\"40g\"}]" };
        var dishDraft = new Dish { Id = 5, Name = "未发布菜品", Status = DishStatus.Draft };

        db.Dishes.AddRange(dish1, dish2, dish3, dish4, dishDraft);

        var group = new DishCandidateGroup
        {
            Id = 1,
            Name = "主食类",
            Items = new List<CandidateGroupItem>
            {
                new() { GroupId = 1, DishId = 1 },
                new() { GroupId = 1, DishId = 4 }
            }
        };
        db.CandidateGroups.Add(group);

        var plan = new Plan
        {
            Id = 1, Name = "均衡抗炎方案", Status = PlanStatus.Online, Sort = 1,
            Stages = new List<Stage>
            {
                new()
                {
                    Id = 1, PlanId = 1, Name = "第 1 阶段", Theme = "均衡启动", Sort = 1,
                    Weeks = new List<Week>
                    {
                        new()
                        {
                            Id = 1, StageId = 1, Label = "第1周", Theme = "启动周", Sort = 1,
                            Days = new List<PlanDay> { NewDay(1, 1), NewDay(2, 2) }
                        }
                    }
                }
            }
        };
        db.Plans.Add(plan);
        await db.SaveChangesAsync();
        return plan;
    }

    private static PlanDay NewDay(int id, int dayNo) => new()
    {
        Id = id, WeekId = 1, DayNo = dayNo,
        MealSlots = new List<MealSlot>
        {
            new()
            {
                Id = (id - 1) * 3 + 1, PlanDayId = id, MealType = MealType.Breakfast, Sort = 1,
                DishItems = new List<DishSlotItem>
                {
                    // 槽位 Id：奇数天 101，偶数天 201 —— 均挂主食候选组
                    new() { Id = (id - 1) * 100 + 101, MealSlotId = (id - 1) * 3 + 1, DishId = 1, CandidateGroupId = 1, Sort = 1 }
                }
            },
            new()
            {
                Id = (id - 1) * 3 + 2, PlanDayId = id, MealType = MealType.Lunch, Sort = 2,
                DishItems = new List<DishSlotItem>
                {
                    new() { Id = (id - 1) * 100 + 102, MealSlotId = (id - 1) * 3 + 2, DishId = 2, Sort = 1 }
                }
            },
            new()
            {
                Id = (id - 1) * 3 + 3, PlanDayId = id, MealType = MealType.Dinner, Sort = 3,
                DishItems = new List<DishSlotItem>
                {
                    new() { Id = (id - 1) * 100 + 103, MealSlotId = (id - 1) * 3 + 3, DishId = 3, Sort = 1 }
                }
            }
        }
    };
}
