import argparse
import csv
import json
import re
import sys
from datetime import datetime
from pathlib import Path


FIELDS = [
    "id",
    "kind",
    "priority",
    "role",
    "phase",
    "title",
    "goal",
    "scope",
    "out_of_scope",
    "read_before",
    "files",
    "commands",
    "verify",
    "required_tools",
    "status",
    "status_writeback",
    "evidence",
    "notes",
]

KINDS = {"TASK", "REVIEW"}
PRIORITIES = {"P0", "P1", "P2", "P3"}
ROLES = {"PM", "策划", "程序", "UI程序", "美术", "知识库", "全局"}
STATUSES = {"TODO", "DOING", "REVIEW", "FIX", "DONE", "BLOCKED"}
ACTIVE_STATUSES = {"TODO", "DOING", "REVIEW", "FIX"}
ROLE_ALIASES = {
    "pm": "PM",
    "plan": "PM",
    "planning": "PM",
    "design": "策划",
    "config": "策划",
    "program": "程序",
    "unity": "程序",
    "ui": "UI程序",
    "ui-program": "UI程序",
    "art": "美术",
    "kb": "知识库",
    "knowledge": "知识库",
    "docs": "知识库",
    "global": "全局",
    "all": "全局",
}


def repo_root() -> Path:
    script_path = Path(__file__).resolve()
    for candidate in script_path.parents:
        if (candidate / "AGENTS.md").exists() and (candidate / ".git").exists():
            return candidate
    return script_path.parents[3]


def to_repo_path(path: Path) -> str:
    try:
        return path.resolve().relative_to(repo_root()).as_posix()
    except ValueError:
        return path.as_posix()


def slugify(value: str) -> str:
    slug = re.sub(r"[^0-9A-Za-z\u4e00-\u9fff]+", "-", value.strip()).strip("-")
    if not slug:
        return "mission"
    return slug[:48]


def read_rows(path: Path):
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        reader = csv.DictReader(handle)
        fieldnames = reader.fieldnames or []
        rows = list(reader)
    return fieldnames, rows


def write_rows(path: Path, rows):
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8-sig", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=FIELDS, quoting=csv.QUOTE_ALL)
        writer.writeheader()
        for row in rows:
            writer.writerow({field: row.get(field, "") for field in FIELDS})


def split_list(value: str):
    return [item.strip() for item in (value or "").split(";") if item.strip()]


def normalize_role(value: str) -> str:
    value = (value or "").strip()
    return ROLE_ALIASES.get(value.lower(), value)


def validate(path: Path, strict: bool = False):
    errors = []
    warnings = []
    if not path.exists():
        return [f"missing CSV: {path}"], warnings, []

    fieldnames, rows = read_rows(path)
    if fieldnames != FIELDS:
        errors.append(
            "header mismatch; expected: "
            + ",".join(FIELDS)
            + "; actual: "
            + ",".join(fieldnames)
        )

    seen_ids = set()
    review_rows = []
    for index, row in enumerate(rows, start=2):
        row_id = (row.get("id") or "").strip()
        kind = (row.get("kind") or "").strip()
        priority = (row.get("priority") or "").strip()
        role = (row.get("role") or "").strip()
        status = (row.get("status") or "").strip()
        title = (row.get("title") or "").strip()

        if not row_id:
            errors.append(f"line {index}: id is required")
        elif row_id in seen_ids:
            errors.append(f"line {index}: duplicate id {row_id}")
        seen_ids.add(row_id)

        if kind not in KINDS:
            errors.append(f"line {index} [{row_id}]: kind must be one of {sorted(KINDS)}")
        if priority not in PRIORITIES:
            errors.append(f"line {index} [{row_id}]: priority must be one of {sorted(PRIORITIES)}")
        if role not in ROLES:
            errors.append(f"line {index} [{row_id}]: role must be one of {sorted(ROLES)}")
        if status not in STATUSES:
            errors.append(f"line {index} [{row_id}]: status must be one of {sorted(STATUSES)}")
        if not title:
            errors.append(f"line {index} [{row_id}]: title is required")

        required_text_fields = ["goal", "scope", "read_before", "verify", "status_writeback"]
        if kind == "TASK":
            for field in required_text_fields:
                if not (row.get(field) or "").strip():
                    errors.append(f"line {index} [{row_id}]: {field} is required for TASK")
            if status == "DONE" and not (row.get("evidence") or "").strip():
                errors.append(f"line {index} [{row_id}]: DONE requires evidence")
        else:
            review_rows.append((index, row_id))
            if not row_id.startswith("REVIEW"):
                warnings.append(f"line {index} [{row_id}]: REVIEW rows should use REVIEW-* ids")

        if status == "BLOCKED" and "blocked:" not in (row.get("notes") or ""):
            errors.append(f"line {index} [{row_id}]: BLOCKED requires notes containing blocked:<reason>")
        if status == "DONE" and "done_at:" not in (row.get("notes") or ""):
            warnings.append(f"line {index} [{row_id}]: DONE rows should include done_at:<date> in notes")
        if strict and row.get("required_tools") and status == "DONE":
            evidence = row.get("evidence") or ""
            for tool in split_list(row.get("required_tools", "")):
                if tool not in evidence:
                    errors.append(f"line {index} [{row_id}]: evidence should mention required tool {tool}")

    if rows and rows[-1].get("kind") != "REVIEW":
        errors.append("last row must be a REVIEW row")
    if not review_rows:
        errors.append("mission must contain at least one REVIEW row")

    return errors, warnings, rows


def progress(rows):
    actionable = [row for row in rows if row.get("kind") == "TASK"]
    done = [row for row in actionable if row.get("status") == "DONE"]
    blocked = [row for row in actionable if row.get("status") == "BLOCKED"]
    return {
        "tasks_total": len(actionable),
        "tasks_done": len(done),
        "tasks_blocked": len(blocked),
        "rows_total": len(rows),
    }


def next_issue(path: Path):
    errors, warnings, rows = validate(path)
    if errors:
        return 2, {"path": to_repo_path(path), "errors": errors, "warnings": warnings}

    for row in rows:
        if row.get("kind") == "REVIEW":
            prior_tasks = [
                item
                for item in rows
                if item.get("kind") == "TASK"
                and rows.index(item) < rows.index(row)
                and item.get("status") != "DONE"
            ]
            if prior_tasks:
                continue
        if row.get("status") in ACTIVE_STATUSES:
            return 0, {
                "path": to_repo_path(path),
                "progress": progress(rows),
                "next": row,
                "warnings": warnings,
            }

    return 0, {
        "path": to_repo_path(path),
        "progress": progress(rows),
        "next": None,
        "warnings": warnings,
    }


def latest_candidate():
    root = repo_root()
    candidates = []
    for pattern in ("missions/*.csv", ".mission/*.csv"):
        candidates.extend(root.glob(pattern))
    unfinished = []
    for path in candidates:
        errors, _warnings, rows = validate(path)
        if errors:
            continue
        if any(row.get("status") in ACTIVE_STATUSES for row in rows):
            unfinished.append(path)
    if not unfinished:
        return None
    return max(unfinished, key=lambda item: item.stat().st_mtime)


def append_note(existing: str, note: str):
    note = (note or "").strip()
    if not note:
        return existing or ""
    if not existing:
        return note
    return existing.rstrip() + "; " + note


def normalize_source_ref(value: str) -> str:
    source = (value or "").strip()
    if not source:
        return ""
    path = Path(source)
    if not path.is_absolute():
        path = repo_root() / path
    if path.exists():
        return to_repo_path(path)
    return source


def cmd_new(args):
    source_ref = normalize_source_ref(args.source_spec)
    if not source_ref:
        print(
            "[p3-mission] ERROR: missing source. Provide -Source/-SourceSpec with a "
            "detailed source path or reference after confirming it is mission-ready; "
            "do not generate a mission from a one-sentence goal.",
            file=sys.stderr,
        )
        return 1

    title = args.title or args.goal
    slug = slugify(title)
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    output = Path(args.output) if args.output else repo_root() / ".mission" / f"{timestamp}-{slug}.csv"
    if not output.is_absolute():
        output = repo_root() / output
    if output.exists() and not args.force:
        print(f"[p3-mission] ERROR: output already exists: {to_repo_path(output)}", file=sys.stderr)
        return 1

    role = normalize_role(args.role)
    if role not in ROLES:
        print(f"[p3-mission] ERROR: invalid role {args.role}; allowed: {', '.join(sorted(ROLES))}", file=sys.stderr)
        return 1
    rows = [
        {
            "id": "PLAN-01",
            "kind": "TASK",
            "priority": "P0",
            "role": role,
            "phase": "1",
            "title": "Plan mission issues",
            "goal": args.goal,
            "scope": "Read the source reference and P3 status, then split the approved phase progress into 3-12 independently verifiable mission rows.",
            "out_of_scope": "Do not invent missing requirements, write a spec, or implement feature work before replacing this planning placeholder with concrete task rows.",
            "read_before": f"{source_ref}; AGENTS.md; PROJECT_STATUS.md; agent_status/pm.md; agent_status/program.md; agent_status/design.md; agent_status/art.md",
            "files": f"{source_ref}; .codex/skills/p3-mission/SKILL.md; tools/p3-mission/SKILL.md",
            "commands": ".\\tools\\p3-mission\\Test-P3Mission.ps1 -Path <mission.csv>",
            "verify": "Mission CSV has concrete TASK rows, one final REVIEW row, required read_before/status_writeback/verify fields, and no placeholder-only implementation rows.",
            "required_tools": "shell",
            "status": "TODO",
            "status_writeback": "Update the status page listed by each concrete task row after meaningful work is done.",
            "evidence": "",
            "notes": f"created_by:New-P3Mission; source_ref:{source_ref}",
        },
        {
            "id": "REVIEW-01",
            "kind": "REVIEW",
            "priority": "P0",
            "role": "全局",
            "phase": "99",
            "title": "Review mission outcome against original goal",
            "goal": args.goal,
            "scope": "Compare completed rows, evidence, status writebacks, and remaining risks against the source reference and original goal.",
            "out_of_scope": "Do not introduce new scope unless a gap is converted into a follow-up TASK row.",
            "read_before": f"{source_ref}; AGENTS.md; PROJECT_STATUS.md; .codex/skills/p3-mission/SKILL.md; .codex/skills/p3-mission/references/execution-protocol.md",
            "files": "<mission.csv>",
            "commands": ".\\tools\\p3-mission\\Test-P3Mission.ps1 -Path <mission.csv> -Strict",
            "verify": "All non-review TASK rows are DONE or explicitly BLOCKED with blocked:<reason>; DONE rows have evidence and status writeback; claims match the source reference and do not overstate evidence level.",
            "required_tools": "shell",
            "status": "TODO",
            "status_writeback": "If the mission changed project priorities, cross-functional handoff, or blockers, update PROJECT_STATUS.md; otherwise confirm task-level status pages are updated.",
            "evidence": "",
            "notes": f"review_kind:mission_outcome; source_ref:{source_ref}",
        },
    ]
    write_rows(output, rows)
    print(f"[p3-mission] created {to_repo_path(output)}")
    return 0


def cmd_validate(args):
    path = Path(args.path)
    if not path.is_absolute():
        path = repo_root() / path
    errors, warnings, rows = validate(path, strict=args.strict)
    for warning in warnings:
        print(f"[p3-mission] WARN: {warning}")
    if errors:
        for error in errors:
            print(f"[p3-mission] ERROR: {error}", file=sys.stderr)
        return 1
    info = progress(rows)
    print(
        "[p3-mission] validation passed. "
        f"tasks={info['tasks_done']}/{info['tasks_total']} done, "
        f"blocked={info['tasks_blocked']}, rows={info['rows_total']}"
    )
    return 0


def cmd_next(args):
    if args.latest:
        path = latest_candidate()
        if path is None:
            print(json.dumps({"next": None, "message": "no unfinished mission found"}, ensure_ascii=False, indent=2))
            return 0
    else:
        path = Path(args.path)
        if not path.is_absolute():
            path = repo_root() / path
    code, payload = next_issue(path)
    print(json.dumps(payload, ensure_ascii=False, indent=2))
    return code


def cmd_update(args):
    path = Path(args.path)
    if not path.is_absolute():
        path = repo_root() / path
    errors, _warnings, rows = validate(path)
    if errors:
        for error in errors:
            print(f"[p3-mission] ERROR: {error}", file=sys.stderr)
        return 1

    target = None
    for row in rows:
        if row.get("id") == args.id:
            target = row
            break
    if target is None:
        print(f"[p3-mission] ERROR: row not found: {args.id}", file=sys.stderr)
        return 1

    if args.status not in STATUSES:
        print(f"[p3-mission] ERROR: invalid status {args.status}", file=sys.stderr)
        return 1
    if args.status == "DONE" and not (args.evidence or target.get("evidence")):
        print("[p3-mission] ERROR: DONE requires evidence", file=sys.stderr)
        return 1
    if args.status == "BLOCKED" and "blocked:" not in (args.notes or target.get("notes") or ""):
        print("[p3-mission] ERROR: BLOCKED requires notes containing blocked:<reason>", file=sys.stderr)
        return 1

    target["status"] = args.status
    if args.evidence:
        target["evidence"] = append_note(target.get("evidence", ""), args.evidence)
    if args.notes:
        target["notes"] = append_note(target.get("notes", ""), args.notes)
    if args.status == "DONE" and "done_at:" not in target.get("notes", ""):
        target["notes"] = append_note(target.get("notes", ""), f"done_at:{datetime.now().strftime('%Y-%m-%d')}")

    write_rows(path, rows)
    print(f"[p3-mission] updated {to_repo_path(path)} row {args.id} -> {args.status}")
    return 0


def main():
    parser = argparse.ArgumentParser(prog="p3_mission.py")
    subparsers = parser.add_subparsers(dest="command", required=True)

    new_parser = subparsers.add_parser("new")
    new_parser.add_argument("--goal", required=True)
    new_parser.add_argument("--source-spec", "--source", dest="source_spec", default="")
    new_parser.add_argument("--title", default="")
    new_parser.add_argument("--role", default="global")
    new_parser.add_argument("--output", default="")
    new_parser.add_argument("--force", action="store_true")
    new_parser.set_defaults(func=cmd_new)

    validate_parser = subparsers.add_parser("validate")
    validate_parser.add_argument("--path", required=True)
    validate_parser.add_argument("--strict", action="store_true")
    validate_parser.set_defaults(func=cmd_validate)

    next_parser = subparsers.add_parser("next")
    next_group = next_parser.add_mutually_exclusive_group(required=True)
    next_group.add_argument("--path")
    next_group.add_argument("--latest", action="store_true")
    next_parser.set_defaults(func=cmd_next)

    update_parser = subparsers.add_parser("update")
    update_parser.add_argument("--path", required=True)
    update_parser.add_argument("--id", required=True)
    update_parser.add_argument("--status", required=True)
    update_parser.add_argument("--evidence", default="")
    update_parser.add_argument("--notes", default="")
    update_parser.set_defaults(func=cmd_update)

    args = parser.parse_args()
    return args.func(args)


if __name__ == "__main__":
    raise SystemExit(main())
