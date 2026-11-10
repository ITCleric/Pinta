#!/usr/bin/env python3
"""Original square desktop icons for palette tabs and plugin manager tabs."""
from pathlib import Path
import subprocess
root=Path(__file__).resolve().parents[1]
icons={
 'tab-tools':('<path d="m9 24 12-16 4 3-13 15z" fill="#e5b66d" stroke="#785326"/><path d="M7 21q-4 2-2 6 7 0 8-3l-4-3z" fill="#385574"/><path d="m8 7 17 17-3 3L5 10V6z" fill="#b4d7ed" stroke="#45647c"/>','#8fc9ef','#3a7aa9'),
 'tab-colors':('<circle cx="16" cy="16" r="10" fill="#ff7285"/><path d="M16 16 16 6a10 10 0 0 1 9 14z" fill="#ffdd6a"/><path d="m16 16 9 4a10 10 0 0 1-14 5z" fill="#8fe1a1"/><path d="m16 16-5 9a10 10 0 0 1-4-14z" fill="#77c9f4"/><path d="m16 16-9-5a10 10 0 0 1 9-5z" fill="#b99aee"/><circle cx="16" cy="16" r="3" fill="white"/>','#bcd9ed','#5a83b6'),
 'tab-history':('<circle cx="17" cy="15" r="9" fill="#f6fcff" stroke="#365d82"/><path d="M17 9v7l5 3" fill="none" stroke="#3e6b94" stroke-width="2"/><path d="M11 24H5v-6m0 6q9 7 19 0" fill="none" stroke="#ffcf68" stroke-width="3"/>','#8fbde1','#466f9e'),
 'tab-layers':('<path d="m5 21 11-5 11 5-11 6z" fill="#6b96c4" stroke="#345779"/><path d="m5 16 11-5 11 5-11 6z" fill="#a4cce8" stroke="#345779"/><path d="m5 11 11-5 11 5-11 6z" fill="#effaff" stroke="#345779"/>','#a5cfe8','#4579ab'),
 'plugin-gallery':('<path d="M6 7h20v18H6z" fill="#ecf7ff" stroke="#49678a"/><path d="M9 10h5v5H9z" fill="#ffcf69"/><path d="M18 10h5v5h-5z" fill="#78c2ed"/><path d="M9 18h5v4H9z" fill="#9bd784"/><path d="M18 18h5v4h-5z" fill="#df90c7"/>','#adcce9','#526ea3'),
 'plugin-installed':('<path d="m6 10 10-5 10 5v14l-10 4-10-4z" fill="#e6c88d" stroke="#886841"/><path d="m6 10 10 5 10-5M16 15v13" fill="none" stroke="#9a784a"/><path d="m15 21 4 4 8-10" fill="none" stroke="#27855a" stroke-width="4"/>','#a7d8ca','#477e77'),
 'plugin-updates':('<path d="M8 12a9 9 0 0 1 16-2l2 4M26 7v7h-7M24 21a9 9 0 0 1-16 2l-2-4M6 26v-7h7" fill="none" stroke="#eafaff" stroke-width="3"/>','#80c6ef','#3b76b2'),
 'plugin-default':('<path d="M7 8h7q-3-6 3-6t3 6h6v7q6-3 6 3t-6 3v6h-7q3-6-3-6t-3 6H7z" transform="translate(1 2) scale(.85)" fill="#f5d477" stroke="#7e663d" stroke-width="1.5"/>','#b6c3df','#6b72a7'),
}
for name,(glyph,top,bottom) in icons.items():
    svg=f'''<svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 32 32"><title>{name}</title><defs><linearGradient id="tile" x2="0" y2="1"><stop stop-color="{top}"/><stop offset="1" stop-color="{bottom}"/></linearGradient></defs><rect x="2" y="3" width="28" height="28" rx="1" fill="#20364a" opacity=".2"/><rect x="1.5" y="1.5" width="28" height="28" rx="1" fill="url(#tile)" stroke="{bottom}"/><path d="M3 16V3h25v7Q16 18 3 16z" fill="white" opacity=".2"/>{glyph}<rect x="2.5" y="2.5" width="26" height="26" rx=".5" fill="none" stroke="white" stroke-opacity=".6"/></svg>'''
    source=root/f'Pinta.Resources/icons/hicolor/scalable/actions/pinta-{name}.svg'
    source.write_text(svg)
    target=root/f'Pinta.Resources/icons/hicolor/64x64/actions/pinta-{name}.png'
    target.parent.mkdir(parents=True,exist_ok=True)
    subprocess.run(['rsvg-convert','-w','64','-h','64',str(source),'-o',str(target)],check=True)
print('Generated 8 original tab/plugin SVG + PNG icon pairs.')
