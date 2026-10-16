"""Hand a self-contained check to another agent and keep only its report.

  python tools/agent/delegate.py <name> <brief.md | -> [--engine codex|claude] [--model M] [--timeout MIN]

The worker gets the brief plus the house rules below and writes its final answer to
tmp/agent_reports/<time>_<name>.md (tmp/ is not in git). Default engine: Codex with the model from ~/.codex/config.toml
(DeepSeek here); `--engine claude --model haiku|sonnet` runs Claude Code headless instead. The script snapshots
`git status` before and after and appends a warning to the report if the worker changed tracked files.
Prints the report path and the report itself.

Use it for long, mechanical checks whose process would flood the lead agent's context: build walkthroughs through
tools/agent/p3cmd.py, simulation sweeps, log triage. Not for design calls or for anything that needs the Unity Editor
while another agent is using it.
"""
import shutil
import subprocess
import sys
import time
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
REPORTS = REPO / "tmp" / "agent_reports"

RULES = """You are a worker agent on Project P3 (Unity 2022.3, repo root is the working directory). Rules:
- Report only. Do not edit, create or delete tracked files, do not commit, do not touch git branches or stashes.
  Scratch files go under tmp/ only.
- Do not use the Unity Editor or Unity MCP unless the brief says so; only one agent may drive the Editor at a time.
- A running build is driven with: python tools/agent/p3cmd.py <command> (send `help` for the list). Prefer text
  (Describe, Occlusion, DragCheck) over screenshots; if you take one, keep it at 640 px wide and say what you saw.
- Stay inside the brief. If something blocks you, stop and report it instead of working around it.
- Final answer (it becomes the report), in Chinese, in this shape:
  结论: PASS / FAIL / 无法完成 (one line why)
  发现: numbered list; each item = what you saw, the exact command and its output excerpt, how sure you are.
  没做到的: what you could not check and why.
"""


def git_status():
    out = subprocess.run(["git", "status", "--porcelain"], cwd=REPO, capture_output=True, text=True, encoding="utf-8")
    return set(line for line in out.stdout.splitlines() if not line.startswith("??"))


def main(argv):
    if len(argv) < 2 or argv[0] in ("-h", "--help"):
        print(__doc__)
        return 0
    name, brief_arg = argv[0], argv[1]
    opts = dict(zip(argv[2::2], argv[3::2]))
    engine = opts.get("--engine", "codex")
    model = opts.get("--model")
    timeout = int(opts.get("--timeout", "30")) * 60
    brief = sys.stdin.buffer.read().decode("utf-8") if brief_arg == "-" else Path(brief_arg).read_text(encoding="utf-8")

    REPORTS.mkdir(parents=True, exist_ok=True)
    stem = f"{time.strftime('%Y%m%d_%H%M%S')}_{name}"
    report = REPORTS / f"{stem}.md"
    # The task travels as a UTF-8 file: Chinese text passed through the Windows .cmd shims (argv or stdin) arrives
    # garbled, so the command line only carries an ASCII pointer to it.
    task_file = REPORTS / f"{stem}.task.md"
    task_file.write_text(RULES + "\n# Brief\n" + brief, encoding="utf-8")
    prompt = f"Read {task_file.relative_to(REPO).as_posix()} (UTF-8) and do exactly what it says."
    if engine == "codex":
        cmd = ["codex", "exec", "-C", str(REPO), "--skip-git-repo-check", "--ephemeral", "-o", str(report)]
        if model:
            cmd += ["-m", model]
        cmd.append(prompt)
    elif engine == "claude":
        cmd = ["claude", "-p", "--model", model or "sonnet", "--permission-mode", "acceptEdits", prompt]
    else:
        sys.exit(f"Unknown engine {engine}.")

    before = git_status()
    started = time.time()
    cmd[0] = shutil.which(cmd[0]) or cmd[0]  # codex is a .cmd shim on Windows
    result = subprocess.run(cmd, cwd=REPO, stdin=subprocess.DEVNULL, capture_output=True, text=True, encoding="utf-8",
                            errors="replace", timeout=timeout)
    if engine == "claude" or not report.exists():
        report.write_text(result.stdout or result.stderr[-4000:], encoding="utf-8")

    changed = git_status() ^ before
    with report.open("a", encoding="utf-8") as f:
        f.write(f"\n\n---\nengine={engine} model={model or 'default'} exit={result.returncode} {int(time.time() - started)}s\n")
        if changed:
            # Anyone's changes show up here, including the lead agent's own commits while the worker ran.
            f.write("WARNING: tracked files changed during the run (by the worker or anyone else):\n" + "\n".join(sorted(changed)) + "\n")

    print(report)
    print(report.read_text(encoding="utf-8"))
    return 0 if result.returncode == 0 else 1


if __name__ == "__main__":
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    sys.exit(main(sys.argv[1:]))
