import argparse
import json
import sys
from pathlib import Path

SCRIPT_DIR = Path(__file__).resolve().parent
REPO_ROOT = SCRIPT_DIR.parent.parent

if str(SCRIPT_DIR) not in sys.path:
    sys.path.insert(0, str(SCRIPT_DIR))

from generate_docs_index import collect_docs


CORE_DOCS = {
    "AGENTS.md",
    "PROJECT_STATUS.md",
    "GEMINI.md",
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
                errors.append(f"related doc is not indexed: {path} -> {target}")
                continue
            if path not in target_doc.get("related", []):
                errors.append(f"related doc is not bidirectional: {path} -> {target}")

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
