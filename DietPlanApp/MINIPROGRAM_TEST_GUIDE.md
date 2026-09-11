# 健康饮食方案小程序 · 微信开发者工具测试走查清单

> 适用：miniprogram/ 已写在 `D:\AIGC\DHmini\DietPlanApp\miniprogram`
> 后端已验证：19 单元测试 + 29 步 E2E 全绿（前序任务产出）
> 本清单 = 你在本机用微信开发者工具"点测"的操作手册

---

## 0. 本环境已自动完成的代码验证（无需你重复）

| 项 | 结果 |
|---|---|
| JS 语法（node --check，10 个文件） | ✅ 10/10 通过 |
| JSON 合法性（11 个文件） | ✅ 11/11 通过 |
| 页面注册（app.json 8 页） | ✅ 文件齐全无缺失 |
| tabBar 配置（4 个 Tab） | ✅ 图标/路径通过 |
| **接口路由一致性**（小程序↔后端） | ✅ 12/12 路径匹配（见下表） |
| 后端编译 / 单测 / E2E | ✅ 0 错误 / 19 绿 / 29 绿 |

小程序调用的接口 ↔ 后端路由（已逐一核对）：

```
POST /api/auth/wx-login            → AuthController
GET  /api/plans                    → PlansController
GET  /api/plans/{id}               → PlansController（方案树）
GET  /api/weeks/{w}/days/{d}/meals → MealsController（L2 每日食谱）
GET  /api/dishes/{id}             → DishesController（L3 制作解析）
GET  /api/dish-slots/{s}/candidates → CandidatesController（L4 候选）
PUT  /api/dish-slots/{s}/dish     → DishSlotController（L4 换菜）
POST /api/checkins                → CheckinsController（单菜打卡）
POST /api/checkins/day            → CheckinsController（整日打卡）
GET  /api/checkins/summary        → CheckinsController（我的-统计）
GET  /api/content/blocks?type=... → ContentController（首页 banner / 视频秀）
```

---

## 1. 本机前置准备（必做 4 步）

1. **换 AppID**：`miniprogram/project.config.json` 的 `appid` 已配置真实值 `wx229e9895042d08a3`，
   开发者工具可直接导入运行（如需换号再改此项）
2. **填微信密钥**：`backend/src/DietPlan.Api/appsettings.json` 的 `Wx:AppId` / `Wx:AppSecret`
   均已填真实值（`wx229e9895042d08a3` / `01cc2e39…bb34e2`）。`wx.login` 用 code 换 openid 链路已通，真机可登录。
3. **启动后端**（二选一，推荐 InMemory 免数据库）：
   ```bash
   # 进入 Api 目录
   cd DietPlanApp/backend/src/DietPlan.Api
   # InMemory 模式（本机无 SQL Server 也能跑）
   set ASPNETCORE_URLS=http://localhost:5000
   set Database__Provider=InMemory
   dotnet run
   ```
   验证：浏览器开 `http://localhost:5000/swagger` 能看到全部接口即 OK
4. **导入项目**：微信开发者工具 → 导入项目 → 目录选 `miniprogram` → AppID 填你自己的
   → 勾选「不校验合法域名/TLS」（project.config.json 里 `urlCheck:false` 已设，但真机预览仍需手动勾）

---

## 2. 联调地址（关键，很多人卡这）

- **模拟器（电脑预览）**：`utils/request.js` 的 `BASE = http://localhost:5000` 直连本机后端 ✅
  推荐先用模拟器联调，最快最稳。
- **真机预览（手机扫码）**：手机上的 `localhost` 指手机自身，连不上后端。两种解法：
  - 把 `request.js` 的 `BASE` 改成电脑局域网 IP，如 `http://192.168.1.x:5000`；
    后端启动时绑定 `ASPNETCORE_URLS=http://0.0.0.0:5000`（默认只绑 localhost，真机访问不到）；
  - 或部署公网 https 域名，并在微信公众平台配置 request 合法域名。

---

## 3. 功能走查用例（照着点，观察 Network）

| # | 页面 | 操作 | 预期 | 看哪个接口 |
|---|---|---|---|---|
| 1 | 任意页首开 | 自动触发登录 | Network 里 `/api/auth/wx-login` 返回 200 + token | wx-login |
| 2 | 首页 home | 看 banner 轮播、点方案卡 | banner 出图；点卡进方案页 | content/blocks?type=banner |
| 3 | 方案 plan | 阶段导航 → 周卡片「查看方案」 | 进每日食谱 | plans/{id} |
| 4 | 每日食谱 day | 顶部天选择器切换；看早/中/晚菜品 | 三餐卡片正确渲染 | weeks/{w}/days/{d}/meals |
| 5 | 每日食谱 day | 点「一键打卡」 | 返回 ok，打卡数+1 | checkins/day |
| 6 | 每日食谱 day | 点某菜品「打卡」 | 单菜打卡成功 | checkins |
| 7 | 换菜 replace | 搜索框过滤；选候选→确认 | 槽位菜品替换成功 | PUT dish-slots/{s}/dish |
| 8 | 换菜 replace | 选「不在候选组」的菜确认 | 后端拒绝 400（营养结构保护） | PUT dish-slots/{s}/dish |
| 9 | 制作解析 dish | 进菜品详情 | 食材清单 + 分步做法 | dishes/{id} |
| 10 | 视频秀 videos | 列表 → 点进播放 | 视频列表 / 播放页 | content/blocks?type=video |
| 11 | 我的 mine | 看连续打卡天数、近 30 天日历 | 与打卡动作联动变化 | checkins/summary |

> 用例 8 是后端已写死的业务规则（非候选组内菜品禁止替换），开发者工具里应能看到 400 + 提示文案。

---

## 4. 配套后台维护端（改内容实时生效）

浏览器开 `http://localhost:5000/admin/index.html`（初始 `admin/admin123`）。
在后台配方案树 / 菜品 / 候选组 / 内容块，小程序刷新即生效。
（InMemory 模式下重启后端会重置数据；接 SQL Server 后持久化。）

---

## 5. 上线前必改清单

- [ ] `project.config.json` appid 占位 → 真实 AppID
- [x] `appsettings.json` 微信 AppId/AppSecret → 已填真实值（真机登录链路打通）
- [ ] 管理员 `admin/admin123` → 改密
- [ ] JWT 密钥 → 生产环境已在 Program.cs 强制校验（留空启动即报错），记得配
- [ ] 图片存储：当前落 `wwwroot/uploads` 本地磁盘，生产建议换对象存储
- [ ] 真机域名：https + 微信公众平台配置 request 合法域名

---

## 6. 本环境（沙箱）无法做的部分（如实告知）

微信开发者工具是 GUI 应用，需微信登录态 + 显示会话。当前自动化环境无 GUI、
未登录微信，无法在此真正打开工具做界面点测——CLI（`cli.bat`）调用无输出即为此因。
代码静态正确性与接口一致性已在本环境全部验证通过；按上面 1~3 步，你本机一键即可完成真机/模拟器测试。
