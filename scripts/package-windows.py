"""Validate a self-contained Godot Windows export, then package it without build caches."""
import hashlib
import json
from pathlib import Path
import shutil
import struct
import sys
import zipfile

package, destination, nuget = map(Path, sys.argv[1:])
data = package / 'data_SpaceSim.Game_windows_x86_64'
required = ['SpaceSim.Game.dll', 'SpaceSim.Core.dll', 'GodotSharp.dll',
            'coreclr.dll', 'hostfxr.dll', 'hostpolicy.dll',
            'System.Private.CoreLib.dll', 'SpaceSim.Game.runtimeconfig.json']
for name in required:
    path = data / name
    if not path.is_file() or path.stat().st_size == 0:
        raise SystemExit(f'Missing runtime component: {name}')
exe = (package / 'SpaceSim.exe').read_bytes()
pe = struct.unpack_from('<I', exe, 0x3C)[0]
assert exe[:2] == b'MZ' and exe[pe:pe + 4] == b'PE\0\0', 'Invalid Windows executable'
assert struct.unpack_from('<H', exe, pe + 4)[0] == 0x8664, 'Expected Windows x64'
assert (package / 'SpaceSim.pck').stat().st_size > 0, 'Missing game resources'
config = json.loads((data / 'SpaceSim.Game.runtimeconfig.json').read_text())['runtimeOptions']
assert 'framework' not in config and 'frameworks' not in config, 'Export needs an external .NET runtime'
framework = next(f for f in config['includedFrameworks'] if f['name'] == 'Microsoft.NETCore.App')
runtime_package = nuget / 'microsoft.netcore.app.runtime.win-x64' / framework['version']
for name in ['LICENSE.TXT', 'THIRD-PARTY-NOTICES.TXT']:
    shutil.copyfile(runtime_package / name, package / 'licenses' / f'DotNet-{name}')

# A manifest permits checking individual files after copying or extracting the ZIP.
files = sorted(p for p in package.rglob('*') if p.is_file())
manifest = ''.join(f'{hashlib.sha256(p.read_bytes()).hexdigest()}  {p.relative_to(package).as_posix()}\n' for p in files)
(package / 'SHA256SUMS.txt').write_text(manifest, encoding='utf-8')
zip_path = destination / f'{package.name}.zip'
partial = zip_path.with_suffix('.zip.part')
with zipfile.ZipFile(partial, 'w', compression=zipfile.ZIP_DEFLATED, compresslevel=6) as z:
    for path in sorted(package.rglob('*')):
        if path.is_file():
            z.write(path, path.relative_to(package.parent))
with zipfile.ZipFile(partial) as z:
    bad = z.testzip()
    assert bad is None, f'ZIP integrity failure: {bad}'
partial.replace(zip_path)
checksum = hashlib.sha256(zip_path.read_bytes()).hexdigest()
zip_path.with_suffix('.zip.sha256').write_text(f'{checksum}  {zip_path.name}\n')
print(f'PORTABLE PACKAGE OK: {zip_path} ({zip_path.stat().st_size / 1024**2:.1f} MiB)')
print(f'Bundled .NET {framework["version"]}; Windows x64; ZIP CRC and required components verified.')
