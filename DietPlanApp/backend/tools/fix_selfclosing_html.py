# -*- coding: utf-8 -*-
"""
修复后台纯 HTML 模板中「自定义元素自闭合写法」导致的浏览器标签嵌套问题。

背景：admin/index.html 是浏览器直接解析的 HTML（非 .vue SFC）。
HTML 规范下 <el-table-column .../> 的 "/" 会被忽略，标签保持打开状态，
后续兄弟元素被嵌套成子元素 —— Element Plus 因此把普通表格误判为多级表头，
日期选择器也会把它后面的按钮 / 表格吞进默认插槽。

做法：把所有自定义元素（含 "-" 的标签名，如 el-xxx）的自闭合写法
改写为 <tag ...></tag>，原生 void 元素（input/br/img...）保持不变。
"""
import re
import sys

VOID = {
    "area", "base", "br", "col", "embed", "hr", "img",
    "input", "link", "meta", "param", "source", "track", "wbr",
}

TAG_RE = re.compile(r"<([a-zA-Z][\w.:-]*)((?:\s[^<>]*?)?)\s*/>")


def fix(html: str):
    changed = []

    def repl(m):
        name = m.group(1)
        attrs = m.group(2) or ""
        low = name.lower()
        if low in VOID:
            return m.group(0)                      # 原生 void 元素，保持原样
        if "-" not in name:                        # 非自定义元素，不动
            return m.group(0)
        changed.append(name)
        return f"<{name}{attrs}></{name}>"

    out = TAG_RE.sub(repl, html)
    return out, changed


def main(path):
    with open(path, encoding="utf-8") as f:
        src = f.read()
    out, changed = fix(src)
    if not changed:
        print("未发现需要修复的自闭合自定义标签")
        return
    with open(path, "w", encoding="utf-8", newline="") as f:
        f.write(out)
    from collections import Counter
    print(f"已修复 {len(changed)} 处自闭合自定义标签：")
    for k, v in Counter(changed).most_common():
        print(f"  {k} x{v}")


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else
         r"D:\AIGC\DHmini\DietPlanApp\backend\src\DietPlan.Api\wwwroot\admin\index.html")
