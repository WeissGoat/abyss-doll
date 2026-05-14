# -*- coding: utf-8 -*-
"""Validate UI design specs and generate a program handoff summary."""

from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any


SCRIPT_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = SCRIPT_DIR.parents[1]

DEFAULT_TOKENS = "美术文档/ui_design/design_tokens.json"
DEFAULT_COMPONENTS = "美术文档/ui_design/component_catalog.json"
DEFAULT_SCREENS = "美术文档/ui_design/screen_layouts.json"
DEFAULT_SEED = "美术文档/art_requirements_seed.json"
DEFAULT_MANIFEST = "美术文档/_generated/art_manifest.json"
DEFAULT_OUTPUT = "美术文档/ui_design/_generated/ui_design_handoff.md"


def resolve_project_path(value: str) -> Path:
    path = Path(value)
    return path if path.is_absolute() else PROJECT_ROOT / path


def repo_path(path: Path) -> str:
    try:
        return path.resolve().relative_to(PROJECT_ROOT.resolve()).as_posix()
    except ValueError:
        return path.as_posix()


def read_json(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8"))


def collect_seed_visual_ids(seed: dict[str, Any]) -> set[str]:
    ids: set[str] = set()
    for entry in seed.get("Entries", []):
        if isinstance(entry, dict) and entry.get("VisualID"):
            ids.add(str(entry["VisualID"]))
    return ids


def collect_manifest_visual_ids(manifest: dict[str, Any]) -> set[str]:
    ids: set[str] = set()
    for entry in manifest.get("Entries", []):
        if isinstance(entry, dict) and entry.get("VisualID"):
            ids.add(str(entry["VisualID"]))
    return ids


def component_visual_ids(component: dict[str, Any]) -> set[str]:
    ids: set[str] = set()
    if component.get("VisualID"):
        ids.add(str(component["VisualID"]))
    state_ids = component.get("StateVisualIDs")
    if isinstance(state_ids, dict):
        for value in state_ids.values():
            if value:
                ids.add(str(value))
    return ids


def screen_visual_ids(screen: dict[str, Any]) -> set[str]:
    ids: set[str] = set()
    for value in screen.get("RequiredVisualIDs", []):
        if value:
            ids.add(str(value))
    background = str(screen.get("BackgroundVisualID", "") or "")
    if background:
        ids.add(background)
    return ids


def as_list(value: Any) -> list[Any]:
    return value if isinstance(value, list) else []


def non_empty_str(value: Any) -> str:
    return str(value).strip() if value is not None else ""


def validate_component_ref(
    errors: list[str],
    screen_id: str,
    context: str,
    component_ref: Any,
    component_ids: set[str],
    required_components: set[str],
) -> None:
    component_id = non_empty_str(component_ref)
    if not component_id:
        return
    if component_id not in component_ids:
        errors.append(f"screen {screen_id} {context} references unknown ComponentID: {component_id}")
        return
    if component_id not in required_components:
        errors.append(
            f"screen {screen_id} {context} references ComponentID not listed in RequiredComponents: {component_id}"
        )


def validate_visual_ref(
    errors: list[str],
    screen_id: str,
    context: str,
    visual_id: Any,
    known_visual_ids: set[str],
    required_visual_ids: set[str],
) -> None:
    visual = non_empty_str(visual_id)
    if not visual:
        return
    if visual not in known_visual_ids:
        errors.append(f"screen {screen_id} {context} references unknown VisualID: {visual}")
        return
    if visual not in required_visual_ids:
        errors.append(f"screen {screen_id} {context} references VisualID not listed in RequiredVisualIDs: {visual}")


def validate(
    tokens: dict[str, Any],
    components: dict[str, Any],
    screens: dict[str, Any],
    known_visual_ids: set[str],
) -> list[str]:
    errors: list[str] = []

    if "ReferenceResolution" not in tokens:
        errors.append("design_tokens.json missing ReferenceResolution")

    component_list = components.get("Components", [])
    if not isinstance(component_list, list):
        errors.append("component_catalog.json Components must be a list")
        component_list = []
    component_ids = set()
    component_required_screens: dict[str, list[str]] = {}
    for component in component_list:
        if not isinstance(component, dict):
            errors.append("component_catalog contains a non-object component")
            continue
        component_id = str(component.get("ComponentID", ""))
        if not component_id:
            errors.append("component missing ComponentID")
            continue
        if component_id in component_ids:
            errors.append(f"duplicate ComponentID: {component_id}")
        component_ids.add(component_id)
        required_screens = component.get("RequiredForScreens", [])
        if isinstance(required_screens, list):
            component_required_screens[component_id] = [str(item) for item in required_screens]
        else:
            errors.append(f"component {component_id} RequiredForScreens must be a list")
        for visual_id in component_visual_ids(component):
            if visual_id not in known_visual_ids:
                errors.append(f"component {component_id} references unknown VisualID: {visual_id}")

    screen_list = screens.get("Screens", [])
    if not isinstance(screen_list, list):
        errors.append("screen_layouts.json Screens must be a list")
        screen_list = []
    screen_ids = set()
    screen_required_components: dict[str, set[str]] = {}
    for screen in screen_list:
        if not isinstance(screen, dict):
            errors.append("screen_layouts contains a non-object screen")
            continue
        screen_id = str(screen.get("ScreenID", ""))
        if not screen_id:
            errors.append("screen missing ScreenID")
            continue
        if screen_id in screen_ids:
            errors.append(f"duplicate ScreenID: {screen_id}")
        screen_ids.add(screen_id)
        required_components = set(str(item) for item in screen.get("RequiredComponents", []))
        if screen_id:
            screen_required_components[screen_id] = required_components
        required_visual_ids = screen_visual_ids(screen)
        zone_ids: set[str] = set()
        for component_id in screen.get("RequiredComponents", []):
            if component_id not in component_ids:
                errors.append(f"screen {screen_id} requires unknown ComponentID: {component_id}")
        for zone in screen.get("Zones", []):
            if not isinstance(zone, dict):
                errors.append(f"screen {screen_id} has non-object zone")
                continue
            zone_id = str(zone.get("ZoneID", ""))
            rect = zone.get("Rect")
            if not zone_id:
                errors.append(f"screen {screen_id} zone missing ZoneID")
            elif zone_id in zone_ids:
                errors.append(f"screen {screen_id} duplicate ZoneID: {zone_id}")
            elif zone_id:
                zone_ids.add(zone_id)
            if not isinstance(rect, dict):
                errors.append(f"screen {screen_id} zone {zone_id or '<unknown>'} missing Rect")
            else:
                for field in ("X", "Y", "Width", "Height"):
                    if field not in rect:
                        errors.append(f"screen {screen_id} zone {zone_id} Rect missing {field}")
            for component_id in zone.get("ComponentRefs", []):
                validate_component_ref(
                    errors,
                    screen_id,
                    f"zone {zone_id}",
                    component_id,
                    component_ids,
                    required_components,
                )
        for visual_id in screen_visual_ids(screen):
            if visual_id not in known_visual_ids:
                errors.append(f"screen {screen_id} requires unknown VisualID: {visual_id}")

        for index, binding in enumerate(as_list(screen.get("ControllerBindings"))):
            if not isinstance(binding, dict):
                errors.append(f"screen {screen_id} ControllerBindings[{index}] must be an object")
                continue
            if not non_empty_str(binding.get("Script")):
                errors.append(f"screen {screen_id} ControllerBindings[{index}] missing Script")
            if not isinstance(binding.get("ExistingFields", []), list):
                errors.append(f"screen {screen_id} ControllerBindings[{index}] ExistingFields must be a list")
            if not isinstance(binding.get("BindingNotes", []), list):
                errors.append(f"screen {screen_id} ControllerBindings[{index}] BindingNotes must be a list")

        for index, item in enumerate(as_list(screen.get("UnityHierarchy"))):
            if not isinstance(item, dict):
                errors.append(f"screen {screen_id} UnityHierarchy[{index}] must be an object")
                continue
            context = f"UnityHierarchy[{index}]"
            if not non_empty_str(item.get("Path")):
                errors.append(f"screen {screen_id} {context} missing Path")
            validate_component_ref(
                errors,
                screen_id,
                context,
                item.get("ComponentRef"),
                component_ids,
                required_components,
            )
            validate_visual_ref(
                errors,
                screen_id,
                context,
                item.get("VisualID"),
                known_visual_ids,
                required_visual_ids,
            )
            if "RaycastTarget" in item and not isinstance(item.get("RaycastTarget"), bool):
                errors.append(f"screen {screen_id} {context} RaycastTarget must be a boolean")

        for index, item in enumerate(as_list(screen.get("SpriteAssignments"))):
            if not isinstance(item, dict):
                errors.append(f"screen {screen_id} SpriteAssignments[{index}] must be an object")
                continue
            context = f"SpriteAssignments[{index}]"
            if not non_empty_str(item.get("Target")):
                errors.append(f"screen {screen_id} {context} missing Target")
            validate_component_ref(
                errors,
                screen_id,
                context,
                item.get("ComponentRef"),
                component_ids,
                required_components,
            )
            validate_visual_ref(
                errors,
                screen_id,
                context,
                item.get("VisualID"),
                known_visual_ids,
                required_visual_ids,
            )
            if "RaycastTarget" in item and not isinstance(item.get("RaycastTarget"), bool):
                errors.append(f"screen {screen_id} {context} RaycastTarget must be a boolean")

        inventory_policy = screen.get("InventoryLayerPolicy")
        if isinstance(inventory_policy, dict):
            target_zone = non_empty_str(inventory_policy.get("TargetZone"))
            if target_zone and target_zone not in zone_ids:
                errors.append(f"screen {screen_id} InventoryLayerPolicy TargetZone is not a ZoneID: {target_zone}")
            token_cell_size = tokens.get("LayoutGrid", {}).get("InventoryCellSize")
            policy_cell_size = inventory_policy.get("GridCellSize")
            if token_cell_size is not None and policy_cell_size is not None and policy_cell_size != token_cell_size:
                errors.append(
                    f"screen {screen_id} InventoryLayerPolicy GridCellSize {policy_cell_size} "
                    f"does not match design token {token_cell_size}"
                )
            if not isinstance(inventory_policy.get("Rules", []), list):
                errors.append(f"screen {screen_id} InventoryLayerPolicy Rules must be a list")
        elif inventory_policy is not None:
            errors.append(f"screen {screen_id} InventoryLayerPolicy must be an object")

        if screen.get("Priority") == "P0":
            for key in ("ControllerBindings", "UnityHierarchy", "SpriteAssignments", "AcceptanceCriteria"):
                if not as_list(screen.get(key)):
                    errors.append(f"screen {screen_id} P0 screen missing {key}")
            uses_inventory = bool(required_components & {"Inventory.Slot", "Inventory.ChassisPanel"})
            if uses_inventory and not isinstance(inventory_policy, dict):
                errors.append(f"screen {screen_id} uses inventory components but missing InventoryLayerPolicy")

    for component_id, required_screens in component_required_screens.items():
        for screen_id in required_screens:
            if screen_id not in screen_ids:
                errors.append(f"component {component_id} lists unknown RequiredForScreens screen: {screen_id}")
                continue
            if component_id not in screen_required_components.get(screen_id, set()):
                errors.append(f"component {component_id} lists {screen_id}, but screen does not require it")

    for screen_id, required_components in screen_required_components.items():
        for component_id in required_components:
            required_screens = component_required_screens.get(component_id)
            if required_screens is None:
                continue
            if screen_id not in required_screens:
                errors.append(f"screen {screen_id} requires {component_id}, but component does not list it")

    return errors


def md_cell(value: Any) -> str:
    text = "" if value is None else str(value)
    return text.replace("|", "\\|").replace("\n", " ")


def append_bullets(lines: list[str], title: str, items: Any) -> None:
    item_list = as_list(items)
    if not item_list:
        return
    lines.append("")
    lines.append(f"{title}:")
    for item in item_list:
        lines.append(f"* {item}")


def append_table(lines: list[str], title: str, headers: list[str], rows: list[list[Any]]) -> None:
    if not rows:
        return
    lines.append("")
    lines.append(f"{title}:")
    lines.append("| " + " | ".join(headers) + " |")
    lines.append("|" + "|".join("---" for _ in headers) + "|")
    for row in rows:
        lines.append("| " + " | ".join(md_cell(value) for value in row) + " |")


def make_handoff_markdown(
    tokens: dict[str, Any],
    components: dict[str, Any],
    screens: dict[str, Any],
    known_visual_ids: set[str],
) -> str:
    component_list = components.get("Components", [])
    screen_list = screens.get("Screens", [])
    ref = tokens.get("ReferenceResolution", {})
    if isinstance(ref, dict):
        reference_text = f"{ref.get('Width', '?')}x{ref.get('Height', '?')}"
    else:
        reference_text = str(ref)

    lines = [
        "# UI Design Handoff",
        "",
        "> Generated by `tools/美术工具/Validate-UIDesign.ps1`.",
        "",
        "## Summary",
        "",
        f"* Reference resolution: `{reference_text}`",
        f"* Components: `{len(component_list)}`",
        f"* Screens: `{len(screen_list)}`",
        "",
        "## Screens",
        "",
        "| Priority | ScreenID | Name | Status | Required Components | Required Visuals |",
        "|---|---|---|---|---:|---:|",
    ]
    for screen in screen_list:
        lines.append(
            f"| `{screen.get('Priority', '')}` | `{screen.get('ScreenID', '')}` | "
            f"{screen.get('DisplayName', '')} | `{screen.get('LayoutStatus', '')}` | "
            f"{len(screen.get('RequiredComponents', []))} | {len(screen.get('RequiredVisualIDs', []))} |"
        )

    lines.extend(["", "## Components", "", "| Priority | ComponentID | VisualID | Resize | Screens |", "|---|---|---|---|---|"])
    for component in component_list:
        visual_ids = sorted(component_visual_ids(component))
        screens_text = ", ".join(f"`{item}`" for item in component.get("RequiredForScreens", []))
        visual_text = ", ".join(f"`{item}`" for item in visual_ids)
        lines.append(
            f"| `{component.get('Priority', '')}` | `{component.get('ComponentID', '')}` | "
            f"{visual_text} | `{component.get('ResizeMode', '')}` | {screens_text} |"
        )

    lines.extend(["", "## Program Handoff", ""])
    for screen in screen_list:
        lines.append(f"### {screen.get('DisplayName', screen.get('ScreenID', ''))}")
        primary_goal = screen.get("PrimaryGoal")
        if primary_goal:
            lines.append("")
            lines.append(f"* Goal: {primary_goal}")
        background = screen.get("BackgroundVisualID")
        if background:
            lines.append(f"* Background: `{background}`")

        zone_rows: list[list[Any]] = []
        for zone in as_list(screen.get("Zones")):
            if not isinstance(zone, dict):
                continue
            rect = zone.get("Rect", {})
            rect_text = ""
            if isinstance(rect, dict):
                rect_text = f"{rect.get('X', '?')},{rect.get('Y', '?')} {rect.get('Width', '?')}x{rect.get('Height', '?')}"
            zone_rows.append(
                [
                    f"`{zone.get('ZoneID', '')}`",
                    zone.get("Anchor", ""),
                    rect_text,
                    ", ".join(f"`{item}`" for item in as_list(zone.get("ComponentRefs"))),
                    zone.get("Purpose", ""),
                ]
            )
        append_table(lines, "Zones", ["ZoneID", "Anchor", "Rect", "Components", "Purpose"], zone_rows)

        handoff = screen.get("ProgramHandoff", {})
        if not isinstance(handoff, dict):
            handoff = {}
        for title, key in (
            ("Layout changes", "LayoutChanges"),
            ("Data bindings", "DataBindings"),
            ("Interaction notes", "InteractionNotes"),
        ):
            append_bullets(lines, title, handoff.get(key, []))

        binding_rows: list[list[Any]] = []
        for binding in as_list(screen.get("ControllerBindings")):
            if not isinstance(binding, dict):
                continue
            binding_rows.append(
                [
                    f"`{binding.get('Script', '')}`",
                    ", ".join(f"`{item}`" for item in as_list(binding.get("ExistingFields"))),
                    "<br>".join(str(item) for item in as_list(binding.get("BindingNotes"))),
                ]
            )
        append_table(lines, "Controller bindings", ["Script", "Existing fields", "Notes"], binding_rows)

        hierarchy_rows: list[list[Any]] = []
        for item in as_list(screen.get("UnityHierarchy")):
            if not isinstance(item, dict):
                continue
            hierarchy_rows.append(
                [
                    f"`{item.get('Path', '')}`",
                    item.get("Layer", ""),
                    f"`{item.get('ComponentRef', '')}`" if item.get("ComponentRef") else "",
                    f"`{item.get('VisualID', '')}`" if item.get("VisualID") else "",
                    item.get("ImageType", ""),
                    item.get("FitMode", ""),
                    item.get("RaycastTarget", ""),
                ]
            )
        append_table(
            lines,
            "Unity hierarchy",
            ["Path", "Layer", "Component", "VisualID", "Image", "Fit", "Raycast"],
            hierarchy_rows,
        )

        sprite_rows: list[list[Any]] = []
        for item in as_list(screen.get("SpriteAssignments")):
            if not isinstance(item, dict):
                continue
            sprite_rows.append(
                [
                    f"`{item.get('Target', '')}`",
                    f"`{item.get('ComponentRef', '')}`" if item.get("ComponentRef") else "",
                    f"`{item.get('VisualID', '')}`" if item.get("VisualID") else "",
                    item.get("ImageType", ""),
                    item.get("FitMode", ""),
                    item.get("RaycastTarget", ""),
                ]
            )
        append_table(
            lines,
            "Sprite assignments",
            ["Target", "Component", "VisualID", "Image", "Fit", "Raycast"],
            sprite_rows,
        )

        inventory_policy = screen.get("InventoryLayerPolicy")
        if isinstance(inventory_policy, dict):
            lines.append("")
            lines.append("Inventory layer policy:")
            lines.append(f"* Uses global inventory: `{inventory_policy.get('UsesGlobalInventory', '')}`")
            lines.append(f"* Target zone: `{inventory_policy.get('TargetZone', '')}`")
            lines.append(f"* Cell size: `{inventory_policy.get('GridCellSize', '')}`")
            lines.append(f"* Spacing: `{inventory_policy.get('GridSpacing', '')}`")
            formula = inventory_policy.get("GridSizeFormula")
            if formula:
                lines.append(f"* Grid size formula: `{formula}`")
            for item in as_list(inventory_policy.get("Rules")):
                lines.append(f"* {item}")

        append_bullets(lines, "Acceptance criteria", screen.get("AcceptanceCriteria", []))
        lines.append("")

    missing_known = sorted(known_visual_ids)
    lines.extend(["## Known VisualID Sources", "", f"* Known VisualID count: `{len(missing_known)}`", ""])
    return "\n".join(lines)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Validate Project P3 UI design specs.")
    parser.add_argument("--tokens-path", default=DEFAULT_TOKENS)
    parser.add_argument("--components-path", default=DEFAULT_COMPONENTS)
    parser.add_argument("--screens-path", default=DEFAULT_SCREENS)
    parser.add_argument("--seed-path", default=DEFAULT_SEED)
    parser.add_argument("--manifest-path", default=DEFAULT_MANIFEST)
    parser.add_argument("--output-path", default=DEFAULT_OUTPUT)
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    tokens_path = resolve_project_path(args.tokens_path)
    components_path = resolve_project_path(args.components_path)
    screens_path = resolve_project_path(args.screens_path)
    seed_path = resolve_project_path(args.seed_path)
    manifest_path = resolve_project_path(args.manifest_path)
    output_path = resolve_project_path(args.output_path)

    tokens = read_json(tokens_path)
    components = read_json(components_path)
    screens = read_json(screens_path)
    seed = read_json(seed_path) if seed_path.exists() else {}
    manifest = read_json(manifest_path) if manifest_path.exists() else {}
    known_visual_ids = collect_seed_visual_ids(seed) | collect_manifest_visual_ids(manifest)

    errors = validate(tokens, components, screens, known_visual_ids)
    if errors:
        print("[ERROR] UI design validation failed:")
        for error in errors:
            print(f"  - {error}")
        return 1

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(
        make_handoff_markdown(tokens, components, screens, known_visual_ids),
        encoding="utf-8",
    )
    print(f"[OK] UI design specs valid.")
    print(f"[OK] Handoff: {repo_path(output_path)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
