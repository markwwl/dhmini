using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace DietPlan.Infrastructure;

/// <summary>JWT 签发服务（C 端用户 + 后台管理员双角色）</summary>
public class TokenService
{
    private readonly string _key;
    private readonly string _issuer;
    private readonly string _audience;

    /// <summary>构造（密钥/签发方来自配置 Jwt:Key / Jwt:Issuer / Jwt:Audience）</summary>
    public TokenService(string key, string issuer, string audience)
    {
        _key = key;
        _issuer = issuer;
        _audience = audience;
    }

    /// <summary>为 C 端用户签发 token（role=user）</summary>
    public string CreateUserToken(int userId, string openId)
    {
        return CreateToken(userId, openId, "user", TimeSpan.FromDays(30));
    }

    /// <summary>为后台管理员签发 token（role=admin）</summary>
    public string CreateAdminToken(int userId, string username)
    {
        return CreateToken(userId, username, "admin", TimeSpan.FromHours(12));
    }

    private string CreateToken(int userId, string name, string role, TimeSpan lifetime)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, name),
            new Claim(ClaimTypes.Role, role)
        };

        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(lifetime),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

/// <summary>密码哈希工具：SHA256(salt + password) 十六进制（与集团既有 Sha256Hex 规范一致）</summary>
public static class PasswordHasher
{
    /// <summary>生成随机盐</summary>
    public static string NewSalt()
    {
        var bytes = RandomNumberGenerator.GetBytes(16);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>计算哈希</summary>
    public static string Hash(string salt, string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(salt + password));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
