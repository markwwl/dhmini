using DietPlan.Infrastructure;
using DietPlan.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DietPlan.Tests;

/// <summary>内容查询服务单测（InMemory）</summary>
public class ContentServiceTests : TestBase
{
    [Fact]
    public async Task PlanTree_OrdersStagesAndWeeks()
    {
        await using var db = CreateDb();
        await SeedStandardPlanAsync(db);
        var svc = new ContentService(db);

        var tree = await svc.GetPlanTreeAsync(1);

        Assert.Equal("均衡抗炎方案", tree.Name);
        var stage = Assert.Single(tree.Stages);
        Assert.Equal("均衡启动", stage.Theme);
        var week = Assert.Single(stage.Weeks);
        Assert.Equal(2, week.DayCount);
    }

    [Fact]
    public async Task DayMeals_ReturnsThreeMealsWithSummary()
    {
        await using var db = CreateDb();
        await SeedStandardPlanAsync(db);
        var svc = new ContentService(db);

        var day = await svc.GetDayMealsAsync(1, 1, userId: null);

        Assert.Equal(3, day.Meals.Count);
        Assert.Equal("早餐", day.Meals[0].MealName);
        var slot = day.Meals[0].Slots[0];
        Assert.Equal("黑米粥", slot.DishName);
        Assert.Equal("有机黑米 20g、粳米 20g", slot.IngredientsSummary);
        Assert.Equal(1, slot.CandidateGroupId);
        Assert.False(slot.Checked);
    }

    [Fact]
    public async Task Candidates_OnlyFromGroup_AndMarksCurrent()
    {
        await using var db = CreateDb();
        await SeedStandardPlanAsync(db);
        var svc = new ContentService(db);

        var candidates = await svc.GetCandidatesAsync(slotId: 101, keyword: null);

        Assert.Equal(2, candidates.Count); // 黑米粥 + 小米粥（草稿菜 5 不出现）
        Assert.Contains(candidates, c => c.IsCurrent && c.DishId == 1);
    }

    [Fact]
    public async Task Candidates_KeywordFiltersByNameAndIngredients()
    {
        await using var db = CreateDb();
        await SeedStandardPlanAsync(db);
        var svc = new ContentService(db);

        var byName = await svc.GetCandidatesAsync(101, "小米");
        var byIngredient = await svc.GetCandidatesAsync(101, "黑米");
        var none = await svc.GetCandidatesAsync(101, "鲍鱼");

        Assert.Single(byName);
        Assert.Single(byIngredient);
        Assert.Empty(none);
    }

    [Fact]
    public async Task DishDetail_ParsesIngredientsAndSteps()
    {
        await using var db = CreateDb();
        await SeedStandardPlanAsync(db);
        var svc = new ContentService(db);

        var detail = await svc.GetDishDetailAsync(1);

        Assert.Equal("黑米粥", detail.Name);
        Assert.Equal(2, detail.Ingredients.Count);
    }

    [Fact]
    public async Task PlanTree_NonOnlinePlan_Throws404()
    {
        await using var db = CreateDb();
        await SeedStandardPlanAsync(db);
        var svc = new ContentService(db);

        var plan = await db.Plans.FirstAsync();
        plan.Status = Domain.PlanStatus.Draft;
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<Application.BizException>(() => svc.GetPlanTreeAsync(1));
        Assert.Equal(404, ex.StatusCode);
    }
}
