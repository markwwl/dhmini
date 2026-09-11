using DietPlan.Application;
using DietPlan.Domain;
using DietPlan.Infrastructure;
using DietPlan.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DietPlan.Api.Controllers.Admin;

/// <summary>后台登录</summary>
[ApiController]
[Route("api/admin/auth")]
public class AdminAuthController : ControllerBase
{
    private readonly AuthService _auth;

    /// <summary>构造</summary>
    public AdminAuthController(AuthService auth) => _auth = auth;

    /// <summary>账号密码登录，返回管理员 JWT</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] AdminLoginRequest req) =>
        Ok(new { token = await _auth.AdminLoginAsync(req.Username, req.Password) });
}

/// <summary>后台登录请求</summary>
public record AdminLoginRequest(string Username, string Password);

/// <summary>后台管理基类：统一要求 admin 角色</summary>
[ApiController]
[Authorize(Roles = "admin")]
public abstract class AdminControllerBase : ControllerBase { }

/// <summary>方案管理</summary>
[Route("api/admin/plans")]
public class AdminPlansController : AdminControllerBase
{
    private readonly AppDbContext _db;

    /// <summary>构造</summary>
    public AdminPlansController(AppDbContext db) => _db = db;

    /// <summary>方案列表（含全部状态）</summary>
    [HttpGet]
    public async Task<IActionResult> List() =>
        Ok(await _db.Plans.AsNoTracking().OrderBy(p => p.Sort).ToListAsync());

    /// <summary>新建方案</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Plan plan)
    {
        plan.Id = 0;
        _db.Plans.Add(plan);
        await _db.SaveChangesAsync();
        return Ok(plan);
    }

    /// <summary>更新方案</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Plan input)
    {
        var plan = await _db.Plans.FindAsync(id) ?? throw new BizException("方案不存在", 404);
        plan.Name = input.Name;
        plan.Intro = input.Intro;
        plan.Status = input.Status;
        plan.Sort = input.Sort;
        await _db.SaveChangesAsync();
        return Ok(plan);
    }

    /// <summary>删除方案（含下级内容级联校验提示）</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var hasStages = await _db.Stages.AnyAsync(s => s.PlanId == id);
        if (hasStages)
        {
            throw new BizException("该方案下存在阶段，请先删除阶段");
        }

        var plan = await _db.Plans.FindAsync(id) ?? throw new BizException("方案不存在", 404);
        _db.Plans.Remove(plan);
        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }
}

/// <summary>阶段管理</summary>
[Route("api/admin/stages")]
public class AdminStagesController : AdminControllerBase
{
    private readonly AppDbContext _db;

    /// <summary>构造</summary>
    public AdminStagesController(AppDbContext db) => _db = db;

    /// <summary>按方案列阶段</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int planId) =>
        Ok(await _db.Stages.AsNoTracking().Where(s => s.PlanId == planId).OrderBy(s => s.Sort).ToListAsync());

    /// <summary>新建阶段</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Stage stage)
    {
        stage.Id = 0;
        _db.Stages.Add(stage);
        await _db.SaveChangesAsync();
        return Ok(stage);
    }

    /// <summary>更新阶段</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Stage input)
    {
        var stage = await _db.Stages.FindAsync(id) ?? throw new BizException("阶段不存在", 404);
        stage.Name = input.Name;
        stage.Theme = input.Theme;
        stage.Sort = input.Sort;
        stage.DetailRichText = input.DetailRichText;
        await _db.SaveChangesAsync();
        return Ok(stage);
    }

    /// <summary>删除阶段（有周时拒绝）</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (await _db.Weeks.AnyAsync(w => w.StageId == id))
        {
            throw new BizException("该阶段下存在周，请先删除周");
        }

        var stage = await _db.Stages.FindAsync(id) ?? throw new BizException("阶段不存在", 404);
        _db.Stages.Remove(stage);
        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }
}

/// <summary>周管理</summary>
[Route("api/admin/weeks")]
public class AdminWeeksController : AdminControllerBase
{
    private readonly AppDbContext _db;

    /// <summary>构造</summary>
    public AdminWeeksController(AppDbContext db) => _db = db;

    /// <summary>按阶段列周</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int stageId) =>
        Ok(await _db.Weeks.AsNoTracking().Where(w => w.StageId == stageId).OrderBy(w => w.Sort).ToListAsync());

    /// <summary>新建周</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Week week)
    {
        week.Id = 0;
        _db.Weeks.Add(week);
        await _db.SaveChangesAsync();
        return Ok(week);
    }

    /// <summary>更新周</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Week input)
    {
        var week = await _db.Weeks.FindAsync(id) ?? throw new BizException("周不存在", 404);
        week.Label = input.Label;
        week.Theme = input.Theme;
        week.CoverImage = input.CoverImage;
        week.Sort = input.Sort;
        await _db.SaveChangesAsync();
        return Ok(week);
    }

    /// <summary>删除周（有天时拒绝）</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (await _db.PlanDays.AnyAsync(d => d.WeekId == id))
        {
            throw new BizException("该周下存在天，请先删除天");
        }

        var week = await _db.Weeks.FindAsync(id) ?? throw new BizException("周不存在", 404);
        _db.Weeks.Remove(week);
        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }
}

/// <summary>天与餐次槽位管理</summary>
[Route("api/admin/days")]
public class AdminDaysController : AdminControllerBase
{
    private readonly AppDbContext _db;

    /// <summary>构造</summary>
    public AdminDaysController(AppDbContext db) => _db = db;

    /// <summary>按周列天（含餐次与菜品槽位全量）</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int weekId) =>
        Ok(await _db.PlanDays.AsNoTracking()
            .Include(d => d.MealSlots.OrderBy(m => m.Sort))
            .ThenInclude(m => m.DishItems.OrderBy(i => i.Sort))
            .Where(d => d.WeekId == weekId)
            .OrderBy(d => d.DayNo)
            .ToListAsync());

    /// <summary>新建天（可选同时生成三餐空槽位）</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDayRequest req)
    {
        var day = new PlanDay { WeekId = req.WeekId, DayNo = req.DayNo };
        if (req.WithDefaultMeals)
        {
            day.MealSlots = new List<MealSlot>
            {
                new() { MealType = MealType.Breakfast, Sort = 1 },
                new() { MealType = MealType.Lunch, Sort = 2 },
                new() { MealType = MealType.Dinner, Sort = 3 }
            };
        }

        _db.PlanDays.Add(day);
        await _db.SaveChangesAsync();
        return Ok(day);
    }

    /// <summary>天新增请求</summary>
    public record CreateDayRequest(int WeekId, int DayNo, bool WithDefaultMeals = true);

    /// <summary>删除天（级联删槽位）</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var day = await _db.PlanDays.Include(d => d.MealSlots)
            .ThenInclude(m => m.DishItems)
            .FirstOrDefaultAsync(d => d.Id == id) ?? throw new BizException("天不存在", 404);

        _db.PlanDays.Remove(day);
        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    /// <summary>天加餐次槽位</summary>
    [HttpPost("{id:int}/slots")]
    public async Task<IActionResult> AddSlot(int id, [FromBody] AddSlotRequest req)
    {
        var day = await _db.PlanDays.FindAsync(id) ?? throw new BizException("天不存在", 404);
        var slot = new MealSlot { PlanDayId = day.Id, MealType = req.MealType, Sort = req.Sort };
        _db.MealSlots.Add(slot);
        await _db.SaveChangesAsync();
        return Ok(slot);
    }

    /// <summary>加餐次槽位请求</summary>
    public record AddSlotRequest(MealType MealType, int Sort = 0);

    /// <summary>餐次槽位加菜品槽位（含候选组）</summary>
    [HttpPost("slots/{slotId:int}/dishes")]
    public async Task<IActionResult> AddDishSlot(int slotId, [FromBody] AddDishSlotRequest req)
    {
        var mealSlot = await _db.MealSlots.FindAsync(slotId) ?? throw new BizException("餐次槽位不存在", 404);
        var item = new DishSlotItem
        {
            MealSlotId = mealSlot.Id,
            DishId = req.DishId,
            CandidateGroupId = req.CandidateGroupId,
            Sort = req.Sort
        };
        _db.DishSlotItems.Add(item);
        await _db.SaveChangesAsync();
        return Ok(item);
    }

    /// <summary>加菜品槽位请求</summary>
    public record AddDishSlotRequest(int DishId, int? CandidateGroupId, int Sort = 0);

    /// <summary>更新菜品槽位（换菜 / 换候选组）</summary>
    [HttpPut("slots/{slotId:int}")]
    public async Task<IActionResult> UpdateDishSlot(int slotId, [FromBody] UpdateDishSlotRequest req)
    {
        var item = await _db.DishSlotItems.FindAsync(slotId) ?? throw new BizException("菜品槽位不存在", 404);
        item.DishId = req.DishId;
        item.CandidateGroupId = req.CandidateGroupId;
        if (req.Sort.HasValue) item.Sort = req.Sort.Value;
        await _db.SaveChangesAsync();
        return Ok(item);
    }

    /// <summary>更新菜品槽位请求</summary>
    public record UpdateDishSlotRequest(int DishId, int? CandidateGroupId, int? Sort);

    /// <summary>删除菜品槽位</summary>
    [HttpDelete("slots/{slotId:int}")]
    public async Task<IActionResult> DeleteDishSlot(int slotId)
    {
        var item = await _db.DishSlotItems.FindAsync(slotId) ?? throw new BizException("菜品槽位不存在", 404);
        _db.DishSlotItems.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }
}
