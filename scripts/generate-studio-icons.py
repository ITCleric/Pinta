#!/usr/bin/env python3
"""Original Studio & Play icon artwork. Builds 60 SVGs, 60 PNGs and a labeled atlas. MIT."""
import json, math, re, subprocess, shutil, textwrap
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor
ROOT=Path(__file__).resolve().parents[1]
items=json.loads((ROOT/'Pinta.Effects/Studio/catalog.json').read_text())
SVG=ROOT/'Pinta.Resources/icons/hicolor/scalable/actions'
PNG=ROOT/'Pinta.Resources/icons/hicolor/64x64/actions'
PNG.mkdir(parents=True,exist_ok=True)
g={
'SplitToning':'<circle cx="16" cy="16" r="9" fill="#6aa8e6"/><path d="M16 7a9 9 0 0 1 0 18z" fill="#ffd17b"/><path d="M16 7v18" stroke="white"/>',
'Vibrance':'<circle cx="13" cy="12" r="6" fill="#ff6580"/><circle cx="20" cy="18" r="6" fill="#62caff"/><circle cx="11" cy="21" r="6" fill="#b1e66f"/><path d="m22 5 1 3 3 1-3 1-1 3-1-3-3-1 3-1z" fill="#fff3a5"/>',
'ColorTemperature':'<path d="M12 20V8a3 3 0 0 1 6 0v12a5 5 0 1 1-6 0z" fill="#eaf5ff" stroke="#405f88"/><path d="M15 9v13" stroke="#f16b62" stroke-width="2.5"/><circle cx="15" cy="23" r="3" fill="#f16b62"/><path d="M21 8h4M21 12h3M21 16h4" stroke="#86d9ff" stroke-width="1.5"/>',
'HighlightRolloff':'<circle cx="14" cy="12" r="5" fill="#ffe794"/><path d="M14 4V2M14 22v-3M6 12H3M25 12h-3M7 5 5 3M21 5l2-2" stroke="#ffdc6a" stroke-width="2"/><path d="M22 16v9m-4-4 4 4 4-4" fill="none" stroke="white" stroke-width="2"/>',
'ShadowLift':'<path d="M16 5a10 10 0 1 0 9 15A10 10 0 0 1 16 5z" fill="#dbe8ff" stroke="#6484b8"/><path d="M22 23V11m-4 4 4-4 4 4" stroke="#ffd77a" stroke-width="2" fill="none"/>',
'FilmicContrast':'<path d="M6 5h21v22H6z" fill="#243b58"/><path d="M10 23c12 0 0-14 13-14" fill="none" stroke="#a8e3ff" stroke-width="2.5"/><path d="M7 8h2M7 13h2M7 18h2M7 23h2M24 8h2M24 13h2M24 18h2M24 23h2" stroke="#ffdf85"/>',
'MatteFade':'<rect x="6" y="6" width="20" height="20" fill="url(#gray)" stroke="#ecedfb"/><path d="M6 21h20v5H6z" fill="#c4b7d3"/><path d="m10 18 5-5 4 2 4-7" fill="none" stroke="white" stroke-width="1.6"/>',
'BleachBypass':'<path d="M16 4c-3 5-9 10-9 15a9 9 0 0 0 18 0c0-5-6-10-9-15z" fill="url(#gray)" stroke="#d6e5f0"/><path d="m9 20 13-9" stroke="#ffe9ac" stroke-width="3"/><path d="M10 21q0 4 4 4" fill="none" stroke="white"/>',
'DuotonePrint':''.join(f'<circle cx="{8+x*4}" cy="{8+y*4}" r="{1.7 if (x+y)%2 else 1.2}" fill="{"#ffd07a" if x+y>3 else "#8dd2ff"}"/>' for y in range(5) for x in range(5)),
'SelectiveColorPop':'<rect x="5" y="6" width="22" height="20" fill="#d9dce8"/><path d="m6 24 7-8 4 4 4-6 5 10z" fill="#77849a"/><path d="M17 16v9" stroke="#4b8665" stroke-width="2"/><path d="M17 16c-12-1-9-10-4-7-1-6 8-6 7 0 5-2 8 6-3 7z" fill="#f34f6c" stroke="#ba2e4b"/>',
'BloomDiffusion':'<circle cx="16" cy="16" r="12" fill="url(#glow)"/><path d="m16 6 2 8 8 2-8 2-2 8-2-8-8-2 8-2z" fill="#fff5be"/><circle cx="16" cy="16" r="3" fill="white"/>',
'OrtonDream':'<rect x="6" y="9" width="16" height="17" fill="#488bad" stroke="#d8efff"/><path d="m7 24 7-10 7 10z" fill="#9bd8bf"/><circle cx="22" cy="11" r="8" fill="url(#glow)"/><path d="m22 6 1 4 4 1-4 1-1 4-1-4-4-1 4-1z" fill="#fff6c7"/>',
'SoftFocus':'<circle cx="16" cy="16" r="11" fill="#b9e8ff" opacity=".3"/><circle cx="16" cy="16" r="8" fill="#b9e8ff" opacity=".4"/><circle cx="16" cy="16" r="5" fill="#effbff"/><path d="M5 7h4M7 5v4M23 23h4M25 21v4" stroke="white"/>',
'UnsharpDetail':'<path d="m5 25 9-18 11 18z" fill="#94b9d0" stroke="#ecf8ff"/><path d="m14 7-2 12 5-3z" fill="white"/><path d="M23 6v10M18 11h10" stroke="#ffda77" stroke-width="2"/>',
'HighPass':'<rect x="5" y="6" width="22" height="20" fill="#98a6b8"/><path d="M6 17h7V9h7v8h6" fill="none" stroke="#f4faff" stroke-width="2"/><path d="M6 20h7v-8h7v8h6" fill="none" stroke="#425c78" stroke-width="2"/>',
'Clarity':'<path d="m5 25 10-19 12 19z" fill="#ecf6ff" stroke="#3a6c96"/><path d="M15 6v19h12z" fill="#4c7eab"/><path d="m15 6-5 11 5-4 5 5z" fill="white"/>',
'TiltShift':'<rect x="5" y="6" width="22" height="20" fill="#6d9fc4"/><path d="M5 9h22M5 23h22" stroke="#c2e5ff" stroke-width="5" opacity=".6"/><path d="m5 20 22-9" stroke="white" stroke-width="3"/><path d="m5 17 22-9m-22 15 22-9" stroke="#ffdc8a" stroke-width=".7"/>',
'BokehDisc':'<circle cx="11" cy="12" r="7" fill="#84d8f8" fill-opacity=".7" stroke="#c1f1ff"/><circle cx="21" cy="15" r="6" fill="#fbe4a0" fill-opacity=".8"/><circle cx="13" cy="23" r="5" fill="#f4a4e0" fill-opacity=".75"/><circle cx="24" cy="6" r="2" fill="white"/>',
'ChromaticFringe':'<rect x="5" y="9" width="15" height="17" fill="none" stroke="#fb6a80" stroke-width="2.5"/><rect x="9" y="7" width="15" height="17" fill="none" stroke="#80edac" stroke-width="2.5"/><rect x="12" y="5" width="15" height="17" fill="none" stroke="#81c4ff" stroke-width="2.5"/>',
'AnamorphicStreak':'<path d="M3 16h26" stroke="#68c6fb" stroke-width="7" opacity=".3"/><path d="M3 16h26" stroke="#a6e4ff" stroke-width="2"/><circle cx="16" cy="16" r="5" fill="url(#glow)"/><circle cx="16" cy="16" r="2" fill="white"/>',
'FineFilmGrain':''.join(f'<circle cx="{6+(i*7)%21}" cy="{6+(i*11)%21}" r="{.6+(i%3)*.3}" fill="{"#fff1ce" if i%2 else "#5d4d45"}"/>' for i in range(53)),
'PaperFibers':'<path d="M7 4h13l6 6v18H7z" fill="#f5e8c8" stroke="#a68b61"/><path d="M20 4v7h6" fill="#d8c49c"/>'+''.join(f'<path d="M9 {13+i*2}q5 -2 14 0" fill="none" stroke="#bca77b" stroke-width=".7"/>' for i in range(6)),
'CanvasWeave':'<rect x="5" y="5" width="22" height="22" fill="#ead4a6"/>'+''.join(f'<path d="M{7+i*4} 5v22M5 {7+i*4}h22" stroke="{"#987951" if i%2 else "#fff1ce"}" stroke-width="2"/>' for i in range(5)),
'BrushedMetal':'<rect x="5" y="6" width="22" height="20" fill="url(#gray)" stroke="#f4fbff"/>'+''.join(f'<path d="M7 {9+i*2}h{14+(i%2)*4}" stroke="{"#6b859d" if i%2 else "#e7f2fa"}" stroke-width=".6"/>' for i in range(8)),
'RainGlass':'<path d="M12 5c-2 4-6 7-6 11a6 6 0 0 0 12 0c0-4-4-7-6-11z" fill="#8bd8f2" stroke="#4b8ea8"/><path d="M21 14c-2 3-4 5-4 8a4 4 0 0 0 8 0c0-3-2-5-4-8z" fill="#9be8fb" stroke="#4b8ea8"/><path d="M9 14q-2 5 2 6M19 22q0 3 2 3" fill="none" stroke="white" stroke-width="1.5"/>',
'WatercolorWash':'<path d="M5 12q1-10 10-6 8-3 10 4 5 10-4 13-10 9-15 0-5-6-1-11z" fill="#f5cf87"/><path d="M6 20q-1-13 10-9 12 4 3 12-8 5-13-3z" fill="#67b6c7" opacity=".8"/><path d="m18 23 8-18 2 1-7 18-4 3z" fill="#996743" stroke="#664a37"/>',
'Crosshatch':'<rect x="5" y="5" width="22" height="22" fill="#f4e4c2"/>'+''.join(f'<path d="M5 {8+i*4} {8+i*4} 5M{5+i*4} 27 27 {5+i*4}" stroke="#67513e"/>' for i in range(5))+'<path d="m7 8 17 17M6 14l12 12M14 6l12 12" stroke="#8d7051"/>',
'ComicInking':'<path d="M5 6h22v16H15l-7 6v-6H5z" fill="#fff1ce" stroke="#493f3d" stroke-width="1.5"/><path d="m10 17 7-9 6 5-9 6z" fill="#5b89b0" stroke="#3c4b5e"/><path d="m12 17 2 2-5 2z" fill="#313b47"/>',
'EngravedLines':'<rect x="5" y="5" width="22" height="22" fill="#f3dcb4"/>'+''.join(f'<path d="M6 {8+i*3}c5 -5 13 5 20 0" fill="none" stroke="#70513a" stroke-width="1.2"/>' for i in range(6)),
'WoodcutPrint':'<rect x="5" y="5" width="22" height="22" fill="#bd8956" stroke="#62442e"/>'+''.join(f'<ellipse cx="17" cy="16" rx="{3+i*3}" ry="{2+i*2}" fill="none" stroke="#f5d4a2" stroke-width="1.2"/>' for i in range(4))+'<path d="m5 11 6 3-6 4M27 10l-5 5 5 4" fill="none" stroke="#6e4a30"/>',
'Kaleidoscope':''.join(f'<path d="M16 16 16 4 26.4 10z" transform="rotate({i*60} 16 16)" fill="{["#8de4dc","#a6d5ff","#d9b5ef","#f7d47e","#f3a5a4","#a9df99"][i]}" stroke="#4b7586" stroke-width=".6"/>' for i in range(6)),
'MirrorQuadrants':'<path d="M16 5v22M5 16h22" stroke="#d5f6fb"/><path d="m7 7 7 2v5l-7-2zM25 7l-7 2v5l7-2zM7 25l7-2v-5l-7 2zM25 25l-7-2v-5l7 2z" fill="#b8e8ee" stroke="#4b8291"/>',
'TinyPlanet':'<circle cx="16" cy="16" r="10" fill="url(#sky)" stroke="#c5f4ff"/><path d="m9 11 6-3 3 4 5-1-2 6-5 1-1 5-6-4 3-4z" fill="#8bc47d" stroke="#4d926b"/><path d="M5 7q11-8 22 4" fill="none" stroke="#d6f5ff" stroke-width="1.5"/>',
'BarrelLens':'<path d="M8 5q8 3 16 0 4 11 0 22-8-3-16 0-4-11 0-22z" fill="#b7e2ea" stroke="#4a8293"/><path d="M11 6q-3 10 0 20M20 6q3 10 0 20M6 12q10-3 20 0M6 20q10 3 20 0" stroke="#5a94a7" fill="none"/>',
'PerspectiveSkew':'<path d="m8 5 18 4-5 18-17-8z" fill="#c0e6ed" stroke="#4b8599"/><path d="m6 12 17 6M13 6 9 21M20 8l-5 16" stroke="#5a91a5"/><path d="m7 3 18 4m-3-3 3 3-4 2" fill="none" stroke="#ffe59a"/>',
'SpiralTunnel':'<path d="M16 16q-4-4 1-6 8-1 8 7-1 13-13 10C-3 23 3 4 16 4q8 0 12 5" fill="none" stroke="#ccf6ff" stroke-width="2.5"/><path d="M16 16q3 0 2 3-3 3-7 0-4-5 1-10" fill="none" stroke="#88d5db"/>',
'RippleRings':''.join(f'<ellipse cx="16" cy="17" rx="{3+i*4}" ry="{2+i*2.5}" fill="none" stroke="{"#ccf7ff" if i%2 else "#79c3d6"}" stroke-width="1.4"/>' for i in range(3))+'<path d="m16 4 3 6a3 3 0 1 1-6 0z" fill="#a8edff"/>',
'SineWeave':''.join(f'<path d="M5 {8+i*5}c5 -8 17 8 22 0" fill="none" stroke="#d1f8ff" stroke-width="1.4"/>' for i in range(4))+''.join(f'<path d="M{8+i*5} 5c-8 5 8 17 0 22" fill="none" stroke="#75c6db"/>' for i in range(4)),
'PixelSort':''.join(f'<path d="M{5+(i%3)*3} {7+i*3}h{21-(i%3)*3}" stroke="{["#e77d93","#f2ba7b","#e2e28b","#92d7aa","#80c6df","#a9a5ed"][i]}" stroke-width="2"/>' for i in range(6)),
'PrismTiles':'<path d="M5 5h22v22H5z" fill="#b8e5ef"/><path d="m5 5 11 11H5zm11 0 11 11H16zM5 16l11 11H5zm11 0 11 11H16z" fill="#70a4c2"/><path d="M5 5 27 27M16 5v22M5 16h22" stroke="#e4faff"/>',
'OuterGlow':'<rect x="5" y="5" width="22" height="22" fill="#9ae5ff" opacity=".25"/><rect x="7" y="7" width="18" height="18" fill="#9ae5ff" opacity=".5"/><rect x="10" y="10" width="12" height="12" fill="#e7f8ff" stroke="#5c9aca"/>',
'LongShadow':'<path d="M8 9h12l9 9v10H18L8 18z" fill="#20364e" opacity=".8"/><rect x="6" y="6" width="13" height="13" fill="#d5edf9" stroke="#5f8ba8"/><path d="m21 7 5 5m-5-2 5 2-2-5" stroke="#ffdf8c" fill="none"/>',
'AlphaErode':'<rect x="5" y="5" width="22" height="22" fill="url(#check)"/><rect x="9" y="9" width="14" height="14" fill="#8ebddc" stroke="#4f7c9c"/><path d="m5 16 4 0m-2-2 2 2-2 2M27 16h-4m2-2-2 2 2 2" fill="none" stroke="#203f5b" stroke-width="1.5"/>',
'AlphaDilate':'<rect x="5" y="5" width="22" height="22" fill="url(#check)"/><rect x="7" y="7" width="18" height="18" fill="#8ebddc" stroke="#4f7c9c"/><rect x="11" y="11" width="10" height="10" fill="#c8e5f6"/><path d="M8 16H4m2-2-2 2 2 2M24 16h4m-2-2 2 2-2 2" fill="none" stroke="#203f5b" stroke-width="1.5"/>',
'EdgeColorize':'<path d="m16 5 3 7 8 1-6 5 2 8-7-4-7 4 2-8-6-5 8-1z" fill="#263b59" stroke="#80dcff" stroke-width="2"/><path d="m16 8 2 6 6 0-5 4" fill="none" stroke="#f6a0e8" stroke-width="1.5"/>',
'NormalMap':'<rect x="5" y="5" width="22" height="22" fill="#837fd5"/><path d="m8 23 8-15 8 15z" fill="#9d7ce4"/><path d="M16 8v15H8z" fill="#69b9e7"/><path d="m16 8 3 9-3-1-4 3z" fill="#ad99f2"/>',
'HeightLight':'<path d="M7 25v-9q9-10 18 0v9z" fill="url(#gray)" stroke="#b8e2fa"/><path d="M7 16q9-10 18 0" fill="none" stroke="white"/><circle cx="24" cy="7" r="3" fill="#ffe7a0"/><path d="M24 2V0M19 7h-3M27 11l3 3" stroke="#ffe7a0"/>',
'DistanceFade':'<rect x="5" y="5" width="22" height="22" fill="url(#check)"/><circle cx="16" cy="16" r="11" fill="url(#fade)"/><circle cx="16" cy="16" r="3" fill="#d5f1ff"/>',
'RoundedCorners':'<path d="M7 25V12a5 5 0 0 1 5-5h13v18z" fill="#c5e5f6" stroke="#5a8cac"/><path d="M7 7h7M7 7v7" stroke="#93b5cf" stroke-dasharray="2 1"/><path d="m13 18 3 3 7-8" fill="none" stroke="#4b9e7b" stroke-width="2"/>',
'SeamlessBlend':'<path d="M5 6h10v8h-4v6h4v7H5z" fill="#9ecdea" stroke="#5889ad"/><path d="M17 6h10v21H17v-9h-4v-2h4z" fill="#d2edfb" stroke="#5889ad"/><path d="m10 4 4-2 4 2M14 2v4" fill="none" stroke="#ffe3a2"/>',
'VhsTape':'<rect x="5" y="8" width="22" height="18" rx="1" fill="#2e405b" stroke="#c2d9ed"/><rect x="7" y="11" width="18" height="7" fill="#e7e5d2"/><circle cx="11" cy="14.5" r="2.5" fill="#344966"/><circle cx="21" cy="14.5" r="2.5" fill="#344966"/><path d="M8 23h16" stroke="#ef8ca3" stroke-width="2"/><path d="M7 6h7M17 5h10" stroke="#7fe4ef"/>',
'CrtScreen':'<path d="M5 5h22v18H5z" fill="#2f3c58" stroke="#b1c7db"/><rect x="8" y="8" width="16" height="12" rx="2" fill="#6b94b5"/><path d="M9 10h14M9 13h14M9 16h14M9 19h14" stroke="#b1e5ed" opacity=".6"/><path d="M13 24h6v3h5v1H8v-1h5z" fill="#acbbcd"/>',
'PocketPalette':'<rect x="7" y="3" width="18" height="26" rx="2" fill="#e3dfd3" stroke="#6e6b6c"/><rect x="10" y="6" width="12" height="10" fill="#5a7448" stroke="#939a83"/><path d="M11 9h5v4h5" stroke="#c4d895" fill="none"/><path d="M10 21h6M13 18v6" stroke="#566273" stroke-width="2"/><circle cx="21" cy="21" r="2" fill="#c46b91"/>',
'ThermalVision':'<path d="M15 5q9 0 8 10l4 5-4 1v6H11v-7q-6-4-5-9t9-6z" fill="url(#heat)" stroke="#b5d0ec"/><circle cx="18" cy="13" r="2" fill="#ffef9e"/>',
'AsciiMosaic':''.join(f'<rect x="{6+x*2}" y="{8+y*2}" width="1.5" height="1.5" fill="#e8f3fc"/>' for y,row in enumerate([10,31,10,10,31,10,0]) for x in range(5) if row&(1<<(4-x)))+'<path d="M20 9h7M23.5 6v6M19 22h8M19 25h8" stroke="#ffdfa1"/>',
'EmojiMosaic':'<circle cx="16" cy="16" r="11" fill="#ffda76" stroke="#bc8c44"/><circle cx="12" cy="13" r="1.5" fill="#65503b"/><circle cx="20" cy="13" r="1.5" fill="#65503b"/><path d="M10 18q6 9 12 0z" fill="#71503d"/><path d="M11 18h10v2H11z" fill="white"/>',
'PixelConfetti':''.join(f'<rect x="{5+(i*7)%22}" y="{5+(i*11)%22}" width="3" height="6" transform="rotate({i*23} {6+(i*7)%22} {8+(i*11)%22})" fill="{["#ffda81","#8de6ee","#faa5d9","#b4e58b"][i%4]}"/>' for i in range(15)),
'ComicBurst':'<path d="m16 3 2 8 7-6-3 9 8 2-9 3 6 8-9-5-2 9-3-9-8 5 5-8-9-3 9-2-3-9 7 6z" fill="#ffde71" stroke="#e684a4" stroke-width="1.4"/><path d="m13 12 7-2-2 7 5 2-8 3-3-5z" fill="#ff9cb8"/>',
'LaserGrid':'<path d="M4 6h24v21H4z" fill="#243452"/><path d="M4 13h24M4 18h24M4 25h24M16 12 4 27M16 12l-4 15M16 12l4 15M16 12l12 15" stroke="#fa85d6" stroke-width="1.2"/><path d="M5 11h22" stroke="#90e6f8" stroke-width="2"/>',
'PartyPixelate':''.join(f'<rect x="{6+x*5}" y="{6+y*5}" width="4.5" height="4.5" fill="{["#f6a1ba","#ffe08b","#a1dfd0","#92c6f2","#c9a6ec"][(x*2+y*3)%5]}"/>' for y in range(4) for x in range(4))+'<path d="m25 3 1 3 3 1-3 1-1 3-1-3-3-1 3-1z" fill="white"/>',
}
assert len(g)==60 and set(g)=={x['kind'] for x in items}
palettes=[('#b39ce6','#634b9f'),('#79c8ed','#326eaa'),('#d9b784','#876044'),('#86cfcb','#3a838b'),('#9ebbd6','#4d6c94'),('#df9bc5','#9b4f8a')]
def defs(top,bottom):return f'''<defs><linearGradient id="tile" x2="0" y2="1"><stop stop-color="{top}"/><stop offset="1" stop-color="{bottom}"/></linearGradient><linearGradient id="shine" x2="0" y2="1"><stop stop-color="white" stop-opacity=".35"/><stop offset="1" stop-color="white" stop-opacity="0"/></linearGradient><linearGradient id="gray"><stop stop-color="#5a7189"/><stop offset=".5" stop-color="#dce8ef"/><stop offset="1" stop-color="#7892a9"/></linearGradient><radialGradient id="glow"><stop stop-color="white"/><stop offset=".3" stop-color="#f4edb6" stop-opacity=".9"/><stop offset="1" stop-color="#c0e4ff" stop-opacity="0"/></radialGradient><radialGradient id="sky" cx=".3" cy=".2"><stop stop-color="#f1fbff"/><stop offset="1" stop-color="#4d9cc1"/></radialGradient><radialGradient id="fade"><stop stop-color="#d6f0ff"/><stop offset="1" stop-color="#6099d3" stop-opacity="0"/></radialGradient><linearGradient id="heat" x2="0" y2="1"><stop stop-color="#4b72ed"/><stop offset=".3" stop-color="#5fe3ae"/><stop offset=".6" stop-color="#fff16d"/><stop offset="1" stop-color="#ed658c"/></linearGradient><pattern id="check" width="6" height="6" patternUnits="userSpaceOnUse"><path d="M0 0h6v6H0z" fill="#f1f3f7"/><path d="M0 0h3v3H0zM3 3h3v3H3z" fill="#a2b6c7"/></pattern></defs>'''
converter=shutil.which('rsvg-convert')
if not converter:raise SystemExit('Install librsvg (rsvg-convert) to render PNG assets.')
paths=[]
for i,item in enumerate(items):
    top,bottom=palettes[i//10]
    body=f'<svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 32 32"><title>{item["name"]}</title>{defs(top,bottom)}<rect x="3" y="4" width="27" height="27" rx="2" fill="#1c2b3c" opacity=".2"/><rect x="2.5" y="2.5" width="27" height="27" rx="1.5" fill="url(#tile)" stroke="{bottom}"/><rect x="3.5" y="3.5" width="25" height="25" rx=".5" fill="none" stroke="white" stroke-opacity=".5"/>{g[item["kind"]]}<path d="M4 4h24v10q-12 5-24 0z" fill="url(#shine)"/></svg>'
    svg=SVG/f'pinta-studio-{item["slug"]}.svg';svg.write_text(body+'\n');paths.append(svg)
def convert(path):
    output=PNG/(path.stem+'.png')
    subprocess.run([converter,'--width','64','--height','64',str(path),'-o',str(output)],check=True,capture_output=True)
with ThreadPoolExecutor(max_workers=4) as pool:list(pool.map(convert,paths))
parts=['<svg xmlns="http://www.w3.org/2000/svg" width="1200" height="850"><rect width="1200" height="850" fill="#edf0f4"/><text x="24" y="30" font-family="sans-serif" font-size="22" fill="#23364c">Pinta · Studio &amp; Play · 60 original effect icons</text>']
atlas=[]
for i,(item,path) in enumerate(zip(items,paths)):
    row,col=divmod(i,10);xx=col*120;yy=50+row*132
    if col==0:parts.append(f'<path d="M14 {yy}h1172" stroke="#bfcbd6"/><text x="18" y="{yy+17}" font-family="sans-serif" font-size="12" font-weight="bold" fill="#3a5068">{item["category"].replace("&","&amp;")}</text>')
    body=path.read_text();prefix=f'i{i}-'
    for token in re.findall(r'id="([^"]+)"',body):body=body.replace(f'id="{token}"',f'id="{prefix+token}"').replace(f'url(#{token})',f'url(#{prefix+token})')
    body=body.replace('width="32" height="32"',f'x="{xx+24}" y="{yy+23}" width="72" height="72"',1)
    parts.append(body)
    for j,line in enumerate(textwrap.wrap(item['name'],19)):
        parts.append(f'<text x="{xx+60}" y="{yy+110+j*13}" text-anchor="middle" font-family="sans-serif" font-size="11" fill="#263c54">{line}</text>')
    atlas.append(dict(number=i+1,id=item['slug'],name=item['name'],x=xx+24,y=yy+23,width=72,height=72,png=f'pinta-studio-{item["slug"]}.png'))
parts.append('</svg>')
preview=ROOT/'docs/assets/studio-spritesheet.svg';preview.write_text('\n'.join(parts))
subprocess.run([converter,str(preview),'-o',str(preview.with_suffix('.png'))],check=True,capture_output=True)
(ROOT/'docs/assets/studio-spritesheet.json').write_text(json.dumps(dict(width=1200,height=850,icons=atlas),indent=2)+'\n')
print('Built 60 SVG icons, 60 separate 64px PNGs and an indexed 1200x850 PNG spritesheet.')
