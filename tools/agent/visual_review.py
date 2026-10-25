# -*- coding: utf-8 -*-
"""Have a vision agent look at the build self-check's screenshots, like a player at the monitor.

Splits the PNGs of a self-check folder into groups (a whole folder in one request is over the relay's 32 MB limit),
writes one brief per group from tools/agent/briefs/visual_review.md, runs the groups in parallel through delegate.py
(read-only, default codex/gemini with codex/deepseek as fallback; both can open images) and writes one summary.
Every reported bug is a lead: open its screenshot before fixing it.

    python tools/agent/visual_review.py                       # newest UnityClient/Logs/Playtest/*_build_smoke
    python tools/agent/visual_review.py <folder> [--group 12] [--profile codex/gemini,codex/deepseek] [--only 01_p]
"""

from __future__ import annotations

import argparse
import subprocess
import sys
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PLAYTEST = ROOT / "UnityClient" / "Logs" / "Playtest"
TEMPLATE = Path(__file__).resolve().parent / "briefs" / "visual_review.md"
BRIEFS = ROOT / "tmp" / "agent_briefs"
REPORTS = ROOT / "tmp" / "agent_reports"


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("folder", nargs="?", help="self-check folder (default: the newest *_build_smoke)")
    parser.add_argument("--group", type=int, default=12, help="screenshots per agent (more than ~15 hits the 32 MB limit)")
    parser.add_argument("--profile", default="codex/gemini,codex/deepseek")
    parser.add_argument("--only", help="only files whose name starts with this")
    parser.add_argument("--timeout", type=int, default=20, help="minutes per group")
    args = parser.parse_args(argv)

    folder = (ROOT / args.folder).resolve() if args.folder else max(PLAYTEST.glob("*_build_smoke"), key=lambda p: p.stat().st_mtime)
    shots = sorted(p for p in folder.glob("*.png") if not args.only or p.name.startswith(args.only))
    if not shots:
        print(f"No screenshots in {folder}")
        return 2

    BRIEFS.mkdir(parents=True, exist_ok=True)
    template = TEMPLATE.read_text(encoding="utf-8")
    stamp = time.strftime("%H%M%S")
    groups = [shots[i:i + args.group] for i in range(0, len(shots), args.group)]
    runs = []
    for n, group in enumerate(groups, 1):
        name = f"visual_review_{stamp}_{n}"
        images = "\n".join(f"- `{p.relative_to(ROOT).as_posix()}`" for p in group)
        brief = BRIEFS / f"{name}.md"
        brief.write_text(template.replace("{group}", f"{group[0].stem} to {group[-1].stem}").replace("{images}", images), encoding="utf-8")
        command = [sys.executable, str(ROOT / "tools" / "agent" / "delegate.py"), "run", name, str(brief),
                   "--profile", args.profile, "--mode", "read", "--timeout", str(args.timeout), "--quiet"]
        runs.append((name, group, subprocess.Popen(command, cwd=ROOT, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True, encoding="utf-8", errors="replace")))
        print(f"{name}: {len(group)} screenshot(s)")

    summary = [f"# Visual review of {folder.relative_to(ROOT).as_posix()}", ""]
    failed = 0
    for name, group, proc in runs:
        out, _ = proc.communicate()
        reports = sorted(REPORTS.glob(f"*_{name}.md"), key=lambda p: p.stat().st_mtime)
        report = reports[-1] if reports else None
        verdict = next((line for line in (out or "").splitlines() if "VERDICT" in line or "结论" in line), "no verdict")
        failed += proc.returncode != 0
        summary.append(f"## {group[0].stem} .. {group[-1].stem} ({verdict.strip()})")
        summary.append(report.read_text(encoding="utf-8") if report else out or "(no report)")
        summary.append("")
        print(f"{name}: {verdict.strip()}")

    path = REPORTS / f"{time.strftime('%Y%m%d')}_{stamp}_visual_review_summary.md"
    path.write_text("\n".join(summary), encoding="utf-8")
    print(f"Summary: {path}")
    return 1 if failed else 0


if __name__ == "__main__":
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    sys.exit(main(sys.argv[1:]))
