using System.Text.Encodings.Web;
using System.Text.Json;
using DietPlan.Domain;
using DietPlan.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DietPlan.Api;

/// <summary>
/// 演示 / 测试数据种子（本地无库调试与开发环境使用，生产环境绝不执行）。
/// 目标：让后端「所有可查询的接口」都有数据——
///   方案(含非上线) / 阶段 / 周 / 天 / 餐次槽位 / 菜品(含草稿下架) / 食材 /
///   候选组 / 内容块(含停用) / 用户 / 打卡记录 / 后台统计。
/// 幂等：库中已有方案则直接跳过，重复启动不会灌重复数据。
/// </summary>
public static class DemoDataSeeder
{
    /// <summary>图片种子前缀（仅用于拼接占位图 URL）</summary>
    private const string P = "dietplan";

    /// <summary>演示视频地址（公开示例 mp4）</summary>
    private const string DemoVideo = "https://www.w3schools.com/html/mov_bbb.mp4";

    /// <summary>
    /// JSON 序列化选项：默认编码器会把中文转义成 \u9E21\u86CB 这类转义序列，
    /// 灌进库后后台「食材/步骤 JSON」列显示一堆转义字符，故改用宽松编码器输出原文。
    /// </summary>
    private static readonly JsonSerializerOptions JsonOpt = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// 占位图：微信 image 组件不受 request 合法域名限制，https 图片可直接展示。
    /// 对中文种子做 URL 编码，保证地址合法。
    /// </summary>
    private static string Img(string seed, int w = 640, int h = 400)
        => $"https://picsum.photos/seed/{Uri.EscapeDataString($"{P}-{seed}")}/{w}/{h}";

    /// <summary>图片列表 JSON（Dish.ImagesJson）</summary>
    private static string ImgList(params string[] seeds)
        => JsonSerializer.Serialize(seeds.Select(s => Img(s)), JsonOpt);

    /// <summary>食材清单 JSON（Dish.IngredientsJson，字段与 IngredientItemDto 对齐：name/amount/image）</summary>
    private static string IngList(params (string Name, string Amount)[] items)
        => JsonSerializer.Serialize(items.Select(i => new
        {
            name = i.Name,
            amount = i.Amount,
            image = Img("ing-" + i.Name, 200, 200)
        }), JsonOpt);

    /// <summary>制作步骤 JSON（Dish.StepsJson，字段与 StepDto 对齐：text/image）</summary>
    private static string StepList(params string[] texts)
        => JsonSerializer.Serialize(texts.Select((t, i) => new { text = t, image = Img("step-" + i, 640, 360) }), JsonOpt);

    /// <summary>灌入演示数据（幂等）</summary>
    public static void Seed(AppDbContext db)
    {
        if (db.Plans.Any()) return;

        // ================= 1. 食材主数据 =================
        db.Ingredients.AddRange(
            new Ingredient { Name = "鸡胸肉", Unit = "g", Image = Img("ing-鸡胸肉", 200, 200) },
            new Ingredient { Name = "鸡腿肉", Unit = "g", Image = Img("ing-鸡腿肉", 200, 200) },
            new Ingredient { Name = "牛柳", Unit = "g", Image = Img("ing-牛柳", 200, 200) },
            new Ingredient { Name = "三文鱼", Unit = "g", Image = Img("ing-三文鱼", 200, 200) },
            new Ingredient { Name = "龙利鱼", Unit = "g", Image = Img("ing-龙利鱼", 200, 200) },
            new Ingredient { Name = "巴沙鱼", Unit = "g", Image = Img("ing-巴沙鱼", 200, 200) },
            new Ingredient { Name = "鲜虾", Unit = "g", Image = Img("ing-鲜虾", 200, 200) },
            new Ingredient { Name = "鸡蛋", Unit = "个", Image = Img("ing-鸡蛋", 200, 200) },
            new Ingredient { Name = "豆腐", Unit = "g", Image = Img("ing-豆腐", 200, 200) },
            new Ingredient { Name = "西兰花", Unit = "g", Image = Img("ing-西兰花", 200, 200) },
            new Ingredient { Name = "菠菜", Unit = "g", Image = Img("ing-菠菜", 200, 200) },
            new Ingredient { Name = "秋葵", Unit = "g", Image = Img("ing-秋葵", 200, 200) },
            new Ingredient { Name = "芦笋", Unit = "g", Image = Img("ing-芦笋", 200, 200) },
            new Ingredient { Name = "口蘑", Unit = "g", Image = Img("ing-口蘑", 200, 200) },
            new Ingredient { Name = "黑木耳", Unit = "g", Image = Img("ing-黑木耳", 200, 200) },
            new Ingredient { Name = "娃娃菜", Unit = "g", Image = Img("ing-娃娃菜", 200, 200) },
            new Ingredient { Name = "冬瓜", Unit = "g", Image = Img("ing-冬瓜", 200, 200) },
            new Ingredient { Name = "番茄", Unit = "个", Image = Img("ing-番茄", 200, 200) },
            new Ingredient { Name = "南瓜", Unit = "g", Image = Img("ing-南瓜", 200, 200) },
            new Ingredient { Name = "玉米", Unit = "g", Image = Img("ing-玉米", 200, 200) },
            new Ingredient { Name = "红薯", Unit = "g", Image = Img("ing-红薯", 200, 200) },
            new Ingredient { Name = "紫薯", Unit = "g", Image = Img("ing-紫薯", 200, 200) },
            new Ingredient { Name = "山药", Unit = "g", Image = Img("ing-山药", 200, 200) },
            new Ingredient { Name = "糙米", Unit = "g", Image = Img("ing-糙米", 200, 200) },
            new Ingredient { Name = "紫米", Unit = "g", Image = Img("ing-紫米", 200, 200) },
            new Ingredient { Name = "小米", Unit = "g", Image = Img("ing-小米", 200, 200) },
            new Ingredient { Name = "藜麦", Unit = "g", Image = Img("ing-藜麦", 200, 200) },
            new Ingredient { Name = "荞麦面", Unit = "g", Image = Img("ing-荞麦面", 200, 200) },
            new Ingredient { Name = "燕麦", Unit = "g", Image = Img("ing-燕麦", 200, 200) },
            new Ingredient { Name = "牛奶", Unit = "ml", Image = Img("ing-牛奶", 200, 200) },
            new Ingredient { Name = "无糖豆浆", Unit = "ml", Image = Img("ing-无糖豆浆", 200, 200) },
            new Ingredient { Name = "希腊酸奶", Unit = "g", Image = Img("ing-希腊酸奶", 200, 200) },
            new Ingredient { Name = "蓝莓", Unit = "g", Image = Img("ing-蓝莓", 200, 200) },
            new Ingredient { Name = "香蕉", Unit = "个", Image = Img("ing-香蕉", 200, 200) },
            new Ingredient { Name = "牛油果", Unit = "个", Image = Img("ing-牛油果", 200, 200) },
            new Ingredient { Name = "综合坚果", Unit = "g", Image = Img("ing-综合坚果", 200, 200) }
        );

        // ================= 2. 菜品（含草稿 / 下架，保证后台状态筛选有数据） =================
        var dishes = new List<Dish>();
        Dish NewDish(string name, string subtitle, string category,
            (string Name, string Amount)[] ings, string[] steps, DishStatus status = DishStatus.Published)
        {
            var d = new Dish
            {
                Name = name,
                Subtitle = subtitle,
                Category = category,
                ImagesJson = ImgList("dish-" + name + "-a", "dish-" + name + "-b"),
                IngredientsJson = IngList(ings),
                StepsJson = StepList(steps),
                Status = status
            };
            dishes.Add(d);
            return d;
        }

        // ---- 早餐（8）----
        var dOat = NewDish("燕麦牛奶碗", "暖胃高纤 · 慢碳供能", "早餐",
            new[] { ("燕麦", "50g"), ("牛奶", "200ml"), ("蓝莓", "30g") },
            new[] { "燕麦加牛奶小火煮 3 分钟至浓稠", "关火后焖 2 分钟，让燕麦充分吸水", "表面铺蓝莓即可，喜甜可加半根香蕉" });
        var dSand = NewDish("全麦鸡蛋三明治", "高蛋白饱腹 · 快手早餐", "早餐",
            new[] { ("鸡蛋", "2个"), ("牛油果", "半个"), ("番茄", "1个") },
            new[] { "鸡蛋加水煮 8 分钟，过凉水剥壳切片", "牛油果压成泥，抹在全麦吐司上", "夹入蛋片与番茄片，对角切开" });
        var dQuin = NewDish("藜麦水果沙拉", "清爽低卡 · 优质蛋白", "早餐",
            new[] { ("藜麦", "40g"), ("希腊酸奶", "100g"), ("蓝莓", "30g") },
            new[] { "藜麦洗净后按 1:2 加水煮 15 分钟", "沥干放凉，拌入希腊酸奶", "点缀蓝莓与少量坚果碎" });
        var dYog = NewDish("希腊酸奶蓝莓杯", "无糖高蛋白 · 控卡首选", "早餐",
            new[] { ("希腊酸奶", "150g"), ("蓝莓", "40g"), ("燕麦", "20g") },
            new[] { "希腊酸奶倒入杯中铺底", "撒一层即食燕麦", "顶部堆满蓝莓，冷藏 10 分钟风味更佳" });
        var dToast = NewDish("牛油果水煮蛋吐司", "好脂肪 + 优质蛋白", "早餐",
            new[] { ("牛油果", "1个"), ("鸡蛋", "1个"), ("番茄", "1个") },
            new[] { "水煮蛋 8 分钟，切片备用", "牛油果压泥，加少许黑胡椒调味", "吐司抹牛油果泥，铺蛋片与番茄" });
        var dPurpleOat = NewDish("紫薯燕麦粥", "花青素 + 慢碳 · 暖胃", "早餐",
            new[] { ("紫薯", "100g"), ("燕麦", "40g"), ("牛奶", "150ml") },
            new[] { "紫薯去皮切小块蒸熟", "燕麦加水煮 3 分钟后加入紫薯", "倒入牛奶搅匀，煮至微沸" });
        var dBananaToast = NewDish("香蕉花生酱吐司", "快速供能 · 训练前佳选", "早餐",
            new[] { ("香蕉", "1个"), ("花生酱", "15g"), ("全麦吐司", "2片") },
            new[] { "全麦吐司烤至微脆", "抹一层薄花生酱", "铺香蕉片，撒少许肉桂粉" });
        var dPumpkinCongee = NewDish("南瓜小米粥", "养胃温和 · 低脂易消化", "早餐",
            new[] { ("南瓜", "120g"), ("小米", "50g") },
            new[] { "小米淘洗后加水煮 20 分钟", "南瓜切块蒸熟压成泥", "将南瓜泥拌入小米粥，再煮 5 分钟" });

        // ---- 主食（6）----
        var dRice = NewDish("杂粮糙米饭", "低 GI · 稳定血糖", "主食",
            new[] { ("糙米", "80g"), ("红薯", "50g") },
            new[] { "糙米提前浸泡 1 小时", "红薯去皮切块与糙米同煮", "电饭煲标准模式煮熟即可" });
        var dQuinRice = NewDish("藜麦杂蔬饭", "高纤饱腹 · 替代白米饭", "主食",
            new[] { ("藜麦", "60g"), ("西兰花", "50g"), ("番茄", "1个") },
            new[] { "藜麦加水煮 15 分钟至开花", "西兰花焯水切小朵，番茄切丁", "与藜麦拌匀，少盐少油调味" });
        var dCornRice = NewDish("玉米红薯饭", "粗粮组合 · 饱腹感强", "主食",
            new[] { ("玉米", "80g"), ("红薯", "80g"), ("糙米", "50g") },
            new[] { "糙米提前浸泡 1 小时", "玉米粒与红薯块一同下锅", "与糙米同煮至软糯" });
        var dSoba = NewDish("荞麦冷面", "低 GI 面食 · 夏日清爽", "主食",
            new[] { ("荞麦面", "80g"), ("番茄", "1个"), ("口蘑", "50g") },
            new[] { "荞麦面煮 4 分钟，过冰水沥干", "番茄与口蘑少油快炒成浇头", "面装碗淋浇头，喜酸可加醋" });
        var dYamCongee = NewDish("山药糙米粥", "健脾养胃 · 温润", "主食",
            new[] { ("山药", "100g"), ("糙米", "60g") },
            new[] { "糙米浸泡 1 小时后煮 25 分钟", "山药去皮切块加入同煮", "煮至山药软烂、粥体浓稠" });
        var dPurpleRice = NewDish("紫米饭", "花青素主食 · 抗氧化", "主食",
            new[] { ("紫米", "60g"), ("糙米", "40g") },
            new[] { "紫米与糙米混合淘洗", "浸泡 40 分钟后入电饭煲", "按 1:1.4 加水煮熟" });

        // ---- 荤菜（8）----
        var dChick = NewDish("香煎鸡胸肉", "少油高蛋白 · 增肌减脂", "荤菜",
            new[] { ("鸡胸肉", "150g"), ("西兰花", "100g"), ("番茄", "1个") },
            new[] { "鸡胸肉拍松，用盐、黑胡椒、蒜末腌 15 分钟", "不粘锅刷薄油，中火两面各煎 3 分钟", "配焯水西兰花与番茄装盘" });
        var dSalmon = NewDish("清蒸三文鱼", "富含 Omega-3 · 好脂肪", "荤菜",
            new[] { ("三文鱼", "150g"), ("菠菜", "100g"), ("番茄", "1个") },
            new[] { "三文鱼用料酒、姜片腌 10 分钟", "上锅大火蒸 8 分钟", "淋少许蒸鱼豉油，配焯水菠菜" });
        var dBeef = NewDish("黑椒牛柳", "铁质补给 · 增肌友好", "荤菜",
            new[] { ("牛柳", "150g"), ("西兰花", "100g"), ("番茄", "1个") },
            new[] { "牛柳切条，用黑胡椒、生抽腌 15 分钟", "热锅少油大火快炒至变色", "加西兰花同炒，出锅前再撒黑椒" });
        var dMushChick = NewDish("香菇滑鸡", "嫩滑低脂 · 家常味", "荤菜",
            new[] { ("鸡胸肉", "150g"), ("菠菜", "80g"), ("番茄", "1个") },
            new[] { "鸡胸切块，用蛋清、淀粉抓匀", "香菇炒香后下鸡肉翻炒", "加少量水焖 3 分钟收汁" });
        var dShrimpWok = NewDish("白灼虾仁", "高蛋白低脂 · 零负担", "荤菜",
            new[] { ("鲜虾", "200g"), ("番茄", "1个"), ("芦笋", "50g") },
            new[] { "鲜虾去壳去虾线洗净", "水加姜片煮开，下虾仁煮 2 分钟", "捞出过冰水，配焯水芦笋" });
        var dTeriyaki = NewDish("照烧鸡腿肉", "去皮低脂 · 酱香入味", "荤菜",
            new[] { ("鸡腿肉", "150g"), ("西兰花", "80g"), ("口蘑", "50g") },
            new[] { "鸡腿肉去皮，用生抽、味淋腌 20 分钟", "不粘锅煎至两面金黄", "倒入少量腌汁收汁，配蔬菜装盘" });
        var dLemonFish = NewDish("柠檬巴沙鱼", "低脂无刺 · 清爽开胃", "荤菜",
            new[] { ("巴沙鱼", "150g"), ("番茄", "1个"), ("芦笋", "50g") },
            new[] { "巴沙鱼用柠檬汁、盐腌 10 分钟", "不粘锅少油两面煎熟", "挤柠檬汁，配焯水蔬菜" });
        var dMushChicken = NewDish("口蘑炒鸡丁", "鲜香低脂 · 快手荤菜", "荤菜",
            new[] { ("鸡胸肉", "150g"), ("口蘑", "100g"), ("番茄", "1个") },
            new[] { "鸡胸切丁，用生抽、淀粉抓匀", "口蘑切片炒至出水", "下鸡丁大火快炒至熟" });

        // ---- 素菜（7）----
        var dBroc = NewDish("蒜蓉西兰花", "高纤维 C · 低卡素菜", "素菜",
            new[] { ("西兰花", "200g"), ("番茄", "1个") },
            new[] { "西兰花掰小朵，沸水焯 1 分钟", "蒜切末爆香", "下西兰花快炒，少盐出锅" });
        var dSpin = NewDish("清炒菠菜", "补铁绿叶菜 · 含叶酸", "素菜",
            new[] { ("菠菜", "200g"), ("番茄", "1个") },
            new[] { "菠菜洗净焯水去草酸", "热锅少油下菠菜", "快速翻炒，加蒜末与少盐" });
        var dOkra = NewDish("凉拌秋葵", "膳食纤维 · 清爽开胃", "素菜",
            new[] { ("秋葵", "150g"), ("番茄", "1个") },
            new[] { "秋葵整根焯水 3 分钟", "过冰水后切段", "加生抽、蒜末、少量香油拌匀" });
        var dBaby = NewDish("上汤娃娃菜", "清淡爽口 · 低热量", "素菜",
            new[] { ("娃娃菜", "200g"), ("番茄", "1个") },
            new[] { "娃娃菜切段备用", "清水加姜片煮开成上汤", "下娃娃菜煮 3 分钟调味" });
        var dAspMush = NewDish("芦笋炒口蘑", "鲜甜脆嫩 · 低卡", "素菜",
            new[] { ("芦笋", "150g"), ("口蘑", "100g") },
            new[] { "芦笋去老根切段，口蘑切片", "热锅少油先炒口蘑至微黄", "下芦笋快炒 1 分钟，少盐出锅" });
        var dWoodEar = NewDish("凉拌黑木耳", "爽脆低卡 · 富含铁质", "素菜",
            new[] { ("黑木耳", "100g"), ("番茄", "1个") },
            new[] { "干木耳泡发后焯水 2 分钟", "过凉水沥干", "加蒜末、香醋、少量香油拌匀" });
        var dAsparagus = NewDish("清炒芦笋", "低卡高纤 · 清香", "素菜",
            new[] { ("芦笋", "200g"), ("口蘑", "50g") },
            new[] { "芦笋切段焯水 30 秒", "热锅少油下锅快炒", "加少许盐与蒜末翻匀" });

        // ---- 汤品（4）----
        var dWinterSoup = NewDish("冬瓜排骨汤", "低卡汤品 · 利水消肿", "汤品",
            new[] { ("冬瓜", "200g"), ("番茄", "1个") },
            new[] { "排骨焯水去浮沫", "加冬瓜与姜片炖 30 分钟", "出锅前少盐调味" });
        var dTomEgg = NewDish("番茄蛋花汤", "快手低卡 · 5 分钟搞定", "汤品",
            new[] { ("番茄", "2个"), ("鸡蛋", "2个") },
            new[] { "番茄切块炒出汁", "加水煮开后淋入蛋液", "轻推成蛋花，加少盐" });
        var dSeaweed = NewDish("紫菜虾皮汤", "补钙 · 低热量", "汤品",
            new[] { ("番茄", "1个"), ("鸡蛋", "1个") },
            new[] { "水开后下紫菜与虾皮", "淋入打散的蛋液", "加少量香油与葱花" });
        var dMisoSoup = NewDish("豆腐味噌汤", "植物蛋白 · 温和低卡", "汤品",
            new[] { ("豆腐", "100g"), ("口蘑", "50g") },
            new[] { "水煮开后下豆腐块与口蘑片", "小火煮 3 分钟", "关火后化入味噌，撒葱花" });

        // ---- 加餐（4）----
        var dNuts = NewDish("综合坚果小包", "好脂肪 · 每日一把", "加餐",
            new[] { ("综合坚果", "25g") },
            new[] { "取无盐混合坚果 25g", "装入密封小袋分装", "下午茶时段食用最佳" });
        var dYogSnack = NewDish("无糖希腊酸奶", "高蛋白 · 控糖加餐", "加餐",
            new[] { ("希腊酸奶", "100g"), ("蓝莓", "20g") },
            new[] { "希腊酸奶倒入小碗", "铺上蓝莓", "可撒少许坚果碎" });
        var dBoiledEgg = NewDish("水煮蛋", "最简优质蛋白", "加餐",
            new[] { ("鸡蛋", "2个") },
            new[] { "鸡蛋冷水下锅", "水开后煮 8 分钟", "过凉水剥壳即可" });
        var dBlueberryBowl = NewDish("蓝莓一小碗", "抗氧化 · 低糖水果", "加餐",
            new[] { ("蓝莓", "100g") },
            new[] { "蓝莓洗净沥干", "装碗即可食用", "冷藏后口感更清爽" });

        // ---- 饮品（3）----
        var dSoyMilk = NewDish("无糖豆浆", "植物蛋白 · 0 蔗糖", "饮品",
            new[] { ("无糖豆浆", "250ml") },
            new[] { "豆浆煮至沸腾后再小火 3 分钟", "放至温热", "不加糖饮用" });
        var dSparkling = NewDish("柠檬气泡水", "0 卡解腻 · 清爽", "饮品",
            new[] { ("番茄", "1个") },
            new[] { "气泡水倒入杯中", "挤入半个柠檬汁", "加柠檬片与冰块" });
        var dGingerTea = NewDish("生姜红茶", "暖身 · 促循环", "饮品",
            new[] { ("生姜", "10g") },
            new[] { "生姜切片放入杯中", "冲入沸水泡 5 分钟", "加入红茶包再焖 2 分钟" });

        // ---- 草稿 / 下架（后台状态筛选用）----
        NewDish("芝士焗南瓜", "待完善配方 · 草稿中", "主食",
            new[] { ("南瓜", "200g"), ("牛奶", "50ml") },
            new[] { "南瓜蒸熟压泥", "拌入牛奶铺入烤碗", "表面铺芝士烤 12 分钟" }, DishStatus.Draft);
        NewDish("韩式拌饭", "待完善配方 · 草稿中", "主食",
            new[] { ("紫米", "60g"), ("菠菜", "80g"), ("鸡蛋", "1个") },
            new[] { "紫米蒸熟铺底", "依次码放焯水蔬菜与煎蛋", "淋少量低卡辣酱拌匀" }, DishStatus.Draft);
        NewDish("奶油蘑菇汤", "待完善配方 · 草稿中", "汤品",
            new[] { ("口蘑", "150g"), ("牛奶", "200ml") },
            new[] { "口蘑切片炒软", "加牛奶小火煮 10 分钟", "料理机打细腻后回锅调味" }, DishStatus.Draft);
        NewDish("炸鸡沙拉", "已下架 · 不符合低脂定位", "荤菜",
            new[] { ("鸡腿肉", "150g"), ("西兰花", "80g") },
            new[] { "鸡腿肉裹粉油炸", "沙拉菜垫底", "摆上炸鸡块淋酱" }, DishStatus.Offline);
        NewDish("焦糖布丁", "已下架 · 含糖过高", "加餐",
            new[] { ("鸡蛋", "2个"), ("牛奶", "200ml") },
            new[] { "鸡蛋加牛奶搅匀过筛", "倒入模具水浴烤 30 分钟", "表面撒焦糖装饰" }, DishStatus.Offline);

        db.Dishes.AddRange(dishes);
        db.SaveChanges();   // 先落库，拿到菜品 Id

        // ================= 3. 候选组（8 组，同类型菜品互换） =================
        var gBreakStaple = new DishCandidateGroup { Name = "早餐主食替换" };
        var gBreakLight = new DishCandidateGroup { Name = "早餐轻食替换" };
        var gLunchStaple = new DishCandidateGroup { Name = "午餐主食替换" };
        var gLunchMeat = new DishCandidateGroup { Name = "午餐荤菜替换" };
        var gLunchVeg = new DishCandidateGroup { Name = "午餐素菜替换" };
        var gDinnerMain = new DishCandidateGroup { Name = "晚餐主菜替换" };
        var gSoup = new DishCandidateGroup { Name = "汤品替换" };
        var gSnackDrink = new DishCandidateGroup { Name = "加餐饮品替换" };
        var groups = new[] { gBreakStaple, gBreakLight, gLunchStaple, gLunchMeat, gLunchVeg, gDinnerMain, gSoup, gSnackDrink };
        db.CandidateGroups.AddRange(groups);
        db.SaveChanges();   // 先落组，拿到组 Id

        void AddGroupMembers(DishCandidateGroup g, params Dish[] members)
        {
            foreach (var d in members)
                db.CandidateGroupItems.Add(new CandidateGroupItem { GroupId = g.Id, DishId = d.Id });
        }
        AddGroupMembers(gBreakStaple, dOat, dSand, dPurpleOat, dPumpkinCongee);
        AddGroupMembers(gBreakLight, dYog, dToast, dQuin, dBananaToast);
        AddGroupMembers(gLunchStaple, dRice, dQuinRice, dCornRice, dPurpleRice);
        AddGroupMembers(gLunchMeat, dChick, dSalmon, dBeef, dTeriyaki, dMushChicken);
        AddGroupMembers(gLunchVeg, dBroc, dSpin, dOkra, dAspMush, dAsparagus);
        AddGroupMembers(gDinnerMain, dLemonFish, dShrimpWok, dMushChick, dBeef);
        AddGroupMembers(gSoup, dWinterSoup, dTomEgg, dSeaweed, dMisoSoup);
        AddGroupMembers(gSnackDrink, dYogSnack, dNuts, dBoiledEgg, dSoyMilk);
        db.SaveChanges();

        // ================= 4. 方案树（3 个方案：2 上线 + 1 下线） =================
        var breakfastPool = new[] { dOat, dSand, dQuin, dYog, dToast, dPurpleOat };
        var staplePool = new[] { dRice, dQuinRice, dCornRice, dSoba, dYamCongee, dPurpleRice };
        var meatPool = new[] { dChick, dSalmon, dBeef, dMushChick, dShrimpWok, dTeriyaki, dLemonFish, dMushChicken };
        var vegPool = new[] { dBroc, dSpin, dOkra, dBaby, dAspMush, dWoodEar, dAsparagus };
        var dinnerPool = new[] { dLemonFish, dShrimpWok, dSalmon, dMushChicken };
        var soupPool = new[] { dWinterSoup, dTomEgg, dSeaweed, dMisoSoup };

        /// 构建一个方案：stages = (阶段名, 主题, 周数)
        Plan BuildPlan(string name, string intro, PlanStatus status, int sort,
            (string Name, string Theme, int Weeks)[] stageDefs)
        {
            var plan = new Plan { Name = name, Intro = intro, Status = status, Sort = sort };
            for (int s = 0; s < stageDefs.Length; s++)
            {
                var stage = new Stage
                {
                    Name = stageDefs[s].Name,
                    Theme = stageDefs[s].Theme,
                    Sort = s + 1,
                    DetailRichText = $"<h3>{stageDefs[s].Theme}</h3><p>本阶段建议每周配合 3 次有氧运动，饮水量保持 2000ml 以上，晚餐尽量在 19:00 前完成。</p>"
                };

                for (int w = 0; w < stageDefs[s].Weeks; w++)
                {
                    var week = new Week
                    {
                        Label = $"第 {w + 1} 周",
                        Theme = w == 0 ? "适应与记录" : "强度进阶",
                        CoverImage = Img($"week-{s}-{w}", 750, 420),
                        Sort = w + 1
                    };

                    for (int d = 1; d <= 7; d++)
                    {
                        int idx = (s * 5 + w * 7 + d - 1);
                        var day = new PlanDay { DayNo = d };

                        var breakfast = new MealSlot { MealType = MealType.Breakfast, Sort = 1 };
                        var bDish = breakfastPool[idx % breakfastPool.Length];
                        var bGroup = Array.IndexOf(breakfastPool, bDish) % 2 == 0 ? gBreakStaple : gBreakLight;
                        breakfast.DishItems.Add(new DishSlotItem { DishId = bDish.Id, Sort = 1, CandidateGroupId = bGroup.Id });
                        day.MealSlots.Add(breakfast);

                        var lunch = new MealSlot { MealType = MealType.Lunch, Sort = 2 };
                        lunch.DishItems.Add(new DishSlotItem { DishId = staplePool[idx % staplePool.Length].Id, Sort = 1, CandidateGroupId = gLunchStaple.Id });
                        lunch.DishItems.Add(new DishSlotItem { DishId = meatPool[idx % meatPool.Length].Id, Sort = 2, CandidateGroupId = gLunchMeat.Id });
                        lunch.DishItems.Add(new DishSlotItem { DishId = vegPool[idx % vegPool.Length].Id, Sort = 3, CandidateGroupId = gLunchVeg.Id });
                        day.MealSlots.Add(lunch);

                        var dinner = new MealSlot { MealType = MealType.Dinner, Sort = 3 };
                        dinner.DishItems.Add(new DishSlotItem { DishId = dinnerPool[idx % dinnerPool.Length].Id, Sort = 1, CandidateGroupId = gDinnerMain.Id });
                        dinner.DishItems.Add(new DishSlotItem { DishId = soupPool[idx % soupPool.Length].Id, Sort = 2, CandidateGroupId = gSoup.Id });
                        day.MealSlots.Add(dinner);

                        week.Days.Add(day);
                    }

                    stage.Weeks.Add(week);
                }

                plan.Stages.Add(stage);
            }
            return plan;
        }

        // 方案 A：主方案，4 阶段 × 每阶段 2 周 = 56 天（时间轴可滚动）
        var planA = BuildPlan(
            "21 天轻体饮食方案",
            "面向减脂人群的轻体方案，四阶段循序渐进，每餐搭配优质蛋白与高纤蔬菜，附多周进阶安排。",
            PlanStatus.Online, 1,
            new[]
            {
                ("第 1 阶段", "均衡启动", 2),
                ("第 2 阶段", "控卡进阶", 2),
                ("第 3 阶段", "高蛋白塑形", 2),
                ("第 4 阶段", "巩固维持", 2)
            });

        // 方案 B：第二个上线方案（列表有多条可选）
        var planB = BuildPlan(
            "14 天高蛋白增肌方案",
            "面向增肌人群的高蛋白方案，两阶段强度递进，训练日搭配加餐补给。",
            PlanStatus.Online, 2,
            new[]
            {
                ("第 1 阶段", "适应期", 1),
                ("第 2 阶段", "强化期", 1)
            });

        // 方案 C：下线方案（后台列表可见、C 端不返回，用于验证状态过滤）
        var planC = BuildPlan(
            "7 天控糖入门方案",
            "面向控糖人群的低 GI 入门方案（已下线，仅后台可见）。",
            PlanStatus.Offline, 3,
            new[]
            {
                ("第 1 阶段", "控糖适应", 1),
                ("第 2 阶段", "稳定血糖", 1)
            });

        db.Plans.AddRange(planA, planB, planC);
        db.SaveChanges();   // 落库拿到全部 Id

        // ================= 5. 运营内容块（18 条，含 3 条停用以验证过滤） =================
        db.ContentBlocks.AddRange(
            new ContentBlock { Type = "banner", Title = "7 天轻体计划 · 春季招募", Image = Img("banner-1", 750, 360), Url = "", Sort = 1, Enabled = true },
            new ContentBlock { Type = "banner", Title = "营养师直播课 · 每周三 20:00", Image = Img("banner-2", 750, 360), Url = "", Sort = 2, Enabled = true },
            new ContentBlock { Type = "banner", Title = "会员专享 · 首月 9.9", Image = Img("banner-3", 750, 360), Url = "", Sort = 3, Enabled = true },
            new ContentBlock { Type = "banner", Title = "（停用）往期活动 banner", Image = Img("banner-4", 750, 360), Url = "", Sort = 4, Enabled = false },

            new ContentBlock { Type = "news", Title = "春季减脂，这 5 种食材别错过", Image = Img("news-1"), Url = "", Sort = 1, Enabled = true },
            new ContentBlock { Type = "news", Title = "高蛋白饮食怎么吃才不伤肾", Image = Img("news-2"), Url = "", Sort = 2, Enabled = true },
            new ContentBlock { Type = "news", Title = "轻断食入门：从 16:8 开始", Image = Img("news-3"), Url = "", Sort = 3, Enabled = true },
            new ContentBlock { Type = "news", Title = "本周上新：0 卡沙拉酱料包", Image = Img("news-4"), Url = "", Sort = 4, Enabled = true, IsNew = true },
            new ContentBlock { Type = "news", Title = "外卖党如何点出低卡餐", Image = Img("news-5"), Url = "", Sort = 5, Enabled = true },
            new ContentBlock { Type = "news", Title = "喝够水真的能帮助减脂吗", Image = Img("news-6"), Url = "", Sort = 6, Enabled = true },
            new ContentBlock { Type = "news", Title = "平台期怎么突破：3 个思路", Image = Img("news-7"), Url = "", Sort = 7, Enabled = true, IsNew = true },
            new ContentBlock { Type = "news", Title = "（停用）往年节日食谱", Image = Img("news-8"), Url = "", Sort = 8, Enabled = false },

            new ContentBlock { Type = "video", Title = "3 分钟学会香煎鸡胸", Image = Img("video-1"), Url = DemoVideo, Sort = 1, Enabled = true, IsNew = true },
            new ContentBlock { Type = "video", Title = "藜麦的正确煮法", Image = Img("video-2"), Url = DemoVideo, Sort = 2, Enabled = true },
            new ContentBlock { Type = "video", Title = "低卡沙拉酱调法", Image = Img("video-3"), Url = DemoVideo, Sort = 3, Enabled = true },
            new ContentBlock { Type = "video", Title = "一周备餐 vlog", Image = Img("video-4"), Url = DemoVideo, Sort = 4, Enabled = true, IsNew = true },
            new ContentBlock { Type = "video", Title = "10 分钟快手早餐合集", Image = Img("video-5"), Url = DemoVideo, Sort = 5, Enabled = true },
            new ContentBlock { Type = "video", Title = "（停用）旧版教程回放", Image = Img("video-6"), Url = DemoVideo, Sort = 6, Enabled = false }
        );

        // ================= 6. 用户（8 个，让后台用户列表 / 统计有数据） =================
        var today = DateOnly.FromDateTime(DateTime.Now);
        var users = new List<User>
        {
            new() { OpenId = "demo_openid_0001", Nickname = "测试小明", Avatar = Img("avatar-1", 200, 200), CreatedAt = DateTime.Now.AddDays(-60), LastActiveAt = DateTime.Now },
            new() { OpenId = "wx_openid_1002", Nickname = "轻食小白", Avatar = Img("avatar-2", 200, 200), CreatedAt = DateTime.Now.AddDays(-45), LastActiveAt = DateTime.Now.AddDays(-1) },
            new() { OpenId = "wx_openid_1003", Nickname = "早起打卡王", Avatar = Img("avatar-3", 200, 200), CreatedAt = DateTime.Now.AddDays(-40), LastActiveAt = DateTime.Now.AddHours(-3) },
            new() { OpenId = "wx_openid_1004", Nickname = "健身阿May", Avatar = Img("avatar-4", 200, 200), CreatedAt = DateTime.Now.AddDays(-35), LastActiveAt = DateTime.Now.AddDays(-2) },
            new() { OpenId = "wx_openid_1005", Nickname = "减脂的猫", Avatar = Img("avatar-5", 200, 200), CreatedAt = DateTime.Now.AddDays(-30), LastActiveAt = DateTime.Now.AddDays(-4) },
            new() { OpenId = "wx_openid_1006", Nickname = "厨房新手", Avatar = Img("avatar-6", 200, 200), CreatedAt = DateTime.Now.AddDays(-25), LastActiveAt = DateTime.Now.AddDays(-6) },
            new() { OpenId = "wx_openid_1007", Nickname = "低卡战士", Avatar = Img("avatar-7", 200, 200), CreatedAt = DateTime.Now.AddDays(-15), LastActiveAt = DateTime.Now.AddDays(-1) },
            new() { OpenId = "wx_openid_1008", Nickname = "素食小绿", Avatar = Img("avatar-8", 200, 200), CreatedAt = DateTime.Now.AddDays(-8), LastActiveAt = DateTime.Now.AddHours(-8) }
        };
        db.Users.AddRange(users);
        db.SaveChanges();

        // ================= 7. 打卡记录（近 30 天，多用户） =================
        // 用方案 A 的各周/天做打卡身份；(weekIdx, dayNo) 在 30 天窗口内两两不同，
        // 避免触发 (UserId, WeekId, DayNo, DishId) 唯一约束。
        var planAWeeks = planA.Stages.SelectMany(s => s.Weeks).ToList();
        var dayDishMap = new Dictionary<(int WeekId, int DayNo), List<(MealType Meal, int DishId)>>();
        foreach (var w in planAWeeks)
        {
            foreach (var day in w.Days)
            {
                dayDishMap[(w.Id, day.DayNo)] = day.MealSlots
                    .SelectMany(ms => ms.DishItems.Select(i => (ms.MealType, i.DishId)))
                    .ToList();
            }
        }

        for (int u = 0; u < users.Count; u++)
        {
            var user = users[u];
            // 演示主账号(demo_openid_0001)每天打卡（保证连续打卡统计好看）；其余用户间隔打卡
            bool always = user.OpenId == "demo_openid_0001";
            for (int d = 0; d < 30; d++)
            {
                DateOnly date = today.AddDays(-d);
                if (!always && ((d + u) % 3 == 0)) continue;   // 部分用户隔天打

                var week = planAWeeks[d % planAWeeks.Count];
                int dayNo = (d % 7) + 1;
                if (!dayDishMap.TryGetValue((week.Id, dayNo), out var items)) continue;

                foreach (var (meal, dishId) in items)
                {
                    db.CheckInRecords.Add(new CheckInRecord
                    {
                        UserId = user.Id,
                        Date = date,
                        PlanId = planA.Id,
                        StageId = week.StageId,
                        WeekId = week.Id,
                        DayNo = dayNo,
                        MealType = meal,
                        DishId = dishId,
                        CreatedAt = date.ToDateTime(new TimeOnly(8 + (int)meal * 3, 15, 0))
                    });
                }
            }
        }

        db.SaveChanges();
    }
}
