# -*- coding: utf-8 -*-
"""Check that Approved art files and VisualAssetRegistry entries point at the same Unity asset.

For each VisualID, the GUID in the .meta next to the Manifest OutputPath must equal the
Sprite GUID of its Registry entry. Read-only; the light runtime art check runs it first.
"""

from __future__ import annotations

import argparse
import json
import re
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


PROJECT_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_MANIFEST = "美术文档/_generated/art_manifest.json"
DEFAULT_REGISTRY = "UnityClient/Assets/Resources/VisualAssetRegistry.asset"
SCHEMA = "p3-art-binding-check@1"

REGISTRY_ENTRY = re.compile(r"^  - VisualID: (\S+)\s*$")
REGISTRY_SPRITE = re.compile(r"^    Sprite: \{fileID: -?\d+(?:, guid: ([0-9a-fA-F]{32}))?")
META_GUID = re.compile(r"(?m)^guid:\s*([0-9a-fA-F]{32})\s*$")


def parse_registry(text: str) -> dict[str, list[str]]:
    """Map each VisualID to the Sprite GUIDs of its entries ("" when the Sprite is unset)."""
    entries: dict[str, list[str]] = {}
    current = None
    for line in text.splitlines():
        entry = REGISTRY_ENTRY.match(line)
        if entry:
            current = entry.group(1)
            entries.setdefault(current, [])
            continue
        sprite = REGISTRY_SPRITE.match(line)
        if sprite and current is not None:
            entries[current].append((sprite.group(1) or "").lower())
            current = None
    return entries


def read_meta_guid(asset_path: Path) -> str:
    meta_path = asset_path.with_name(asset_path.name + ".meta")
    if not meta_path.exists():
        return ""
    match = META_GUID.search(meta_path.read_text(encoding="utf-8-sig"))
    return match.group(1).lower() if match else ""


def check_visual_id(
    visual_id: str,
    manifest_entry: dict[str, Any] | None,
    registry: dict[str, list[str]],
    root: Path,
) -> dict[str, str]:
    output_path = str((manifest_entry or {}).get("OutputPath") or "")
    guids = registry.get(visual_id, [])
    meta_guid = read_meta_guid(root / output_path) if output_path else ""
    registry_guid = guids[0] if len(guids) == 1 else ""
    if manifest_entry is None:
        result = "fail:manifest_missing"
    elif not output_path or not (root / output_path).is_file():
        result = "fail:approved_file_missing"
    elif not meta_guid:
        result = "fail:meta_missing"
    elif not guids:
        result = "fail:registry_missing"
    elif len(guids) > 1:
        result = "fail:registry_duplicate"
    elif not registry_guid:
        result = "fail:registry_sprite_empty"
    elif registry_guid != meta_guid:
        result = "fail:guid_mismatch"
    else:
        result = "pass"
    return {
        "VisualID": visual_id,
        "OutputPath": output_path,
        "MetaGUID": meta_guid,
        "RegistryGUID": registry_guid,
        "Result": result,
    }


def default_visual_ids(entries: dict[str, dict[str, Any]], registry: dict[str, list[str]]) -> list[str]:
    """Every Manifest VisualID that is in the Registry or claims RegistryStatus=registered."""
    return [
        visual_id
        for visual_id, entry in entries.items()
        if visual_id in registry or entry.get("RegistryStatus") == "registered"
    ]


def run_check(
    root: Path,
    manifest_path: Path,
    registry_path: Path,
    visual_ids: list[str] | None = None,
) -> dict[str, Any]:
    manifest = json.loads(manifest_path.read_text(encoding="utf-8-sig"))
    entries = {
        str(entry.get("VisualID")): entry
        for entry in manifest.get("Entries", [])
        if isinstance(entry, dict) and entry.get("VisualID")
    }
    registry = parse_registry(registry_path.read_text(encoding="utf-8-sig"))
    selected = visual_ids or default_visual_ids(entries, registry)
    items = [check_visual_id(visual_id, entries.get(visual_id), registry, root) for visual_id in selected]
    failed = sum(1 for item in items if item["Result"] != "pass")
    return {
        "schema": SCHEMA,
        "checked_at": datetime.now(timezone.utc).isoformat(timespec="seconds"),
        "manifest": manifest_path.as_posix(),
        "registry": registry_path.as_posix(),
        "summary": {"checked": len(items), "passed": len(items) - failed, "failed": failed},
        "items": items,
    }


def resolve(root: Path, value: str) -> Path:
    path = Path(value)
    return path if path.is_absolute() else root / path


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--visual-id", action="append", default=[], help="VisualID to check; repeatable. Default: every bound VisualID.")
    parser.add_argument("--manifest", default=DEFAULT_MANIFEST)
    parser.add_argument("--registry", default=DEFAULT_REGISTRY)
    parser.add_argument("--root", default=str(PROJECT_ROOT), help="Project root that OutputPath values are relative to.")
    parser.add_argument("--out", default="", help="Write the JSON result here, e.g. UnityClient/Logs/P3ArtCheck/<RunID>/binding.json.")
    args = parser.parse_args(argv)

    root = Path(args.root)
    report = run_check(root, resolve(root, args.manifest), resolve(root, args.registry), args.visual_id)
    if args.out:
        out_path = resolve(root, args.out)
        out_path.parent.mkdir(parents=True, exist_ok=True)
        out_path.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    summary = report["summary"]
    print(f"binding check: {summary['passed']}/{summary['checked']} passed")
    for item in report["items"]:
        if item["Result"] != "pass":
            print(f"  {item['VisualID']}: {item['Result']}")
    return 1 if summary["failed"] else 0


if __name__ == "__main__":
    sys.exit(main())
