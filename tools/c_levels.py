"""Split C/C++ formulae that have dependencies into build levels.

usage: c_levels.py <formula.json (brew info --json=v2 --eval-all)> <candidates file> <out prefix>
A formula is in level N when its dependencies are all in the tap or in levels < N; levels are
written to <out prefix>-l<N>.txt, and formulae waiting on something outside the candidates are
listed in <out prefix>-blocked.txt.
"""
import json
import pathlib
import sys

REPO = pathlib.Path(__file__).resolve().parent.parent
formulae = {f["name"]: f for f in json.loads(pathlib.Path(sys.argv[1]).read_text(encoding="utf-8"))}
candidates = set(pathlib.Path(sys.argv[2]).read_text().split())
tap = {p.stem for p in (REPO / "tap" / "homebrew-core" / "Formula").rglob("*.rb")}
pending = {n for n in candidates if n not in tap}
levels, done = [], set(tap)
while True:
    ready = sorted(n for n in pending if all(d in done for d in formulae[n].get("dependencies", [])))
    if not ready:
        break
    levels.append(ready)
    done |= set(ready)
    pending -= set(ready)
for i, names in enumerate(levels, 1):
    pathlib.Path(f"{sys.argv[3]}-l{i}.txt").write_text("\n".join(names) + "\n", encoding="utf-8", newline="\n")
pathlib.Path(f"{sys.argv[3]}-blocked.txt").write_text("\n".join(sorted(pending)) + "\n", encoding="utf-8", newline="\n")
print([len(l) for l in levels], "blocked:", len(pending))
