"""Find the homebrew-core formulae to rebuild after moving to a newer upstream commit.

usage: sync_upstream.py <homebrew-core clone (blobless is fine)> <old commit> <new commit> <batch file>

Rebuilds formulae we imported from upstream (the ones with a Windows bottle) whose upstream file
changed, and tries formulae upstream added that aren't deprecated, disabled or platform-bound.
Hand-written Windows formulae in the tap are left alone.
"""
import pathlib
import re
import subprocess
import sys

REPO = pathlib.Path(__file__).resolve().parent.parent
TAP = REPO / "tap" / "homebrew-core" / "Formula"
PORTABLE = set((REPO / "tools" / "platform-portable.txt").read_text().split())
SKIP = re.compile(r"^  (disable!|deprecate!)|^  depends_on :(macos|linux)\b", re.M)


def git(core, *args):
    return subprocess.run(["git", "-C", str(core), *args], check=True, capture_output=True, text=True).stdout


def main():
    core, old, new, batch = pathlib.Path(sys.argv[1]), sys.argv[2], sys.argv[3], pathlib.Path(sys.argv[4])
    imported = {p.stem for p in TAP.rglob("*.rb") if "x86_64_windows" in p.read_text(encoding="utf-8")}
    ours = {p.stem for p in TAP.rglob("*.rb")}
    rebuild, added = [], []
    for line in git(core, "diff", "--name-status", "--no-renames", old, new, "--", "Formula").splitlines():
        status, path = line.split("\t", 1)
        name = pathlib.PurePosixPath(path).stem
        if status == "M" and name in imported:
            rebuild.append(name)
        elif status == "A" and name not in ours:
            text = git(core, "show", f"{new}:{path}")
            if not SKIP.search(text) or (name in PORTABLE and not re.search(r"^  (disable!|deprecate!)", text, re.M)):
                added.append(name)
    names = sorted(set(rebuild)) + sorted(set(added))
    batch.write_text("\n".join(names) + "\n" if names else "", encoding="utf-8", newline="\n")
    print(f"{len(rebuild)} changed, {len(added)} new")


main()
