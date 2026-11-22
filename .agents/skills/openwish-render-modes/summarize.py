"""Render the per-boundary report from a complete benchmark run."""

from collections import defaultdict
import json
from pathlib import Path
from statistics import median
import sys


directory = Path(sys.argv[1])
modes = ["Auto", "Server", "WebAssembly"]
inventory = [
    row for row in json.loads((directory / "components.json").read_text())
    if not row["component"].endswith("/_Imports")
]
metrics = defaultdict(list)
browsers = set()
counts = set()
for mode in modes:
    data = json.loads((directory / f"{mode}.json").read_text())
    browsers.add(data["browser"])
    for sample in data["samples"]:
        for key, probe in sample["probes"].items():
            metrics[sample["profile"], sample["cache"], key, mode].append((
                probe["readyMs"], median(probe["actionMs"]), sample["frameworkBytes"],
                probe["renderer"],
            ))

lines = [
    "# OpenWish render-mode benchmark",
    "",
    f"Chromium: {', '.join(sorted(browsers))}. Timings in milliseconds.",
    "Each cell: interactive-ready / synthetic click / framework KiB (renderer). "
    "Values are medians, with the ready-time range in brackets. "
    "Framework transfer is per page, shared by roots on the same page.",
    "",
]
for profile, cache in sorted({(profile, cache) for profile, cache, _, _ in metrics}):
    lines += [
        f"## {profile.title()}, {cache} browser cache",
        "",
        "| Component | Auto | Server | WebAssembly |",
        "| --- | ---: | ---: | ---: |",
    ]
    for component in inventory:
        if component["boundary"] != "Auto":
            continue
        key = component["key"]
        cells = []
        for mode in modes:
            rows = metrics[profile, cache, key, mode]
            ready, action, size, renderers = zip(*rows)
            counts.add(len(rows))
            renderers = "/".join(sorted(set(renderers)))
            cells.append(f"{median(ready):.0f} [{min(ready):.0f}-{max(ready):.0f}] / "
                         f"{median(action):.1f} / {median(size)/1024:.0f} ({renderers}; n={len(rows)})")
        lines.append(f"| `{key}` | " + " | ".join(cells) + " |")
    lines.append("")

lines += [
    "## Components without independent renderer choices",
    "",
    "Static components retain SSR; inherited components follow their parent.",
    "A static host can still contain independently interactive children.",
    "",
]
for boundary in ["Static", "Inherited"]:
    lines += [f"### {boundary}", ""]
    lines += [f"- `{row['component']}.razor`" for row in inventory if row["boundary"] == boundary]
    lines.append("")

if len(counts) != 1:
    raise ValueError(f"Inconsistent sample counts: {counts}")
(directory / "summary.md").write_text("\n".join(lines), encoding="utf-8")
