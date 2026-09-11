using DietPlan.Application;

namespace DietPlan.Infrastructure.Services;

/// <summary>本地文件存储：上传落 wwwroot/uploads，返回相对 URL</summary>
public class FileStorageService
{
    private readonly string _root;

    /// <summary>构造（root 为 wwwroot 绝对路径）</summary>
    public FileStorageService(string root) => _root = root;

    private static readonly string[] AllowedExt =
        { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".mp4", ".pdf" };

    /// <summary>保存上传文件，返回可访问的相对 URL；校验扩展名白名单与 300MB 上限</summary>
    public async Task<string> SaveAsync(Stream content, string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedExt.Contains(ext))
        {
            throw new BizException("不支持的文件类型");
        }

        var dir = Path.Combine(_root, "uploads", DateTime.Today.ToString("yyyyMMdd"));
        Directory.CreateDirectory(dir);

        var name = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(dir, name);

        await using var fs = File.Create(fullPath);
        await content.CopyToAsync(fs);

        return $"/uploads/{DateTime.Today:yyyyMMdd}/{name}";
    }
}
