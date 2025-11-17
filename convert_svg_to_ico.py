#!/usr/bin/env python3
"""
SVG to ICO Converter
将 SVG 图标转换为 Windows ICO 格式
"""

import os
from pathlib import Path

def convert_svg_to_ico():
    """使用 PIL/Pillow 将 SVG 转换为 ICO"""
    try:
        from PIL import Image
        import cairosvg

        # 定义输入输出路径
        icons_dir = Path("src/RecordTime.Avalonia/Assets/Icons")

        svg_files = [
            ("TrayIconIdle.svg", "TrayIconIdle.ico"),
            ("TrayIconActive.svg", "TrayIconActive.ico")
        ]

        for svg_name, ico_name in svg_files:
            svg_path = icons_dir / svg_name
            ico_path = icons_dir / ico_name

            if not svg_path.exists():
                print(f"❌ SVG 文件不存在: {svg_path}")
                continue

            # SVG -> PNG -> ICO
            # 先转换为 PNG (多个尺寸)
            png_path = icons_dir / f"{svg_name.replace('.svg', '.png')}"

            # 使用 cairosvg 将 SVG 转换为 PNG
            cairosvg.svg2png(
                url=str(svg_path),
                write_to=str(png_path),
                output_width=32,
                output_height=32
            )

            # 使用 PIL 将 PNG 转换为 ICO (包含多个尺寸)
            img = Image.open(png_path)

            # 创建多个尺寸的图标
            icon_sizes = [(16, 16), (24, 24), (32, 32), (48, 48)]

            img.save(
                ico_path,
                format='ICO',
                sizes=icon_sizes
            )

            # 删除临时 PNG 文件
            png_path.unlink()

            print(f"✅ 转换成功: {svg_name} -> {ico_name}")

        print("\n🎉 所有图标转换完成!")
        return True

    except ImportError as e:
        print(f"❌ 缺少必要的 Python 包: {e}")
        print("\n请安装以下包:")
        print("  pip install Pillow cairosvg")
        return False
    except Exception as e:
        print(f"❌ 转换过程出错: {e}")
        return False

if __name__ == "__main__":
    print("开始转换 SVG 图标为 ICO 格式...\n")
    convert_svg_to_ico()
