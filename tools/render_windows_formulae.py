"""Render pinned Windows Ruby formulae from the audited package catalog.
No network requests. Re-run after deliberately updating windows-packages.json.
"""
import json
from pathlib import Path
root = Path(__file__).resolve().parent.parent
packages = json.loads((root / 'tools/windows-packages.json').read_text())
quote = lambda value: json.dumps(value, ensure_ascii=False)
for package in packages:
    name = package['name']
    classname = ''.join(part.capitalize() for part in name.replace('-', '_').split('_'))
    lines = [f'class {classname} < Formula',
             f'  desc {quote(package.get("description", name + " for Windows"))}',
             f'  homepage {quote(package["homepage"])}',
             f'  version {quote(package["version"])}',
             f'  url {quote(package["url"])}',
             f'  sha256 {quote(package["sha256"])}',
             f'  license {quote(package["license"])}']
    for dependency in package['dependencies']:
        lines += [f'  depends_on {quote(dependency)}']
    for resource in package['resources']:
        lines += ['', f'  resource {quote(resource["name"])} do',
                  f'    url {quote(resource["url"])}',
                  f'    sha256 {quote(resource["sha256"])}', '  end']
    if package['notes']:
        lines += ['', '  # ' + package['notes']]
    lines += ['', '  def install']
    for method, *args in package['operations']:
        lines += ['    ' + method + (' ' + ', '.join(map(quote, args)) if args else '')]
    lines += ['  end', 'end', '']
    path = root / 'tap/homebrew-core/Formula' / name[0] / (name + '.rb')
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text('\n'.join(lines), encoding='utf-8')
print(f'Rendered {len(packages)} pinned Windows formulae.')
