"""Group a bottles run's failures by error signature.

usage: triage.py <results json> <directory with downloaded logs> [--full]
"""
import collections
import json
import pathlib
import re
import sys

results = json.loads(pathlib.Path(sys.argv[1]).read_text(encoding="utf-8"))
logs = {p.stem: p for p in pathlib.Path(sys.argv[2]).rglob("*.log")}
full = "--full" in sys.argv

NOISE = re.compile(r"^(Error: Testing requires|Error: No such keg|==> |\s*$)")


def signature(text):
    """The most telling error line, with paths, versions and numbers blanked out."""
    lines = text.splitlines()
    def useful(l):
        return not NOISE.match(l) and "checking for" not in l and not re.match(r"\s*(gcc|g\+\+|cc|c\+\+|clang)\s", l)
    # Compiler/linker diagnostics first, then anything error-shaped.
    picks = [l for l in lines if re.search(r"error:|fatal error|undefined reference|Error:|NameError|NoMethodError", l) and useful(l)]
    picks += [l for l in lines if re.search(r"\berror\b|not found|No such file|undefined", l, re.I) and useful(l) and l not in picks]
    line = picks[0] if picks else (lines[-1] if lines else "")
    line = re.sub(r"[A-Za-z]:[\\/][^\s'\"]*", "<path>", line)
    line = re.sub(r"\d+(\.\d+)+", "<v>", line)
    return line.strip()[:160], (picks[:3] if picks else lines[-3:])


groups = collections.defaultdict(list)
for r in results:
    if r["status"] == "ok":
        continue
    log = logs.get(r["name"])
    text = log.read_text(encoding="utf-8", errors="replace") if log else ""
    # Only the failing step: the section after the last successful brew command.
    step = {"build": "brew install", "test": "brew test", "test-deps": "brew install", "bottle": "brew bottle"}.get(r["status"], "")
    if step and step in text:
        text = text[text.rfind("==> " + step):]
        text = text[:text.find("==> brew uninstall")] if "==> brew uninstall" in text else text
    sig, sample = signature(text)
    groups[(r["status"], sig)].append((r["name"], sample))

for (status, sig), items in sorted(groups.items(), key=lambda kv: -len(kv[1])):
    print(f"[{len(items):3}] {status}: {sig}")
    print("      " + " ".join(n for n, _ in items[:25]) + (" ..." if len(items) > 25 else ""))
    if full:
        for l in items[0][1]:
            print("        | " + l[:220])
