using System.Net.Http.Json;
using System.Text.Json;
using DietPlan.Application;

namespace DietPlan.Infrastructure;

/// <summary>微信 code2Session 客户端；本地联调可切到固定演示账号，未配置 AppId 时返回确定性 stub openid</summary>
public class WeChatService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly string _appId;
    private readonly string _appSecret;
    private readonly bool _useDemoLogin;
    private readonly string _demoOpenId;

    /// <summary>构造（AppId/Secret 来自配置 Wx:AppId / Wx:AppSecret；UseDemoLogin 仅供本地联调）</summary>
    public WeChatService(IHttpClientFactory httpFactory, string appId, string appSecret,
        bool useDemoLogin = false, string demoOpenId = "demo_openid_0001")
    {
        _httpFactory = httpFactory;
        _appId = appId;
        _appSecret = appSecret;
        _useDemoLogin = useDemoLogin;
        _demoOpenId = demoOpenId;
    }

    /// <summary>用 js_code 换 openid；未配置凭据时返回 dev_{code}（确定性，便于测试）</summary>
    public async Task<string> GetOpenIdAsync(string jsCode)
    {
        // 本地联调开关（Wx:UseDemoLogin）：跳过微信，直接落到已灌演示数据的账号，
        // 使小程序手工测试时"我的"页打卡/统计也有数据；生产环境由 Program.cs 强制禁止开启。
        if (_useDemoLogin)
        {
            return _demoOpenId;
        }

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
