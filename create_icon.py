#!/usr/bin/env python3
"""
RecordTime Application Icon Generator
Based on competitor analysis and design research
"""

from PIL import Image, ImageDraw
import math

def create_recordtime_icon(size=256):
    """
    Create RecordTime icon based on optimized design:
    - Teal/cyan color scheme (#14B8A6)
    - Circular clock face
    - 75-degree progress arc
    - Minimalist clock hands at 10:10
    """
    # Color scheme (Tailwind teal)
    TEAL_500 = (20, 184, 166)      # #14B8A6 - Main color
    TEAL_600 = (13, 148, 136)      # #0D9488 - Dark variant
    TEAL_300 = (94, 234, 212)      # #5EEAD4 - Light variant
    WHITE = (255, 255, 255)

    # Create transparent image
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # Calculate dimensions
    center = size // 2
    outer_radius = int(size * 0.42)  # 42% of size
    inner_radius = int(size * 0.35)  # 35% of size

    # Draw subtle shadow for depth
    shadow_offset = int(size * 0.02)
    shadow_color = (13, 148, 136, 60)  # Semi-transparent teal-600
    draw.ellipse(
        [center - outer_radius + shadow_offset, center - outer_radius + shadow_offset,
         center + outer_radius + shadow_offset, center + outer_radius + shadow_offset],
        fill=shadow_color
    )

    # Draw main circle (clock face) with gradient effect
    # Outer circle
    draw.ellipse(
        [center - outer_radius, center - outer_radius,
         center + outer_radius, center + outer_radius],
        fill=TEAL_500,
        outline=TEAL_600,
        width=int(size * 0.015)
    )

    # Inner circle (lighter for depth)
    inner_circle_radius = int(outer_radius * 0.85)
    draw.ellipse(
        [center - inner_circle_radius, center - inner_circle_radius,
         center + inner_circle_radius, center + inner_circle_radius],
        fill=TEAL_300,
        outline=None
    )

    # Draw 75-degree progress arc (top-right)
    # This represents "recording in progress"
    arc_width = int(size * 0.035)
    arc_radius = outer_radius + int(size * 0.015)
    arc_box = [
        center - arc_radius, center - arc_radius,
        center + arc_radius, center + arc_radius
    ]

    # Draw progress arc (from -90° to -15°, which is 75 degrees)
    for i in range(arc_width):
        current_radius = arc_radius - i
        current_box = [
            center - current_radius, center - current_radius,
            center + current_radius, center + current_radius
        ]
        # Fade from teal-600 to teal-500
        alpha = int(255 * (1 - i / arc_width))
        arc_color = TEAL_600 + (alpha,)
        draw.arc(current_box, start=-90, end=-15, fill=arc_color, width=2)

    # Draw 4 minimalist tick marks (12, 3, 6, 9 o'clock)
    tick_positions = [
        (0, -1),    # 12 o'clock
        (1, 0),     # 3 o'clock
        (0, 1),     # 6 o'clock
        (-1, 0),    # 9 o'clock
    ]

    tick_length = int(size * 0.04)
    tick_width = int(size * 0.012)
    tick_start_radius = inner_circle_radius - int(size * 0.02)
    tick_end_radius = tick_start_radius - tick_length

    for dx, dy in tick_positions:
        start_x = center + dx * tick_start_radius
        start_y = center + dy * tick_start_radius
        end_x = center + dx * tick_end_radius
        end_y = center + dy * tick_end_radius

        draw.line(
            [(start_x, start_y), (end_x, end_y)],
            fill=TEAL_600,
            width=tick_width
        )

    # Draw clock hands at 10:10 position (classic watch advertisement angle)
    # Hour hand (pointing to 10)
    hour_angle = math.radians(300)  # 10 o'clock = 300 degrees from top
    hour_length = int(inner_circle_radius * 0.45)
    hour_width = int(size * 0.02)
    hour_x = center + hour_length * math.sin(hour_angle)
    hour_y = center - hour_length * math.cos(hour_angle)
    draw.line(
        [(center, center), (hour_x, hour_y)],
        fill=TEAL_600,
        width=hour_width
    )

    # Minute hand (pointing to 2)
    minute_angle = math.radians(60)  # 2 o'clock = 60 degrees from top
    minute_length = int(inner_circle_radius * 0.65)
    minute_width = int(size * 0.015)
    minute_x = center + minute_length * math.sin(minute_angle)
    minute_y = center - minute_length * math.cos(minute_angle)
    draw.line(
        [(center, center), (minute_x, minute_y)],
        fill=TEAL_600,
        width=minute_width
    )

    # Draw center dot
    center_dot_radius = int(size * 0.025)
    draw.ellipse(
        [center - center_dot_radius, center - center_dot_radius,
         center + center_dot_radius, center + center_dot_radius],
        fill=TEAL_600
    )

    return img

# Generate icon at multiple sizes
sizes = [256, 128, 64, 48, 32, 16]

for size in sizes:
    icon = create_recordtime_icon(size)
    filename = f'E:\\recordtime\\recordtime_icon_{size}x{size}.png'
    icon.save(filename, 'PNG')
    print(f'Created: {filename}')

# Create the main 256x256 icon
main_icon = create_recordtime_icon(256)
main_icon.save('E:\\recordtime\\recordtime_icon.png', 'PNG')
print('Created: E:\\recordtime\\recordtime_icon.png')

print('\n✅ All RecordTime icons generated successfully!')
print('📊 Design based on:')
print('   - Toggl, Clockify, RescueTime competitor analysis')
print('   - Single teal color scheme (#14B8A6)')
print('   - 75-degree progress arc')
print('   - 10:10 clock hands (classic angle)')
print('   - Optimized for 16x16px system tray')
