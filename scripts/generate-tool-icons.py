#!/usr/bin/env python3
"""Original small desktop drawing-tool icons, MIT licensed. No external artwork."""
from pathlib import Path
import xml.etree.ElementTree as ET
ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Pinta.Resources/icons/hicolor/scalable/actions'
# Shapes are deliberately drawn on a 24 px grid, with colored materials and crisp outlines.
selection = 'fill="#c3e7fa" fill-opacity=".65" stroke="#2a6b95" stroke-width="1" stroke-dasharray="2 1.5"'
brush = '<path d="m9 14 9-11q2-2 3 0L12 16z" fill="url(#wood)" stroke="#79512f"/><path d="m9 13 4 3-2 3-4-3z" fill="url(#steel)" stroke="#5a6870"/><path d="M8 16q-4 0-4 4L2 22q8 1 9-4z" fill="#536778" stroke="#2f404b"/><path d="m4 21 3-3" stroke="#d9e9f2" stroke-width=".8"/>'
icons = {
 'select-rectangle': ('Rectangle selection', f'<rect x="3.5" y="4.5" width="17" height="15" {selection}/><path d="M5 6h14v12H5z" fill="none" stroke="white" opacity=".75"/>'),
 'select-ellipse': ('Ellipse selection', f'<ellipse cx="12" cy="12" rx="9" ry="7.5" {selection}/>'),
 'select-lasso': ('Lasso selection', '<path d="M5 6C1 8 3 15 9 16c8 2 14-4 11-9C18 2 8 3 5 6z" fill="#d6eef9" stroke="#876044" stroke-width="1.5"/><path d="m8 16-2 3q-1 3 3 3 4 0 4-3l-2-4" fill="none" stroke="#876044" stroke-width="1.4"/>'),
 'select-magicwand': ('Magic selection', '<path d="m4 21 11-11 2 2L6 23z" fill="#566375" stroke="#273746"/><path d="m13 11 2-2 3 3-2 2z" fill="url(#steel)"/><path d="m16 2 1.5 4L22 7l-4 2-1 4-2-4-4-1 4-2z" fill="#ffdc68" stroke="#b78b24" stroke-width=".7"/><path d="m6 3 1 2 2 1-2 1-1 2-1-2-2-1 2-1z" fill="#ffe67b"/><path d="M20 16h3M21.5 14.5v3" stroke="#e6bd43"/>'),
 'move': ('Move pixels', '<path d="m13 2 3 4h-2v4h4V8l4 3-4 3v-2h-4v5h2l-3 4-3-4h2v-5H7v2l-4-3 4-3v2h5V6h-2z" fill="url(#blue)" stroke="#245c82" stroke-width=".7"/><path d="M2 2v13l3-4 3 6 2-1-3-6h5z" fill="#f6fbff" stroke="#354655"/>'),
 'move-selection': ('Move selection', f'<rect x="3" y="3" width="13" height="12" {selection}/><path d="m16 10 3 3h-2v3h3v-2l3 3-3 3v-2h-3v3h2l-3 3-3-3h2v-3h-3v2l-3-3 3-3v2h3v-3h-2z" fill="#4a93c4" stroke="#285675" stroke-width=".5"/>'),
 'zoom': ('Zoom', '<path d="m14 15 6 6q2 1 2-1l-6-6z" fill="url(#wood)" stroke="#725137"/><circle cx="9.5" cy="9.5" r="7" fill="url(#lens)" stroke="#536f84" stroke-width="1.8"/><path d="M5 8q0-4 5-4" fill="none" stroke="white" stroke-width="1.5"/><path d="M6 10h7M9.5 6.5v7" stroke="#457493" stroke-width="1"/>'),
 'pan': ('Pan', '<path d="M7 12V5q0-2 2-2t2 2v6-8q0-2 2-2t2 2v8-6q0-2 2-2t2 2v7-3q0-2 2-2t1 3l-1 9q-1 4-6 4h-4q-3 0-4-3l-5-7q-1-2 1-3t4 3z" fill="url(#skin)" stroke="#9b6c44" stroke-width=".9"/><path d="M11 13v5M15 12v6M19 13v4" stroke="#d2a478" stroke-width=".8"/>'),
 'paintbrush': ('Paintbrush', brush),
 'pencil': ('Pencil', '<path d="m3 16 12-13 6 5L8 21l-6 1z" fill="#f4c54f" stroke="#9b732b"/><path d="m7 17 11-12 2 2L9 19z" fill="#ffe792"/><path d="m3 16 5 5-6 1z" fill="#e8cf9e"/><path d="m2 19 3 3-3 0z" fill="#424b52"/><path d="m15 3 2-2q1-1 2 0l3 3q1 1 0 2l-1 2z" fill="#e799a3" stroke="#965b67"/><path d="m14 4 6 5 1-1-6-5z" fill="url(#steel)"/>'),
 'eraser': ('Eraser', '<path d="m3 14 11-11q1-1 3 0l5 5q1 1 0 3L11 22H7z" fill="#ef9eaf" stroke="#92586b"/><path d="m3 14 7-7 8 8-7 7H7z" fill="#f7cad4"/><path d="m3 14 8 8H7l-4-4z" fill="#d8dce4"/><path d="m8 8 8 8" stroke="#c3768a"/><path d="M5 23h14" stroke="#8a98a4"/>'),
 'colorpicker': ('Color picker', '<path d="m6 17 9-10 3 3-10 9-5 2z" fill="url(#steel)" stroke="#4e6374"/><path d="m3 21 5-2-2-2z" fill="#5b9bc3"/><path d="m12 7 5 5 2-2-5-5z" fill="#456b8f" stroke="#284b6b"/><path d="m15 5 3-3q3-2 5 0t-1 5l-3 3z" fill="url(#blue)" stroke="#315a7a"/><path d="m9 15 4-4" stroke="white"/>'),
 'clonestamp': ('Clone stamp', '<path d="M4 17h16l2 3v2H2v-2z" fill="url(#steel)" stroke="#56626c"/><path d="M5 15h14v4H5z" fill="url(#wood)" stroke="#845d3b"/><path d="M9 6q-2-1-2-3 1-3 5-3t5 3q0 2-2 3l-1 9h-4z" fill="url(#wood)" stroke="#845d3b"/><path d="M3 22h18" stroke="#3a4b59" stroke-width="2"/>'),
 'recolor': ('Recolor', '<circle cx="7" cy="7" r="5" fill="#e56b61" stroke="#9d4945"/><circle cx="17" cy="8" r="5" fill="#73b0e8" stroke="#3b71a0"/><path d="m5 20 11-8 3 3-12 7-4 1z" fill="url(#wood)" stroke="#79604b"/><path d="m15 12 3-3 4 4-3 3z" fill="url(#steel)"/><path d="m18 9 2-4q4 0 4 4l-2 4z" fill="#4587bf" stroke="#38618a"/>'),
 'paintbucket': ('Paint bucket', '<path d="m4 9 7-7 10 10-9 9z" fill="url(#steel)" stroke="#566b7b"/><path d="m4 9 7 7 9-4-8 8z" fill="#6a9bb9"/><path d="m5 8 7-6 3 3-7 6z" fill="#edf5f9"/><path d="M8 3Q4-1 2 4q-2 5 4 7" fill="none" stroke="#6e8391" stroke-width="1.3"/><path d="M21 14q-5 6-2 8 5 2 4-3z" fill="#58a4e1" stroke="#337db3"/>'),
 'gradient': ('Gradient', '<rect x="2.5" y="3.5" width="19" height="17" fill="url(#spectrum)" stroke="#526b7d"/><rect x="3.5" y="4.5" width="17" height="15" fill="none" stroke="white" stroke-opacity=".75"/><path d="M5 17 18 7" stroke="white" stroke-width="1.2"/><rect x="4" y="16" width="3" height="3" fill="#253c54"/><rect x="17" y="5" width="3" height="3" fill="white" stroke="#516879" stroke-width=".5"/>'),
 'text': ('Text', '<path d="M3 3h18v6h-2l-1-4h-4v15l3 1v1H7v-1l3-1V5H6L5 9H3z" fill="url(#blue)" stroke="#315a75" stroke-width=".7"/><path d="M4 3h16" stroke="#c9edff"/>'),
 'line': ('Line and curve', '<path d="M4 20C8 2 16 25 21 4" fill="none" stroke="#3c7fae" stroke-width="2"/><path d="m4 20 3-12M21 4l-3 12" stroke="#8b9da9" stroke-width=".8"/><rect x="2" y="18" width="4" height="4" fill="#eaf6ff" stroke="#477a9c"/><rect x="19" y="2" width="4" height="4" fill="#eaf6ff" stroke="#477a9c"/><circle cx="7" cy="8" r="1.5" fill="#e8b85b"/><circle cx="18" cy="16" r="1.5" fill="#e8b85b"/>'),
 'rectangle': ('Rectangle', '<rect x="3" y="5" width="18" height="15" fill="url(#shape)" stroke="#3e789e" stroke-width="1.3"/><path d="M4 6h16v13H4z" fill="none" stroke="#e0f7ff"/>'),
 'rectangle-rounded': ('Rounded rectangle', '<rect x="3" y="4" width="18" height="16" rx="4" fill="url(#shape)" stroke="#3e789e" stroke-width="1.3"/><rect x="4" y="5" width="16" height="14" rx="3" fill="none" stroke="#e0f7ff"/>'),
 'ellipse': ('Ellipse', '<ellipse cx="12" cy="12" rx="9" ry="8" fill="url(#shape)" stroke="#3e789e" stroke-width="1.3"/><ellipse cx="12" cy="12" rx="8" ry="7" fill="none" stroke="#e0f7ff"/>'),
 'freeformshape': ('Freeform shape', '<path d="m3 5 8 3 7-5 3 12-8 6-10-4 3-6z" fill="url(#shape)" stroke="#3e789e" stroke-width="1.3"/><path d="m5 7 6 3 6-4" fill="none" stroke="#e0f7ff"/>'),
}
DEFS = '''<defs>
<linearGradient id="blue" x2="0" y2="1"><stop stop-color="#acd9f5"/><stop offset=".5" stop-color="#5b9cc7"/><stop offset="1" stop-color="#2d668e"/></linearGradient>
<linearGradient id="shape" x2="0" y2="1"><stop stop-color="#e5f8ff"/><stop offset="1" stop-color="#86c2e5"/></linearGradient>
<linearGradient id="wood" x2=".8" y2="1"><stop stop-color="#ecd1a2"/><stop offset=".45" stop-color="#bf8a51"/><stop offset="1" stop-color="#89552f"/></linearGradient>
<linearGradient id="steel" x2="1" y2=".4"><stop stop-color="#f2f7fa"/><stop offset=".5" stop-color="#aabecb"/><stop offset="1" stop-color="#748a9a"/></linearGradient>
<linearGradient id="lens" x2=".7" y2="1"><stop stop-color="#f3fcff" stop-opacity=".85"/><stop offset="1" stop-color="#7bbce3" stop-opacity=".65"/></linearGradient>
<linearGradient id="skin" x2="1" y2="1"><stop stop-color="#ffe9c7"/><stop offset="1" stop-color="#d4a477"/></linearGradient>
<linearGradient id="spectrum"><stop stop-color="#25304d"/><stop offset=".45" stop-color="#4b8ed0"/><stop offset="1" stop-color="#f2fbff"/></linearGradient>
</defs>'''
for name,(title,paths) in icons.items():
    svg=f'<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><title>{title}</title>{DEFS}{paths}</svg>'
    ET.fromstring(svg)
    (OUT/f'pinta-tool-{name}.svg').write_text(svg+'\n')
# Source contact sheet: exactly the same icons at their intended size and 2x size, on both themes.
parts=['<svg xmlns="http://www.w3.org/2000/svg" width="1000" height="440"><rect width="1000" height="440" fill="#e7e9ec"/><text x="24" y="30" font-family="sans-serif" font-size="20" fill="#26384a">Pinta · Original desktop tool icons and toolbar tiles</text>']
for row,(bg,fg,border,tile) in enumerate([('#f7f7f7','#26384a','#a6afb8','#ffffff'),('#252525','#e8edf2','#61717d','#343b42')]):
    y=50+row*190
    parts.append(f'<rect x="16" y="{y}" width="968" height="180" fill="{bg}"/>')
    for i,(name,(title,paths)) in enumerate(icons.items()):
        x=28+(i%11)*87;yy=y+8+(i//11)*84
        raw=(OUT/f'pinta-tool-{name}.svg').read_text()
        for token in ['blue','shape','wood','steel','lens','skin','spectrum']:
            raw=raw.replace('id="'+token+'"','id="'+token+str(row)+str(i)+'"').replace('url(#'+token+')','url(#'+token+str(row)+str(i)+')')
        raw=raw.replace('width="24" height="24"',f'x="{x+18}" y="{yy+5}" width="40" height="40"')
        parts.append(f'<rect x="{x+13}" y="{yy}" width="50" height="50" fill="{tile}" stroke="{border}"/>')
        parts.append(raw)
        label=name.replace('select-','').replace('rectangle-rounded','round rect').replace('freeformshape','freeform').replace('paint','').replace('move-selection','move outline')
        parts.append(f'<text x="{x+38}" y="{yy+67}" font-family="sans-serif" font-size="10" text-anchor="middle" fill="{fg}">{label}</text>')
parts.append('</svg>')
(ROOT/'docs/assets/tool-icons.svg').write_text('\n'.join(parts))
print(f'Generated {len(icons)} original colored tool icons and contact sheet.')
