# -*- coding: utf-8 -*-
"""Generate PromptEN readiness report for Formal V2 program-integrate assets."""

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
GENERIC_FRAGMENTS = [
    "single readable game asset",
    "single readable asset",
]
FORBIDDEN_PROMPT_TERMS = [
    "Unity",
    "UGUI",
    "Made in Abyss",
    "Tarkov",
]


def timestamp_text() -> str:
    return datetime.now(timezone.utc).astimezone().isoformat(timespec="seconds")


def timestamp_filename() -> str:
    return datetime.now(timezone.utc).astimezone().strftime("%Y%m%d_%H%M%S")


def safe_snapshot_tag(value: str) -> str:
    text = re.sub(r"[^A-Za-z0-9_.-]+", "_", value.strip())
    text = re.sub(r"_+", "_", text).strip("._-")
    return text[:80]


def read_json(path: Path) -> Any:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def write_json(path: Path, payload: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def find_art_generated_dir() -> Path:
    for path in PROJECT_ROOT.iterdir():
        generated = path / "_generated"
        if generated.is_dir() and (generated / "art_manifest.json").exists():
            return generated
    raise FileNotFoundError("Cannot find art _generated directory with art_manifest.json.")


def find_json_with_key(generated_dir: Path, key: str) -> Path:
    for path in sorted(generated_dir.glob("*.json")):
        try:
            data = read_json(path)
        except Exception:
            continue
        if isinstance(data, dict) and key in data:
            return path
    raise FileNotFoundError(f"Cannot find JSON containing key: {key}")


def repo_path(path: Path) -> str:
    try:
        return path.resolve().relative_to(PROJECT_ROOT.resolve()).as_posix()
    except ValueError:
        return path.as_posix()


def default_manifest_path() -> Path:
    return find_art_generated_dir() / "art_manifest.json"


def default_handoff_path() -> Path:
    return find_json_with_key(find_art_generated_dir(), "ProgramIntegrateVisuals")


def default_output_dir() -> Path:
    return find_art_generated_dir() / "formal_v2_prompt_readiness"


def default_snapshot_dir() -> Path:
    return find_art_generated_dir() / "formal_v2_prompt_readiness_snapshots"


def as_list(value: Any) -> list[Any]:
    return value if isinstance(value, list) else []


def manifest_by_visual_id(manifest: dict[str, Any]) -> dict[str, dict[str, Any]]:
    result: dict[str, dict[str, Any]] = {}
    for entry in as_list(manifest.get("Entries")):
        if isinstance(entry, dict) and entry.get("VisualID"):
            result[str(entry["VisualID"])] = entry
    return result


def collect_program_integrate(handoff: dict[str, Any]) -> list[dict[str, Any]]:
    values = []
    for item in as_list(handoff.get("ProgramIntegrateVisuals")):
        if isinstance(item, dict) and item.get("VisualID"):
            values.append(item)
    return values


def source_spec(entry: dict[str, Any]) -> dict[str, Any]:
    spec = entry.get("Spec")
    if not isinstance(spec, dict):
        return {}
    nested = spec.get("SourceSpec")
    return nested if isinstance(nested, dict) else spec


def prompt_issues(entry: dict[str, Any]) -> tuple[list[str], list[str]]:
    issues: list[str] = []
    warnings: list[str] = []
    prompt = str(entry.get("PromptEN", "") or "")
    negative = str(entry.get("NegativePromptEN", "") or "")
    spec = source_spec(entry)
    if not prompt.strip():
        issues.append("missing_prompt_en")
    if not negative.strip():
        issues.append("missing_negative_prompt_en")
    if any(fragment in prompt for fragment in GENERIC_FRAGMENTS):
        issues.append("generic_prompt_fragment")
    if any(term.lower() in prompt.lower() or term.lower() in negative.lower() for term in FORBIDDEN_PROMPT_TERMS):
        issues.append("forbidden_prompt_term")
    if re.search(r"[\u4e00-\u9fff]", prompt) or re.search(r"[\u4e00-\u9fff]", negative):
        issues.append("cjk_text_in_generation_prompt")
    if not all(spec.get(field) for field in ("Format", "Width", "Height")):
        issues.append("missing_source_spec")
    if len(prompt) < 120:
        warnings.append("prompt_may_be_too_short")
    return issues, warnings


def build_item(handoff_item: dict[str, Any], manifest_entry: dict[str, Any] | None) -> dict[str, Any]:
    visual_id = str(handoff_item.get("VisualID", "") or "")
    entry = manifest_entry or {}
    issues, warnings = prompt_issues(entry)
    prompt = str(entry.get("PromptEN", "") or "")
    return {
        "VisualID": visual_id,
        "Priority": str(handoff_item.get("Priority", entry.get("Priority", "")) or ""),
        "Domain": str(handoff_item.get("Domain", entry.get("Domain", "")) or ""),
        "AssetType": str(handoff_item.get("AssetType", entry.get("AssetType", "")) or ""),
        "ConfigID": str(entry.get("ConfigID", "") or ""),
        "Status": str(entry.get("Status", "") or ""),
        "QualityTier": str(entry.get("QualityTier", "") or ""),
        "PromptReady": bool(not issues),
        "Issues": issues,
        "Warnings": warnings,
        "PromptPreview": prompt[:220],
        "ApprovedPath": str(handoff_item.get("ApprovedPath", "") or ""),
    }


def command_for(items: list[dict[str, Any]], batch_id: str, variants: int) -> str:
    ids = ",".join(item["VisualID"] for item in items)
    return (
        "$runArtGeneration = Get-ChildItem -Path .\\tools -Recurse -Filter Run-ArtGeneration.ps1 "
        "| Select-Object -First 1 -ExpandProperty FullName\n"
        "$artToolDir = Split-Path -Parent $runArtGeneration\n"
        "$config = Join-Path $artToolDir 'ai_image_gateway.local.yaml'\n"
        "& $runArtGeneration "
        "-Config $config "
        "-Provider novelai -Status approved "
        f"-VisualID {ids} "
        f"-Variants {variants} -Concurrency 1 -DelaySeconds 1 "
        f"-BatchID {batch_id} -PreserveStatus"
    )


def make_markdown(payload: dict[str, Any]) -> str:
    summary = payload["Summary"]
    lines = [
        "# Formal V2 Prompt Readiness",
        "",
        f"GeneratedAt: `{payload['GeneratedAt']}`",
        "",
        "## Summary",
        "",
        f"- ProgramIntegrateVisuals: `{summary['ProgramIntegrateVisualCount']}`",
        f"- PromptReady: `{summary['PromptReadyCount']}`",
        f"- PromptBlocked: `{summary['PromptBlockedCount']}`",
        f"- GenericPromptRemaining: `{summary['GenericPromptRemainingCount']}`",
        f"- CjkPromptViolations: `{summary['CjkPromptViolationCount']}`",
        f"- DomainCounts: `{summary['DomainCounts']}`",
        "",
        "## Rerun Commands",
        "",
        "These commands are dry-run verified in the current toolchain shape only after `Run-ArtGeneration.ps1 -DryRun` is executed separately. Use them after setting `NAI_ACCESS_TOKEN`; keep `-Concurrency 1 -DelaySeconds 1`.",
        "",
    ]
    for domain, command in payload["RerunCommandsByDomain"].items():
        lines.extend([f"### {domain}", "", "```powershell", command, "```", ""])
    lines.extend([
        "## Items",
        "",
        "| Ready | Priority | VisualID | Domain | Type | ConfigID | Issues | Warnings | PromptPreview |",
        "|---|---|---|---|---|---|---|---|---|",
    ])
    for item in payload["Items"]:
        ready = "yes" if item["PromptReady"] else "no"
        issues = ", ".join(item["Issues"]) or "-"
        warnings = ", ".join(item["Warnings"]) or "-"
        preview = item["PromptPreview"].replace("|", "/")
        lines.append(
            f"| {ready} | {item['Priority']} | `{item['VisualID']}` | {item['Domain']} | {item['AssetType']} | "
            f"{item['ConfigID']} | {issues} | {warnings} | {preview} |"
        )
    lines.extend(["", "## Source Files", "", f"- Manifest: `{payload['SourceFiles']['Manifest']}`", f"- Program handoff: `{payload['SourceFiles']['ProgramHandoff']}`", ""])
    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate Formal V2 PromptEN readiness report.")
    parser.add_argument("--manifest-path", default="")
    parser.add_argument("--handoff-path", default="")
    parser.add_argument("--output-dir", default="")
    parser.add_argument("--snapshot-dir", default="")
    parser.add_argument("--snapshot-tag", default="formalv2_prompt_readiness")
    parser.add_argument("--batch-id", default="nai_formalv2_program_integrate_prompt_specific_20260609_01")
    parser.add_argument("--variants", type=int, default=1)
    args = parser.parse_args()

    manifest_path = Path(args.manifest_path) if args.manifest_path else default_manifest_path()
    handoff_path = Path(args.handoff_path) if args.handoff_path else default_handoff_path()
    output_dir = Path(args.output_dir) if args.output_dir else default_output_dir()
    snapshot_dir = Path(args.snapshot_dir) if args.snapshot_dir else default_snapshot_dir()

    manifest = read_json(manifest_path)
    handoff = read_json(handoff_path)
    by_id = manifest_by_visual_id(manifest)
    handoff_items = collect_program_integrate(handoff)
    items = [build_item(item, by_id.get(str(item.get("VisualID", "") or ""))) for item in handoff_items]
    items.sort(key=lambda x: (x["Priority"], x["Domain"], x["AssetType"], x["VisualID"]))

    domain_groups: dict[str, list[dict[str, Any]]] = defaultdict(list)
    for item in items:
        domain_groups[item["Domain"]].append(item)

    summary = {
        "ProgramIntegrateVisualCount": len(items),
        "PromptReadyCount": sum(1 for item in items if item["PromptReady"]),
        "PromptBlockedCount": sum(1 for item in items if not item["PromptReady"]),
        "GenericPromptRemainingCount": sum(1 for item in items if "generic_prompt_fragment" in item["Issues"]),
        "CjkPromptViolationCount": sum(1 for item in items if "cjk_text_in_generation_prompt" in item["Issues"]),
        "DomainCounts": dict(Counter(item["Domain"] for item in items)),
        "IssueCounts": dict(Counter(issue for item in items for issue in item["Issues"])),
        "WarningCounts": dict(Counter(warning for item in items for warning in item["Warnings"])),
    }

    payload = {
        "GeneratedAt": timestamp_text(),
        "Summary": summary,
        "RerunCommandsByDomain": {
            domain: command_for(group, f"{args.batch_id}_{domain}", args.variants)
            for domain, group in sorted(domain_groups.items())
            if group
        },
        "Items": items,
        "SourceFiles": {
            "Manifest": repo_path(manifest_path),
            "ProgramHandoff": repo_path(handoff_path),
        },
    }

    output_json = output_dir / "formal_v2_prompt_readiness.json"
    output_md = output_dir / "formal_v2_prompt_readiness.md"
    write_json(output_json, payload)
    output_md.parent.mkdir(parents=True, exist_ok=True)
    output_md.write_text(make_markdown(payload), encoding="utf-8")

    tag = safe_snapshot_tag(args.snapshot_tag)
    stamp = timestamp_filename()
    write_json(snapshot_dir / f"{stamp}_{tag}.json", payload)
    (snapshot_dir / f"{stamp}_{tag}.md").write_text(make_markdown(payload), encoding="utf-8")

    print(f"[OK] Formal V2 prompt readiness: {repo_path(output_md)}")
    print(
        "[OK] "
        f"program_integrate={summary['ProgramIntegrateVisualCount']}, "
        f"prompt_ready={summary['PromptReadyCount']}, "
        f"prompt_blocked={summary['PromptBlockedCount']}, "
        f"generic_remaining={summary['GenericPromptRemainingCount']}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
