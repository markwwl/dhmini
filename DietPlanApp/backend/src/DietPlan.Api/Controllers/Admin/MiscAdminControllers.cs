using DietPlan.Application;
using DietPlan.Domain;
using DietPlan.Infrastructure;
using DietPlan.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DietPlan.Api.Controllers.Admin;

/// <summary>菜品管理</summary>
[Route("api/admin/dishes")]
public class AdminDishesController : AdminControllerBase
{
    private readonly AppDbContext _db;

    /// <summary>构造</summary>
    public AdminDishesController(AppDbContext db) => _db = db;

    /// <summary>菜品列表（关键字/分类/状态过滤）</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? keyword, [FromQuery] string? category, [FromQuery] DishStatus? status)
    {
        var query = _db.Dishes.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(d => d.Name.Contains(keyword) || d.IngredientsJson.Contains(keyword));
        }
        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(d => d.Category == category);
        }
        if (status.HasValue)
        {
            query = query.Where(d => d.Status == status);
        }

        return Ok(await query.OrderByDescending(d => d.Id).Take(500).ToListAsync());
    }

    /// <summary>菜品详情</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var dish = await _db.Dishes.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id)
            ?? throw new BizException("菜品不存在", 404);
        return Ok(dish);
    }

    /// <summary>新建菜品</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Dish dish)
    {
        dish.Id = 0;
        _db.Dishes.Add(dish);
        await _db.SaveChangesAsync();
        return Ok(dish);
    }

    /// <summary>更新菜品（含食材/步骤/图片 JSON）</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Dish input)
    {
        var dish = await _db.Dishes.FindAsync(id) ?? throw new BizException("菜品不存在", 404);
        dish.Name = input.Name;
        dish.Subtitle = input.Subtitle;
        dish.Category = input.Category;
        dish.ImagesJson = input.ImagesJson;
        dish.IngredientsJson = input.IngredientsJson;
        dish.StepsJson = input.StepsJson;
        dish.Status = input.Status;
        await _db.SaveChangesAsync();
        return Ok(dish);
    }

    /// <summary>删除菜品（被方案槽位或候选组引用时拒绝）</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (await _db.DishSlotItems.AnyAsync(i => i.DishId == id))
        {
            throw new BizException("该菜品已被方案引用，请先从方案中移除");
        }
        if (await _db.CandidateGroupItems.AnyAsync(i => i.DishId == id))
        {
            throw new BizException("该菜品在候选组中，请先移出候选组");
        }

        var dish = await _db.Dishes.FindAsync(id) ?? throw new BizException("菜品不存在", 404);
        _db.Dishes.Remove(dish);
        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }
}

/// <summary>候选组管理</summary>
[Route("api/admin/candidate-groups")]
public class AdminCandidateGroupsController : AdminControllerBase
{
    private readonly AppDbContext _db;

    /// <summary>构造</summary>
    public AdminCandidateGroupsController(AppDbContext db) => _db = db;

    /// <summary>候选组列表（含成员）</summary>
    [HttpGet]
    public async Task<IActionResult> List() =>
        Ok(await _db.CandidateGroups.AsNoTracking()
            .Include(g => g.Items)
            .OrderBy(g => g.Id)
            .ToListAsync());

    /// <summary>新建候选组（先存组拿 Id，再挂成员，避免依赖 EF 关系修正）</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DishCandidateGroup group)
    {
        var items = group.Items.Select(i => new CandidateGroupItem { DishId = i.DishId }).ToList();
        group.Id = 0;
        group.Items = new List<CandidateGroupItem>();
        _db.CandidateGroups.Add(group);
        await _db.SaveChangesAsync();

        foreach (var item in items)
        {
            item.GroupId = group.Id;
            _db.CandidateGroupItems.Add(item);
        }
        await _db.SaveChangesAsync();
        group.Items = items;
        return Ok(group);
    }

    /// <summary>更新候选组（全量覆盖成员）</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] DishCandidateGroup input)
    {
        var group = await _db.CandidateGroups.Include(g => g.Items)
            .FirstOrDefaultAsync(g => g.Id == id) ?? throw new BizException("候选组不存在", 404);

        group.Name = input.Name;
        group.Items = input.Items.Select(i => new CandidateGroupItem { GroupId = id, DishId = i.DishId }).ToList();
        await _db.SaveChangesAsync();
        return Ok(group);
    }

    /// <summary>删除候选组（被槽位引用时拒绝）</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (await _db.DishSlotItems.AnyAsync(i => i.CandidateGroupId == id))
        {
            throw new BizException("该候选组被菜品槽位引用，请先解除引用");
        }

        var group = await _db.CandidateGroups.FindAsync(id) ?? throw new BizException("候选组不存在", 404);
        _db.CandidateGroups.Remove(group);
        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }
}

/// <summary>食材管理</summary>
[Route("api/admin/ingredients")]
public class AdminIngredientsController : AdminControllerBase
{
    private readonly AppDbContext _db;

    /// <summary>构造</summary>
    public AdminIngredientsController(AppDbContext db) => _db = db;

    /// <summary>食材列表</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? keyword) =>
        Ok(await _db.Ingredients.AsNoTracking()
            .Where(i => keyword == null || i.Name.Contains(keyword))
            .OrderBy(i => i.Id)
            .Take(500)
            .ToListAsync());

    /// <summary>新建食材</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Ingredient ingredient)
    {
        ingredient.Id = 0;
        _db.Ingredients.Add(ingredient);
        await _db.SaveChangesAsync();
        return Ok(ingredient);
    }

    /// <summary>更新食材</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Ingredient input)
    {
        var item = await _db.Ingredients.FindAsync(id) ?? throw new BizException("食材不存在", 404);
        item.Name = input.Name;
        item.Unit = input.Unit;
        item.Image = input.Image;
        await _db.SaveChangesAsync();
        return Ok(item);
    }

    /// <summary>删除食材</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Ingredients.FindAsync(id) ?? throw new BizException("食材不存在", 404);
        _db.Ingredients.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }
}

/// <summary>运营内容块管理（banner/资讯/视频）</summary>
[Route("api/admin/content-blocks")]
public class AdminContentBlocksController : AdminControllerBase
{
    private readonly AppDbContext _db;

    /// <summary>构造</summary>
    public AdminContentBlocksController(AppDbContext db) => _db = db;

    /// <summary>内容块列表</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? type) =>
        Ok(await _db.ContentBlocks.AsNoTracking()
            .Where(b => type == null || b.Type == type)
            .OrderBy(b => b.Sort)
            .ToListAsync());

    /// <summary>新建内容块</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ContentBlock block)
    {
        block.Id = 0;
        _db.ContentBlocks.Add(block);
        await _db.SaveChangesAsync();
        return Ok(block);
    }

    /// <summary>更新内容块</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ContentBlock input)
    {
        var block = await _db.ContentBlocks.FindAsync(id) ?? throw new BizException("内容块不存在", 404);
        block.Type = input.Type;
        block.Title = input.Title;
        block.Image = input.Image;
        block.Url = input.Url;
        block.Sort = input.Sort;
        block.Enabled = input.Enabled;
        await _db.SaveChangesAsync();
        return Ok(block);
    }

    /// <summary>删除内容块</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var block = await _db.ContentBlocks.FindAsync(id) ?? throw new BizException("内容块不存在", 404);
        _db.ContentBlocks.Remove(block);
        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }
}

/// <summary>文件上传（图片/视频，落 wwwroot/uploads）</summary>
[Route("api/admin/upload")]
public class AdminUploadController : AdminControllerBase
{
    private readonly FileStorageService _storage;

    /// <summary>构造</summary>
    public AdminUploadController(FileStorageService storage) => _storage = storage;

    /// <summary>上传文件，返回可访问 URL</summary>
    [HttpPost]
    [RequestSizeLimit(300 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new BizException("文件为空");
        }

        await using var stream = file.OpenReadStream();
        var url = await _storage.SaveAsync(stream, file.FileName);
        return Ok(new { url });
    }
}
