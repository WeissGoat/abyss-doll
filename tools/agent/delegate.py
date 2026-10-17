"""Hand a self-contained check to a cheaper worker agent with P3's worker rules; keep only its report.

  python tools/agent/delegate.py <name> <brief.md | -> [--profile P] [--mode read|full] [--lock build] [--quiet] ...
  python tools/agent/delegate.py profiles | probe

A thin wrapper around the user-level `delegate` skill (agent-hub): it adds --rules tools/agent/delegate_rules.md
and runs in this repo. Reports go to tmp/agent_reports/. See the skill's SKILL.md for options, briefs and how to
read the report.
"""
import os
import subprocess
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
RULES = REPO / "tools" / "agent" / "delegate_rules.md"


def find_skill():
    candidates = [os.environ.get("DELEGATE_SKILL")] + [str(Path.home() / d / "skills" / "delegate")
                                                       for d in (".claude", ".codex", ".gemini")]
    for c in filter(None, candidates):
        script = Path(c) / "scripts" / "delegate.py"
        if script.exists():
            return script
    sys.exit("The delegate skill is not installed (looked in DELEGATE_SKILL and ~/.claude|.codex|.gemini/skills). "
             "It lives in agent-hub; run agent-hub's scripts/link.ps1.")


def main(argv):
    args = list(argv)
    if args and args[0] not in ("profiles", "probe", "-h", "--help", "help"):
        if args[0] != "run":
            args.insert(0, "run")
        args += ["--rules", str(RULES)] + ([] if "--cwd" in args else ["--cwd", str(REPO)])
    return subprocess.call([sys.executable, str(find_skill()), *args])


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
