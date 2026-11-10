import os
from PIL import Image, ImageDraw

# 6x9 pixel grid for each character:
# 3: Highlight, 2: Bright tone, 1: Mid tone, ' ': Empty

N = [
    '33  33',
    '333 33',
    '323 32',
    '222222',
    '222222',
    '12 212',
    '11 111',
    '11  11',
    '11  11'
]

W = [
    '      ',
    '      ',
    '      ',
    '22  22',
    '222222',
    '122212',
    '111111',
    '11  11',
    ' 1  1 '
]

C = [
    '      ',
    '      ',
    '      ',
    ' 2222 ',
    '222222',
    '12    ',
    '11    ',
    '111111',
    ' 1111 '
]

I = [
    '333333',
    '333333',
    '  32  ',
    '  22  ',
    '  22  ',
    '  12  ',
    '  11  ',
    '111111',
    '111111'
]

O = [
    ' 3333 ',
    '333333',
    '32  32',
    '22  22',
    '22  22',
    '12  12',
    '11  11',
    '111111',
    ' 1111 '
]

# Color palettes:
# Nwc: Navisworks Emerald Green
pal_nwc = {
    0: '#052E16', # Deep shadow
    1: '#15803D', # Mid tone
    2: '#22C55E', # Bright tone
    3: '#86EFAC', # Highlight
}

# IO: Electric Royal Blue / Azure
pal_io = {
    0: '#0B1B3D', # Deep shadow
    1: '#1D4ED8', # Mid tone
    2: '#3B82F6', # Bright tone
    3: '#93C5FD', # Highlight
}

def build_svg(width, height, rx, block_size, base_xs, start_y, shadow_offset, letters, title):
    out = [f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {width} {height}" width="{width}" height="{height}" role="img"><title>{title}</title><rect width="{width}" height="{height}" rx="{rx}" fill="#12161C"/><g shape-rendering="crispEdges">']
    
    # Render in two letter groups: NWC, then IO
    for group_name, letter_indices, pal in [('nwc', [0, 1, 2], pal_nwc), ('io', [3, 4], pal_io)]:
        # Shadow layers (6 extrusion steps)
        shadow_rects = []
        for idx in letter_indices:
            grid = letters[idx]
            base_x = base_xs[idx]
            for step in range(1, 7):
                ox = base_x + step * shadow_offset
                oy = start_y + step * shadow_offset
                for r in range(9):
                    c = 0
                    while c < 6:
                        if grid[r][c] != ' ':
                            start_c = c
                            while c < 6 and grid[r][c] != ' ':
                                c += 1
                            span_w = (c - start_c) * block_size
                            x = ox + start_c * block_size
                            y = oy + r * block_size
                            
                            x_str = f"{x:.1f}".rstrip('0').rstrip('.') if isinstance(x, float) else str(x)
                            y_str = f"{y:.1f}".rstrip('0').rstrip('.') if isinstance(y, float) else str(y)
                            w_str = f"{span_w:.1f}".rstrip('0').rstrip('.') if isinstance(span_w, float) else str(span_w)
                            h_str = f"{block_size:.1f}".rstrip('0').rstrip('.') if isinstance(block_size, float) else str(block_size)
                            shadow_rects.append(f"M{x_str} {y_str}h{w_str}v{h_str}h-{w_str}z")
                        else:
                            c += 1
        out.append(f'<path fill="{pal[0]}" d="{"".join(shadow_rects)}"/>')

        # Front faces grouped by color
        # In rvtio, each segment is rendered in raster order
        # Let's collect spans for each face color
        for color_val in [3, 2, 1]:
            face_rects = []
            for idx in letter_indices:
                grid = letters[idx]
                base_x = base_xs[idx]
                ox = base_x
                oy = start_y
                for r in range(9):
                    c = 0
                    while c < 6:
                        if grid[r][c] == str(color_val):
                            start_c = c
                            while c < 6 and grid[r][c] == str(color_val):
                                c += 1
                            span_w = (c - start_c) * block_size
                            x = ox + start_c * block_size
                            y = oy + r * block_size
                            
                            x_str = f"{x:.1f}".rstrip('0').rstrip('.') if isinstance(x, float) else str(x)
                            y_str = f"{y:.1f}".rstrip('0').rstrip('.') if isinstance(y, float) else str(y)
                            w_str = f"{span_w:.1f}".rstrip('0').rstrip('.') if isinstance(span_w, float) else str(span_w)
                            h_str = f"{block_size:.1f}".rstrip('0').rstrip('.') if isinstance(block_size, float) else str(block_size)
                            face_rects.append(f"M{x_str} {y_str}h{w_str}v{h_str}h-{w_str}z")
                        else:
                            c += 1
            if face_rects:
                out.append(f'<path fill="{pal[color_val]}" d="{"".join(face_rects)}"/>')

    out.append('</g></svg>')
    return "".join(out)

def render_png(width, height, rx, block_size, base_xs, start_y, shadow_offset, letters, scale=1, alpha_bg=False):
    sw = int(width * scale)
    sh = int(height * scale)
    mode = 'RGBA' if alpha_bg else 'RGB'
    bg = (0, 0, 0, 0) if alpha_bg else '#12161C'
    img = Image.new(mode, (sw, sh), bg)
    draw = ImageDraw.Draw(img)

    if alpha_bg:
        draw.rounded_rectangle([0, 0, sw - 1, sh - 1], radius=int(rx * scale), fill='#12161C')

    # Draw shadow
    for group_name, letter_indices, pal in [('nwc', [0, 1, 2], pal_nwc), ('io', [3, 4], pal_io)]:
        shadow_color = pal[0]
        for idx in letter_indices:
            grid = letters[idx]
            base_x = base_xs[idx]
            for step in range(1, 7):
                ox = (base_x + step * shadow_offset) * scale
                oy = (start_y + step * shadow_offset) * scale
                for r in range(9):
                    for c in range(6):
                        if grid[r][c] != ' ':
                            x1 = int(ox + c * block_size * scale)
                            y1 = int(oy + r * block_size * scale)
                            x2 = int(ox + (c + 1) * block_size * scale - 1)
                            y2 = int(oy + (r + 1) * block_size * scale - 1)
                            draw.rectangle([x1, y1, x2, y2], fill=shadow_color)

    # Draw faces
    for group_name, letter_indices, pal in [('nwc', [0, 1, 2], pal_nwc), ('io', [3, 4], pal_io)]:
        for idx in letter_indices:
            grid = letters[idx]
            base_x = base_xs[idx]
            ox = base_x * scale
            oy = start_y * scale
            for r in range(9):
                for c in range(6):
                    val = grid[r][c]
                    if val != ' ':
                        color = pal[int(val)]
                        x1 = int(ox + c * block_size * scale)
                        y1 = int(oy + r * block_size * scale)
                        x2 = int(ox + (c + 1) * block_size * scale - 1)
                        y2 = int(oy + (r + 1) * block_size * scale - 1)
                        draw.rectangle([x1, y1, x2, y2], fill=color)

    return img

def render_icon():
    # 128x128 icon with letter N centered
    size = 128
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    draw.rounded_rectangle([0, 0, size - 1, size - 1], radius=24, fill='#12161C')

    # Grid: N is 6x9 blocks
    # Block size 10 -> 60x90 px
    block = 10
    start_x = (size - 6 * block) // 2 - 3 # center accounting for shadow
    start_y = (size - 9 * block) // 2 - 3
    shadow_offset = 1.2

    # Shadow
    for step in range(1, 7):
        ox = start_x + step * shadow_offset
        oy = start_y + step * shadow_offset
        for r in range(9):
            for c in range(6):
                if N[r][c] != ' ':
                    x1 = int(ox + c * block)
                    y1 = int(oy + r * block)
                    x2 = int(ox + (c + 1) * block - 1)
                    y2 = int(oy + (r + 1) * block - 1)
                    draw.rectangle([x1, y1, x2, y2], fill=pal_nwc[0])

    # Face
    for r in range(9):
        for c in range(6):
            val = N[r][c]
            if val != ' ':
                color = pal_nwc[int(val)]
                x1 = int(start_x + c * block)
                y1 = int(start_y + r * block)
                x2 = int(start_x + (c + 1) * block - 1)
                y2 = int(start_y + (r + 1) * block - 1)
                draw.rectangle([x1, y1, x2, y2], fill=color)

    return img

def main():
    os.makedirs('docs/images', exist_ok=True)
    letters = [N, W, C, I, O]

    # 1. nwcio-logo.svg (590x214, rx=18, block=14, base_xs=[23, 135, 247, 359, 471], start_y=38, shadow=2)
    logo_svg = build_svg(590, 214, 18, 14, [23, 135, 247, 359, 471], 38, 2, letters, "NwcIO")
    with open('docs/images/nwcio-logo.svg', 'w', encoding='utf-8') as f:
        f.write(logo_svg)
    print("Wrote docs/images/nwcio-logo.svg")

    # 2. nwcio-logo.png (1180x428 2x scale RGBA)
    logo_png = render_png(590, 214, 18, 14, [23, 135, 247, 359, 471], 38, 2, letters, scale=2, alpha_bg=True)
    logo_png.save('docs/images/nwcio-logo.png')
    print("Wrote docs/images/nwcio-logo.png")

    # 3. nwcio-banner.svg (1280x320, rx=0, block=24, base_xs=[178, 370, 562, 754, 946], start_y=46, shadow=3.4)
    banner_svg = build_svg(1280, 320, 0, 24, [178, 370, 562, 754, 946], 46, 3.4, letters, "NwcIO")
    with open('docs/images/nwcio-banner.svg', 'w', encoding='utf-8') as f:
        f.write(banner_svg)
    print("Wrote docs/images/nwcio-banner.svg")

    # 4. nwcio-banner.png (1280x320 RGB)
    banner_png = render_png(1280, 320, 0, 24, [178, 370, 562, 754, 946], 46, 3.4, letters, scale=1, alpha_bg=False)
    banner_png.save('docs/images/nwcio-banner.png')
    print("Wrote docs/images/nwcio-banner.png")

    # 5. icon.png for packages (src/NwcIO/icon.png and src/NwcIO.Cli/icon.png)
    icon_png = render_icon()
    os.makedirs('src/NwcIO', exist_ok=True)
    os.makedirs('src/NwcIO.Cli', exist_ok=True)
    icon_png.save('src/NwcIO/icon.png')
    icon_png.save('src/NwcIO.Cli/icon.png')
    print("Wrote src/NwcIO/icon.png and src/NwcIO.Cli/icon.png")

if __name__ == '__main__':
    main()
