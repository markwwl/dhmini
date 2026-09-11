using DietPlan.Application;
using DietPlan.Domain;
using Microsoft.EntityFrameworkCore;

namespace DietPlan.Infrastructure.Services;

/// <summary>登录服务：微信静默登录（用户）+ 账号密码登录（管理员）</summary>
public class AuthService
{
    private readonly AppDbContext _db;
    private readonly WeChatService _wechat;
    private readonly TokenService _tokens;

    /// <summary>构造</summary>
    public AuthService(AppDbContext db, WeChatService wechat, TokenService tokens)
    {
        _db = db;
        _wechat = wechat;
        _tokens = tokens;
    }

    /// <summary>微信登录：code 换 openid，注册或复用既有用户，签发 JWT</summary>
    public async Task<(string Token, int UserId, string Nickname)> WxLoginAsync(string code)
    {
        var openId = await _wechat.GetOpenIdAsync(code);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.OpenId == openId);
        if (user == null)
        {
            user = new User
            {
                OpenId = openId,
                Nickname = "健康饮食用户",
                Avatar = string.Empty,
                LastActiveAt = DateTime.Now
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }
        else
        {
            user.LastActiveAt = DateTime.Now;
            await _db.SaveChangesAsync();
        }

        return (_tokens.CreateUserToken(user.Id, user.OpenId), user.Id, user.Nickname);
    }

    /// <summary>后台登录：用户名 + 密码（Sha256Hex），签发管理员 JWT</summary>
    public async Task<string> AdminLoginAsync(string username, string password)
    {
        var admin = await _db.AdminUsers.FirstOrDefaultAsync(a => a.Username == username)
            ?? throw new BizException("用户名或密码错误", 401);

        var hash = PasswordHasher.Hash(admin.Salt, password);
        if (hash != admin.PasswordHash)
        {
            throw new BizException("用户名或密码错误", 401);
        }

        return _tokens.CreateAdminToken(admin.Id, admin.Username);
    }
}
