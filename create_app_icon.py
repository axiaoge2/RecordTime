#!/usr/bin/env python3
"""
Main Application Icon Generator for RecordTime
生成主应用图标（立体时钟表盘设计）
"""

from PIL import Image, ImageDraw
from pathlib import Path
import math


def create_app_icon_256():
    """创建 256x256 完整细节版本"""
    size = 256
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    center = size // 2  # 128
    radius = 100  # 表盘半径

    # 颜色定义
    blue_start = (0, 122, 255)  # #007AFF
    blue_end = (0, 81, 213)     # #0051D5

    # 绘制外圈光晕（浅蓝色，半透明）
    glow_radius = 120
    glow_color = (90, 200, 250, 80)  # #5AC8FA with alpha
    draw.ellipse(
        [center - glow_radius, center - glow_radius,
         center + glow_radius, center + glow_radius],
        fill=glow_color
    )

    # 绘制主表盘（蓝色渐变效果通过叠加多个圆实现）
    # PIL 不支持原生渐变，用多层叠加模拟
    for i in range(radius, 0, -2):
        # 计算渐变色
        ratio = (radius - i) / radius
        r = int(blue_start[0] + (blue_end[0] - blue_start[0]) * ratio)
        g = int(blue_start[1] + (blue_end[1] - blue_start[1]) * ratio)
        b = int(blue_start[2] + (blue_end[2] - blue_start[2]) * ratio)

        draw.ellipse(
            [center - i, center - i, center + i, center + i],
            fill=(r, g, b, 255)
        )

    # 绘制表盘高光效果（左上角）
    highlight_offset_x = -30
    highlight_offset_y = -30
    for i in range(50, 0, -2):
        alpha = int(255 * (50 - i) / 50 * 0.3)  # 最大 30% 不透明度
        draw.ellipse(
            [center + highlight_offset_x - i,
             center + highlight_offset_y - i,
             center + highlight_offset_x + i,
             center + highlight_offset_y + i],
            fill=(255, 255, 255, alpha)
        )

    # 绘制 12 个时刻点
    hour_marks = [
        (128, 38, 4, 204),   # 12点 (80% opacity)
        (178, 53, 4, 153),   # 1点 (60% opacity)
        (211, 89, 4, 153),   # 2点
        (218, 128, 4, 204),  # 3点 (80% opacity)
        (211, 167, 4, 153),  # 4点
        (178, 203, 4, 153),  # 5点
        (128, 218, 4, 204),  # 6点 (80% opacity)
        (78, 203, 4, 153),   # 7点
        (45, 167, 4, 153),   # 8点
        (38, 128, 4, 204),   # 9点 (80% opacity)
        (45, 89, 4, 153),    # 10点
        (78, 53, 4, 153)     # 11点
    ]

    for x, y, r, alpha in hour_marks:
        draw.ellipse([x - r, y - r, x + r, y + r],
                    fill=(255, 255, 255, alpha))

    # 绘制时针（指向10点）- 带阴影
    hour_hand_end_x = 78
    hour_hand_end_y = 89

    # 时针阴影
    shadow_offset = 2
    draw.line(
        [(center + shadow_offset, center + shadow_offset),
         (hour_hand_end_x + shadow_offset, hour_hand_end_y + shadow_offset)],
        fill=(0, 0, 0, 80), width=8
    )
    draw.ellipse(
        [hour_hand_end_x - 6 + shadow_offset, hour_hand_end_y - 6 + shadow_offset,
         hour_hand_end_x + 6 + shadow_offset, hour_hand_end_y + 6 + shadow_offset],
        fill=(0, 0, 0, 80)
    )

    # 时针主体
    draw.line(
        [(center, center), (hour_hand_end_x, hour_hand_end_y)],
        fill=(255, 255, 255, 242), width=8
    )
    draw.ellipse(
        [hour_hand_end_x - 6, hour_hand_end_y - 6,
         hour_hand_end_x + 6, hour_hand_end_y + 6],
        fill=(255, 255, 255, 242)
    )

    # 绘制分针（指向12点）- 带阴影
    minute_hand_end_x = 128
    minute_hand_end_y = 48

    # 分针阴影
    draw.line(
        [(center + shadow_offset, center + shadow_offset),
         (minute_hand_end_x + shadow_offset, minute_hand_end_y + shadow_offset)],
        fill=(0, 0, 0, 80), width=6
    )
    draw.ellipse(
        [minute_hand_end_x - 5 + shadow_offset, minute_hand_end_y - 5 + shadow_offset,
         minute_hand_end_x + 5 + shadow_offset, minute_hand_end_y + 5 + shadow_offset],
        fill=(0, 0, 0, 80)
    )

    # 分针主体
    draw.line(
        [(center, center), (minute_hand_end_x, minute_hand_end_y)],
        fill=(255, 255, 255, 242), width=6
    )
    draw.ellipse(
        [minute_hand_end_x - 5, minute_hand_end_y - 5,
         minute_hand_end_x + 5, minute_hand_end_y + 5],
        fill=(255, 255, 255, 242)
    )

    # 中心覆盖圆点
    draw.ellipse(
        [center - 10, center - 10, center + 10, center + 10],
        fill=(255, 255, 255, 255)
    )

    return img


def create_app_icon_128():
    """创建 128x128 简化版本（保留时刻点，简化阴影）"""
    size = 128
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    center = size // 2  # 64
    radius = 50

    blue_start = (0, 122, 255)
    blue_end = (0, 81, 213)

    # 绘制表盘
    for i in range(radius, 0, -1):
        ratio = (radius - i) / radius
        r = int(blue_start[0] + (blue_end[0] - blue_start[0]) * ratio)
        g = int(blue_start[1] + (blue_end[1] - blue_start[1]) * ratio)
        b = int(blue_start[2] + (blue_end[2] - blue_start[2]) * ratio)
        draw.ellipse([center - i, center - i, center + i, center + i],
                    fill=(r, g, b, 255))

    # 12 个时刻点（缩小版）
    hour_marks = [
        (64, 19, 2, 204), (89, 26.5, 2, 153), (105.5, 44.5, 2, 153),
        (109, 64, 2, 204), (105.5, 83.5, 2, 153), (89, 101.5, 2, 153),
        (64, 109, 2, 204), (39, 101.5, 2, 153), (22.5, 83.5, 2, 153),
        (19, 64, 2, 204), (22.5, 44.5, 2, 153), (39, 26.5, 2, 153)
    ]

    for x, y, r, alpha in hour_marks:
        draw.ellipse([x - r, y - r, x + r, y + r],
                    fill=(255, 255, 255, alpha))

    # 时针（指向10点）
    draw.line([(center, center), (39, 44.5)],
             fill=(255, 255, 255, 242), width=4)
    draw.ellipse([36, 41.5, 42, 47.5], fill=(255, 255, 255, 242))

    # 分针（指向12点）
    draw.line([(center, center), (64, 24)],
             fill=(255, 255, 255, 242), width=3)
    draw.ellipse([61.5, 21.5, 66.5, 26.5], fill=(255, 255, 255, 242))

    # 中心点
    draw.ellipse([center - 5, center - 5, center + 5, center + 5],
                fill=(255, 255, 255, 255))

    return img


def create_app_icon_64():
    """创建 64x64 简化版本（移除时刻点，保留渐变）"""
    size = 64
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    center = size // 2  # 32
    radius = 25

    blue_start = (0, 122, 255)
    blue_end = (0, 81, 213)

    # 绘制表盘（渐变）
    for i in range(radius, 0, -1):
        ratio = (radius - i) / radius
        r = int(blue_start[0] + (blue_end[0] - blue_start[0]) * ratio)
        g = int(blue_start[1] + (blue_end[1] - blue_start[1]) * ratio)
        b = int(blue_start[2] + (blue_end[2] - blue_start[2]) * ratio)
        draw.ellipse([center - i, center - i, center + i, center + i],
                    fill=(r, g, b, 255))

    # 时针
    draw.line([(center, center), (19.5, 22)],
             fill=(255, 255, 255, 242), width=3)

    # 分针
    draw.line([(center, center), (32, 12)],
             fill=(255, 255, 255, 242), width=2)

    # 中心点
    draw.ellipse([center - 3, center - 3, center + 3, center + 3],
                fill=(255, 255, 255, 255))

    return img


def create_app_icon_32():
    """创建 32x32 扁平化版本"""
    size = 32
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    center = size // 2  # 16
    radius = 13

    # 单色填充
    blue_color = (0, 122, 255, 255)  # #007AFF

    draw.ellipse([center - radius, center - radius,
                 center + radius, center + radius],
                fill=blue_color)

    # 时针
    draw.line([(center, center), (10, 11)],
             fill=(255, 255, 255, 255), width=2)

    # 分针
    draw.line([(center, center), (16, 6)],
             fill=(255, 255, 255, 255), width=2)

    # 中心点
    draw.ellipse([center - 2, center - 2, center + 2, center + 2],
                fill=(255, 255, 255, 255))

    return img


def create_app_icon_16():
    """创建 16x16 版本（与托盘图标一致）"""
    size = 16
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    center = size // 2  # 8
    radius = 6

    # 实心圆
    blue_color = (0, 122, 255, 255)
    draw.ellipse([center - radius, center - radius,
                 center + radius, center + radius],
                fill=blue_color)

    # 单个指针（指向3点方向）
    draw.line([(center, center), (center + 5, center)],
             fill=(255, 255, 255, 255), width=2)

    return img


def save_as_ico(images, output_path):
    """保存为多尺寸 ICO 文件"""
    # ICO 格式要求尺寸从大到小
    images_sorted = sorted(images, key=lambda x: x.size[0], reverse=True)

    # 保存为 ICO
    images_sorted[0].save(
        output_path,
        format='ICO',
        sizes=[(img.size[0], img.size[1]) for img in images_sorted]
    )


def main():
    """主函数"""
    print("=== 开始生成主应用图标 ===\n")

    # 定义输出目录
    icons_dir = Path("src/RecordTime.Avalonia/Assets/Icons")
    icons_dir.mkdir(parents=True, exist_ok=True)

    # 生成各个尺寸
    print("[1/6] 生成 256x256 完整细节版本...")
    img_256 = create_app_icon_256()
    img_256.save(icons_dir / "AppIcon_256_preview.png")
    print("      OK: 256x256 (完整细节)")

    print("\n[2/6] 生成 128x128 简化版本...")
    img_128 = create_app_icon_128()
    img_128.save(icons_dir / "AppIcon_128_preview.png")
    print("      OK: 128x128 (简化阴影)")

    print("\n[3/6] 生成 64x64 简化版本...")
    img_64 = create_app_icon_64()
    img_64.save(icons_dir / "AppIcon_64_preview.png")
    print("      OK: 64x64 (移除时刻点)")

    print("\n[4/6] 生成 48x48 版本...")
    img_48 = img_64.resize((48, 48), Image.Resampling.LANCZOS)
    img_48.save(icons_dir / "AppIcon_48_preview.png")
    print("      OK: 48x48")

    print("\n[5/6] 生成 32x32 扁平化版本...")
    img_32 = create_app_icon_32()
    img_32.save(icons_dir / "AppIcon_32_preview.png")
    print("      OK: 32x32 (扁平化)")

    print("\n[6/6] 生成 16x16 版本...")
    img_16 = create_app_icon_16()
    img_16.save(icons_dir / "AppIcon_16_preview.png")
    print("      OK: 16x16 (与托盘图标一致)")

    # 打包为 ICO
    print("\n[*] 打包为 ICO 文件...")
    ico_path = icons_dir / "AppIcon.ico"
    save_as_ico([img_256, img_128, img_64, img_48, img_32, img_16], ico_path)
    print(f"    OK: {ico_path}")

    print("\n=== 主应用图标生成完成! ===")
    print(f"\n[*] 图标位置: {icons_dir.absolute()}")
    print("    - AppIcon.ico (多尺寸 ICO 文件)")
    print("    - AppIcon.svg (矢量源文件)")
    print("    - AppIcon_*_preview.png (各尺寸预览)")

    print("\n[*] 包含的尺寸:")
    print("    - 256x256: 完整细节（渐变、阴影、时刻点）")
    print("    - 128x128: 简化版本（保留时刻点）")
    print("    - 64x64:   进一步简化（移除时刻点）")
    print("    - 48x48:   标准尺寸")
    print("    - 32x32:   扁平化设计")
    print("    - 16x16:   与托盘图标一致")


if __name__ == "__main__":
    try:
        from PIL import Image, ImageDraw
        main()
    except ImportError:
        print("[ERROR] 需要安装 Pillow")
        print("        请运行: pip install Pillow")
