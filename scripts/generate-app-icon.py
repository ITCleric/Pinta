#!/usr/bin/env python3
"""Export the original app SVG into GTK PNGs, a Windows ICO and a complete macOS ICNS."""
from pathlib import Path
import struct
import subprocess
import tempfile

root = Path(__file__).resolve().parents[1]
name = 'io.github.itcleric.pintamodern'
source = root / f'Pinta.Resources/icons/hicolor/scalable/apps/{name}.svg'
with tempfile.TemporaryDirectory(prefix='pinta-icon-') as temporary:
    folder = Path(temporary)
    iconset = folder / 'PintaModern.iconset'
    iconset.mkdir()
    pngs = {}
    for size in (16, 32, 48, 64, 96, 128, 256, 512, 1024):
        path = folder / f'{size}.png'
        subprocess.run(['rsvg-convert', '-w', str(size), '-h', str(size), str(source), '-o', str(path)], check=True)
        pngs[size] = path.read_bytes()
        destination = root / f'Pinta.Resources/icons/hicolor/{size}x{size}/apps/{name}.png'
        destination.parent.mkdir(parents=True, exist_ok=True)
        destination.write_bytes(pngs[size])
    for size in (16, 32, 128, 256, 512):
        (iconset / f'icon_{size}x{size}.png').write_bytes(pngs[size])
        (iconset / f'icon_{size}x{size}@2x.png').write_bytes(pngs[size*2])
    subprocess.run(['iconutil', '-c', 'icns', str(iconset), '-o', str(root / 'installer/macos/pinta-modern.icns')], check=True)
    sizes = (16, 32, 48, 64, 128, 256)
    header = struct.pack('<HHH', 0, 1, len(sizes))
    offset = 6 + 16 * len(sizes)
    entries = []
    for size in sizes:
        data = pngs[size]
        entries.append(struct.pack('<BBBBHHII', size % 256, size % 256, 0, 0, 1, 32, len(data), offset))
        offset += len(data)
    (root / 'Pinta/Pinta.ico').write_bytes(header + b''.join(entries) + b''.join(pngs[size] for size in sizes))
print('Exported GTK PNGs, Windows ICO and macOS ICNS from the original app icon.')
