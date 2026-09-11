using DietPlan.Application;
using DietPlan.Application.DTOs;
using DietPlan.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DietPlan.Api.Controllers;

/// <summary>打卡（单菜 / 整日）与统计</summary>
[ApiController]
[Route("api/checkins")]
[Authorize]
public class CheckinsController : ControllerBase
{
    private readonly CheckinService _checkin;

    /// <summary>构造</summary>
    public CheckinsController(CheckinService checkin) => _checkin = checkin;

    /// <summary>单菜打卡（按槽位）</summary>
    [HttpPost]
    public async Task<IActionResult> Checkin([FromBody] CheckinRequest req)
    {
        await _checkin.CheckinAsync(this.GetUserId(), req.WeekId, req.DayNo, req.SlotId);
        return Ok(new { ok = true });
    }

    /// <summary>整日打卡（当日全部餐次），返回本次新打卡数</summary>
    [HttpPost("day")]
    public async Task<IActionResult> CheckinDay([FromBody] DayCheckinRequest req)
    {
        var count = await _checkin.CheckinDayAsync(this.GetUserId(), req.WeekId, req.DayNo);
        return Ok(new { ok = true, count });
    }

    /// <summary>我的打卡统计（累计/连续/近30天日历）</summary>
    [HttpGet("summary")]
    public async Task<ActionResult<CheckinSummaryDto>> GetSummary() =>
        Ok(await _checkin.GetSummaryAsync(this.GetUserId()));
}

/// <summary>首页运营内容块（banner/资讯/视频）</summary>
[ApiController]
[Route("api/content")]
public class ContentController : ControllerBase
{
    private readonly Infrastructure.AppDbContext _db;

    /// <summary>构造</summary>
    public ContentController(Infrastructure.AppDbContext db) => _db = db;

    /// <summary>按类型取启用中的内容块</summary>
    [HttpGet("blocks")]
    public async Task<IActionResult> GetBlocks([FromQuery] string type)
    {
        var list = await _db.ContentBlocks.AsNoTracking()
            .Where(b => b.Enabled && (type == null || b.Type == type))
            .OrderBy(b => b.Sort)
            .ToListAsync();
        return Ok(list);
    }
}
