import argparse
import json
import re
from collections import defaultdict
from pathlib import Path


REQUIRED_FIELDS = [
    "id",
    "title",
    "type",
    "role",
    "domain",
    "status",
]

EXCLUDED_PARTS = {
    ".git",
    ".pytest_cache",
    "__pycache__",
    "_generated",
    "_IncomingAI",
    "Library",
    "Temp",
    "Logs",
    "obj",
    "UserSettings",
    "StreamingAssets",
    "ai-image-gateway",
    "ComfyUI_NAIDGenerator",
}


def to_posix(path: Path) -> str:
    return path.as_posix()


def should_skip(path: Path) -> bool:
    return any(part in EXCLUDED_PARTS for part in path.parts)


def read_text(path: Path) -> str:
    return path.read_text(encoding="utf-8-sig")


def parse_scalar(value: str):
    value = value.strip()
    if value == "":
        return ""
    if value.lower() == "true":
        return True
    if value.lower() == "false":
        return False
    if value.startswith("[") and value.endswith("]"):
        inner = value[1:-1].strip()
        if not inner:
            return []
        return [item.strip().strip("'\"") for item in inner.split(",")]
    return value.strip("'\"")


def parse_front_matter(text: str):
    lines = text.splitlines()
    if not lines or lines[0].strip() != "---":
        return {}, text

    end_index = None
    for index in range(1, len(lines)):
        if lines[index].strip() == "---":
            end_index = index
            break

    if end_index is None:
        return {}, text

    metadata = {}
    current_key = None
    for raw_line in lines[1:end_index]:
        line = raw_line.rstrip()
        if not line.strip() or line.lstrip().startswith("#"):
            continue
        if line.startswith("  - ") and current_key:
            metadata.setdefault(current_key, [])
            metadata[current_key].append(parse_scalar(line[4:]))
            continue
        match = re.match(r"^([A-Za-z0-9_]+):\s*(.*)$", line)
        if match:
            current_key = match.group(1)
            raw_value = match.group(2)
            if raw_value == "":
                metadata[current_key] = []
            else:
                metadata[current_key] = parse_scalar(raw_value)
            continue

    body = "\n".join(lines[end_index + 1 :]).lstrip("\r\n")
    return metadata, body


def first_heading(text: str):
    for line in text.splitlines():
        if line.startswith("# "):
            return line[2:].strip()
    return ""


def collect_docs(repo_root: Path):
    docs = []
    for path in sorted(repo_root.rglob("*.md")):
        rel = path.relative_to(repo_root)
        if should_skip(rel):
            continue
        text = read_text(path)
        metadata, body = parse_front_matter(text)
        title = metadata.get("title") or first_heading(body) or rel.stem
        missing = [field for field in REQUIRED_FIELDS if field not in metadata or metadata[field] in ("", [])]
        docs.append(
            {
                "path": to_posix(rel),
                "title": title,
                "id": metadata.get("id", ""),
                "type": metadata.get("type", "unclassified"),
                "role": metadata.get("role", "未分类"),
                "domain": metadata.get("domain", "unclassified"),
                "status": metadata.get("status", "missing_metadata" if missing else "active"),
                "source_of_truth": bool(metadata.get("source_of_truth", False)),
                "related": metadata.get("related", []),
                "last_verified": metadata.get("last_verified", ""),
                "update_rule": metadata.get("update_rule", ""),
                "has_metadata": bool(metadata),
                "missing_fields": missing,
            }
        )
    return docs


def write_json(path: Path, docs):
    payload = {
        "version": 1,
        "generated_by": "tools/docs/generate_docs_index.py",
        "required_fields": REQUIRED_FIELDS,
        "documents": docs,
    }
    path.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def write_markdown(path: Path, docs):
    by_role = defaultdict(list)
    for doc in docs:
        by_role[doc["role"]].append(doc)

    lines = [
        "---",
        "id: docs_index",
        "title: 文档索引",
        "type: index",
        "role: 全局",
        "domain: knowledge_base",
        "status: generated",
        "source_of_truth: false",
        "related:",
        "  - 知识库/README.md",
        "last_verified: 2026-05-23",
        "update_rule: 由 tools/docs/Generate-DocsIndex.ps1 生成，不手写维护。",
        "---",
        "",
        "# 文档索引",
        "",
        "> 本文件由 `tools/docs/Generate-DocsIndex.ps1` 生成。不要手写维护。",
        "",
        "## 概览",
        "",
        f"- 文档总数：{len(docs)}",
        f"- 已补元数据：{sum(1 for doc in docs if doc['has_metadata'])}",
        f"- 缺少元数据：{sum(1 for doc in docs if not doc['has_metadata'])}",
        f"- 事实来源文档：{sum(1 for doc in docs if doc['source_of_truth'])}",
        "",
        "## 事实来源",
        "",
    ]

    source_docs = [doc for doc in docs if doc["source_of_truth"]]
    if source_docs:
        for doc in source_docs:
            lines.append(f"- [{doc['title']}]({doc['path']}) - `{doc['type']}` / `{doc['domain']}`")
    else:
        lines.append("- 暂无。")

    lines.extend(["", "## 按职能分组", ""])
    for role in sorted(by_role.keys()):
        lines.extend([f"### {role}", "", "| 文档 | 类型 | 状态 | 领域 | 元数据 |", "|---|---|---|---|---|"])
        for doc in sorted(by_role[role], key=lambda item: item["path"]):
            metadata_state = "完整" if not doc["missing_fields"] else "缺失：" + ", ".join(doc["missing_fields"])
            lines.append(
                f"| [{doc['title']}]({doc['path']}) | `{doc['type']}` | `{doc['status']}` | `{doc['domain']}` | {metadata_state} |"
            )
        lines.append("")

    missing_docs = [doc for doc in docs if doc["missing_fields"]]
    lines.extend(["## 元数据缺口", ""])
    if missing_docs:
        for doc in missing_docs:
            fields = ", ".join(doc["missing_fields"])
            lines.append(f"- [{doc['title']}]({doc['path']})：缺少 `{fields}`")
    else:
        lines.append("- 暂无。")

    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--markdown", default="DOCS_INDEX.md")
    parser.add_argument("--json", default="docs_index.json")
    args = parser.parse_args()

    repo_root = Path.cwd()
    docs = collect_docs(repo_root)
    write_json(repo_root / args.json, docs)
    write_markdown(repo_root / args.markdown, docs)
    print(f"[docs] indexed {len(docs)} markdown files")


if __name__ == "__main__":
    main()
