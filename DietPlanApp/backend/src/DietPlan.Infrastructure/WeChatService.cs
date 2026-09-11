using System.Net.Http.Json;
using System.Text.Json;
using DietPlan.Application;

namespace DietPlan.Infrastructure;

/// <summary>微信 code2Session 客户端；未配置 AppId 时返回确定性 stub openid（本地联调用）</summary>
public class WeChatService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly string _appId;
    private readonly string _appSecret;

    /// <summary>构造（AppId/Secret 来自配置 Wx:AppId / Wx:AppSecret）</summary>
    public WeChatService(IHttpClientFactory httpFactory, string appId, string appSecret)
    {
        _httpFactory = httpFactory;
        _appId = appId;
        _appSecret = appSecret;
    }

    /// <summary>用 js_code 换 openid；未配置凭据时返回 dev_{code}（确定性，便于测试）</summary>
    public async Task<string> GetOpenIdAsync(string jsCode)
    {
        if (string.IsNullOrWhiteSpace(_appId) || string.IsNullOrWhiteSpace(_appSecret))
        {
            // 开发模式：不连微信，生成确定性 openid
            return $"dev_{jsCode}";
        }

        var url = $"https://api.weixin.qq.com/sns/jscode2session?appid={_appId}" +
                  $"&secret={_appSecret}&js_code={Uri.EscapeDataString(jsCode)}&grant_type=authorization_code";

        var client = _httpFactory.CreateClient("wx");
        using var resp = await client.GetAsync(url);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        if (json.TryGetProperty("openid", out var openid))
        {
            return openid.GetString() ?? throw new BizException("微信登录失败：openid 为空");
        }

        var errCode = json.TryGetProperty("errcode", out var code) ? code.GetInt32() : -1;
        throw new BizException($"微信登录失败：errcode={errCode}", 401);
    }
}
