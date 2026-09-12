/**
 * 小程序静态自检：
 *   1) 所有 .json 可解析
 *   2) 所有 .js 语法通过（node --check）
 *   3) 所有 .wxml 标签闭合平衡、image 自闭合
 *   4) WXML 中用到的 class 在 app.wxss 或该页 .wxss 里有定义（支持动态 class 前缀匹配）
 *   5) app.json tabBar 图标文件存在
 * 用法：node tools/check_miniprogram.js
 */
const fs = require('fs');
const path = require('path');
const { execFileSync } = require('child_process');

const ROOT = path.join(__dirname, '..', 'miniprogram');
let errors = 0;
const fail = (m) => { console.log('  [FAIL] ' + m); errors++; };
const ok = (m) => console.log('  [ok]   ' + m);

function walk(dir, out = []) {
  for (const name of fs.readdirSync(dir)) {
    const p = path.join(dir, name);
    const st = fs.statSync(p);
    if (st.isDirectory()) walk(p, out);
    else out.push(p);
  }
  return out;
}

const files = walk(ROOT);
const appWxss = fs.readFileSync(path.join(ROOT, 'app.wxss'), 'utf8');

/* ---------- 1) JSON ---------- */
console.log('\n== JSON 合法性 ==');
for (const f of files.filter(f => f.endsWith('.json'))) {
  try {
    JSON.parse(fs.readFileSync(f, 'utf8'));
    ok(path.relative(ROOT, f));
  } catch (e) {
    fail(path.relative(ROOT, f) + ' -> ' + e.message);
  }
}

/* ---------- 2) JS 语法 ---------- */
console.log('\n== JS 语法 ==');
for (const f of files.filter(f => f.endsWith('.js'))) {
  try {
    execFileSync(process.execPath, ['--check', f], { stdio: 'pipe' });
    ok(path.relative(ROOT, f));
  } catch (e) {
    fail(path.relative(ROOT, f) + ' -> ' + String(e.stderr || e.message).split('\n')[0]);
  }
}

/* ---------- 3) WXML 结构 + 4) class 覆盖 ---------- */
const definedClasses = new Set(
  (appWxss.match(/\.([A-Za-z0-9_-]+)\s*[,{]/g) || [])
    .map(s => s.replace(/[.,{\s]/g, ''))
);

console.log('\n== WXML 结构 / class 覆盖 ==');
for (const f of files.filter(f => f.endsWith('.wxml'))) {
  const rel = path.relative(ROOT, f);
  const s = fs.readFileSync(f, 'utf8');

  const vOpen = (s.match(/<view\b/g) || []).length;
  const vClose = (s.match(/<\/view>/g) || []).length;
  const imgOpen = (s.match(/<image\b/g) || []).length;
  const imgSelf = (s.match(/<image\b[^>]*\/>/g) || []).length;

  if (vOpen !== vClose) fail(`${rel} view 不闭合 ${vOpen}/${vClose}`);
  if (imgOpen !== imgSelf) fail(`${rel} image 未自闭合 ${imgOpen}/${imgSelf}`);

  // 该页自身的 wxss
  const pageWxssPath = f.replace(/\.wxml$/, '.wxss');
  const pageWxss = fs.existsSync(pageWxssPath) ? fs.readFileSync(pageWxssPath, 'utf8') : '';
  const localClasses = new Set(
    (pageWxss.match(/\.([A-Za-z0-9_-]+)\s*[,{]/g) || []).map(s => s.replace(/[.,{\s]/g, ''))
  );

  const stripped = s.replace(/\{\{[^}]*\}\}/g, '');
  const used = new Set();
  for (const m of stripped.matchAll(/class="([^"]*)"/g)) {
    m[1].split(/\s+/).filter(Boolean).forEach(c => used.add(c));
  }

  const missing = [];
  for (const c of used) {
    if (definedClasses.has(c) || localClasses.has(c)) continue;
    // 动态 class（尾部被 {{}} 截断）→ 前缀匹配
    const prefixHit = [...definedClasses, ...localClasses].some(d => d.startsWith(c) && d !== c);
    if (!prefixHit) missing.push(c);
  }
  if (missing.length) fail(`${rel} 未定义 class: ${missing.join(', ')}`);
  else ok(`${rel} (view ${vOpen}/${vClose}, class ${used.size} 个)`);
}

/* ---------- 5) tabBar 图标 ---------- */
console.log('\n== tabBar 图标 ==');
const appJson = JSON.parse(fs.readFileSync(path.join(ROOT, 'app.json'), 'utf8'));
for (const item of (appJson.tabBar && appJson.tabBar.list) || []) {
  for (const key of ['iconPath', 'selectedIconPath']) {
    const p = item[key] && path.join(ROOT, item[key]);
    if (p && fs.existsSync(p)) {
      const kb = (fs.statSync(p).size / 1024).toFixed(1);
      ok(`${item.text} ${key} -> ${item[key]} (${kb}KB)`);
    } else {
      fail(`${item.text} ${key} 缺失: ${item[key]}`);
    }
  }
}

console.log(`\n结果：${errors === 0 ? '全部通过 ✅' : errors + ' 项失败 ❌'}`);
process.exit(errors === 0 ? 0 : 1);
