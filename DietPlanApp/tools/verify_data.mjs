// 后端全量数据校验（Node，UTF-8 安全）
const BASE = 'http://127.0.0.1:24661';
const out = [];
const log = (...a) => out.push(a.join(' '));

async function j(path, opts = {}) {
  const r = await fetch(BASE + path, opts);
  const txt = await r.text();
  let body = null;
  try { body = JSON.parse(txt); } catch { body = txt.slice(0, 200); }
  return { status: r.status, body };
}

(async () => {
  const login = await j('/api/admin/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username: 'admin', password: 'admin123' })
  });
  const H = { Authorization: 'Bearer ' + login.body.token };
  log('LOGIN', login.status);

  const ov = await j('/api/admin/stats/overview', { headers: H });
  log('OVERVIEW', JSON.stringify(ov.body));

  // 候选组成员（修复后应 > 0）
  const g = await j('/api/admin/candidate-groups', { headers: H });
  log('CANDIDATE-GROUPS  count=' + g.body.length);
  for (const x of g.body) log('   g' + x.id, x.name, 'members=' + (x.items ? x.items.length : 'null'));

  // 分类筛选（中文查询参数）
  const cats = ['早餐', '主食', '荤菜', '素菜', '汤品', '加餐', '饮品'];
  log('CATEGORY FILTER:');
  for (const c of cats) {
    const r = await j('/api/admin/dishes?category=' + encodeURIComponent(c), { headers: H });
    log('   ' + c + ' rows=' + (Array.isArray(r.body) ? r.body.length : r.status));
  }

  // 关键词搜索
  const kw = await j('/api/admin/dishes?keyword=' + encodeURIComponent('鸡'), { headers: H });
  log('KEYWORD 鸡 rows=' + kw.body.length);

  // 方案树：主方案周数
  const t1 = await j('/api/plans/1');
  log('PLAN1', t1.body.name, 'stages=' + t1.body.stages.length);
  for (const s of t1.body.stages) log('   ' + s.name + '/' + s.theme + ' weeks=' + s.weeks.length);

  // 取第 2 阶段第 1 周，看每天
  const w = t1.body.stages[1].weeks[0];
  log('WEEK', w.label, 'dayCount=' + w.dayCount, 'id=' + w.id);
  for (const d of [1, 2, 3]) {
    const m = await j(`/api/weeks/${w.id}/days/${d}/meals`);
    const slots = m.body.meals.reduce((a, x) => a + x.slots.length, 0);
    log(`   day${d} meals=${m.body.meals.length} slots=${slots} slot0=${m.body.meals[0].slots[0].dishName}`);
  }

  // 候选（第 2 阶段第 1 周第 1 天午餐第 1 个槽位）
  const md = await j(`/api/weeks/${w.id}/days/1/meals`);
  const lunchSlot = md.body.meals[1].slots[0];
  const cd = await j(`/api/dish-slots/${lunchSlot.slotId}/candidates`);
  log('CANDIDATES slot' + lunchSlot.slotId + ' rows=' + cd.body.length + ' -> ' + cd.body.map(c => c.name).join('/'));

  // 打卡统计（演示账号）
  const wl = await j('/api/auth/wx-login', {
    method: 'POST', headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ code: 'demo' })
  });
  const sm = await j('/api/checkins/summary', { headers: { Authorization: 'Bearer ' + wl.body.token } });
  log('CHECKIN SUMMARY totalDays=' + sm.body.totalDays + ' streak=' + sm.body.streakDays + ' calendarDays=' + sm.body.calendar.length);

  console.log(out.join('\n'));
})();
