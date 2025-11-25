#!/usr/bin/env python3
"""
Create .ico file from PNG icons for Windows application
"""

from PIL import Image

# Load all sizes
sizes = [16, 32, 48, 64, 128, 256]
images = []

for size in sizes:
    img = Image.open(f'E:\\recordtime\\recordtime_icon_{size}x{size}.png')
    images.append(img)

# Save as ICO with all sizes
images[0].save(
    'E:\\recordtime\\recordtime_icon.ico',
    format='ICO',
    sizes=[(s, s) for s in sizes],
    append_images=images[1:]
)

print('Created: E:\\recordtime\\recordtime_icon.ico')
print(f'Included sizes: {sizes}')
