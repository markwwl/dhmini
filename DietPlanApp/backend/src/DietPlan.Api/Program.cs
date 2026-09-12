using System.Text;
using DietPlan.Infrastructure;
using DietPlan.Infrastructure.Services;
using DietPlan.Domain;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Cryptography;
using DietPlan.Application;

var builder = WebApplication.CreateBuilder(args);

// ---------- 真机预览/局域网联调：监听所有网卡，使手机能访问本机后端（默认只听 localhost） ----------
builder.WebHost.UseUrls("http://0.0.0.0:24661");

// ---------- EF Core：默认 SQL Server；Database:Provider=InMemory 供本地冒烟/E2E（无库环境） ----------
var dbProvider = builder.Configuration["Database:Provider"] ?? "SqlServer";
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    if (string.Equals(dbProvider, "InMemory", StringComparison.OrdinalIgnoreCase))
    {
        opt.UseInMemoryDatabase("DietPlanSmoke");
    }
    else
    {
        opt.UseSqlServer(builder.Configuration.GetConnectionString("Default"));
    }
});

// ---------- 业务服务 ----------
builder.Services.AddHttpClient("wx");
// 本地联调开关：开启后 wx-login 直接落到演示账号（AppDbContext 里的 demo_openid_0001），
// 使小程序手工测试时用户态数据（打卡/统计）也有内容。生产环境禁止开启。
var useDemoLogin = builder.Configuration.GetValue<bool>("Wx:UseDemoLogin");
if (useDemoLogin && builder.Environment.IsProduction())
{
    throw new InvalidOperationException("Wx:UseDemoLogin 不得在生产环境开启");
}
builder.Services.AddScoped<WeChatService>(sp =>
{
    var http = sp.GetRequiredService<IHttpClientFactory>();
    return new WeChatService(
        http,
        builder.Configuration["Wx:AppId"] ?? string.Empty,
        builder.Configuration["Wx:AppSecret"] ?? string.Empty,
        useDemoLogin,
        builder.Configuration["Wx:DemoOpenId"] ?? "demo_openid_0001");
});
var jwtKey = builder.Configuration["Jwt:Key"] ?? string.Empty;
// 安全：生产环境必须显式配置强随机 JWT 密钥，否则拒绝启动（防伪造 token）
if (string.IsNullOrWhiteSpace(jwtKey))
{
    if (!builder.Environment.IsDevelopment())
    {
        throw new InvalidOperationException("缺少 Jwt:Key 配置：生产环境必须提供强随机密钥");
    }
    jwtKey = "DietPlanDevKey_0123456789_ABCDEFGHIJKL_0123456789";
}
builder.Services.AddSingleton(new TokenService(
    jwtKey,
    builder.Configuration["Jwt:Issuer"] ?? "DietPlan",
    builder.Configuration["Jwt:Audience"] ?? "DietPlan"));
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ContentService>();
builder.Services.AddScoped<CheckinService>();

// ---------- 鉴权：JWT Bearer，user/admin 双角色 ----------
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "DietPlan",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "DietPlan",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization();

// ---------- 控制器 + Swagger + CORS + 静态文件 ----------
builder.Services.AddControllers(o =>
{
    // BizException → 对应 HTTP 状态码（400/404），避免业务异常变 500
    o.Filters.Add<DietPlan.Api.BizExceptionFilter>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "健康饮食方案小程序 API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "粘贴登录返回的 token（无需 Bearer 前缀）"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// ---------- 启动初始化：建库 + 种子后台账号 admin/admin123 ----------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    // 演示数据：InMemory（本地无库）或在 Development 环境下的空库（如 LocalDB）都灌，
    // 保证"起服务就有数据可浏览"；生产环境绝不灌。Seed 内部幂等（库中已有方案则跳过）。
    if (string.Equals(dbProvider, "InMemory", StringComparison.OrdinalIgnoreCase) || app.Environment.IsDevelopment())
    {
        DietPlan.Api.DemoDataSeeder.Seed(db);
    }

    if (!db.AdminUsers.Any())
    {
        var salt = PasswordHasher.NewSalt();
        db.AdminUsers.Add(new AdminUser
        {
            Username = "admin",
            Salt = salt,
            PasswordHash = PasswordHasher.Hash(salt, "admin123"),
            DisplayName = "系统管理员"
        });
        db.SaveChanges();
    }
}

app.UseStaticFiles();   // wwwroot/uploads 图片直出
app.UseCors();

// 全局异常处理：BizException 按其状态码返回，其余 500
app.UseExceptionHandler(a => a.Run(async context =>
{
    var ex = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
    var code = ex is BizException b ? b.StatusCode : 500;
    context.Response.StatusCode = code;
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsJsonAsync(new { error = ex?.Message ?? "服务器内部错误" });
}));

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

/// <summary>供集成测试使用的 Program 入口（partial 暴露）</summary>
public partial class Program { }
