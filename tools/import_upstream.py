"""Import formulae from Homebrew/homebrew-core (BSD-2-Clause) into the Windows core tap.

The upstream file is kept verbatim except for its macOS/Linux `bottle do` block; Windows
bottles are built by .github/workflows/bottles.yml and merged with `brew bottle --merge --write`.
Small Windows edits are tools/windows-patches/<name>.patch (a git diff of the tap file, applied
after import); tools/windows-overrides/<name>.rb replaces a formula entirely.

usage: import_upstream.py <homebrew-core checkout> <names file (one per line) | name...>
"""
import pathlib
import re
import subprocess
import sys

REPO = pathlib.Path(__file__).resolve().parent.parent
TAP = REPO / "tap" / "homebrew-core" / "Formula"
OVERRIDES = REPO / "tools" / "windows-overrides"
PATCHES = REPO / "tools" / "windows-patches"
BOTTLE = re.compile(r"^  bottle do\n.*?^  end\n\n?", re.M | re.S)


def formula_dir(name):
    return TAP / (name[0] if name[0].isalpha() else "#")


def convert(text):
    text = text.replace("\r\n", "\n")
    return BOTTLE.sub("", text, count=1)


def main():
    core = pathlib.Path(sys.argv[1]) / "Formula"
    names = sys.argv[2:]
    if len(names) == 1 and pathlib.Path(names[0]).is_file():
        names = pathlib.Path(names[0]).read_text().split()
    written = 0
    for name in names:
        override = OVERRIDES / f"{name}.rb"
        source = override if override.exists() else core / name[0] / f"{name}.rb"
        if not source.exists():
            source = next(core.glob(f"*/{name}.rb"), None)
            if source is None:
                print(f"missing upstream formula: {name}", file=sys.stderr)
                continue
        target = formula_dir(name) / f"{name}.rb"
        existing = target.read_text(encoding="utf-8") if target.exists() else ""
        text = source.read_text(encoding="utf-8")
        text = text if source == override else convert(text)
        # Keep a bottle block we already built for this exact formula version.
        kept = BOTTLE.search(existing)
        if kept and source != override and same_version(existing, text):
            text = insert_bottle(text, kept.group(0).rstrip("\n") + "\n")
        if text != existing:
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_text(text, encoding="utf-8", newline="\n")
            written += 1
        # Small Windows edits on top of the upstream file, so upstream updates keep flowing.
        patch = PATCHES / f"{name}.patch"
        if patch.exists() and source != override:
            check = subprocess.run(["git", "apply", "--check", "--reverse", str(patch)], cwd=REPO, capture_output=True)
            if check.returncode != 0:
                subprocess.run(["git", "apply", str(patch)], cwd=REPO, check=True)
    print(f"wrote {written} formulae")


def stanza(text, key):
    m = re.search(rf'^  {key} "([^"]+)"', text, re.M)
    return m.group(1) if m else None


def same_version(a, b):
    return all(stanza(a, k) == stanza(b, k) for k in ("url", "version", "sha256", "revision"))


def insert_bottle(text, block):
    m = re.search(r"^  livecheck do\n.*?^  end\n", text, re.M | re.S)
    if m:
        return text[: m.end()] + "\n" + block + text[m.end():]
    m = re.search(r"^\n(?=  (?:head|depends_on|uses_from_macos|on_\w+|resource|def |test do))", text, re.M)
    return text[: m.start()] + "\n" + block + text[m.start():] if m else text


if __name__ == "__main__":
    main()
