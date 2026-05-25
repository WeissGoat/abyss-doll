# -*- coding: utf-8 -*-
"""Normalize legacy local_v0 quality markers in the art Manifest.

Older approved assets may have been generated locally before QualityTier was
introduced. This script converts those legacy markers into the machine-readable
`QualityTier=local_v0` field so later tools do not need to infer from Notes.
"""

from __future__ import annotations

import argparse
import json
import re
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


SCRIPT_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = SCRIPT_DIR.parents[1]

DEFAULT_MANIFEST = "美术文档/_generated/art_manifest.json"
DEFAULT_OUTPUT_JSON = "美术文档/_generated/local_v0_quality_normalization.json"
DEFAULT_OUTPUT_MARKDOWN = "美术文档/_generated/local_v0_quality_normalization.md"
DEFAULT_SNAPSHOT_DIR = "美术文档/_generated/art_quality_snapshots"

FORMAL_QUALITY_TIERS = {"formal_ai_v2", "final", "production"}
LOCAL_QUALITY_TIERS = {"local_v0", "placeholder", "temporary", "fallback"}
LOCAL_MARKERS = (
    "local_v0",
    "generated locally",
    "approved directly in approved",
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


def read_json(path: Path) -> dict[str, Any]:
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


def append_note(entry: dict[str, Any], message: str) -> None:
    existing = str(entry.get("Notes", "") or "").strip()
    if message in existing:
        return
    entry["Notes"] = f"{existing}\n{message}".strip() if existing else message


def quality_tier(entry: dict[str, Any]) -> str:
    return str(entry.get("QualityTier", "") or "").strip().lower()


def has_legacy_local_marker(entry: dict[str, Any]) -> bool:
    tier = quality_tier(entry)
    if tier in FORMAL_QUALITY_TIERS:
        return False
    if tier in LOCAL_QUALITY_TIERS:
        return True
    fields = [
        str(entry.get("BatchID", "") or ""),
        str(entry.get("Notes", "") or ""),
        str(entry.get("SelectedPath", "") or ""),
        str(entry.get("ApprovedPath", "") or ""),
    ]
    text = "\n".join(fields).lower()
    return any(marker in text for marker in LOCAL_MARKERS)


def should_update(entry: dict[str, Any]) -> bool:
    tier = quality_tier(entry)
    if tier in FORMAL_QUALITY_TIERS or tier in LOCAL_QUALITY_TIERS:
        return False
    return has_legacy_local_marker(entry)


def normalize_manifest(manifest: dict[str, Any], *, dry_run: bool) -> dict[str, Any]:
    entries = [entry for entry in manifest.get("Entries", []) if isinstance(entry, dict)]
    generated_at = timestamp_text()
    updated: list[dict[str, Any]] = []
    quality_counts_before = Counter(quality_tier(entry) or "<empty>" for entry in entries)
    local_candidates = [entry for entry in entries if has_legacy_local_marker(entry)]

    for entry in entries:
        if not should_update(entry):
            continue
        visual_id = str(entry.get("VisualID", "") or "")
        record = {
            "VisualID": visual_id,
            "Domain": str(entry.get("Domain", "") or ""),
            "AssetType": str(entry.get("AssetType", "") or ""),
            "BatchID": str(entry.get("BatchID", "") or ""),
            "ApprovedPath": str(entry.get("ApprovedPath", "") or entry.get("OutputPath", "") or ""),
            "PreviousQualityTier": str(entry.get("QualityTier", "") or ""),
            "NewQualityTier": "local_v0",
        }
        updated.append(record)
        if dry_run:
            continue
        entry["QualityTier"] = "local_v0"
        entry.setdefault("QualityUpdatedAt", generated_at)
        append_note(entry, f"[{generated_at}] normalized QualityTier=local_v0 from legacy local generation markers.")

    quality_counts_after = Counter(quality_tier(entry) or "<empty>" for entry in entries)
    return {
        "GeneratedAt": generated_at,
        "DryRun": dry_run,
        "Summary": {
            "TotalManifestEntries": len(entries),
            "LegacyLocalCandidates": len(local_candidates),
            "UpdatedEntries": len(updated),
            "QualityTierBefore": dict(sorted(quality_counts_before.items())),
            "QualityTierAfter": dict(sorted(quality_counts_after.items())),
        },
        "UpdatedEntries": updated,
    }


def md_cell(value: Any) -> str:
    if value is None:
        text = ""
    else:
        text = str(value)
    return text.replace("|", "\\|").replace("\n", " ")


def make_markdown(payload: dict[str, Any]) -> str:
    summary = payload["Summary"]
    lines = [
        "# local_v0 QualityTier Normalization",
        "",
        "> Generated by `tools/美术工具/Normalize-ArtQualityTier.ps1`.",
        "",
        "## Summary",
        "",
        f"* GeneratedAt: `{payload['GeneratedAt']}`",
        f"* DryRun: `{payload['DryRun']}`",
        f"* Manifest entries: `{summary['TotalManifestEntries']}`",
        f"* Legacy local candidates: `{summary['LegacyLocalCandidates']}`",
        f"* Updated entries: `{summary['UpdatedEntries']}`",
        f"* QualityTier before: `{json.dumps(summary['QualityTierBefore'], ensure_ascii=False)}`",
        f"* QualityTier after: `{json.dumps(summary['QualityTierAfter'], ensure_ascii=False)}`",
        "",
        "## Updated Entries",
        "",
    ]
    if payload["UpdatedEntries"]:
        lines.extend(
            [
                "| VisualID | Domain | Type | BatchID | Previous | New | Approved |",
                "|---|---|---|---|---|---|---|",
            ]
        )
        for item in payload["UpdatedEntries"]:
            lines.append(
                "| "
                + " | ".join(
                    [
                        f"`{md_cell(item['VisualID'])}`",
                        md_cell(item["Domain"]),
                        md_cell(item["AssetType"]),
                        md_cell(item["BatchID"]),
                        md_cell(item["PreviousQualityTier"]),
                        md_cell(item["NewQualityTier"]),
                        md_cell(item["ApprovedPath"]),
                    ]
                )
                + " |"
            )
    else:
        lines.append("- No Manifest entries needed normalization.")
    lines.extend(
        [
            "",
            "## Rule",
            "",
            "- Entries with `QualityTier=formal_ai_v2/final/production` are never downgraded.",
            "- Entries already marked `local_v0/placeholder/temporary/fallback` are left unchanged.",
            "- Legacy entries are normalized only when `BatchID`, `Notes`, `SelectedPath`, or `ApprovedPath` carries a local generation marker.",
        ]
    )
    return "\n".join(lines) + "\n"


def write_snapshot(args: argparse.Namespace, payload: dict[str, Any], markdown: str) -> tuple[Path, Path]:
    snapshot_dir = resolve_project_path(args.snapshot_dir)
    tag = safe_snapshot_tag(args.snapshot_tag or "local_v0_quality_normalization")
    stem = timestamp_filename()
    if tag:
        stem = f"{stem}_{tag}"
    snapshot_json = snapshot_dir / f"{stem}.json"
    snapshot_markdown = snapshot_dir / f"{stem}.md"
    write_json(snapshot_json, payload)
    snapshot_markdown.parent.mkdir(parents=True, exist_ok=True)
    snapshot_markdown.write_text(markdown, encoding="utf-8")
    return snapshot_json, snapshot_markdown


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Normalize legacy local_v0 quality markers in art_manifest.json.")
    parser.add_argument("--manifest-path", default=DEFAULT_MANIFEST)
    parser.add_argument("--output-json", default=DEFAULT_OUTPUT_JSON)
    parser.add_argument("--output-markdown", default=DEFAULT_OUTPUT_MARKDOWN)
    parser.add_argument("--snapshot-dir", default=DEFAULT_SNAPSHOT_DIR)
    parser.add_argument("--snapshot-tag", default="")
    parser.add_argument("--snapshot", action="store_true")
    parser.add_argument("--dry-run", action="store_true")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    manifest_path = resolve_project_path(args.manifest_path)
    manifest = read_json(manifest_path)
    payload = normalize_manifest(manifest, dry_run=args.dry_run)
    markdown = make_markdown(payload)

    if not args.dry_run:
        write_json(manifest_path, manifest)

    output_json = resolve_project_path(args.output_json)
    output_markdown = resolve_project_path(args.output_markdown)
    write_json(output_json, payload)
    output_markdown.parent.mkdir(parents=True, exist_ok=True)
    output_markdown.write_text(markdown, encoding="utf-8")

    snapshot_paths: tuple[Path, Path] | None = None
    if args.snapshot:
        snapshot_paths = write_snapshot(args, payload, markdown)

    print(f"[OK] local_v0 normalization: {repo_path(output_markdown)}")
    if snapshot_paths:
        _, snapshot_markdown = snapshot_paths
        print(f"[OK] local_v0 normalization snapshot: {repo_path(snapshot_markdown)}")
    print(
        "[OK] "
        f"legacy_candidates={payload['Summary']['LegacyLocalCandidates']}, "
        f"updated={payload['Summary']['UpdatedEntries']}, "
        f"dry_run={payload['DryRun']}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
