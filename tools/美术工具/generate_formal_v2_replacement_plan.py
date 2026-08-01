# -*- coding: utf-8 -*-
"""Build a method-neutral same-VisualID replacement plan from active Formal V2 facts."""

from __future__ import annotations

import argparse
import json
import re
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from art_prompt_revision import PROMPT_FORMATS


SCRIPT_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = SCRIPT_DIR.parents[1]

DEFAULT_OVERVIEW = "美术文档/ui_design/formal_v2/00_formal_v2_ux_ui_overview.md"
DEFAULT_MANIFEST = "美术文档/_generated/art_manifest.json"
DEFAULT_OUTPUT_JSON = "美术文档/_generated/FormalV2主动迭代计划.json"
DEFAULT_OUTPUT_MARKDOWN = "美术文档/_generated/FormalV2主动迭代计划.md"
DEFAULT_REQUEST_CATALOG = "美术文档/_generated/art_generation_requests.json"
DEFAULT_SNAPSHOT_DIR = "美术文档/_generated/formal_v2_replacement_snapshots"

VISUAL_TOKEN_RE = re.compile(r"`([A-Za-z][A-Za-z0-9_]*)`")
ASSET_PREFIXES = (
    "bg_",
    "cg_",
    "chassis_",
    "doll_",
    "faction_",
    "fx_",
    "item_",
    "memento_",
    "monster_",
    "node_",
    "order_",
    "portrait_",
    "prosthetic_",
    "rumor_",
    "ui_",
    "vfx_",
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
    return re.sub(r"_+", "_", text).strip("._-")[:80]


def asset_class(visual_id: str, entry: dict[str, Any]) -> str:
    spec = entry.get("Spec") if isinstance(entry.get("Spec"), dict) else {}
    process_spec = spec.get("ProcessSpec") if isinstance(spec.get("ProcessSpec"), dict) else {}
    nine_slice = process_spec.get("NineSlice") if isinstance(process_spec.get("NineSlice"), dict) else {}
    if bool(nine_slice.get("Enabled")) or str(entry.get("Domain", "")) == "ui":
        if bool(nine_slice.get("Enabled")):
            return "ui_skin"
        if str(entry.get("AssetType", "")) == "icon":
            return "icon"
        return "standard_asset"
    if visual_id.startswith("bg_") or str(entry.get("Domain", "")) == "background":
        return "background"
    if visual_id.startswith(("fx_", "vfx_")):
        return "vfx"
    return "standard_asset"


def _is_visual_token(token: str) -> bool:
    return token.startswith(ASSET_PREFIXES)


def parse_formal_v2_overview(path: Path) -> dict[str, str]:
    """Extract runtime asset IDs from the UI Skin baseline and V2-A active scene rows."""

    result: dict[str, str] = {}
    section = ""
    for raw_line in path.read_text(encoding="utf-8-sig").splitlines():
        line = raw_line.strip()
        if line.startswith("### 4.4"):
            section = "ui_skin"
            continue
        if line.startswith("### 4.5"):
            section = "scene"
            continue
        if line.startswith("### "):
            section = ""
            continue
        if not line.startswith("|") or line.startswith("|---"):
            continue
        if section == "scene" and "V2-A active" not in line:
            continue
        for token in VISUAL_TOKEN_RE.findall(line):
            if not _is_visual_token(token):
                continue
            if section == "ui_skin":
                result[token] = "ui_skin"
            elif section == "scene":
                result[token] = "background" if token.startswith("bg_") else "standard_asset"
    return result


def resolve_prompt_evidence(
    entry: dict[str, Any],
    request: dict[str, Any] | None,
) -> dict[str, Any]:
    compiled = entry.get("CompiledRequest")
    compiled = compiled if isinstance(compiled, dict) else {}
    evidence = {
        "RequestID": str((request or {}).get("RequestID", "")),
        "RequirementFingerprint": str((request or {}).get("RequirementFingerprint", "")),
        "PromptAuthoringStatus": str((request or {}).get("PromptAuthoringStatus", "")),
        "PromptRevisionID": str((request or {}).get("ActivePromptRevisionID", "")),
        "PromptRevisionFingerprint": "",
        "PromptFormats": [],
        "PromptReady": False,
        "PromptBlockReason": "",
    }
    if request is None:
        evidence["PromptBlockReason"] = "catalog_request_missing"
        return evidence
    if request.get("RequirementStatus") != "ready":
        evidence["PromptBlockReason"] = "requirement_not_ready"
        return evidence
    if request.get("PromptAuthoringStatus") != "prompt_ready":
        evidence["PromptBlockReason"] = "prompt_authoring_required"
        return evidence
    for field in (
        "RequestID",
        "RequirementFingerprint",
        "PromptAuthoringStatus",
        "ActivePromptRevisionID",
    ):
        if compiled.get(field) != request.get(field):
            evidence["PromptBlockReason"] = f"manifest_pointer_mismatch:{field}"
            return evidence

    revision = next(
        (
            value
            for value in request.get("PromptRevisions", [])
            if isinstance(value, dict)
            and value.get("PromptRevisionID") == request.get("ActivePromptRevisionID")
        ),
        None,
    )
    if revision is None:
        evidence["PromptBlockReason"] = "active_prompt_revision_missing"
        return evidence
    evidence["PromptRevisionFingerprint"] = str(revision.get("RevisionFingerprint", ""))
    if revision.get("RequirementFingerprint") != request.get("RequirementFingerprint"):
        evidence["PromptBlockReason"] = "prompt_revision_stale"
        return evidence
    if revision.get("Status") != "ready":
        evidence["PromptBlockReason"] = "prompt_revision_not_ready"
        return evidence

    variants = revision.get("Variants")
    variants = variants if isinstance(variants, dict) else {}
    evidence["PromptFormats"] = [
        format_id
        for format_id in PROMPT_FORMATS
        if isinstance(variants.get(format_id), dict)
        and variants[format_id].get("Status") == "ready"
    ]
    if not evidence["PromptFormats"]:
        evidence["PromptBlockReason"] = "prompt_revision_no_ready_v2_variant"
        return evidence
    evidence["PromptReady"] = True
    return evidence


def build_item(
    visual_id: str,
    source_class: str,
    entry: dict[str, Any],
    batch_id: str,
    variants: int,
    delay_seconds: float,
    request: dict[str, Any] | None = None,
) -> dict[str, Any]:
    approved_value = str(entry.get("ApprovedPath") or entry.get("OutputPath") or "")
    profile_root = "character_portraits" if entry.get("ProductionProfile") == "character_portrait_set" else "standard_assets"
    workspace = f"UnityClient/Assets/Art/_IncomingAI/{profile_root}/{visual_id}"
    prompt_evidence = resolve_prompt_evidence(entry, request)
    item = {
        "VisualID": visual_id,
        "Action": "visual_v2_replace",
        "Source": "formal_v2_active",
        "SourceClass": source_class,
        "AssetClass": asset_class(visual_id, entry),
        "Domain": str(entry.get("Domain", "") or ""),
        "AssetType": str(entry.get("AssetType", "") or ""),
        "Priority": str(entry.get("Priority", "") or ""),
        "DisplayName": str(entry.get("DisplayName", "") or ""),
        "ProductionProfile": str(entry.get("ProductionProfile", "standard_asset")),
        "CurrentQuality": str(entry.get("QualityTier", "") or "approved"),
        "ApprovedPath": approved_value,
        "Workspace": {
            "Base": workspace,
            "Raw": f"{workspace}/raw",
            "Processed": f"{workspace}/processed",
            "Selected": f"{workspace}/selected",
        },
        "RunConfig": {
            "BatchID": batch_id,
            "Variants": variants,
            "DelaySeconds": delay_seconds,
            "Provider": "agent_selected",
            "Method": "agent_selected",
        },
        "Constraints": [
            "preserve VisualID and ApprovedPath",
            "preserve DisplaySpec and Unity GUID",
            "publish the next numeric processed round",
            "do not sync Approved automatically",
        ],
    }
    item.update(prompt_evidence)
    return item


def build_payload(
    overview_path: Path,
    manifest_path: Path,
    batch_id: str,
    variants: int,
    delay_seconds: float,
    visual_ids: set[str] | None = None,
    request_catalog_path: Path | None = None,
) -> dict[str, Any]:
    source_ids = parse_formal_v2_overview(overview_path)
    if visual_ids:
        source_ids = {visual_id: value for visual_id, value in source_ids.items() if visual_id in visual_ids}
    manifest = read_json(manifest_path)
    request_map: dict[str, dict[str, Any]] = {}
    catalog_path = request_catalog_path
    if catalog_path is not None and catalog_path.exists():
        catalog = read_json(catalog_path)
        request_map = {
            str(request.get("VisualID", "")): request
            for request in catalog.get("Requests", [])
            if isinstance(request, dict) and request.get("VisualID")
        }
    entries = {
        str(entry.get("VisualID", "")): entry
        for entry in manifest.get("Entries", [])
        if isinstance(entry, dict) and entry.get("VisualID")
    }
    items: list[dict[str, Any]] = []
    skipped: list[dict[str, str]] = []
    for visual_id, source_class in sorted(source_ids.items()):
        entry = entries.get(visual_id)
        if entry is None:
            skipped.append({"VisualID": visual_id, "Reason": "manifest_missing"})
            continue
        status = str(entry.get("Status", "") or "")
        approved_value = str(entry.get("ApprovedPath") or entry.get("OutputPath") or "")
        approved_path = resolve_project_path(approved_value) if approved_value else None
        if status not in {"approved", "registered", "validated"}:
            skipped.append({"VisualID": visual_id, "Reason": f"status_{status or 'missing'}"})
            continue
        if approved_path is None or not approved_path.exists():
            skipped.append({"VisualID": visual_id, "Reason": "approved_missing"})
            continue
        request = request_map.get(visual_id)
        items.append(build_item(visual_id, source_class, entry, batch_id, variants, delay_seconds, request))

    return {
        "GeneratedAt": timestamp_text(),
        "Inputs": {
            "FormalV2Overview": repo_path(overview_path),
            "Manifest": repo_path(manifest_path),
        },
        "RunConfig": {
            "BatchID": batch_id,
            "Variants": variants,
            "DelaySeconds": delay_seconds,
            "Provider": "agent_selected",
            "Method": "agent_selected",
            "SerialRequired": True,
            "Action": "visual_v2_replace",
        },
        "Summary": {
            "RequestedVisualIDs": sorted(visual_ids or []),
            "ExtractedVisualCount": len(source_ids),
            "PlannedItems": len(items),
            "PromptReadyItems": sum(1 for item in items if item["PromptReady"]),
            "SkippedItems": skipped,
            "AssetClassCounts": dict(sorted(Counter(item["AssetClass"] for item in items).items())),
        },
        "Items": items,
    }


def make_markdown(payload: dict[str, Any]) -> str:
    summary = payload["Summary"]
    run = payload["RunConfig"]
    lines = [
        "# Formal V2 主动迭代计划",
        "",
        "> This is a generated replacement plan, not a second Manifest or progress table.",
        "",
        f"- GeneratedAt: `{payload['GeneratedAt']}`",
        f"- BatchID: `{run['BatchID']}`",
        f"- Action: `{run['Action']}`",
        f"- Provider: `{run['Provider']}`",
        f"- Method: `{run['Method']}`",
        f"- Extracted: `{summary['ExtractedVisualCount']}`",
        f"- Planned: `{summary['PlannedItems']}`",
        f"- Prompt ready: `{summary['PromptReadyItems']}`",
        f"- Asset classes: `{json.dumps(summary['AssetClassCounts'], ensure_ascii=False)}`",
        "",
        "## Rules",
        "",
        "- Keep `VisualID`, `ApprovedPath`, `DisplaySpec`, `.meta`, and GUID unchanged.",
        "- Choose the provider and generation method per run; this plan intentionally does not hard-code either.",
        "- Publish a new numeric `processed/<n>` round and stop on failed or decision-required rounds.",
        "- Do not sync Approved from this plan automatically.",
        "",
        "## Items",
        "",
        "| Order | VisualID | Class | Domain | Type | Prompt | Current | ApprovedPath |",
        "|---:|---|---|---|---|---|---|---|",
    ]
    for index, item in enumerate(payload["Items"], start=1):
        lines.append(
            f"| {index} | `{item['VisualID']}` | {item['AssetClass']} | {item['Domain']} | {item['AssetType']} | "
            f"{'ready' if item['PromptReady'] else 'blocked'} | {item['CurrentQuality']} | {item['ApprovedPath']} |"
        )
    if summary["SkippedItems"]:
        lines.extend(["", "## Skipped", ""])
        for item in summary["SkippedItems"]:
            lines.append(f"- `{item['VisualID']}`: {item['Reason']}")
    return "\n".join(lines) + "\n"


def write_snapshot(snapshot_dir: Path, tag: str, payload: dict[str, Any], markdown: str) -> Path:
    target = snapshot_dir / f"{timestamp_filename()}_{safe_snapshot_tag(tag)}"
    target.mkdir(parents=True, exist_ok=True)
    write_json(target / "FormalV2主动迭代计划.json", payload)
    (target / "FormalV2主动迭代计划.md").write_text(markdown, encoding="utf-8")
    return target


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate a Formal V2 active same-VisualID replacement plan.")
    parser.add_argument("--overview-path", default=DEFAULT_OVERVIEW)
    parser.add_argument("--manifest-path", default=DEFAULT_MANIFEST)
    parser.add_argument("--output-json", default=DEFAULT_OUTPUT_JSON)
    parser.add_argument("--output-markdown", default=DEFAULT_OUTPUT_MARKDOWN)
    parser.add_argument("--snapshot-dir", default=DEFAULT_SNAPSHOT_DIR)
    parser.add_argument("--snapshot-tag", default="formalv2_replacement")
    parser.add_argument("--batch-id", default="formalv2_replacement_20260719_01")
    parser.add_argument("--variants", type=int, default=4)
    parser.add_argument("--delay-seconds", type=float, default=1.0)
    parser.add_argument("--visual-id", action="append", default=[])
    parser.add_argument("--snapshot", action="store_true")
    parser.add_argument("--request-catalog-path", default=DEFAULT_REQUEST_CATALOG)
    args = parser.parse_args()
    if args.variants < 1:
        raise ValueError("--variants must be at least 1")

    overview_path = resolve_project_path(args.overview_path)
    manifest_path = resolve_project_path(args.manifest_path)
    requested_visual_ids = {
        visual_id.strip()
        for value in args.visual_id
        for visual_id in value.split(",")
        if visual_id.strip()
    }
    payload = build_payload(
        overview_path,
        manifest_path,
        args.batch_id,
        args.variants,
        args.delay_seconds,
        requested_visual_ids,
        resolve_project_path(args.request_catalog_path),
    )
    markdown = make_markdown(payload)
    output_json = resolve_project_path(args.output_json)
    output_markdown = resolve_project_path(args.output_markdown)
    write_json(output_json, payload)
    output_markdown.parent.mkdir(parents=True, exist_ok=True)
    output_markdown.write_text(markdown, encoding="utf-8")
    if args.snapshot:
        snapshot = write_snapshot(resolve_project_path(args.snapshot_dir), args.snapshot_tag, payload, markdown)
        print(f"[OK] Formal V2 replacement snapshot: {repo_path(snapshot)}")
    print(f"[OK] Formal V2 replacement plan: {repo_path(output_markdown)}")
    print(
        f"[OK] extracted={payload['Summary']['ExtractedVisualCount']}, "
        f"planned={payload['Summary']['PlannedItems']}, "
        f"prompt_ready={payload['Summary']['PromptReadyItems']}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
