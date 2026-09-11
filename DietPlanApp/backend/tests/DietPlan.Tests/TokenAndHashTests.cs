using DietPlan.Infrastructure;
using Xunit;

namespace DietPlan.Tests;

/// <summary>Token 与密码哈希单测</summary>
public class TokenAndHashTests
{
    [Fact]
    public void UserToken_RoundTrip_ContainsClaims()
    {
        var svc = new TokenService("unit_test_key_0123456789_ABCDEFGHIJKL", "DietPlan", "DietPlan");
        var token = svc.CreateUserToken(42, "openid_abc");

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.Equal("42", jwt.Claims.First(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier).Value);
        Assert.Equal("openid_abc", jwt.Claims.First(c => c.Type == System.Security.Claims.ClaimTypes.Name).Value);
        Assert.Equal("user", jwt.Claims.First(c => c.Type == System.Security.Claims.ClaimTypes.Role).Value);
    }

    [Fact]
    public void AdminToken_HasAdminRole()
    {
        var svc = new TokenService("unit_test_key_0123456789_ABCDEFGHIJKL", "DietPlan", "DietPlan");
        var token = svc.CreateAdminToken(1, "admin");

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        Assert.Equal("admin", jwt.Claims.First(c => c.Type == System.Security.Claims.ClaimTypes.Role).Value);
    }

    [Fact]
    public void PasswordHash_IsDeterministicWithSalt()
    {
        var salt = PasswordHasher.NewSalt();
        var h1 = PasswordHasher.Hash(salt, "admin123");
        var h2 = PasswordHasher.Hash(salt, "admin123");

        Assert.Equal(h1, h2);
        Assert.NotEqual(PasswordHasher.Hash(PasswordHasher.NewSalt(), "admin123"), h1);
        Assert.Equal(64, h1.Length); // SHA256 hex
    }
}
