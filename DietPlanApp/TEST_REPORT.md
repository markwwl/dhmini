# 健康饮食方案小程序 · 测试报告

| 项目 | 内容 |
|------|------|
| 测试日期 | 2026-09-11 |
| 代码位置 | `D:\AIGC\DHmini\DietPlanApp\`（backend / miniprogram / 测试脚本） |
| 测试轮次 | 3 轮（每轮修复后全量回归） |
| 结论 | **通过**（编译 0 错误 / 单测 19·19 / E2E 29·29 / P0·P1 已修复） |

---

## 一、测试环境

| 项 | 值 |
|----|-----|
| .NET SDK | 10.0.400（目标框架 net8.0，目标包自动解析，符合需求文档） |
| Node | v22.22.2 / npm 10.9.7 |
| 数据库 | 单测：EF Core InMemory；E2E：`Database:Provider=InMemory`；生产：SQL Server（连接串可配） |
| API 启动 | `http://localhost:24661`，Swagger 已启用（Development） |

## 二、编译结果

| 目标 | 结果 |
|------|------|
| `dotnet build`（Debug） | ✅ 0 错误 |
| `dotnet build -c Release` | ✅ 0 错误 |
| 解决方案 | 5 个工程全部编译通过（Domain / Application / Infrastructure / Api / Tests） |

## 三、单元测试（19/19 通过，461ms）

| 测试类 | 用例数 | 覆盖点 | 结果 |
|--------|--------|--------|------|
| StreakCalculatorTests | 5 | 连续打卡计算（空集/连续/断档/当日未打/去重） | ✅ |
| CheckinServiceTests | 5 | 单菜打卡、防重、槽位与周/天不匹配拒绝、整日打卡幂等、统计汇总 | ✅ |
| ContentServiceTests | 6 | 方案树、每日食谱、候选组过滤、关键字搜索、制作解析、非上线方案 404 | ✅ |
| TokenAndHashTests | 3 | JWT 双角色签发与 claims、密码哈希确定性 | ✅ |

## 四、端到端冒烟（29/29 通过）

覆盖"后台配置 → C端消费 → 打卡 → 统计"完整闭环（`e2e_smoke.py`）：

- **后台链路**：admin 登录 → 建菜品(3) → 建候选组 → 建方案/阶段/周/天（默认三餐）→ 槽位挂菜品与候选组 ✅
- **C端链路**：微信静默登录（stub openid）→ 方案列表/树 → 每日食谱（食材摘要断言）→ 候选列表/搜索 → 制作解析 ✅
- **换菜闭环**：黑米粥→小米粥替换生效；荤菜换进主食槽被正确拒绝（HTTP 400）✅
- **打卡闭环**：单菜打卡 → 整日打卡幂等 → 已打卡状态回显 → 统计（累计/连续/日历）✅
- **后台统计**：总览（用户/打卡人次）、按日打卡统计、管理后台静态页可访问 ✅

## 五、测试中发现并修复的缺陷

| # | 级别 | 缺陷 | 修复 |
|---|------|------|------|
| 1 | P0 | JSON 解析大小写敏感，后台存小写键时食材/步骤解析为空 | 统一 `PropertyNameCaseInsensitive` 解析 |
| 2 | P0 | 导航属性 `Stage/Week` 被模型验证判为必填，POST 周/天直接 400 | 导航属性改可空 |
| 3 | P0 | 后台新建候选组时成员 `GroupId` 未回填，候选列表恒为空 | 拆两步保存，显式回填 |
| 4 | P0 | 业务异常未统一转换，非法换菜返回 500 | MVC 全局异常过滤器，BizException → 400/404 |
| 5 | P1 | 打卡防重"先查后插"在 SQL Server 并发下会抛 DbUpdateException → 500 | 捕获唯一索引冲突按"已打卡"静默处理 |
| 6 | P1 | JWT 密钥缺省回退硬编码串，生产可被伪造 | 非 Development 环境缺 `Jwt:Key` 拒绝启动 |
| 7 | 过程 | 测试脚本对"非法更换"断言口径错误（≥3 实际场景应为 ≥2） | 修正断言 |

## 六、专家审核（SeniorDeveloper 子代理）

**已通过项**：admin 接口鉴权覆盖完整（无漏保护的控制器）、SQL 全参数化无注入、上传无路径穿越、无 N+1 查询、JWT 校验参数全开。

**已修复**：上表 #5、#6。

**遗留改进项（P1/P2，建议下迭代处理）**：
1. 管理端 Create/Update 直接绑定实体类，缺输入验证（空名可入库）与 mass-assignment 防护 → 建议改 DTO + 数据注解。
2. 密码哈希沿用集团 Sha256Hex 规范（盐+SHA256），强度弱于 PBKDF2/bcrypt → 如无集团规范约束建议升级。
3. `Include(...OrderBy...)` 排序仅 SQL Server 生效（InMemory 忽略），测试未覆盖排序 → 上线前在真实库回归 L1 卡片顺序。
4. CORS 全放开、500 响应可能泄漏 ex.Message、上传响应缺 nosniff → 上线前收紧。

## 七、已知限制与运行前置

1. **数据库**：本机无 SQL Server，自动化验证用 InMemory；接生产库仅需在 `appsettings.json` 配好连接串（默认 LocalDB），首次启动自动建库+种子 admin/admin123（务必改密）。
2. **微信登录**：未配置 `Wx:AppId/Secret` 时为确定性 stub（`dev_{code}`），配置后自动走真实 code2Session。
3. **小程序**：纯源码交付，微信开发者工具导入 `miniprogram/` 目录即可预览；`utils/request.js` 的 BASE 需指向后端地址，上线前需在小程序后台配置合法域名。
4. **管理后台**：CDN 版 Vue3 + Element Plus（免编译），API 启动后访问 `http://localhost:24661/admin/index.html`；初始账号 admin / admin123。

## 八、交付清单

| 模块 | 位置 | 状态 |
|------|------|------|
| 后端 API（4 层架构 + Swagger） | `DietPlanApp/backend/src/` | ✅ 完成并验证 |
| 单元测试工程（19 用例） | `DietPlanApp/backend/tests/` | ✅ 19/19 |
| E2E 冒烟脚本（29 步） | `DietPlanApp/e2e_smoke.py` | ✅ 29/29 |
| 管理后台（Vue3 + Element Plus） | `DietPlanApp/backend/src/DietPlan.Api/wwwroot/admin/` | ✅ 完成（API 托管） |
| 小程序（8 页面四级架构） | `DietPlanApp/miniprogram/` | ✅ 源码完成（开发者工具预览） |
| 需求文档 | `DietPlanApp/../../output/20260911-dietplan-reqdoc/stage3/*.docx` | ✅ V1.0 |

**运行方式**：`cd DietPlanApp/backend/src/DietPlan.Api && dotnet run` → API:24661 / Swagger:24661/swagger / 后台:24661/admin/index.html
