import argparse
import os
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path


GENERATED_PATH_MARKERS = (
    "/_generated/",
    "/StreamingAssets/Configs/",
)

LOCAL_TOOL_PATHS = (
    "tools/ComfyUI_NAIDGenerator/",
)

SUBMODULE_PATHS = (
    "tools/ai-image-gateway",
)

MAX_LIST_ITEMS = 12


@dataclass
class StatusEntry:
    code: str
    path: str
    original_path: str = ""


def run(command, cwd=None, env=None):
    return subprocess.run(
        command,
        cwd=cwd,
        env=env,
        text=True,
        encoding="utf-8",
        errors="replace",
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
    )


def repo_root():
    result = run(["git", "rev-parse", "--show-toplevel"])
    if result.returncode != 0:
        return None, result.stderr.strip() or result.stdout.strip()
    return Path(result.stdout.strip()).resolve(), ""


def parse_status(output):
    entries = []
    records = [record for record in output.split("\0") if record]
    index = 0
    while index < len(records):
        record = records[index]
        code = record[:2]
        path = record[3:] if len(record) > 3 else ""
        original_path = ""
        if code[0] in ("R", "C"):
            index += 1
            if index < len(records):
                original_path = records[index]
        entries.append(StatusEntry(code=code, path=path, original_path=original_path))
        index += 1
    return entries


def git_status_entries(root):
    result = run(
        ["git", "-c", "core.quotepath=false", "status", "--porcelain=v1", "-z"],
        cwd=root,
    )
    if result.returncode != 0:
        raise RuntimeError(result.stderr.strip() or result.stdout.strip())
    return parse_status(result.stdout)


def is_untracked(entry):
    return entry.code == "??"


def is_staged(entry):
    return entry.code[0] not in (" ", "?")


def is_unstaged(entry):
    return entry.code[1] not in (" ", "?")


def normalize(path):
    return path.replace("\\", "/")


def has_marker(path, markers):
    normalized = "/" + normalize(path).strip("/")
    return any(marker in normalized for marker in markers)


def starts_with_any(path, prefixes):
    normalized = normalize(path)
    return any(normalized == prefix.rstrip("/") or normalized.startswith(prefix) for prefix in prefixes)


def format_paths(entries):
    lines = []
    for entry in entries[:MAX_LIST_ITEMS]:
        suffix = f" <- {entry.original_path}" if entry.original_path else ""
        lines.append(f"  - {entry.code} {entry.path}{suffix}")
    if len(entries) > MAX_LIST_ITEMS:
        lines.append(f"  - ... 还有 {len(entries) - MAX_LIST_ITEMS} 项")
    return lines


def print_section(title, state, details=None):
    print(f"[{state}] {title}")
    for line in details or []:
        print(line)


def check_docs(root):
    script = root / "tools" / "docs" / "validate_docs.py"
    if not script.exists():
        return "ERROR", [f"  - 缺少文档校验脚本：{script}"]
    result = run([sys.executable, str(script)], cwd=root)
    output = (result.stdout + result.stderr).strip()
    details = [f"  - {line}" for line in output.splitlines()] if output else []
    if result.returncode != 0:
        return "ERROR", details
    return "PASS", details


def check_skills(root):
    script = root / "tools" / "agent" / "validate_skills.py"
    if not script.exists():
        return "ERROR", [f"  - 缺少 Skill 校验脚本：{script}"]
    env = dict(os.environ, PYTHONIOENCODING="utf-8")
    result = run([sys.executable, str(script)], cwd=root, env=env)
    output = (result.stdout + result.stderr).strip()
    details = [f"  - {line}" for line in output.splitlines()] if output else []
    if result.returncode != 0:
        return "ERROR", details
    if "WARN:" in result.stdout:
        return "WARN", details
    return "PASS", details


def summarize_status(entries):
    staged = [entry for entry in entries if is_staged(entry)]
    unstaged = [entry for entry in entries if is_unstaged(entry)]
    untracked = [entry for entry in entries if is_untracked(entry)]

    details = [
        f"  - 已暂存：{len(staged)}",
        f"  - 未暂存：{len(unstaged)}",
        f"  - 未跟踪：{len(untracked)}",
    ]
    if entries:
        details.append("  - 当前改动预览：")
        details.extend(format_paths(entries))
        return "WARN", details
    return "PASS", details


def check_risky_paths(entries):
    generated = [entry for entry in entries if has_marker(entry.path, GENERATED_PATH_MARKERS)]
    local_tools = [entry for entry in entries if starts_with_any(entry.path, LOCAL_TOOL_PATHS)]
    submodules = [entry for entry in entries if starts_with_any(entry.path, SUBMODULE_PATHS)]

    details = []
    if generated:
        details.append("  - 生成物或运行时副本有改动，提交前确认是否应由脚本刷新：")
        details.extend(format_paths(generated))
    if local_tools:
        details.append("  - 本地工具目录未纳入当前项目归属，提交前先决定 vendor/submodule/忽略策略：")
        details.extend(format_paths(local_tools))
    if submodules:
        details.append("  - submodule 有改动，通常应进入子模块内部单独处理：")
        details.extend(format_paths(submodules))

    if details:
        return "WARN", details
    return "PASS", ["  - 未发现生成物、运行时副本、本地工具或 submodule 风险。"]


def check_branch(root):
    branch = run(["git", "branch", "--show-current"], cwd=root)
    head = run(["git", "rev-parse", "--short", "HEAD"], cwd=root)
    details = []
    if branch.returncode == 0:
        details.append(f"  - 当前分支：{branch.stdout.strip() or '(detached)'}")
    if head.returncode == 0:
        details.append(f"  - 当前提交：{head.stdout.strip()}")
    return "PASS", details


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--strict",
        action="store_true",
        help="把警告也作为失败返回，适合提交前或自动化检查。",
    )
    args = parser.parse_args()

    root, error = repo_root()
    if root is None:
        print_section("仓库定位", "ERROR", [f"  - {error}"])
        return 1

    checks = []
    checks.append(("仓库位置", *check_branch(root)))

    try:
        entries = git_status_entries(root)
    except RuntimeError as exc:
        print_section("Git 状态", "ERROR", [f"  - {exc}"])
        return 1

    checks.append(("工作区状态", *summarize_status(entries)))
    checks.append(("易误提交路径", *check_risky_paths(entries)))
    checks.append(("知识库校验", *check_docs(root)))
    checks.append(("Skill 校验", *check_skills(root)))

    has_error = False
    has_warning = False
    print("Project P3 智能体开工健康检查")
    print(f"仓库：{root}")
    print("")
    for title, state, details in checks:
        print_section(title, state, details)
        print("")
        has_error = has_error or state == "ERROR"
        has_warning = has_warning or state == "WARN"

    if has_error:
        print("[RESULT] FAIL：存在必须处理的问题。")
        return 1
    if has_warning:
        print("[RESULT] WARN：可以继续，但提交前需要收窄暂存范围并确认风险项。")
        return 1 if args.strict else 0

    print("[RESULT] PASS：开工前检查通过。")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
