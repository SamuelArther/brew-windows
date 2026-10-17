"""Record Windows edits to an imported formula as tools/windows-patches/<name>.patch.

Workflow: import_upstream.py <core> <name>; edit the tap file; make_windows_patch.py <core> <name>.
The patch is the diff from the converted upstream file to the edited tap file.

usage: make_windows_patch.py <homebrew-core checkout> <name>...
"""
import difflib
import pathlib
import sys

from import_upstream import PATCHES, REPO, convert, formula_dir


def main():
    core = pathlib.Path(sys.argv[1]) / "Formula"
    PATCHES.mkdir(parents=True, exist_ok=True)
    for name in sys.argv[2:]:
        # homebrew-core shards lib* formulae under Formula/lib/, as import_upstream.py's fallback finds them.
        source = core / name[0] / f"{name}.rb"
        if not source.exists():
            source = next(core.glob(f"*/{name}.rb"))
        upstream = convert(source.read_text(encoding="utf-8"))
        target = formula_dir(name) / f"{name}.rb"
        edited = target.read_text(encoding="utf-8")
        rel = target.relative_to(REPO).as_posix()
        diff = "".join(difflib.unified_diff(upstream.splitlines(True), edited.splitlines(True), f"a/{rel}", f"b/{rel}"))
        if not diff:
            print(f"{name}: no changes")
            continue
        (PATCHES / f"{name}.patch").write_text(diff, encoding="utf-8", newline="\n")
        print(f"{name}: wrote {PATCHES / f'{name}.patch'}")


if __name__ == "__main__":
    main()
