# 健康饮食方案小程序（dhmini）

微信原生小程序 + ASP.NET Core 8 后端 + Vue3 管理后台。小程序提供「四级方案导航（L1–L4）」查看饮食方案、每日食谱、菜品详情、替换建议、教学视频与打卡；后端提供完整维护 API 与管理后台，所有方案内容、图片均可自行维护。

> 本项目所有微信 AppSecret、JWT 密钥、数据库连接串均**不入库**（写入 `.gitignore`），仅提供 `appsettings.example.json` 模板。

## 技术栈

| 层 | 技术 |
| --- | --- |
| 小程序端 | 微信原生小程序（8 个页面） |
| 后端 API | ASP.NET Core 8 / Clean Architecture（Domain·Application·Infrastructure·Api） |
| 数据 | EF Core 8 + SQL Server（本地无库时用内置 InMemory 跑通全链路） |
| 鉴权 | JWT Bearer（user / admin 双角色），微信 `wx.login` 静默登录换 openid |
| 管理后台 | Vue3 + Element Plus（CDN 免编译 SPA，由 API 的 `wwwroot/admin` 直接托管） |

## 目录结构

```
DietPlanApp/
├── backend/                # ASP.NET Core 8 后端（Clean Architecture）
│   └── src/DietPlan.Api/   # 启动工程（监听 0.0.0.0:24661）
│       └── wwwroot/admin/  # 管理后台 SPA（Vue3 + Element Plus CDN）
├── miniprogram/            # 微信原生小程序（用微信开发者工具导入此目录）
└── tests/                  # xUnit 单元测试
output/                     # 需求文档等交付物
```

## 前置条件

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- 微信开发者工具（导入 `miniprogram` 目录）
- SQL Server（可选）：本地无库时用 `Database:Provider=InMemory` 即可跑通全部接口与测试

## 快速开始

### 1. 配置密钥

复制模板，填入你自己的小程序 AppID / AppSecret 与 JWT 密钥：

```bash
cp appsettings.example.json DietPlanApp/backend/src/DietPlan.Api/appsettings.json
```

编辑 `appsettings.json`：

```json
{
  "Jwt": { "Key": "强随机密钥(生产必填)" },
  "Wx":  { "AppId": "wx你的AppID", "AppSecret": "你的AppSecret" }
}
```

生成强随机 JWT 密钥：`python -c "import secrets;print(secrets.token_hex(32))"`

### 2. 启动后端

```bash
cd DietPlanApp/backend/src/DietPlan.Api
dotnet run                      # 默认监听 http://0.0.0.0:24661
# 本地无 SQL Server 时，用 InMemory 跑：
# set Database:Provider=InMemory && dotnet run
```

- Swagger 文档：`http://localhost:24661/swagger`
- 管理后台：`http://localhost:24661/admin/`
- 图片直出：`http://localhost:24661/uploads/...`

> 首次启动会自动建库并植入后台账号 **admin / admin123**（上线前务必修改）。

### 3. 导入小程序

1. 微信开发者工具 → 导入项目 → 目录选 `DietPlanApp/miniprogram`
2. `project.config.json` 的 `appid` 已是真实 AppID（如为占位请改为你自己的）
3. 模拟器默认 `urlCheck:false`，可直接联本地 `http://localhost:24661`

### 4. 真机联调

手机无法通过 `localhost` 访问你电脑，需三步：

1. `miniprogram/utils/request.js` 的 `BASE` 改为电脑局域网 IP，如 `http://192.168.3.100:24661`
2. 后端已 `UseUrls("http://0.0.0.0:24661")` 监听所有网卡（程序内已配，无需改动）
3. 开发者工具真机预览时勾选「不校验合法域名 / TLS」，并在 Windows 防火墙放行 TCP 24661

## 运行测试

```bash
cd DietPlanApp/backend
dotnet test                     # 19 个 xUnit 单元用例（InMemory 提供）
```

端到端冒烟脚本（`e2e_smoke.py`）以 InMemory 启动 API 后调用 29 个接口校验，详见 `DietPlanApp/MINIPROGRAM_TEST_GUIDE.md`。

## 安全提示

- `appsettings.json` 含真实密钥，**已 gitignore，切勿提交到仓库**；生产环境建议用环境变量或 User-Secrets 覆盖。
- 默认后台账号 `admin/admin123` 上线前必须修改。
- 生产环境 `Jwt:Key` 缺失会直接拒绝启动（防伪造 token）。

## 部署说明

Web 发布：`dotnet publish -c Release`，将 `wwwroot/admin` 一并产出即可托管管理后台。反向代理（nginx 等）将 `/api` 转发到后端 24661 端口。
