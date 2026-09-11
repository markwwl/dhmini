using DietPlan.Application;
using DietPlan.Application.DTOs;
using DietPlan.Infrastructure;
using DietPlan.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DietPlan.Api.Controllers;

/// <summary>登录（微信 code 换 token）</summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;

    /// <summary>构造</summary>
    public AuthController(AuthService auth) => _auth = auth;

    /// <summary>微信登录：js_code → JWT</summary>
    [HttpPost("wx-login")]
    [AllowAnonymous]
    public async Task<IActionResult> WxLogin([FromBody] WxLoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Code))
        {
            throw new BizException("code 不能为空");
        }

        var (token, userId, nickname) = await _auth.WxLoginAsync(req.Code);
        return Ok(new WxLoginResponse(token, userId, nickname));
    }
}

/// <summary>微信登录请求</summary>
public record WxLoginRequest(string Code);

/// <summary>微信登录响应</summary>
public record WxLoginResponse(string Token, int UserId, string Nickname);

/// <summary>当前登录用户 Id（claims 提取）</summary>
public static class PrincipalExtensions
{
    /// <summary>从 JWT claims 取用户 Id</summary>
    public static int GetUserId(this ControllerBase controller) =>
        int.Parse(controller.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
}

/// <summary>方案（L1）</summary>
[ApiController]
[Route("api/plans")]
public class PlansController : ControllerBase
{
    private readonly ContentService _content;

    /// <summary>构造</summary>
    public PlansController(ContentService content) => _content = content;

    /// <summary>在线方案列表</summary>
    [HttpGet]
    public async Task<ActionResult<List<PlanSummaryDto>>> GetPlans() =>
        Ok(await _content.GetOnlinePlansAsync());

    /// <summary>方案树（阶段 → 周）</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PlanTreeDto>> GetPlanTree(int id) =>
        Ok(await _content.GetPlanTreeAsync(id));
}

/// <summary>每日食谱（L2）</summary>
[ApiController]
[Route("api/weeks/{weekId:int}/days/{dayNo:int}/meals")]
public class MealsController : ControllerBase
{
    private readonly ContentService _content;

    /// <summary>构造</summary>
    public MealsController(ContentService content) => _content = content;

    /// <summary>某天三餐食谱（含打卡状态；未登录也可见，打卡状态为空）</summary>
    [HttpGet]
    public async Task<ActionResult<DayMealsDto>> GetDayMeals(int weekId, int dayNo)
    {
        int? userId = User.Identity?.IsAuthenticated == true ? this.GetUserId() : null;
        return Ok(await _content.GetDayMealsAsync(weekId, dayNo, userId));
    }
}

/// <summary>菜品制作解析（L3）</summary>
[ApiController]
[Route("api/dishes")]
public class DishesController : ControllerBase
{
    private readonly ContentService _content;

    /// <summary>构造</summary>
    public DishesController(ContentService content) => _content = content;

    /// <summary>菜品详情（食材清单 + 制作步骤）</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<DishDetailDto>> GetDish(int id) =>
        Ok(await _content.GetDishDetailAsync(id));
}

/// <summary>候选菜品（L4）</summary>
[ApiController]
[Route("api/dish-slots/{slotId:int}/candidates")]
public class CandidatesController : ControllerBase
{
    private readonly ContentService _content;

    /// <summary>构造</summary>
    public CandidatesController(ContentService content) => _content = content;

    /// <summary>槽位可替换候选列表（支持关键字）</summary>
    [HttpGet]
    public async Task<ActionResult<List<CandidateDto>>> GetCandidates(int slotId, [FromQuery] string? keyword) =>
        Ok(await _content.GetCandidatesAsync(slotId, keyword));
}

/// <summary>更换菜品（L4 确认）</summary>
[ApiController]
[Route("api/dish-slots")]
[Authorize]
public class DishSlotController : ControllerBase
{
    private readonly Infrastructure.AppDbContext _db;

    /// <summary>构造</summary>
    public DishSlotController(Infrastructure.AppDbContext db) => _db = db;

    /// <summary>确认更换：把槽位菜品替换为候选组内所选菜品</summary>
    [HttpPut("{slotId:int}/dish")]
    public async Task<IActionResult> ReplaceDish(int slotId, [FromBody] ReplaceDishRequest req)
    {
        var slot = await _db.DishSlotItems.FirstOrDefaultAsync(i => i.Id == slotId)
            ?? throw new BizException("菜品槽位不存在", 404);

        // 只允许换成候选组内成员（营养结构保护）
        var inGroup = slot.CandidateGroupId.HasValue && await _db.CandidateGroupItems.AnyAsync(
            g => g.GroupId == slot.CandidateGroupId.Value && g.DishId == req.DishId);
        if (!inGroup)
        {
            throw new BizException("所选菜品不在候选组内");
        }

        slot.DishId = req.DishId;
        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }
}
