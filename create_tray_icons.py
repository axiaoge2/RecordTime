#!/usr/bin/env python3
"""
Simple Icon Generator for RecordTime
直接使用 PIL/Pillow 绘制托盘图标
"""

from PIL import Image, ImageDraw
from pathlib import Path


def create_idle_icon(size=32):
    """创建未监控状态图标 (灰色空心圆)"""
    # 创建透明背景
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # 计算圆心和半径
    center = size // 2
    radius = int(size * 0.4)  # 半径约为 40% 的图标尺寸
    pointer_length = int(size * 0.33)  # 指针长度

    # 绘制空心圆 (灰色描边)
    # Apple System Gray: #8E8E93
    gray_color = (142, 142, 147, 255)
    stroke_width = max(1, size // 10)

    draw.ellipse(
        [center - radius, center - radius, center + radius, center + radius],
        outline=gray_color,
        width=stroke_width
    )

    # 绘制指针 (指向3点钟方向,即水平向右)
    pointer_width = max(2, size // 8)
    draw.line(
        [(center, center), (center + pointer_length, center)],
        fill=gray_color,
        width=pointer_width
    )

    return img


def create_active_icon(size=32):
    """创建监控中状态图标 (蓝色实心圆)"""
    # 创建透明背景
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # 计算圆心和半径
    center = size // 2
    radius = int(size * 0.4)
    pointer_length = int(size * 0.35)

    # 绘制实心圆 (蓝色填充)
    # Apple System Blue: #007AFF
    blue_color = (0, 122, 255, 255)

    draw.ellipse(
        [center - radius, center - radius, center + radius, center + radius],
        fill=blue_color,
        outline=None
    )

    # 绘制白色指针
    white_color = (255, 255, 255, 255)
    pointer_width = max(2, size // 8)

    draw.line(
        [(center, center), (center + pointer_length, center)],
        fill=white_color,
        width=pointer_width
    )

    return img


def save_as_ico(img, output_path):
    """保存为多尺寸 ICO 文件"""
    # 创建多个尺寸的图标
    sizes = [16, 24, 32, 48]
    images = []

    for size in sizes:
        resized = img.resize((size, size), Image.Resampling.LANCZOS)
        images.append(resized)

    # 保存为 ICO (包含所有尺寸)
    img.save(
        output_path,
        format='ICO',
        sizes=[(s, s) for s in sizes]
    )


def main():
    """主函数"""
    print("=== 开始生成托盘图标 ===\n")

    # 定义输出目录
    icons_dir = Path("src/RecordTime.Avalonia/Assets/Icons")
    icons_dir.mkdir(parents=True, exist_ok=True)

    # 生成未监控状态图标 (32x32 基础尺寸)
    print("[1/2] 生成 TrayIconIdle.ico (灰色空心圆)...")
    idle_img = create_idle_icon(size=32)
    idle_path = icons_dir / "TrayIconIdle.ico"
    save_as_ico(idle_img, idle_path)
    print(f"      OK: {idle_path}")

    # 生成监控中状态图标
    print("\n[2/2] 生成 TrayIconActive.ico (蓝色实心圆)...")
    active_img = create_active_icon(size=32)
    active_path = icons_dir / "TrayIconActive.ico"
    save_as_ico(active_img, active_path)
    print(f"      OK: {active_path}")

    print("\n=== 图标生成完成! ===")
    print(f"\n[*] 图标位置: {icons_dir.absolute()}")
    print("    - TrayIconIdle.ico   (灰色空心圆 - 未监控)")
    print("    - TrayIconActive.ico (蓝色实心圆 - 监控中)")

    # 同时保存 PNG 预览
    print("\n[*] 正在生成 PNG 预览...")
    idle_img.save(icons_dir / "TrayIconIdle_preview.png")
    active_img.save(icons_dir / "TrayIconActive_preview.png")
    print("    OK: 已保存预览文件 (32x32)")


if __name__ == "__main__":
    try:
        from PIL import Image, ImageDraw
        main()
    except ImportError:
        print("[ERROR] 需要安装 Pillow")
        print("        请运行: pip install Pillow")
