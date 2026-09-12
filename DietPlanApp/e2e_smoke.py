# -*- coding: utf-8 -*-
"""E2E 冒烟：后台配置全链内容 → C端浏览/换菜/打卡 → 统计"""
import json
import urllib.request

BASE = "http://localhost:24661"
ok_count = 0
fail_count = 0
results = []


def call(name, method, path, body=None, token=None, expect=200):
    global ok_count, fail_count
    req = urllib.request.Request(BASE + path, method=method)
    req.add_header("Content-Type", "application/json")
    if token:
        req.add_header("Authorization", "Bearer " + token)
    data = json.dumps(body).encode() if body is not None else None
    try:
        with urllib.request.urlopen(req, data) as resp:
            status = resp.status
            payload = json.loads(resp.read().decode())
        assert status == expect, f"HTTP {status} != {expect}"
        ok_count += 1
        results.append(("PASS", name, ""))
        return payload
    except urllib.error.HTTPError as e:
        fail_count += 1
        detail = e.read().decode()[:200]
        results.append(("FAIL", name, f"HTTP {e.code}: {detail}"))
        raise
    except Exception as e:
        fail_count += 1
        results.append(("FAIL", name, str(e)[:200]))
        raise


def main():
    global ok_count, fail_count
    # ---- 1. 后台登录 ----
    admin = call("后台登录 admin/admin123", "POST", "/api/admin/auth/login",
                 {"username": "admin", "password": "admin123"})
    at = admin["token"]

    # ---- 2. 后台建菜品 ----
    d1 = call("建菜品 黑米粥", "POST", "/api/admin/dishes",
              {"name": "黑米粥", "subtitle": "粗粮主食", "category": "主食",
               "imagesJson": "[]",
               "ingredientsJson": json.dumps([{"name": "有机黑米", "amount": "20g"}, {"name": "粳米", "amount": "20g"}], ensure_ascii=False),
               "stepsJson": json.dumps([{"text": "淘洗黑米与粳米"}, {"text": "加水熬煮40分钟"}], ensure_ascii=False),
               "status": 1}, token=at)
    d2 = call("建菜品 小米粥", "POST", "/api/admin/dishes",
              {"name": "小米粥", "subtitle": "养胃主食", "category": "主食",
               "imagesJson": "[]",
               "ingredientsJson": json.dumps([{"name": "小米", "amount": "40g"}], ensure_ascii=False),
               "stepsJson": json.dumps([{"text": "小米淘洗后熬煮30分钟"}], ensure_ascii=False),
               "status": 1}, token=at)
    d3 = call("建菜品 清蒸鲈鱼", "POST", "/api/admin/dishes",
              {"name": "清蒸鲈鱼", "category": "荤菜", "imagesJson": "[]",
               "ingredientsJson": json.dumps([{"name": "鲈鱼", "amount": "150g"}], ensure_ascii=False),
               "stepsJson": "[]", "status": 1}, token=at)

    # ---- 3. 候选组（主食互换）----
    g1 = call("建候选组 主食互换组", "POST", "/api/admin/candidate-groups",
              {"name": "主食互换组", "items": [{"dishId": d1["id"]}, {"dishId": d2["id"]}]}, token=at)

    # ---- 4. 方案 → 阶段 → 周 → 天 → 槽位 ----
    p1 = call("建方案 均衡抗炎方案", "POST", "/api/admin/plans",
              {"name": "均衡抗炎方案", "intro": "四阶段均衡抗炎饮食方案", "status": 1, "sort": 1}, token=at)
    s1 = call("建阶段 第1阶段 均衡启动", "POST", "/api/admin/stages",
              {"planId": p1["id"], "name": "第 1 阶段", "theme": "均衡启动", "sort": 1,
               "detailRichText": "<p>第1阶段目标：建立均衡饮食结构。</p>"}, token=at)
    w1 = call("建周 第1周", "POST", "/api/admin/weeks",
              {"stageId": s1["id"], "label": "第1周", "theme": "启动周", "coverImage": "", "sort": 1}, token=at)
    day1 = call("建第1天（默认三餐）", "POST", "/api/admin/days",
                {"weekId": w1["id"], "dayNo": 1, "withDefaultMeals": True}, token=at)

    # 查天详情拿三餐槽位 id
    days = call("查周内天与槽位", "GET", f"/api/admin/days?weekId={w1['id']}", token=at)
    meals = {m["mealType"]: m for m in days[0]["mealSlots"]}
    breakfast, lunch = meals[1], meals[2]

    slot_b = call("早餐槽位挂 黑米粥+候选组", "POST", f"/api/admin/days/slots/{breakfast['id']}/dishes",
                  {"dishId": d1["id"], "candidateGroupId": g1["id"], "sort": 1}, token=at)
    call("午餐槽位挂 清蒸鲈鱼", "POST", f"/api/admin/days/slots/{lunch['id']}/dishes",
         {"dishId": d3["id"], "candidateGroupId": None, "sort": 1}, token=at)

    # ---- 5. C端微信登录（未配 AppId → dev stub）----
    wxr = call("C端微信静默登录", "POST", "/api/auth/wx-login", {"code": "e2e_code_001"})
    ut = wxr["token"]

    # ---- 6. C端浏览 ----
    plans = call("C端方案列表", "GET", "/api/plans")
    tree = call("C端方案树", "GET", f"/api/plans/{p1['id']}")
    assert tree["stages"][0]["weeks"][0]["dayCount"] == 1, "周天数应为1"

    day = call("C端第1天食谱", "GET", f"/api/weeks/{w1['id']}/days/1/meals")
    b_slot = day["meals"][0]["slots"][0]
    assert b_slot["dishName"] == "黑米粥", "早餐应为黑米粥"
    assert b_slot["ingredientsSummary"] == "有机黑米 20g、粳米 20g", "食材摘要不匹配"

    cands = call("C端候选菜品列表", "GET", f"/api/dish-slots/{b_slot['slotId']}/candidates")
    assert len(cands) == 2, f"候选应2项，实际{len(cands)}"
    call("C端候选搜索 小米", "GET", f"/api/dish-slots/{b_slot['slotId']}/candidates?keyword=%E5%B0%8F%E7%B1%B3")

    dish = call("C端菜品制作解析", "GET", f"/api/dishes/{d1['id']}")
    assert len(dish["steps"]) == 2, "步骤应2条"

    # ---- 7. 更换菜品（黑米粥 → 小米粥）----
    call("C端确认更换菜品", "PUT", f"/api/dish-slots/{b_slot['slotId']}/dish",
         {"slotId": b_slot["slotId"], "dishId": d2["id"]}, token=ut)
    day2 = call("C端复查更换结果", "GET", f"/api/weeks/{w1['id']}/days/1/meals", token=ut)
    assert day2["meals"][0]["slots"][0]["dishName"] == "小米粥", "更换未生效"

    # 非法更换（不在候选组）应被拒：直接发请求，验证 400
    req = urllib.request.Request(BASE + f"/api/dish-slots/{b_slot['slotId']}/dish", method="PUT")
    req.add_header("Content-Type", "application/json")
    req.add_header("Authorization", "Bearer " + ut)
    try:
        urllib.request.urlopen(req, json.dumps({"slotId": b_slot["slotId"], "dishId": d3["id"]}).encode())
        results.append(("FAIL", "非法更换（荤菜换进主食槽）应失败", "未拒绝"))
        fail_count += 1
    except urllib.error.HTTPError as e:
        if e.code == 400:
            ok_count += 1
            results.append(("PASS", "非法更换被正确拒绝(400)", ""))
        else:
            results.append(("FAIL", "非法更换（荤菜换进主食槽）应失败", f"HTTP {e.code}"))
            fail_count += 1

    # ---- 8. 打卡 ----
    call("单菜打卡", "POST", "/api/checkins",
         {"weekId": w1["id"], "dayNo": 1, "slotId": b_slot["slotId"]}, token=ut)
    r = call("整日打卡（幂等补齐剩余）", "POST", "/api/checkins/day",
             {"weekId": w1["id"], "dayNo": 1}, token=ut)
    summary = call("C端打卡统计", "GET", "/api/checkins/summary", token=ut)
    assert summary["totalDays"] == 1, f"累计天数应1，实际{summary['totalDays']}"

    day3 = call("复查打卡状态", "GET", f"/api/weeks/{w1['id']}/days/1/meals", token=ut)
    checked = [s["checked"] for m in day3["meals"] for s in m["slots"]]
    assert all(checked), "整日打卡后全部菜品应为已打卡"

    # ---- 9. 后台统计 ----
    ov = call("后台统计总览", "GET", "/api/admin/stats/overview", token=at)
    assert ov["todayCheckins"] >= 2, f"今日打卡人次应>=2，实际{ov['todayCheckins']}"
    stats = call("后台按日统计", "GET", "/api/admin/stats/checkins?from=2026-09-11&to=2026-09-11", token=at)

    # ---- 10. 管理后台页面可访问 ----
    with urllib.request.urlopen(BASE + "/admin/index.html") as resp:
        assert resp.status == 200
    ok_count += 1
    results.append(("PASS", "管理后台静态页 /admin/index.html 可访问", ""))


if __name__ == "__main__":
    import sys
    import urllib.error
    crashed = False
    try:
        main()
    except SystemExit:
        raise
    except BaseException as e:
        crashed = True
        results.append(("FAIL", "E2E 中断", str(e)[:200]))
    print("\n===== E2E 结果 =====")
    for st, name, msg in results:
        print(f"[{st}] {name}" + (f"  -- {msg}" if msg else ""))
    print(f"\n通过 {ok_count} / 失败 {fail_count}")
    sys.exit(1 if (crashed or fail_count) else 0)
