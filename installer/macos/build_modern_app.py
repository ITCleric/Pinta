#!/usr/bin/env python3
"""Package the fork as a self-contained, ad-hoc signed local macOS application.

Uses the existing upstream GTK dependency collector. No Developer ID identity or
notarization credentials are required for this local installation workflow.
"""
import argparse
from datetime import datetime
from pathlib import Path
import platform
import plistlib
import shutil
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[2]
APP_ID = 'io.github.itcleric.pintamodern'
APP_NAME = 'Pinta Modern.app'


def run(*args, **kwargs):
    subprocess.run([str(a) for a in args], check=True, **kwargs)


def hicolor_index(folder):
    directories = sorted(p for p in folder.glob('*/*') if p.is_dir())
    lines = ['[Icon Theme]', 'Name=Hicolor', 'Comment=Pinta Modern icons', 'Hidden=true',
             'Directories=' + ','.join(str(p.relative_to(folder)) for p in directories), '']
    for p in directories:
        size = p.parent.name
        lines += [f'[{p.relative_to(folder)}]', f'Context={p.name.title()}']
        if size == 'scalable':
            lines += ['Size=48', 'MinSize=8', 'MaxSize=1024', 'Type=Scalable']
        else:
            lines += [f'Size={int(size.split("x")[0])}', 'Type=Fixed']
        lines.append('')
    (folder / 'index.theme').write_text('\n'.join(lines))


def replace_own_bundle(source, destination):
    if destination.is_symlink():
        raise RuntimeError(f'Refusing to replace a symbolic link: {destination}')
    if destination.exists():
        with (destination / 'Contents/Info.plist').open('rb') as stream:
            info = plistlib.load(stream)
        if info.get('CFBundleIdentifier') != APP_ID or destination.is_symlink():
            raise RuntimeError(f'Refusing to replace an unrelated app: {destination}')
        backups = ROOT / 'artifacts/macos-backups'
        backups.mkdir(parents=True, exist_ok=True)
        shutil.move(str(destination), str(backups / (datetime.now().strftime('%Y%m%d-%H%M%S-%f-') + APP_NAME)))
    # Preserve extended-attribute signatures on managed .NET assemblies.
    run('/usr/bin/ditto', '--rsrc', '--extattr', source, destination)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--runtime', default='osx-arm64' if platform.machine() == 'arm64' else 'osx-x64', choices=['osx-arm64', 'osx-x64'])
    parser.add_argument('--publish-dir', type=Path, help='Reuse an existing self-contained publish directory')
    parser.add_argument('--install', action='store_true', help='Install the verified bundle into /Applications')
    args = parser.parse_args()
    output = ROOT / 'artifacts/macos'
    output.mkdir(parents=True, exist_ok=True)
    publish = args.publish_dir.resolve() if args.publish_dir else ROOT / 'artifacts/macos-publish'
    if not args.publish_dir:
        dotnet = ROOT / '.dotnet/dotnet'
        run(dotnet if dotnet.exists() else 'dotnet', 'publish', ROOT / 'Pinta/Pinta.csproj', '-c', 'Release', '-r', args.runtime,
            '--self-contained', 'true', '-p:BuildTranslations=true', '-o', publish, '--nologo', cwd=ROOT)
    if not (publish / 'libcoreclr.dylib').is_file():
        raise RuntimeError('A self-contained .NET publish is required.')
    with tempfile.TemporaryDirectory(prefix='pinta-package-', dir=output) as temporary:
        app = Path(temporary) / APP_NAME
        contents = app / 'Contents'
        executable_dir = contents / 'MacOS'
        resources = contents / 'Resources'
        share = resources / 'share'
        shutil.copytree(publish, executable_dir)
        share.mkdir(parents=True)
        for name in ('icons', 'locale'):
            shutil.move(str(executable_dir / name), str(share / name))
        for debug_file in executable_dir.glob('*.pdb'):
            debug_file.unlink()
        with (ROOT / 'installer/macos/Info.plist').open('rb') as stream:
            info = plistlib.load(stream)
        info.update(CFBundleIdentifier=APP_ID, CFBundleName='Pinta Modern', CFBundleDisplayName='Pinta Modern',
                    CFBundleIconFile='pinta-modern.icns', NSHighResolutionCapable=True)
        for document in info.get('CFBundleDocumentTypes', []):
            document['CFBundleTypeIconFile'] = 'pinta-modern.icns'
        for declaration in info.get('UTImportedTypeDeclarations', []):
            declaration['UTTypeIconFile'] = 'pinta-modern.icns'
        with (contents / 'Info.plist').open('wb') as stream:
            plistlib.dump(info, stream)
        shutil.copyfile(ROOT / 'installer/macos/pinta-modern.icns', resources / 'pinta-modern.icns')
        shutil.copyfile(ROOT / 'license-mit.txt', resources / 'license-mit.txt')
        shutil.copyfile(ROOT / 'license-pdn.txt', resources / 'license-pdn.txt')
        run(sys.executable, ROOT / 'installer/macos/bundle_gtk.py', '--runtime', args.runtime, '--resource_dir', resources)
        hicolor_index(share / 'icons/hicolor')
        executable = executable_dir / 'Pinta'
        run('install_name_tool', '-add_rpath', '@executable_path/../Resources/lib', executable)
        # Modified Homebrew binaries and the apphost must be re-signed on Apple Silicon.
        for directory in (resources, executable_dir):
            for library in sorted(directory.rglob('*')):
                if library.is_file() and not library.is_symlink() and library.suffix in ('.dylib', '.so'):
                    run('codesign', '--force', '--sign', '-', '--timestamp=none', library)
        run('codesign', '--deep', '--force', '--sign', '-', '--timestamp=none', '--entitlements', ROOT / 'installer/macos/entitlements-local.plist', app)
        run('codesign', '--verify', '--deep', '--strict', '--verbose=2', app)
        run('plutil', '-lint', contents / 'Info.plist')
        destination = output / APP_NAME
        replace_own_bundle(app, destination)
    if args.install:
        destination = Path('/Applications') / APP_NAME
        replace_own_bundle(output / APP_NAME, destination)
        run('codesign', '--verify', '--deep', '--strict', '--verbose=2', destination)
        run('/System/Library/Frameworks/CoreServices.framework/Frameworks/LaunchServices.framework/Support/lsregister', '-f', destination)
    print(f'Application ready: {destination}', flush=True)


if __name__ == '__main__':
    main()
