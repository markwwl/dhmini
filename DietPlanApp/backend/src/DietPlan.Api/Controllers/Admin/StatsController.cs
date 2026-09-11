using DietPlan.Application;
using DietPlan.Domain;
using DietPlan.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DietPlan.Api.Controllers.Admin;

/// <summary>运营统计（打卡 / 用户）</summary>
[Route("api/admin/stats")]
public class AdminStatsController : AdminControllerBase
{
    private readonly AppDbContext _db;

    /// <summary>构造</summary>
    public AdminStatsController(AppDbContext db) => _db = db;

    /// <summary>总览：注册用户数、打卡总人次、今日打卡人次</summary>
    [HttpGet("overview")]
    public async Task<IActionResult> Overview()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return Ok(new
        {
            users = await _db.Users.CountAsync(),
            totalCheckins = await _db.CheckInRecords.CountAsync(),
            todayCheckins = await _db.CheckInRecords.CountAsync(c => c.Date == today)
        });
    }

    /// <summary>按日打卡汇总（含打卡人数与打卡率，支持导出）</summary>
    [HttpGet("checkins")]
    public async Task<IActionResult> Checkins([FromQuery] DateOnly from, [FromQuery] DateOnly to)
    {
        if (to < from) (from, to) = (to, from);

        var records = await _db.CheckInRecords.AsNoTracking()
            .Where(c => c.Date >= from && c.Date <= to)
            .Select(c => new { c.Date, c.UserId })
            .ToListAsync();

        var userCount = await _db.Users.CountAsync();
        var rows = records.GroupBy(r => r.Date)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                date = g.Key,
                checkins = g.Count(),
                users = g.Select(x => x.UserId).Distinct().Count(),
                rate = userCount == 0 ? 0 : Math.Round(100.0 * g.Select(x => x.UserId).Distinct().Count() / userCount, 1)
            });

        return Ok(rows);
    }

    /// <summary>用户列表（最近活跃排序）</summary>
    [HttpGet("users")]
    public async Task<IActionResult> Users([FromQuery] int page = 1, [FromQuery] int size = 20)
    {
        var total = await _db.Users.CountAsync();
        var list = await _db.Users.AsNoTracking()
            .OrderByDescending(u => u.LastActiveAt ?? u.CreatedAt)
            .Skip((page - 1) * size).Take(size)
            .Select(u => new { u.Id, u.OpenId, u.Nickname, u.Avatar, u.CreatedAt, u.LastActiveAt })
            .ToListAsync();
        return Ok(new { total, page, size, items = list });
    }
}
