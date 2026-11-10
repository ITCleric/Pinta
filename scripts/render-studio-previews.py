#!/usr/bin/env python3
"""Build six contact sheets from actual Pinta.StudioPreview PNG exports."""
import base64
import html
import json
from pathlib import Path
import subprocess

root = Path(__file__).resolve().parents[1]
directory = root / 'artifacts/studio-previews'
catalog = json.loads((root / 'Pinta.Effects/Studio/catalog.json').read_text())
for group in range(6):
    entries = catalog[group * 10:(group + 1) * 10]
    svg = ['<svg xmlns="http://www.w3.org/2000/svg" width="1200" height="850"><defs><pattern id="check" width="16" height="16" patternUnits="userSpaceOnUse"><rect width="16" height="16" fill="#eee"/><path d="M0 0h8v8H0zM8 8h8v8H8z" fill="#ccc"/></pattern></defs><rect width="1200" height="850" fill="#eaf0f5"/>', f'<text x="20" y="30" font-family="sans-serif" font-size="22" fill="#24384b">{html.escape(entries[0]["category"])} — actual Pinta renders, default settings</text>']
    for i, item in enumerate([{'slug': 'source', 'name': 'Original / transparent border'}] + entries):
        x = 18 + i % 4 * 298
        y = 55 + i // 4 * 262
        data = base64.b64encode((directory / (item['slug'] + '.png')).read_bytes()).decode()
        svg += [f'<rect x="{x}" y="{y}" width="282" height="188" fill="url(#check)"/>', f'<image x="{x}" y="{y}" width="282" height="188" href="data:image/png;base64,{data}"/>', f'<text x="{x}" y="{y+212}" font-family="sans-serif" font-size="14" fill="#24384b">{html.escape(item["name"])}</text>']
    svg.append('</svg>')
    path = directory / f'contact-{group+1}.svg'
    path.write_text(''.join(svg))
    subprocess.run(['rsvg-convert', str(path), '-o', str(path.with_suffix('.png'))], check=True)
print('Built six contact sheets in', directory)
