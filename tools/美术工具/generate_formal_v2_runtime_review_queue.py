# -*- coding: utf-8 -*-
"""Generate the art-side FormalV2 runtime screenshot review priority queue."""

from __future__ import annotations

import argparse
import json
import re
import sys
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


SCRIPT_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = SCRIPT_DIR.parents[1]

DEFAULT_SEMANTIC_REVIEW = "美术文档/_generated/formal_v2_asset_review/visual_semantic_review.json"
DEFAULT_HANDOFF = "美术文档/_generated/程序接入交接清单.json"
DEFAULT_RUNTIME_STATUS = "美术文档/_generated/FormalV2运行时验收状态.json"
DEFAULT_OUTPUT_JSON = "美术文档/_generated/FormalV2运行时复验优先级清单.json"
DEFAULT_OUTPUT_MARKDOWN = "美术文档/_generated/FormalV2运行时复验优先级清单.md"
DEFAULT_SNAPSHOT_DIR = "美术文档/_generated/formal_v2_runtime_review_queue_snapshots"

RISK_RANK = {
    "secondary_replacement_candidate": 0,
    "style_mismatch_watch": 1,
    "watch_runtime_readability": 2,
    "ok_for_current_v2": 9,
}

PRIORITY_RANK = {"P0": 0, "P1": 1, "P2": 2, "P3": 3, "P4": 4}


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


def visual_lookup(handoff: dict[str, Any]) -> dict[str, dict[str, Any]]:
    result: dict[str, dict[str, Any]] = {}
    for item in as_list(handoff.get("ProgramIntegrateVisuals")):
        if isinstance(item, dict) and item.get("VisualID"):
            result[str(item["VisualID"])] = item
    return result


def infer_review_screens(item: dict[str, Any], handoff_item: dict[str, Any] | None) -> list[str]:
    visual_id = str(item.get("VisualID", ""))
    domain = str(item.get("Domain", ""))
    asset_type = str(item.get("AssetType", ""))
    screens = [str(value) for value in as_list((handoff_item or {}).get("ReferencedScreens")) if str(value)]
    if screens:
        return sorted(set(screens))
    if domain == "ui" and "combat_feedback" in visual_id:
        return ["combat_hud"]
    if domain == "monster":
        if asset_type == "portrait" or visual_id.endswith("_portrait"):
            return ["combat_hud", "order_board", "rumor_board"]
        return ["combat_hud"]
    if domain == "item":
        if visual_id.startswith("item_order_"):
            return ["order_board", "shop_staging"]
        if visual_id.startswith("item_trade_"):
            return ["shop_staging", "business_settlement"]
        return ["combat_hud", "inventory_loot", "safe_room", "stairs_room"]
    if domain == "memento":
        return ["doll_room", "doll_interaction"]
    if domain == "order":
        return ["order_board", "shop_staging", "business_settlement"]
    if domain == "rumor":
        return ["rumor_board", "daily_bill_report"]
    if domain == "background":
        if "workshop" in visual_id:
            return ["workshop_main", "maintenance_panel", "prosthetic_panel", "chassis_upgrade_panel"]
        if "shop" in visual_id or "daily_bill" in visual_id or "town" in visual_id:
            return ["shop_staging", "daily_bill_report", "sell_panel"]
        return ["dungeon_map", "layer_select", "safe_room", "stairs_room"]
    return []


def review_bucket(item: dict[str, Any]) -> str:
    visual_id = str(item.get("VisualID", ""))
    domain = str(item.get("Domain", ""))
    asset_type = str(item.get("AssetType", ""))
    if domain == "monster" and (asset_type == "portrait" or visual_id.endswith("_portrait")):
        return "monster_portrait_readability"
    if domain == "monster":
        return "monster_combat_silhouette"
    if domain == "item":
        return "item_icon_semantics"
    if domain == "order":
        return "order_icon_semantics"
    if domain == "rumor":
        return "rumor_icon_semantics"
    if domain == "memento":
        return "memento_icon_semantics"
    if domain == "ui":
        return "ui_feedback_readability"
    if domain == "background":
        return "background_fit_and_mood"
    return "general_runtime_review"


def build_payload(
    semantic_review_path: Path,
    handoff_path: Path,
    runtime_status_path: Path,
) -> dict[str, Any]:
    semantic = read_json(semantic_review_path, {})
    handoff = read_json(handoff_path, {})
    runtime = read_json(runtime_status_path, {})
    lookup = visual_lookup(handoff if isinstance(handoff, dict) else {})
    raw_items = [item for item in as_list(semantic.get("ReviewItems")) if isinstance(item, dict)]
    queue_items: list[dict[str, Any]] = []
    for item in raw_items:
        risk = str(item.get("RiskLevel", ""))
        if risk == "ok_for_current_v2":
            continue
        visual_id = str(item.get("VisualID", ""))
        handoff_item = lookup.get(visual_id)
        priority = str(item.get("Priority", "") or (handoff_item or {}).get("Priority", ""))
        domain = str(item.get("Domain", ""))
        bucket = review_bucket(item)
        screens = infer_review_screens(item, handoff_item)
        queue_items.append(
            {
                "VisualID": visual_id,
                "Priority": priority,
                "RiskLevel": risk,
                "ReviewBucket": bucket,
                "Domain": domain,
                "AssetType": str(item.get("AssetType", "")),
                "DisplayName": str(item.get("DisplayName", "")),
                "ReviewScreens": screens,
                "ReasonCN": str(item.get("ReasonCN", "")),
                "RecommendationCN": str(item.get("RecommendationCN", "")),
                "ApprovedPath": str(item.get("ApprovedPath", "")),
                "CanProgramIntegrate": bool(item.get("CanProgramIntegrate", False)),
                "RegistryHasSprite": bool((handoff_item or {}).get("RegistryHasSprite", False)),
            }
        )
    queue_items.sort(
        key=lambda item: (
            RISK_RANK.get(str(item["RiskLevel"]), 8),
            PRIORITY_RANK.get(str(item["Priority"]), 9),
            str(item["ReviewBucket"]),
            str(item["VisualID"]),
        )
    )
    bucket_counts = Counter(str(item["ReviewBucket"]) for item in queue_items)
    screen_counts = Counter(screen for item in queue_items for screen in item["ReviewScreens"])
    risk_counts = Counter(str(item["RiskLevel"]) for item in queue_items)
    runtime_summary = runtime.get("Summary", {}) if isinstance(runtime, dict) else {}
    handoff_summary = handoff.get("Summary", {}) if isinstance(handoff, dict) else {}
    return {
        "GeneratedAt": timestamp_text(),
        "Inputs": {
            "SemanticReviewPath": repo_path(semantic_review_path),
            "HandoffPath": repo_path(handoff_path),
            "RuntimeStatusPath": repo_path(runtime_status_path),
        },
        "Summary": {
            "ProgramIntegrateVisualCount": int(handoff_summary.get("ProgramIntegrateVisualCount", 0) or 0),
            "RuntimeGate": str(runtime.get("Gate", "") if isinstance(runtime, dict) else ""),
            "MissingRegistryCount": int(runtime_summary.get("MissingRegistryCount", 0) or 0),
            "ReviewQueueCount": len(queue_items),
            "RiskCounts": dict(sorted(risk_counts.items())),
            "BucketCounts": dict(sorted(bucket_counts.items())),
            "ScreenCounts": dict(sorted(screen_counts.items())),
        },
        "RequiredNextAction": str(runtime.get("RequiredNextAction", "") if isinstance(runtime, dict) else ""),
        "ReviewItems": queue_items,
    }


def md_cell(value: Any) -> str:
    if isinstance(value, list):
        value = ", ".join(f"`{item}`" for item in value)
    text = str(value)
    return text.replace("|", "/").replace("\n", " ")


def make_markdown(payload: dict[str, Any]) -> str:
    summary = payload["Summary"]
    lines = [
        "# FormalV2 运行时复验优先级清单",
        "",
        "> 这份清单只用于美术侧在程序重建 Registry 并重跑 ArtAcceptance 后逐屏审图。它不阻塞程序登记，也不替代 `visual_semantic_review` 的全量记录。",
        "",
        f"- GeneratedAt: `{payload['GeneratedAt']}`",
        f"- Runtime gate: `{summary['RuntimeGate']}`",
        f"- Program integrate VisualIDs: `{summary['ProgramIntegrateVisualCount']}`",
        f"- Missing registry: `{summary['MissingRegistryCount']}`",
        f"- Review queue: `{summary['ReviewQueueCount']}`",
        f"- Required next action: {payload['RequiredNextAction']}",
        "",
        "## Bucket Counts",
        "",
    ]
    for key, value in summary["BucketCounts"].items():
        lines.append(f"- `{key}`: `{value}`")
    lines.extend(["", "## Screen Counts", ""])
    for key, value in summary["ScreenCounts"].items():
        lines.append(f"- `{key}`: `{value}`")
    lines.extend(
        [
            "",
            "## Review Queue",
            "",
            "| Risk | Priority | Bucket | VisualID | Screens | Reason | Recommendation |",
            "|---|---|---|---|---|---|---|",
        ]
    )
    for item in payload["ReviewItems"]:
        lines.append(
            "| "
            + " | ".join(
                [
                    f"`{md_cell(item['RiskLevel'])}`",
                    f"`{md_cell(item['Priority'])}`",
                    f"`{md_cell(item['ReviewBucket'])}`",
                    f"`{md_cell(item['VisualID'])}`",
                    md_cell(item["ReviewScreens"]),
                    md_cell(item["ReasonCN"]),
                    md_cell(item["RecommendationCN"]),
                ]
            )
            + " |"
        )
    return "\n".join(lines) + "\n"


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--semantic-review-path", default=DEFAULT_SEMANTIC_REVIEW)
    parser.add_argument("--handoff-path", default=DEFAULT_HANDOFF)
    parser.add_argument("--runtime-status-path", default=DEFAULT_RUNTIME_STATUS)
    parser.add_argument("--output-json", default=DEFAULT_OUTPUT_JSON)
    parser.add_argument("--output-markdown", default=DEFAULT_OUTPUT_MARKDOWN)
    parser.add_argument("--snapshot-dir", default=DEFAULT_SNAPSHOT_DIR)
    parser.add_argument("--snapshot-tag", default="")
    parser.add_argument("--snapshot", action="store_true")
    args = parser.parse_args()

    semantic_review_path = resolve_project_path(args.semantic_review_path)
    handoff_path = resolve_project_path(args.handoff_path)
    runtime_status_path = resolve_project_path(args.runtime_status_path)
    output_json = resolve_project_path(args.output_json)
    output_markdown = resolve_project_path(args.output_markdown)
    payload = build_payload(semantic_review_path, handoff_path, runtime_status_path)
    markdown = make_markdown(payload)
    write_json(output_json, payload)
    output_markdown.parent.mkdir(parents=True, exist_ok=True)
    output_markdown.write_text(markdown, encoding="utf-8")

    if args.snapshot:
        snapshot_dir = resolve_project_path(args.snapshot_dir)
        tag = safe_snapshot_tag(args.snapshot_tag) or "formalv2_runtime_review_queue"
        stamp = timestamp_filename()
        write_json(snapshot_dir / f"{stamp}_{tag}.json", payload)
        (snapshot_dir / f"{stamp}_{tag}.md").write_text(markdown, encoding="utf-8")
        print(f"[OK] FormalV2 runtime review queue snapshot: {repo_path(snapshot_dir / f'{stamp}_{tag}.md')}")

    summary = payload["Summary"]
    print(f"[OK] FormalV2 runtime review queue: {repo_path(output_markdown)}")
    print(
        "[OK] "
        f"gate={summary['RuntimeGate']}, "
        f"program_integrate={summary['ProgramIntegrateVisualCount']}, "
        f"review_queue={summary['ReviewQueueCount']}, "
        f"missing_registry={summary['MissingRegistryCount']}"
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
