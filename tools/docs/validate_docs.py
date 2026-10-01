import argparse
import json
import re
import sys
from pathlib import Path

SCRIPT_DIR = Path(__file__).resolve().parent
REPO_ROOT = SCRIPT_DIR.parent.parent

if str(SCRIPT_DIR) not in sys.path:
    sys.path.insert(0, str(SCRIPT_DIR))

from generate_docs_index import collect_docs, parse_front_matter, read_text, should_skip


CORE_DOCS = {
    "AGENTS.md",
    "PROJECT_STATUS.md",
    "DOCS_INDEX.md",
    "知识库/README.md",
    "版本规划/09_正式版核心纵切开发路线.md",
    "设计文档/GDD/GDD_00_系统关联总图.md",
    "开发文档/00_程序开发大纲.md",
    "美术文档/README.md",
    "agent_status/art.md",
    "agent_status/design.md",
    "agent_status/program.md",
}

ACTIVE_ROLES = {
    "全局",
    "游戏导演",
    "Owner",
    "剧情",
    "策划",
    "程序",
    "美术",
    "知识库",
}

ACTIVE_STATUS_DOCS = {
    "agent_status/director.md",
    "agent_status/design.md",
    "agent_status/program.md",
    "agent_status/art.md",
}

ACTIVE_ROLE_VIEWS = {
    "知识库/views/director.md",
    "知识库/views/owner.md",
    "知识库/views/narrative.md",
    "知识库/views/design.md",
    "知识库/views/program.md",
    "知识库/views/art.md",
}

STATUS_REQUIRED_HEADINGS = {
    "最后更新",
    "当前关注",
    "最近完成",
    "下一步建议",
    "问题 / 阻塞",
    "关键证据入口",
}

PROJECT_STATUS_REQUIRED_HEADINGS = {
    "最后更新",
    "当前阶段",
    "当前顶层目标",
    "当前优先级",
    "跨职能交接",
    "问题 / 阻塞",
    "下一步总建议",
    "关键入口",
}

BODY_LINE_LIMIT = 80
BODY_CHAR_LIMIT = 4000
LINE_CHAR_LIMIT = 240
DATED_LOG_HEADING = re.compile(r"^#{1,2} \d{4}-\d{2}-\d{2}(?:\s|$)")
DATED_LOG_ENTRY = re.compile(r"^- \d{4}-\d{2}-\d{2}(?:\s|$)")


def second_level_headings(body):
    return [line[3:].strip() for line in body.splitlines() if line.startswith("## ")]


def validate_required_headings(path, body, required):
    headings = set(second_level_headings(body))
    return [
        f"{path} missing required heading: {heading}"
        for heading in sorted(required)
        if heading not in headings
    ]


def validate_body_line_limit(path, body, limit):
    line_count = len(body.splitlines())
    if line_count <= limit:
        return []
    return [f"{path} body exceeds {limit} lines: {line_count}"]


def validate_body_char_limit(path, body, limit):
    char_count = len(body.replace("\r\n", "\n"))
    if char_count <= limit:
        return []
    return [f"{path} body exceeds {limit} characters: {char_count}"]


def validate_line_char_limit(path, body, limit):
    return [
        f"{path} body line {number} exceeds {limit} characters: {len(line)}"
        for number, line in enumerate(body.splitlines(), start=1)
        if len(line) > limit
    ]


def validate_no_dated_logs(path, body):
    errors = []
    for line in body.splitlines():
        if DATED_LOG_HEADING.match(line):
            errors.append(f"{path} contains dated log heading: {line}")
        elif DATED_LOG_ENTRY.match(line):
            errors.append(f"{path} contains dated log entry: {line}")
    return errors


def document_body(path):
    _, body = parse_front_matter(read_text(REPO_ROOT / Path(path)))
    return body


def validate_progressive_disclosure_structure():
    errors = []

    project_body = document_body("PROJECT_STATUS.md")
    errors.extend(
        validate_required_headings(
            "PROJECT_STATUS.md", project_body, PROJECT_STATUS_REQUIRED_HEADINGS
        )
    )
    errors.extend(
        validate_body_line_limit("PROJECT_STATUS.md", project_body, BODY_LINE_LIMIT)
    )
    errors.extend(
        validate_body_char_limit("PROJECT_STATUS.md", project_body, BODY_CHAR_LIMIT)
    )
    errors.extend(
        validate_line_char_limit("PROJECT_STATUS.md", project_body, LINE_CHAR_LIMIT)
    )
    errors.extend(validate_no_dated_logs("PROJECT_STATUS.md", project_body))

    for path in sorted(ACTIVE_STATUS_DOCS):
        body = document_body(path)
        errors.extend(validate_required_headings(path, body, STATUS_REQUIRED_HEADINGS))
        errors.extend(validate_body_line_limit(path, body, BODY_LINE_LIMIT))
        errors.extend(validate_body_char_limit(path, body, BODY_CHAR_LIMIT))
        errors.extend(validate_line_char_limit(path, body, LINE_CHAR_LIMIT))
        errors.extend(validate_no_dated_logs(path, body))

    for path in sorted(ACTIVE_ROLE_VIEWS):
        body = document_body(path)
        errors.extend(validate_required_headings(path, body, {"必读", "按任务读取"}))
        if not any(
            heading.startswith("验收 / 恢复时")
            for heading in second_level_headings(body)
        ):
            errors.append(f"{path} missing required heading: 验收 / 恢复时")
        errors.extend(validate_no_dated_logs(path, body))

    return errors


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--index", default="docs_index.json")
    args = parser.parse_args()

    index_path = Path(args.index)
    if not index_path.exists():
        print(f"[docs] missing index: {index_path}", file=sys.stderr)
        return 1

    payload = json.loads(index_path.read_text(encoding="utf-8-sig"))
    docs = payload.get("documents", [])
    by_path = {doc["path"]: doc for doc in docs}
    current_docs = collect_docs(REPO_ROOT)
    current_paths = {doc["path"] for doc in current_docs}
    indexed_paths = set(by_path)

    errors = []
    missing_from_index = sorted(current_paths - indexed_paths)
    stale_in_index = sorted(indexed_paths - current_paths)
    if missing_from_index:
        errors.append(
            "docs index is stale; missing current markdown: " + ", ".join(missing_from_index)
        )
    if stale_in_index:
        errors.append(
            "docs index is stale; contains removed markdown: " + ", ".join(stale_in_index)
        )

    for path in sorted(CORE_DOCS):
        doc = by_path.get(path)
        if doc is None:
            errors.append(f"core doc is not indexed: {path}")
            continue
        if doc.get("missing_fields"):
            errors.append(f"core doc missing metadata fields: {path} -> {', '.join(doc['missing_fields'])}")

    ids = {}
    for doc in docs:
        doc_id = doc.get("id")
        if not doc_id:
            continue
        if doc_id in ids:
            errors.append(f"duplicate doc id '{doc_id}': {ids[doc_id]} and {doc['path']}")
        ids[doc_id] = doc["path"]

    for doc in docs:
        if doc.get("status") != "active":
            continue
        role = doc.get("role")
        if role not in ACTIVE_ROLES:
            errors.append(f"active doc uses unsupported role '{role}': {doc['path']}")

    for doc in docs:
        path = doc["path"]
        related = doc.get("related", [])
        if len(related) != len(set(related)):
            errors.append(f"duplicate related entries: {path}")
        for target in related:
            if target == path:
                errors.append(f"doc relates to itself: {path}")
                continue
            target_doc = by_path.get(target)
            if target_doc is None:
                if should_skip(Path(target)):
                    continue
                errors.append(f"related doc is not indexed: {path} -> {target}")
                continue
            if path not in target_doc.get("related", []):
                errors.append(f"related doc is not bidirectional: {path} -> {target}")

    errors.extend(validate_progressive_disclosure_structure())

    if errors:
        for error in errors:
            print(f"[docs] ERROR: {error}", file=sys.stderr)
        return 1

    missing = sum(1 for doc in docs if doc.get("missing_fields"))
    relation_edges = payload.get("relationship_summary", {}).get("relation_edges", 0)
    cross_role_edges = payload.get("relationship_summary", {}).get("cross_role_edges", 0)
    print(
        "[docs] validation passed. "
        f"indexed={len(docs)}, missing_metadata={missing}, "
        f"relation_edges={relation_edges}, cross_role_edges={cross_role_edges}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
