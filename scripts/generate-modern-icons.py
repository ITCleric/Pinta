#!/usr/bin/env python3
"""Build original, editable Aero-style SVG effect icons. MIT licensed."""
from pathlib import Path
import math
ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Pinta.Resources/icons/hicolor/scalable/actions'
OUT.mkdir(parents=True, exist_ok=True)
icons = {
 'gradient-map': ('#ca8be5','#6642a6','<path d="M6 9h20v15H6z" fill="#572a8b"/><path d="m6 24 9-15h11v15z" fill="#ffce68"/>'),
 'drop-shadow': ('#9fb5c9','#425a72','<rect x="12" y="12" width="15" height="15" rx="2" fill="#27364c" opacity=".7"/><rect x="6" y="6" width="15" height="15" rx="2" fill="#f2f9ff"/>'),
 'pointillism': ('#f1ba77','#ac6434','<circle cx="9" cy="10" r="3" fill="#ef6156"/><circle cx="20" cy="8" r="3" fill="#f7e17d"/><circle cx="15" cy="16" r="3" fill="#77baff"/><circle cx="24" cy="19" r="3" fill="#b8e773"/><circle cx="9" cy="24" r="3" fill="#9de8cb"/><circle cx="20" cy="26" r="2" fill="#f9dbaf"/>'),
 'stained-glass': ('#e8a9da','#854685','<path d="m6 6 11 2-3 10-8-3z" fill="#6fc7ee" stroke="#403353"/><path d="m17 8 9-2v14l-12-2z" fill="#f8df72" stroke="#403353"/><path d="m6 15 8 3 5 8H6z" fill="#f08579" stroke="#403353"/><path d="m14 18 12 2v6h-7z" fill="#a7d987" stroke="#403353"/>'),
 'transparency': ('#42b7ed','#126da8','<path d="M8 8h16v16H8z" fill="url(#check)"/><path d="M16 8h8v16h-8z" fill="white" opacity=".6"/>'),
 'invert-alpha': ('#9aabb9','#475669','<circle cx="16" cy="16" r="9" fill="#fff"/><path d="M16 7a9 9 0 0 1 0 18z" fill="#27384a"/>'),
 'color-balance': ('#49cddc','#087f9b','<path d="M16 7v17M8 11h16M10 11l-4 8h8zM22 11l-4 8h8z" fill="#ffdb71" stroke="#725d1e" stroke-width="1.2"/>'),
 'monochrome': ('#a7b7c7','#455769','<circle cx="16" cy="16" r="9" fill="white"/><path d="M16 7a9 9 0 0 1 0 18z" fill="#20262c"/>'),
 'alpha-gray': ('#c3d2e2','#526881','<path d="M7 8h7v16H7z" fill="url(#check)"/><path d="M20 8h5v16h-5z" fill="white"/><path d="m14 14 4 2-4 2z" fill="#fff"/>'),
 'gray-alpha': ('#95b9d3','#355c80','<path d="M7 8h5v16H7z" fill="white"/><path d="M19 8h7v16h-7z" fill="url(#check)"/><path d="m13 14 4 2-4 2z" fill="#fff"/>'),
 'complementary': ('#b37bea','#593e9c','<circle cx="12" cy="14" r="7" fill="#ffb72e"/><circle cx="20" cy="19" r="7" fill="#5d9cff" opacity=".9"/>'),
 'combined': ('#50c5f1','#126eab','<path d="M9 7v18M16 7v18M23 7v18" stroke="white" stroke-width="2"/><path d="M6 11h6M13 21h6M20 14h6" stroke="#ffda5b" stroke-width="5"/>'),
 'channels': ('#8e8fe7','#414196','<path d="M7 10h18M7 16h18M7 22h18" stroke="white" stroke-width="1.2"/><circle cx="11" cy="10" r="3" fill="#ff5b4f"/><circle cx="21" cy="16" r="3" fill="#6ee26c"/><circle cx="15" cy="22" r="3" fill="#62b7ff"/>'),
 'grid': ('#7dbbea','#2c6a9e','<path d="M7 7h18v18H7z" fill="#fff"/><path d="M7 13h18M7 19h18M13 7v18M19 7v18" stroke="#24749d" stroke-width="2"/>'),
 'stars': ('#f7d87c','#c17b1c','<path d="m16 5 3.2 7.1 7.8.8-5.8 5.2 1.5 7.7-6.7-4-6.7 4 1.5-7.7L5 12.9l7.8-.8z" fill="#fff1a3" stroke="#956115"/>'),
 'ordered-dither': ('#8caac8','#3a6285',''.join(f'<rect x="{7+x*4}" y="{7+y*4}" width="3" height="3" fill="white" opacity="{.2+((x*3+y*7)%5)*.2}"/>' for x in range(5) for y in range(5))),
 'halftone': ('#a1bacb','#405d76',''.join(f'<circle cx="{8+x*5}" cy="{8+y*5}" r="{.7+x*.35}" fill="white"/>' for x in range(4) for y in range(4))),
 'spherize': ('#8be4f7','#1672b9','<circle cx="16" cy="16" r="10" fill="url(#orb)" stroke="#1b5a8c"/><ellipse cx="16" cy="16" rx="5" ry="10" fill="none" stroke="white" opacity=".8"/><path d="M6 16h20" stroke="white"/>'),
 'waves': ('#8adeef','#147ba9','<path d="M5 11q5-7 11 0t11 0M5 17q5-7 11 0t11 0M5 23q5-7 11 0t11 0" stroke="white" stroke-width="2.5" fill="none"/>'),
 'mosaic': ('#e8b66d','#a46032','<path d="M6 7h8v8H6z" fill="#fff3a1"/><path d="M17 7h9v8h-9z" fill="#69c8ef"/><path d="M6 18h8v8H6z" fill="#91d979"/><path d="M17 18h9v8h-9z" fill="#f38369"/>'),
 'linocut': ('#b7b1a6','#5a554e','<path d="M6 25 12 7l4 11 5-8 6 15z" fill="#fff9e9"/><path d="m9 23 4-8 2 8m3-1 3-7 3 8" fill="#33302b"/>'),
 'chroma-key': ('#a6e36d','#388430','<path d="M7 7h18v18H7z" fill="url(#check)"/><path d="m7 25 7-17 6 11 5-9v15z" fill="#47d642"/><circle cx="22" cy="10" r="4" fill="#ffe680"/>'),
 'inner-shadow': ('#acb7d1','#4a5b83','<rect x="6" y="6" width="20" height="20" rx="3" fill="#f4f8ff"/><path d="M6 24V6h20v5H11v13z" fill="#526a8b"/>'),
 'bevel': ('#c1d7eb','#4e7d9d','<path d="M6 7h20v19H6z" fill="#87bddf"/><path d="m6 7 5 5h10l5-5z" fill="#edfaff"/><path d="m6 7 5 5v9l-5 5z" fill="#c4e8ff"/><path d="m6 26 5-5h10l5 5zM26 7l-5 5v9l5 5z" fill="#2f6a95"/>'),
}

def svg(name, top, bottom, glyph):
 return f'''<svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 32 32"><title>{name}</title><defs>
 <linearGradient id="body" x2="0" y2="1"><stop stop-color="{top}"/><stop offset="1" stop-color="{bottom}"/></linearGradient>
 <linearGradient id="shine" x2="0" y2="1"><stop stop-color="white" stop-opacity=".55"/><stop offset="1" stop-color="white" stop-opacity="0"/></linearGradient>
 <radialGradient id="orb" cx=".3" cy=".2" r=".8"><stop stop-color="white"/><stop offset=".4" stop-color="#71d2ff"/><stop offset="1" stop-color="#1666ad"/></radialGradient>
 <pattern id="check" width="8" height="8" patternUnits="userSpaceOnUse"><path d="M0 0h8v8H0z" fill="#fff"/><path d="M0 0h4v4H0zM4 4h4v4H4z" fill="#a4b4c5"/></pattern></defs>
 <rect x="3" y="4" width="27" height="27" rx="5" fill="#172c43" opacity=".22"/>
 <rect x="2.5" y="2.5" width="27" height="27" rx="5" fill="url(#body)" stroke="{bottom}"/>
 <rect x="3.5" y="3.5" width="25" height="25" rx="4" fill="none" stroke="white" stroke-opacity=".65"/>
 {glyph}<path d="M7 4h18q3 0 3 4v7Q16 20 4 15V8q0-4 3-4z" fill="url(#shine)"/>
 </svg>'''
for name,(a,b,glyph) in icons.items():
 (OUT/f'pinta-modern-{name}.svg').write_text(svg(name,a,b,glyph))
print(f'Created {len(icons)} original SVG icons in {OUT}')
