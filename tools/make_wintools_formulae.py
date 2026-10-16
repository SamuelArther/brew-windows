"""Write tap overrides for the formulae wintools provides (wintools/formulae/<formula>.json).

Each formula downloads the wintools release zip and links its command names to wintools.exe.

usage: make_wintools_formulae.py <version> <sha256 of wintools-<version>.zip>
Writes tools/windows-overrides/<formula>.rb and tools/batches/wintools.txt (which also lists the
hand-written overrides for these formulae, e.g. official Windows builds).
"""
import json
import pathlib
import re
import sys

REPO = pathlib.Path(__file__).resolve().parent.parent
OVERRIDES = REPO / "tools" / "windows-overrides"
RELEASES = "https://github.com/SamuelArther/brew-windows/releases/download"


def class_name(formula):
    # Homebrew's Formulary.class_s: "osx-cpu-temp" -> OsxCpuTemp, "python@3.14" -> PythonAT314
    name = formula.replace("+", "x").replace("@", "AT")
    name = re.sub(r"(^|[-_.\s])(\w)", lambda m: m.group(2).upper(), name)
    return name.replace(".", "")


def ruby_string(text):
    return '"' + text.replace("\\", "\\\\").replace('"', '\\"').replace("#{", "\\#{") + '"'


def formula(meta, version, sha256):
    name = meta["formula"]
    commands = "\n".join(
        f'    windows_exe "libexec/wintools.exe", {ruby_string(c)}, {ruby_string(c)}' for c in meta["commands"]
    )
    test = "\n".join(("    " + line).rstrip() for line in meta["test"].splitlines())
    caveats = ""
    if meta.get("caveats"):
        body = "\n".join("      " + line for line in meta["caveats"].splitlines())
        caveats = f"\n  def caveats\n    <<~EOS\n{body}\n    EOS\n  end\n"
    return f'''class {class_name(name)} < Formula
  desc {ruby_string(meta["desc"])}
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "{RELEASES}/wintools-{version}/wintools-{version}.zip"
  sha256 "{sha256}"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
{commands}
  end
{caveats}
  test do
{test}
  end
end
'''


def main():
    version, sha256 = sys.argv[1], sys.argv[2]
    names = []
    for path in sorted((REPO / "wintools" / "formulae").glob("*.json")):
        meta = json.loads(path.read_text(encoding="utf-8"))
        OVERRIDES.mkdir(parents=True, exist_ok=True)
        (OVERRIDES / f"{meta['formula']}.rb").write_text(formula(meta, version, sha256), encoding="utf-8", newline="\n")
        names.append(meta["formula"])
    # Hand-written overrides for the same platform-only formulae (official Windows builds) ride along.
    for path in sorted(OVERRIDES.glob("*.rb")):
        if path.stem not in names and "brew-windows' wintools" not in path.read_text(encoding="utf-8"):
            names.append(path.stem)
    (REPO / "tools" / "batches" / "wintools.txt").write_text("\n".join(names) + "\n", encoding="utf-8", newline="\n")
    print(f"wrote {len(names)} formulae")


main()
