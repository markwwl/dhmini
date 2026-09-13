# -*- coding: utf-8 -*-
"""
生成小程序 tabBar 图标（4 组 × 2 态）。
图标风格：Material 填充式，4 倍尺寸绘制后 LANCZOS 缩到 81×81 抗锯齿。
颜色与设计系统一致：未选中 = #98A69D（--c-text-3），选中 = #646cff（--c-primary）。
运行：<venv>/Scripts/python.exe gen_tabbar_icons.py
"""
import os
from PIL import Image, ImageDraw

S = 324            # 绘制尺寸（81 的 4 倍）
OUT = 81           # 输出尺寸
C_OFF = (152, 166, 157)   # #98A69D
C_ON = (100, 108, 255)   # #646cff
W = 255            # 遮罩白
K = 0              # 遮罩黑（挖空）


def mask_home():
    m = Image.new('L', (S, S), 0)
    d = ImageDraw.Draw(m)
    d.polygon([(162, 44), (308, 178), (16, 178)], fill=W)          # 屋顶
    d.rounded_rectangle([62, 176, 262, 286], radius=16, fill=W)     # 主体
    d.rounded_rectangle([134, 212, 190, 292], radius=10, fill=K)    # 门（挖空）
    return m


def mask_plan():
    m = Image.new('L', (S, S), 0)
    d = ImageDraw.Draw(m)
    d.rounded_rectangle([44, 40, 280, 284], radius=34, fill=W)      # 清单卡
    for y in (100, 154, 208):                                       # 三行条目
        d.ellipse([86, y, 114, y + 28], fill=K)                     # 圆点（挖空）
        d.rounded_rectangle([138, y + 4, 246, y + 22], radius=9, fill=K)  # 文字线（挖空）
    return m


def mask_videos():
    m = Image.new('L', (S, S), 0)
    d = ImageDraw.Draw(m)
    d.rounded_rectangle([34, 58, 290, 266], radius=36, fill=W)      # 屏幕
    d.polygon([(130, 104), (130, 220), (216, 162)], fill=K)         # 播放三角（挖空）
    return m


def mask_mine():
    m = Image.new('L', (S, S), 0)
    d = ImageDraw.Draw(m)
    d.ellipse([104, 52, 220, 168], fill=W)                          # 头
    d.pieslice([58, 176, 266, 390], start=180, end=360, fill=W)     # 肩（上半椭圆）
    return m


ICONS = {'home': mask_home, 'plan': mask_plan, 'videos': mask_videos, 'mine': mask_mine}


def main():
    out_dir = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                           '..', 'miniprogram', 'images', 'tabbar')
    out_dir = os.path.normpath(out_dir)
    os.makedirs(out_dir, exist_ok=True)

    for name, build in ICONS.items():
        base = build()
        for suffix, color in (('', C_OFF), ('-on', C_ON)):
            icon = Image.new('RGBA', (S, S), color + (255,))
            icon.putalpha(base)
            icon = icon.resize((OUT, OUT), Image.LANCZOS)
            path = os.path.join(out_dir, f'{name}{suffix}.png')
            icon.save(path, 'PNG', optimize=True)
            print('OK', os.path.basename(path), os.path.getsize(path), 'bytes')


if __name__ == '__main__':
    main()
