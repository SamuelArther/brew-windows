"""Pick the homebrew-core formulae that can build next, now that the tap has what it has.

usage: next_wave.py <formula.json (brew info --json=v2 --eval-all, or the API's formula.json)> <out dir> [skip file ...]

A formula is ready when everything it depends on (runtime, build, test and uses_from_macos, which
macOS provides but Windows doesn't) is already in the tap or comes with the MSYS2 build toolchain.
Ready formulae are written to <out dir>/ready-<kind>.txt by how they build (node, java, python, go,
rust, c), so each batch can run with the right machine setup. Formulae listed in the skip files
(batches already queued or running) are left out. The blockers report says which missing
dependencies hold back the most formulae.
"""
import collections
import json
import pathlib
import sys

REPO = pathlib.Path(__file__).resolve().parent.parent
TAP = REPO / "tap" / "homebrew-core" / "Formula"
# BuildEnvironment.ProvidesBuildTool: MSYS2 supplies these on build machines.
TOOLCHAIN = {"autoconf", "automake", "libtool", "make", "m4", "bison", "flex", "texinfo", "gawk",
             "pkgconf", "pkg-config", "cmake", "ninja", "meson", "patch", "diffutils"}


def names(entries):
    for e in entries or []:
        yield e if isinstance(e, str) else next(iter(e))


def kind(f):
    deps = set(names(f.get("dependencies"))) | set(names(f.get("build_dependencies")))
    if "node" in deps or any(d.startswith("node@") for d in deps):
        return "node"
    if any(d.startswith("openjdk") for d in deps):
        return "java"
    if any(d.startswith("python@") for d in deps) and not deps & {"go", "rust"}:
        return "python"
    if "go" in deps:
        return "go"
    if "rust" in deps:
        return "rust"
    return "c"


def main():
    formulae = {f["name"]: f for f in json.loads(pathlib.Path(sys.argv[1]).read_text(encoding="utf-8"))}
    out = pathlib.Path(sys.argv[2])
    out.mkdir(parents=True, exist_ok=True)
    skip = set()
    for path in sys.argv[3:]:
        skip |= set(pathlib.Path(path).read_text(encoding="utf-8").split())
    tap = {p.stem for p in TAP.rglob("*.rb")}
    # Dependencies may name an alias ("python" for python@3.15).
    alias = {a: n for n, f in formulae.items() for a in f.get("aliases") or []}
    pending = [n for n, f in formulae.items() if n not in tap and n not in skip]
    ready, blockers = collections.defaultdict(list), collections.Counter()
    for name in sorted(pending):
        f = formulae[name]
        deps = set(names(f.get("dependencies"))) | set(names(f.get("build_dependencies"))) \
            | set(names(f.get("test_dependencies"))) | set(names(f.get("uses_from_macos")))
        missing = {d for d in (alias.get(d, d) for d in deps) if d not in tap and d not in TOOLCHAIN}
        if missing:
            blockers.update(missing)
        else:
            ready[kind(f)].append(name)
    for k, items in ready.items():
        (out / f"ready-{k}.txt").write_text("\n".join(items) + "\n", encoding="utf-8", newline="\n")
    (out / "blockers.txt").write_text("".join(f"{n}\t{c}\n" for n, c in blockers.most_common()), encoding="utf-8", newline="\n")
    print({k: len(v) for k, v in sorted(ready.items())}, "blocked:", len(pending) - sum(map(len, ready.values())))
    print("top blockers:", ", ".join(f"{n} ({c})" for n, c in blockers.most_common(15)))
    # What the biggest blockers themselves wait on, to pick the formulae that unlock the most.
    with open(out / "blockers.txt", "a", encoding="utf-8", newline="\n") as report:
        report.write("\n# top blockers and what they need\n")
        for n, c in blockers.most_common(60):
            f = formulae.get(n, {})
            needs = {alias.get(d, d) for key in ("dependencies", "build_dependencies", "test_dependencies", "uses_from_macos")
                     for d in names(f.get(key))} - tap - TOOLCHAIN
            report.write(f"{n}\t{c}\t{'READY' if not needs else ' '.join(sorted(needs))}\n")


main()
