# -*- coding: utf-8 -*-
"""Generate a program-facing checklist for VisualAssetRegistry gaps."""

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

DEFAULT_HANDOFF = "美术文档/_generated/程序接入交接清单.json"
DEFAULT_REGISTRY = "UnityClient/Assets/Resources/VisualAssetRegistry.asset"
DEFAULT_OUTPUT_JSON = "美术文档/_generated/VisualAssetRegistry登记缺口清单.json"
DEFAULT_OUTPUT_MARKDOWN = "美术文档/_generated/VisualAssetRegistry登记缺口清单.md"
DEFAULT_SNAPSHOT_DIR = "美术文档/_generated/art_registry_gap_snapshots"


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


def read_json(path: Path) -> Any:
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


def parse_registry_visual_ids(path: Path) -> set[str]:
    if not path.exists():
        return set()
    text = path.read_text(encoding="utf-8-sig", errors="replace")
    return set(re.findall(r"^\s*-\s*VisualID:\s*(\S+)\s*$", text, flags=re.MULTILINE))


def md_cell(value: Any) -> str:
    if value is None or value == "":
        text = "-"
    elif isinstance(value, list):
        text = ", ".join(str(item) for item in value) if value else "-"
    else:
        text = str(value)
    return text.replace("|", "\\|").replace("\n", " ")


def build_payload(handoff_path: Path, registry_path: Path) -> dict[str, Any]:
    handoff = read_json(handoff_path)
    registry_ids = parse_registry_visual_ids(registry_path)
    visuals = handoff.get("ProgramIntegrateVisuals", [])
    items = []
    for visual in visuals:
        visual_id = str(visual.get("VisualID", "") or "").strip()
        if not visual_id:
            continue
        approved_path = resolve_project_path(str(visual.get("ApprovedPath", "") or ""))
        meta_path = approved_path.with_suffix(approved_path.suffix + ".meta") if approved_path.suffix else None
        in_registry = visual_id in registry_ids
        items.append(
            {
                "VisualID": visual_id,
                "Priority": str(visual.get("Priority", "") or ""),
                "Domain": str(visual.get("Domain", "") or ""),
                "AssetType": str(visual.get("AssetType", "") or ""),
                "DisplayName": str(visual.get("DisplayName", "") or ""),
                "ApprovedPath": repo_path(approved_path),
                "ApprovedExists": approved_path.exists(),
                "MetaPath": repo_path(meta_path),
                "MetaExists": bool(meta_path and meta_path.exists()),
                "RegistryHasVisualID": in_registry,
                "RequiredAction": "none" if in_registry else "register_visual_id",
            }
        )

    action_counts = Counter(item["RequiredAction"] for item in items)
    payload = {
        "GeneratedAt": timestamp_text(),
        "Inputs": {
            "HandoffPath": repo_path(handoff_path),
            "RegistryPath": repo_path(registry_path),
        },
        "Summary": {
            "ProgramIntegrateVisualCount": len(items),
            "RegistryExistingCount": len(registry_ids),
            "MissingRegistryCount": action_counts.get("register_visual_id", 0),
            "AlreadyRegisteredCount": action_counts.get("none", 0),
            "MissingApprovedFileCount": sum(1 for item in items if not item["ApprovedExists"]),
            "MissingMetaFileCount": sum(1 for item in items if not item["MetaExists"]),
            "DomainCounts": dict(Counter(item["Domain"] for item in items)),
            "PriorityCounts": dict(Counter(item["Priority"] for item in items)),
        },
        "ProgramAction": {
            "UnityMenu": "Tools/P3 Art/Rebuild Approved Sprite Registry",
            "ThenRun": "ArtAcceptance / VisualAsset validation after Unity imports sprites",
        },
        "Items": items,
    }
    return payload


def make_markdown(payload: dict[str, Any]) -> str:
    summary = payload["Summary"]
    lines = [
        "# VisualAssetRegistry 登记缺口清单",
        "",
        "> 美术侧生成的程序登记核对清单。它不修改 Unity 资产，只对比 `程序接入交接清单` 和当前 `VisualAssetRegistry.asset`。",
        "",
        f"- GeneratedAt: `{payload['GeneratedAt']}`",
        f"- ProgramIntegrateVisualCount: `{summary['ProgramIntegrateVisualCount']}`",
        f"- RegistryExistingCount: `{summary['RegistryExistingCount']}`",
        f"- MissingRegistryCount: `{summary['MissingRegistryCount']}`",
        f"- AlreadyRegisteredCount: `{summary['AlreadyRegisteredCount']}`",
        f"- MissingApprovedFileCount: `{summary['MissingApprovedFileCount']}`",
        f"- MissingMetaFileCount: `{summary['MissingMetaFileCount']}`",
        "",
        "## 程序侧动作",
        "",
        "1. 在 Unity Editor 中执行 `Tools/P3 Art/Rebuild Approved Sprite Registry`。",
        "2. 保存 `UnityClient/Assets/Resources/VisualAssetRegistry.asset`。",
        "3. 重跑 VisualAsset / ArtAcceptance 验收，并把新的 `UnityClient/Logs/ArtAcceptance/latest` 交给美术侧验收。",
        "",
        "## 缺口明细",
        "",
        "| VisualID | Priority | Domain | Type | Registered | Approved | Meta | Path |",
        "|---|---|---|---|---|---|---|---|",
    ]
    for item in payload["Items"]:
        if item["RequiredAction"] == "none":
            continue
        lines.append(
            "| "
            + " | ".join(
                [
                    f"`{md_cell(item['VisualID'])}`",
                    md_cell(item["Priority"]),
                    md_cell(item["Domain"]),
                    md_cell(item["AssetType"]),
                    "yes" if item["RegistryHasVisualID"] else "no",
                    "yes" if item["ApprovedExists"] else "no",
                    "yes" if item["MetaExists"] else "no",
                    f"`{md_cell(item['ApprovedPath'])}`",
                ]
            )
            + " |"
        )
    return "\n".join(lines) + "\n"


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--handoff-path", default=DEFAULT_HANDOFF)
    parser.add_argument("--registry-path", default=DEFAULT_REGISTRY)
    parser.add_argument("--output-json", default=DEFAULT_OUTPUT_JSON)
    parser.add_argument("--output-markdown", default=DEFAULT_OUTPUT_MARKDOWN)
    parser.add_argument("--snapshot-dir", default=DEFAULT_SNAPSHOT_DIR)
    parser.add_argument("--snapshot-tag", default="")
    parser.add_argument("--snapshot", action="store_true")
    args = parser.parse_args()

    handoff_path = resolve_project_path(args.handoff_path)
    registry_path = resolve_project_path(args.registry_path)
    output_json = resolve_project_path(args.output_json)
    output_markdown = resolve_project_path(args.output_markdown)

    payload = build_payload(handoff_path, registry_path)
    markdown = make_markdown(payload)

    write_json(output_json, payload)
    output_markdown.parent.mkdir(parents=True, exist_ok=True)
    output_markdown.write_text(markdown, encoding="utf-8")

    if args.snapshot:
        snapshot_dir = resolve_project_path(args.snapshot_dir)
        tag = safe_snapshot_tag(args.snapshot_tag) or "registry_gap_checklist"
        stamp = timestamp_filename()
        write_json(snapshot_dir / f"{stamp}_{tag}.json", payload)
        (snapshot_dir / f"{stamp}_{tag}.md").write_text(markdown, encoding="utf-8")
        print(f"[OK] Art registry gap snapshot: {repo_path(snapshot_dir / f'{stamp}_{tag}.md')}")

    summary = payload["Summary"]
    print(f"[OK] Art registry gap checklist: {repo_path(output_markdown)}")
    print(
        "[OK] "
        f"program_integrate={summary['ProgramIntegrateVisualCount']}, "
        f"missing_registry={summary['MissingRegistryCount']}, "
        f"missing_approved={summary['MissingApprovedFileCount']}, "
        f"missing_meta={summary['MissingMetaFileCount']}"
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
