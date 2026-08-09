# -*- coding: utf-8 -*-
"""Scan docs and config files for potential visual asset requirements.

This report is intentionally advisory. It helps the art agent find VisualID
mentions or visual-language requirements that are not yet represented in the
Manifest, but it does not write Manifest or seed entries automatically.
"""

from __future__ import annotations

import argparse
import json
import re
from collections import Counter, defaultdict
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


SCRIPT_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = SCRIPT_DIR.parents[1]

DEFAULT_MANIFEST = "美术文档/_generated/art_manifest.json"
DEFAULT_SEED = "美术文档/art_requirements_seed.json"
DEFAULT_APPROVED_ROOT = "UnityClient/Assets/Art/Approved"
DEFAULT_DECISION_PATH = "美术文档/art_requirement_candidate_decisions.json"
DEFAULT_OUTPUT_JSON = "美术文档/_generated/美术需求候选清单.json"
DEFAULT_OUTPUT_MARKDOWN = "美术文档/_generated/美术需求候选清单.md"
DEFAULT_SNAPSHOT_DIR = "美术文档/_generated/art_requirement_candidate_snapshots"
DEFAULT_SCAN_ROOTS = [
    "设计文档",
    "配置表(JSON)",
    "版本规划",
    "美术文档/ui_design",
]

SCAN_SUFFIXES = {".md", ".json"}
EXCLUDED_PARTS = {
    ".git",
    "_archive",
    "archive",
    "_generated",
    "art_requirement_candidate_snapshots",
    "mvp_baseline_2026-05-22",
}

VISUAL_ID_TOKEN_RE = re.compile(
    r"\b(?:bg|ui|item|monster|node|chassis|prosthetic|faction|rumor|order|memento|doll|vfx|fx|sfx)_[A-Za-z0-9_]+\b"
)
VISUAL_FIELD_RE = re.compile(
    r"(?i)\b(?:VisualID|VisualIDs|IconID|IconVisualID|PortraitID|CombatVisualID|BackgroundVisualID|"
    r"FallbackVisualID|SnapshotVisualID|ExpressionVisualIDs|ActionVfxRef|HitFeedbackRef|DefeatFeedbackRef|"
    r"VfxRef|VisualRef)\b"
)
QUOTED_VALUE_RE = re.compile(r"['\"]([A-Za-z0-9_./-]+)['\"]")
VISUAL_TEXT_KEYWORDS = (
    "美术需求",
    "美术 / UI",
    "视觉需求",
    "表现资源",
    "图标缺失",
    "图标",
    "背景",
    "立绘",
    "徽章",
    "特效",
    "VFX",
    "动画",
    "VisualID",
)
DOMAIN_PREFIXES = {
    "bg_": "background",
    "ui_": "ui",
    "item_": "item",
    "monster_": "monster",
    "node_": "node",
    "chassis_": "chassis",
    "prosthetic_": "prosthetic",
    "faction_": "faction",
    "rumor_": "rumor",
    "order_": "order",
    "memento_": "memento",
    "doll_": "doll",
    "vfx_": "vfx",
    "fx_": "vfx",
    "sfx_": "audio",
}
PRIORITY_ORDER = {"P0": 0, "P1": 1, "P2": 2, "P3": 3, "P4": 4, "review": 99}
STATUS_ORDER = {
    "new_candidate": 0,
    "approved_without_manifest": 1,
    "seed_only": 2,
    "deferred_candidate": 3,
    "ignored_candidate": 4,
    "manifest_managed": 5,
}
DECISION_STATUS = {
    "defer": "deferred_candidate",
    "deferred": "deferred_candidate",
    "ignore": "ignored_candidate",
    "ignored": "ignored_candidate",
}
DIRECT_VISUAL_PREFIXES = ("bg_", "vfx_", "fx_", "sfx_", "memento_")
DIRECT_VISUAL_SUFFIXES = (
    "_icon",
    "_portrait",
    "_combat",
    "_stand",
    "_sprite",
    "_emblem",
    "_badge",
    "_overlay",
    "_shadow",
    "_ring",
    "_plate",
    "_line",
    "_divider",
    "_slot",
    "_row",
    "_button",
    "_bar",
    "_skin",
    "_marker",
    "_feedback",
)
NON_UI_DIRECT_VISUAL_SUFFIXES = (
    "_icon",
    "_portrait",
    "_combat",
    "_stand",
    "_sprite",
    "_emblem",
    "_badge",
    "_overlay",
    "_shadow",
    "_ring",
    "_marker",
    "_feedback",
)
DERIVABLE_ICON_PREFIXES = ("item_", "chassis_", "prosthetic_", "faction_", "rumor_", "order_")
LAYOUT_TOKEN_SUFFIXES = (
    "_action_panel",
    "_background",
    "_bay",
    "_card",
    "_detail",
    "_detail_panel",
    "_header",
    "_list_panel",
    "_panel",
    "_stage",
    "_summary",
)
IGNORED_TOKEN_EXACT = {
    "ui_design",
    "ui_design_handoff",
    "item_abc",
    "item_instance_id",
    "item_tag",
    "chassis_lv",
    "order_pool_",
    "vfx_space",
}
IGNORED_TOKEN_SUFFIXES = (
    "_rules",
    "_lifecycle",
    "_growth",
    "_relationship",
    "_state_affection",
    "_handoff",
    "_design",
    "_display_area",
    "_space",
)


def resolve_project_path(value: str) -> Path:
    path = Path(value)
    return path if path.is_absolute() else PROJECT_ROOT / path


def repo_path(path: Path | None) -> str:
    if path is None:
        return ""
    try:
        return path.resolve().relative_to(PROJECT_ROOT.resolve()).as_posix()
    except ValueError:
        return path.as_posix()


def read_json(path: Path, default: Any) -> Any:
    if not path.exists():
        return default
    return json.loads(path.read_text(encoding="utf-8-sig"))


def write_json(path: Path, payload: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def load_candidate_decisions(path: Path) -> dict[str, dict[str, Any]]:
    payload = read_json(path, {})
    entries = payload.get("Entries") if isinstance(payload, dict) else None
    if not isinstance(entries, list):
        return {}

    decisions: dict[str, dict[str, Any]] = {}
    for entry in entries:
        if not isinstance(entry, dict):
            continue
        visual_id = str(entry.get("VisualID", "")).strip()
        decision = str(entry.get("Decision", "")).strip().lower()
        if not visual_id or decision not in DECISION_STATUS:
            continue
        decisions[visual_id] = entry
    return decisions


def apply_candidate_decisions(
    visual_items: list[dict[str, Any]],
    decisions: dict[str, dict[str, Any]],
) -> None:
    for item in visual_items:
        if item.get("Status") != "new_candidate":
            continue
        decision = decisions.get(str(item.get("VisualID", "")))
        if not decision:
            continue

        decision_value = str(decision.get("Decision", "")).strip().lower()
        status = DECISION_STATUS.get(decision_value)
        if not status:
            continue

        item["Status"] = status
        item["Decision"] = {
            "Decision": decision_value,
            "Reason": str(decision.get("Reason", "")).strip(),
            "ReviewedAt": str(decision.get("ReviewedAt", "")).strip(),
            "ReplacementVisualID": str(decision.get("ReplacementVisualID", "")).strip(),
        }


def timestamp_text() -> str:
    return datetime.now(timezone.utc).astimezone().isoformat(timespec="seconds")


def timestamp_filename() -> str:
    return datetime.now(timezone.utc).astimezone().strftime("%Y%m%d_%H%M%S")


def safe_snapshot_tag(value: str) -> str:
    text = re.sub(r"[^A-Za-z0-9_.-]+", "_", value.strip())
    text = re.sub(r"_+", "_", text).strip("._-")
    return text[:80]


def as_list(value: Any) -> list[Any]:
    return value if isinstance(value, list) else []


def should_scan(path: Path) -> bool:
    if path.suffix.lower() not in SCAN_SUFFIXES:
        return False
    parts = set(path.parts)
    return not any(part in EXCLUDED_PARTS for part in parts)


def iter_scan_files(roots: list[str]) -> list[Path]:
    files: list[Path] = []
    for root_value in roots:
        root = resolve_project_path(root_value)
        if root.is_file() and should_scan(root):
            files.append(root)
        elif root.is_dir():
            files.extend(path for path in root.rglob("*") if path.is_file() and should_scan(path))
    return sorted(set(files), key=lambda path: repo_path(path))


def collect_manifest_ids(manifest_path: Path) -> set[str]:
    manifest = read_json(manifest_path, {})
    return {
        str(entry.get("VisualID", "")).strip()
        for entry in as_list(manifest.get("Entries"))
        if isinstance(entry, dict) and str(entry.get("VisualID", "")).strip()
    }


def collect_seed_ids(seed_path: Path) -> set[str]:
    seed = read_json(seed_path, {})
    return {
        str(entry.get("VisualID", "")).strip()
        for entry in as_list(seed.get("Entries"))
        if isinstance(entry, dict) and str(entry.get("VisualID", "")).strip()
    }


def collect_approved_ids(approved_root: Path) -> set[str]:
    if not approved_root.exists():
        return set()
    return {path.stem for path in approved_root.rglob("*.png") if path.is_file()}


def infer_domain(visual_id: str) -> str:
    for prefix, domain in DOMAIN_PREFIXES.items():
        if visual_id.startswith(prefix):
            return domain
    return "unknown"


def infer_priority(path: Path, line: str) -> str:
    text = f"{repo_path(path)} {line}"
    match = re.search(r"\bP[0-4]\b", text)
    return match.group(0) if match else "review"


def source_confidence(source_kind: str, status: str, path: Path, candidate_kind: str) -> str:
    if status in {"new_candidate", "approved_without_manifest"} and source_kind == "json_field":
        return "high"
    if candidate_kind == "derived_visual_candidate":
        return "medium"
    if source_kind in {"json_field", "explicit_visual_field"}:
        return "medium"
    if source_kind == "visual_id_token":
        return "medium"
    return "low"


def classify_visual_id(visual_id: str, manifest_ids: set[str], seed_ids: set[str], approved_ids: set[str]) -> str:
    if visual_id in manifest_ids:
        return "manifest_managed"
    if visual_id in seed_ids:
        return "seed_only"
    if visual_id in approved_ids:
        return "approved_without_manifest"
    return "new_candidate"


def is_ignored_token(token: str, line_text: str) -> bool:
    if token.endswith("_"):
        return True
    if token in IGNORED_TOKEN_EXACT:
        return True
    if any(token.endswith(suffix) for suffix in IGNORED_TOKEN_SUFFIXES):
        return True
    if re.search(r"_v\d+$", token):
        return True
    if re.search(rf"\bdomain:\s*{re.escape(token)}\b", line_text, re.IGNORECASE):
        return True
    if token.startswith("order_pool_"):
        return True
    if re.fullmatch(r"node_\d+", token):
        return True
    return False


def is_layout_token(token: str) -> bool:
    return any(token.endswith(suffix) for suffix in LAYOUT_TOKEN_SUFFIXES)


def is_direct_visual_token(token: str) -> bool:
    if token.startswith(DIRECT_VISUAL_PREFIXES):
        return True
    if token.startswith("ui_"):
        return any(suffix in token for suffix in DIRECT_VISUAL_SUFFIXES) or token.startswith("ui_icon_")
    return any(token.endswith(suffix) for suffix in NON_UI_DIRECT_VISUAL_SUFFIXES)


def derive_visual_id(token: str) -> str:
    if is_layout_token(token):
        return ""
    if token.endswith("_icon"):
        return ""
    if not token.startswith(DERIVABLE_ICON_PREFIXES):
        return ""
    return f"{token}_icon"


def normalize_candidate_visual_id(
    token: str,
    *,
    source_kind: str,
    line_text: str,
    manifest_ids: set[str],
    seed_ids: set[str],
    approved_ids: set[str],
) -> tuple[str, str, str]:
    """Return (visual_id, candidate_kind, source_id); empty visual_id means skip."""
    token = token.strip().strip("`'\"")
    if not VISUAL_ID_TOKEN_RE.fullmatch(token):
        return "", "", ""
    if is_ignored_token(token, line_text):
        return "", "", ""
    if token in manifest_ids or token in seed_ids or token in approved_ids:
        return token, "explicit_visual_id", token
    if source_kind in {"json_field", "explicit_visual_field"}:
        return token, "explicit_visual_id", token
    if is_direct_visual_token(token):
        return token, "explicit_visual_id", token
    derived = derive_visual_id(token)
    if derived:
        return derived, "derived_visual_candidate", token
    return "", "", ""


def add_visual_mention(
    mentions: dict[str, dict[str, Any]],
    *,
    visual_id: str,
    source_kind: str,
    path: Path,
    line_no: int,
    line_text: str,
    manifest_ids: set[str],
    seed_ids: set[str],
    approved_ids: set[str],
    json_pointer: str = "",
) -> None:
    original_token = visual_id.strip().strip("`'\"")
    visual_id, candidate_kind, source_id = normalize_candidate_visual_id(
        original_token,
        source_kind=source_kind,
        line_text=line_text,
        manifest_ids=manifest_ids,
        seed_ids=seed_ids,
        approved_ids=approved_ids,
    )
    if not visual_id:
        return
    status = classify_visual_id(visual_id, manifest_ids, seed_ids, approved_ids)
    item = mentions.setdefault(
        visual_id,
        {
            "VisualID": visual_id,
            "SourceID": source_id,
            "CandidateKind": candidate_kind,
            "Status": status,
            "Domain": infer_domain(visual_id),
            "Priority": infer_priority(path, line_text),
            "Confidence": source_confidence(source_kind, status, path, candidate_kind),
            "MentionCount": 0,
            "Sources": [],
        },
    )
    item["MentionCount"] += 1
    if PRIORITY_ORDER.get(infer_priority(path, line_text), 99) < PRIORITY_ORDER.get(item["Priority"], 99):
        item["Priority"] = infer_priority(path, line_text)
    if STATUS_ORDER.get(status, 99) < STATUS_ORDER.get(item["Status"], 99):
        item["Status"] = status
    item["Sources"].append(
        {
            "File": repo_path(path),
            "Line": line_no,
            "Kind": source_kind,
            "SourceID": source_id,
            "CandidateKind": candidate_kind,
            "JsonPointer": json_pointer,
            "Text": line_text.strip()[:220],
        }
    )


def scan_json_value(
    value: Any,
    *,
    path: Path,
    pointer: str,
    key_name: str,
    mentions: dict[str, dict[str, Any]],
    manifest_ids: set[str],
    seed_ids: set[str],
    approved_ids: set[str],
) -> None:
    if isinstance(value, dict):
        for key, nested in value.items():
            next_pointer = f"{pointer}/{key}" if pointer else f"/{key}"
            scan_json_value(
                nested,
                path=path,
                pointer=next_pointer,
                key_name=str(key),
                mentions=mentions,
                manifest_ids=manifest_ids,
                seed_ids=seed_ids,
                approved_ids=approved_ids,
            )
        return
    if isinstance(value, list):
        for index, nested in enumerate(value):
            next_pointer = f"{pointer}/{index}" if pointer else f"/{index}"
            scan_json_value(
                nested,
                path=path,
                pointer=next_pointer,
                key_name=key_name,
                mentions=mentions,
                manifest_ids=manifest_ids,
                seed_ids=seed_ids,
                approved_ids=approved_ids,
            )
        return
    if not VISUAL_FIELD_RE.search(key_name):
        return
    text = str(value or "")
    for token in VISUAL_ID_TOKEN_RE.findall(text):
        add_visual_mention(
            mentions,
            visual_id=token,
            source_kind="json_field",
            path=path,
            line_no=0,
            line_text=f"{key_name}: {text}",
            manifest_ids=manifest_ids,
            seed_ids=seed_ids,
            approved_ids=approved_ids,
            json_pointer=pointer,
        )


def scan_markdown(
    path: Path,
    mentions: dict[str, dict[str, Any]],
    text_reviews: list[dict[str, Any]],
    manifest_ids: set[str],
    seed_ids: set[str],
    approved_ids: set[str],
    max_text_reviews: int,
) -> None:
    for line_no, line in enumerate(path.read_text(encoding="utf-8-sig", errors="replace").splitlines(), start=1):
        line_tokens = set(VISUAL_ID_TOKEN_RE.findall(line))
        source_kind = "explicit_visual_field" if VISUAL_FIELD_RE.search(line) else "visual_id_token"
        for token in sorted(line_tokens):
            add_visual_mention(
                mentions,
                visual_id=token,
                source_kind=source_kind,
                path=path,
                line_no=line_no,
                line_text=line,
                manifest_ids=manifest_ids,
                seed_ids=seed_ids,
                approved_ids=approved_ids,
            )

        if len(text_reviews) >= max_text_reviews:
            continue
        if line_tokens:
            continue
        stripped = line.strip()
        if not stripped or len(stripped) < 8:
            continue
        if any(keyword in stripped for keyword in VISUAL_TEXT_KEYWORDS):
            text_reviews.append(
                {
                    "File": repo_path(path),
                    "Line": line_no,
                    "Priority": infer_priority(path, stripped),
                    "Reason": "包含视觉/美术关键词但未出现可直接纳管的 VisualID。",
                    "Text": stripped[:240],
                }
            )


def scan_json_file(
    path: Path,
    mentions: dict[str, dict[str, Any]],
    manifest_ids: set[str],
    seed_ids: set[str],
    approved_ids: set[str],
) -> None:
    try:
        payload = read_json(path, None)
    except json.JSONDecodeError:
        return
    scan_json_value(
        payload,
        path=path,
        pointer="",
        key_name="",
        mentions=mentions,
        manifest_ids=manifest_ids,
        seed_ids=seed_ids,
        approved_ids=approved_ids,
    )


def trim_sources(item: dict[str, Any], limit: int) -> None:
    sources = item.get("Sources")
    if isinstance(sources, list) and len(sources) > limit:
        item["Sources"] = sources[:limit]
        item["SourcesTruncated"] = len(sources) - limit
    else:
        item["SourcesTruncated"] = 0


def sort_visual_item(item: dict[str, Any]) -> tuple[int, int, str, str]:
    return (
        STATUS_ORDER.get(str(item.get("Status", "")), 99),
        PRIORITY_ORDER.get(str(item.get("Priority", "")), 99),
        str(item.get("Domain", "")),
        str(item.get("VisualID", "")),
    )


def md_cell(value: Any) -> str:
    if value is None:
        text = ""
    elif isinstance(value, list):
        text = ", ".join(str(item) for item in value)
    else:
        text = str(value)
    return text.replace("|", "\\|").replace("\n", " ")


def make_markdown(payload: dict[str, Any]) -> str:
    summary = payload["Summary"]
    lines = [
        "# 美术需求候选清单",
        "",
        "> Generated by `tools/美术工具/Scan-ArtRequirementCandidates.ps1`.",
        "> 本报告只用于审查，不会自动修改 Manifest 或 seed。",
        "",
        "## Summary",
        "",
        f"* GeneratedAt: `{payload['GeneratedAt']}`",
        f"* Scanned files: `{summary['ScannedFiles']}`",
        f"* Unique VisualID mentions: `{summary['UniqueVisualIDs']}`",
        f"* New candidates: `{summary['StatusCounts'].get('new_candidate', 0)}`",
        f"* Approved without Manifest: `{summary['StatusCounts'].get('approved_without_manifest', 0)}`",
        f"* Seed only: `{summary['StatusCounts'].get('seed_only', 0)}`",
        f"* Deferred candidates: `{summary['StatusCounts'].get('deferred_candidate', 0)}`",
        f"* Ignored candidates: `{summary['StatusCounts'].get('ignored_candidate', 0)}`",
        f"* Manifest managed: `{summary['StatusCounts'].get('manifest_managed', 0)}`",
        f"* Candidate decisions: `{summary.get('DecisionCount', 0)}`",
        f"* Text review items: `{len(payload['TextReviewItems'])}`",
        "",
        "## Review Queue",
        "",
    ]
    review_items = [item for item in payload["VisualIDItems"] if item["Status"] != "manifest_managed"]
    if review_items:
        lines.extend(
            [
        "| Status | Priority | VisualID | SourceID | Kind | Domain | Confidence | Mentions | First Source |",
        "|---|---|---|---|---|---|---|---:|---|",
            ]
        )
        for item in review_items:
            source = item["Sources"][0] if item.get("Sources") else {}
            first_source = f"{source.get('File', '')}:{source.get('Line', '')} {source.get('Text', '')}".strip()
            lines.append(
                "| "
                + " | ".join(
                    [
                        f"`{md_cell(item['Status'])}`",
                        md_cell(item["Priority"]),
                        f"`{md_cell(item['VisualID'])}`",
                        f"`{md_cell(item.get('SourceID', ''))}`",
                        md_cell(item.get("CandidateKind", "")),
                        md_cell(item["Domain"]),
                        md_cell(item["Confidence"]),
                        md_cell(item["MentionCount"]),
                        md_cell(first_source),
                    ]
                )
                + " |"
            )
    else:
        lines.append("- 当前没有未纳管 VisualID 候选。")

    lines.extend(["", "## Text Review", ""])
    if payload["TextReviewItems"]:
        lines.extend(["| Priority | Source | Reason | Text |", "|---|---|---|---|"])
        for item in payload["TextReviewItems"]:
            source = f"{item['File']}:{item['Line']}"
            lines.append(
                "| "
                + " | ".join(
                    [
                        md_cell(item["Priority"]),
                        md_cell(source),
                        md_cell(item["Reason"]),
                        md_cell(item["Text"]),
                    ]
                )
                + " |"
            )
    else:
        lines.append("- 当前没有文本型视觉需求审查项。")

    lines.extend(
        [
            "",
            "## Status Meanings",
            "",
            "- `new_candidate`: 文档或配置中出现了像 VisualID 的资源 ID，但当前 Manifest / seed / Approved 都没有纳管；需要美术判断是否补 seed 或等待配置落地。",
            "- `approved_without_manifest`: Approved 目录已有同名 PNG，但 Manifest 未纳管；需要判断是历史残留还是应补 Manifest。",
            "- `seed_only`: seed 中已有但 Manifest 未出现；通常需要重新运行 `Update-ArtManifest.ps1`。",
            "- `deferred_candidate`: 美术侧已审查，但当前阶段先不纳入 Manifest；后续 active UI 或配置明确需要时再准入。",
            "- `ignored_candidate`: 美术侧已审查为误报、概念图文件名、布局区域名、旧 ID 或已有替代 VisualID，不进入素材生产。",
            "- `manifest_managed`: 已纳入 Manifest，本报告不要求处理。",
            "",
            "## Recommended Flow",
            "",
            "1. 先人工审查 `new_candidate` 和 `approved_without_manifest`。",
            "2. 确认应纳管的项写入 `美术文档/art_requirements_seed.json`，或等待正式配置 JSON 落地。",
            "3. 运行 `Sync-Configs -> Update-ArtManifest -> Compile-ArtGenerationRequests -> Generate-ArtIntegrationCandidates`。",
            "4. 若出现 `generate_needed`，导出 authoring package、发布 PromptRevision，再生成缺图计划并进入正式生产流。",
        ]
    )
    return "\n".join(lines) + "\n"


def build_payload(args: argparse.Namespace) -> dict[str, Any]:
    manifest_path = resolve_project_path(args.manifest_path)
    seed_path = resolve_project_path(args.seed_path)
    approved_root = resolve_project_path(args.approved_root)
    decision_path = resolve_project_path(args.decision_path)
    manifest_ids = collect_manifest_ids(manifest_path)
    seed_ids = collect_seed_ids(seed_path)
    approved_ids = collect_approved_ids(approved_root)
    candidate_decisions = load_candidate_decisions(decision_path)
    scan_files = iter_scan_files(args.scan_root)

    mentions: dict[str, dict[str, Any]] = {}
    text_reviews: list[dict[str, Any]] = []
    for path in scan_files:
        if path.suffix.lower() == ".json":
            scan_json_file(path, mentions, manifest_ids, seed_ids, approved_ids)
        else:
            scan_markdown(path, mentions, text_reviews, manifest_ids, seed_ids, approved_ids, args.max_text_reviews)

    visual_items = list(mentions.values())
    apply_candidate_decisions(visual_items, candidate_decisions)
    visual_items = sorted(visual_items, key=sort_visual_item)
    for item in visual_items:
        trim_sources(item, args.max_sources_per_item)

    status_counts = Counter(item["Status"] for item in visual_items)
    domain_counts = Counter(item["Domain"] for item in visual_items)
    priority_counts = Counter(item["Priority"] for item in visual_items)
    return {
        "GeneratedAt": timestamp_text(),
        "SnapshotTag": str(args.snapshot_tag or ""),
        "Inputs": {
            "ManifestPath": repo_path(manifest_path),
            "SeedPath": repo_path(seed_path),
            "ApprovedRoot": repo_path(approved_root),
            "DecisionPath": repo_path(decision_path),
            "ScanRoots": [repo_path(resolve_project_path(root)) for root in args.scan_root],
        },
        "Summary": {
            "ScannedFiles": len(scan_files),
            "UniqueVisualIDs": len(visual_items),
            "StatusCounts": dict(sorted(status_counts.items())),
            "DomainCounts": dict(sorted(domain_counts.items())),
            "PriorityCounts": dict(sorted(priority_counts.items())),
            "DecisionCount": len(candidate_decisions),
        },
        "VisualIDItems": visual_items,
        "TextReviewItems": text_reviews,
    }


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Scan docs and configs for visual asset requirement candidates.")
    parser.add_argument("--manifest-path", default=DEFAULT_MANIFEST)
    parser.add_argument("--seed-path", default=DEFAULT_SEED)
    parser.add_argument("--approved-root", default=DEFAULT_APPROVED_ROOT)
    parser.add_argument("--decision-path", default=DEFAULT_DECISION_PATH)
    parser.add_argument("--output-json", default=DEFAULT_OUTPUT_JSON)
    parser.add_argument("--output-markdown", default=DEFAULT_OUTPUT_MARKDOWN)
    parser.add_argument("--snapshot-dir", default=DEFAULT_SNAPSHOT_DIR)
    parser.add_argument("--snapshot-tag", default="")
    parser.add_argument("--snapshot", action="store_true")
    parser.add_argument("--scan-root", action="append", default=[])
    parser.add_argument("--max-text-reviews", type=int, default=80)
    parser.add_argument("--max-sources-per-item", type=int, default=5)
    args = parser.parse_args()
    if not args.scan_root:
        args.scan_root = list(DEFAULT_SCAN_ROOTS)
    return args


def write_snapshot(args: argparse.Namespace, payload: dict[str, Any], markdown: str) -> tuple[Path, Path]:
    snapshot_dir = resolve_project_path(args.snapshot_dir)
    tag = safe_snapshot_tag(args.snapshot_tag)
    stem = timestamp_filename()
    if tag:
        stem = f"{stem}_{tag}"
    snapshot_json = snapshot_dir / f"{stem}.json"
    snapshot_markdown = snapshot_dir / f"{stem}.md"
    write_json(snapshot_json, payload)
    snapshot_markdown.write_text(markdown, encoding="utf-8")
    return snapshot_json, snapshot_markdown


def main() -> int:
    args = parse_args()
    payload = build_payload(args)
    markdown = make_markdown(payload)
    output_json = resolve_project_path(args.output_json)
    output_markdown = resolve_project_path(args.output_markdown)
    write_json(output_json, payload)
    output_markdown.parent.mkdir(parents=True, exist_ok=True)
    output_markdown.write_text(markdown, encoding="utf-8")
    snapshot_paths: tuple[Path, Path] | None = None
    if args.snapshot:
        snapshot_paths = write_snapshot(args, payload, markdown)

    counts = payload["Summary"]["StatusCounts"]
    print(f"[OK] Art requirement candidates: {repo_path(output_markdown)}")
    if snapshot_paths is not None:
        _, snapshot_markdown = snapshot_paths
        print(f"[OK] Art requirement candidates snapshot: {repo_path(snapshot_markdown)}")
    print(
        "[OK] "
        f"new_candidate={counts.get('new_candidate', 0)}, "
        f"approved_without_manifest={counts.get('approved_without_manifest', 0)}, "
        f"seed_only={counts.get('seed_only', 0)}, "
        f"deferred_candidate={counts.get('deferred_candidate', 0)}, "
        f"ignored_candidate={counts.get('ignored_candidate', 0)}, "
        f"manifest_managed={counts.get('manifest_managed', 0)}, "
        f"text_review={len(payload['TextReviewItems'])}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
