# -*- coding: utf-8 -*-
"""Update Project P3 visual asset manifest from gameplay config JSON files."""

from __future__ import annotations

import argparse
import copy
import json
from pathlib import Path
from typing import Any, Dict, List

from art_catalog_integrity import (
    canonicalize_manifest_entries,
    primary_source_rank,
    validate_manifest_uniqueness,
)
from art_workspace import STANDARD_PROFILE, normalize_entry_workspace_paths


STATUS_FLOW = [
    "todo",
    "prompted",
    "generated",
    "selected",
    "approved",
    "registered",
    "validated",
    "rejected",
    "deprecated",
]

PRESERVE_FIELDS = [
    "Status",
    "PromptCN",
    "PromptEN",
    "NegativePromptEN",
    "Spec",
    "BatchID",
    "RawPath",
    "SelectedPath",
    "ApprovedPath",
    "RegistryStatus",
    "Notes",
    "QualityTier",
    "QualityUpdatedAt",
    "ReplacementBatchID",
    "CandidateBatchID",
    "CandidateRawFiles",
    "CandidateRawPath",
    "ProductionProfile",
    "AssetSetID",
    "AssetID",
    "SetRole",
    "SourceAssets",
    "PoseSpec",
    "StyleRef",
    "VisualIntent",
    "CompiledRequest",
    "RequirementSources",
    "VisualReusePolicy",
]


def read_json(path: Path) -> Dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8"))


def validate_seed_asset_sets(asset_sets: Any) -> dict[str, dict[str, Any]]:
    if not isinstance(asset_sets, dict):
        raise ValueError("Preset seed AssetSets must be an object")
    result: dict[str, dict[str, Any]] = {}
    for asset_set_id, value in asset_sets.items():
        if not isinstance(value, dict):
            raise ValueError(f"asset_set_invalid:{asset_set_id}")
        contract = value.get("IdentityContract")
        if not isinstance(contract, dict) or int(contract.get("Version", 0) or 0) < 1:
            raise ValueError(f"asset_set_identity_contract_invalid:{asset_set_id}")
        if not all(isinstance(contract.get(key), list) for key in ("Required", "Forbidden", "Conditional")):
            raise ValueError(f"asset_set_identity_contract_invalid:{asset_set_id}")
        result[str(asset_set_id)] = copy.deepcopy(value)
    return result


def merge_seed_asset_sets(
    seed_sets: dict[str, dict[str, Any]],
    existing_sets: dict[str, Any],
) -> dict[str, dict[str, Any]]:
    result = copy.deepcopy(seed_sets)
    for asset_set_id, asset_set in result.items():
        existing = existing_sets.get(asset_set_id, {}) if isinstance(existing_sets, dict) else {}
        review = existing.get("LatestConsistencyReview") if isinstance(existing, dict) else None
        if isinstance(review, dict):
            asset_set["LatestConsistencyReview"] = copy.deepcopy(review)
    return result


def repo_path(path: Path, root: Path) -> str:
    try:
        return path.resolve().relative_to(root.resolve()).as_posix()
    except ValueError:
        return path.as_posix()


def join_repo_path(*parts: str) -> str:
    return "/".join(part.strip("/\\") for part in parts if part)


def prop(data: Dict[str, Any] | None, name: str, default: Any = "") -> Any:
    if not isinstance(data, dict):
        return default
    value = data.get(name, default)
    return default if value is None else value


def tag_text(tags: Any) -> str:
    if not isinstance(tags, list):
        return ""
    return ", ".join(str(tag) for tag in tags)


def is_test_config(path: Path) -> bool:
    return path.stem.endswith("_test")


def new_entry(
    *,
    domain: str,
    source_type: str,
    derive_rule: str,
    config_source: str,
    config_id: str,
    display_name: str,
    asset_type: str,
    visual_id: str,
    output_path: str,
    priority: str,
    source_facts_cn: str,
    extra_fields: Dict[str, Any] | None = None,
) -> Dict[str, Any]:
    entry = {
        "Domain": domain,
        "SourceType": source_type,
        "DeriveRule": derive_rule,
        "ConfigSource": config_source,
        "ConfigID": config_id,
        "DisplayName": display_name,
        "AssetType": asset_type,
        "VisualID": visual_id,
        "ProductionProfile": STANDARD_PROFILE,
        "OutputPath": output_path,
        "Priority": priority,
        "Status": "todo",
        "SourceFactsCN": source_facts_cn,
        "PromptCN": "",
        "PromptEN": "",
        "NegativePromptEN": "",
        "Spec": {},
        "StyleRef": {},
        "VisualIntent": {},
        "CompiledRequest": {},
        "BatchID": "",
        "RawPath": "",
        "SelectedPath": "",
        "ApprovedPath": "",
        "RegistryStatus": "unregistered",
        "Notes": "",
    }
    if extra_fields:
        for key, value in extra_fields.items():
            if value not in (None, ""):
                entry[key] = value
    return entry


def preserve_entry_fields(
    entry: Dict[str, Any],
    existing: Dict[str, Any] | None,
    source_owned_fields: set[str] | None = None,
) -> Dict[str, Any]:
    if not existing:
        return normalize_entry_workspace_paths(entry)

    source_owned = source_owned_fields or set()
    existing_status = existing.get("Status", "")
    for field in PRESERVE_FIELDS:
        if field in source_owned:
            continue
        if existing_status == "todo" and field in ("PromptCN", "PromptEN", "NegativePromptEN", "Spec"):
            continue
        value = existing.get(field)
        if value not in (None, ""):
            entry[field] = value

    if entry.get("Status") == "deprecated":
        entry["Status"] = "todo"

    return normalize_entry_workspace_paths(entry)


def add_entry(
    entries: List[Dict[str, Any]],
    existing_map: Dict[str, Dict[str, Any]],
    entry: Dict[str, Any],
    source_owned_fields: set[str] | None = None,
) -> None:
    entries.append(
        preserve_entry_fields(
            entry,
            existing_map.get(entry["VisualID"]),
            source_owned_fields,
        )
    )


def scan_items(config_root: Path, project_root: Path, existing_map: Dict[str, Dict[str, Any]], entries: List[Dict[str, Any]]) -> None:
    item_dir = config_root / "Items"
    if not item_dir.exists():
        return

    for path in sorted(item_dir.glob("*.json")):
        if is_test_config(path):
            continue
        data = read_json(path)
        visual_id = data.get("IconID") or f"item_{data['ConfigID']}_icon"
        grid_cost = prop(prop(data, "Grid", {}), "GridCost", "")
        source_facts = (
            f"配置表物品：{data.get('Name', data['ConfigID'])}。类型 {data.get('ItemType', '')}，"
            f"稀有度 {data.get('Rarity', '')}，占格 {grid_cost}，标签 {tag_text(data.get('Tags'))}。"
        )
        source_binding = {
            "SourceType": "config",
            "ConfigID": data["ConfigID"],
            "ConfigSource": repo_path(path, project_root),
            "DisplayName": data.get("Name", data["ConfigID"]),
            "SourceFactsCN": source_facts,
            "VisualIDField": "IconID" if data.get("IconID") else "derived",
            "ExplicitVisualID": bool(data.get("IconID")),
        }
        add_entry(
            entries,
            existing_map,
            new_entry(
                domain="item",
                source_type="config",
                derive_rule="Items/*.json -> item icon",
                config_source=repo_path(path, project_root),
                config_id=data["ConfigID"],
                display_name=data.get("Name", data["ConfigID"]),
                asset_type="icon",
                visual_id=visual_id,
                output_path=join_repo_path("UnityClient/Assets/Art/Approved/Items/Icons", f"{visual_id}.png"),
                priority="P0",
                source_facts_cn=source_facts,
                extra_fields={"RequirementSources": [source_binding]},
            ),
            {"RequirementSources"},
        )


def scan_monsters(config_root: Path, project_root: Path, existing_map: Dict[str, Dict[str, Any]], entries: List[Dict[str, Any]]) -> None:
    monster_dir = config_root / "Monsters"
    if not monster_dir.exists():
        return

    for path in sorted(monster_dir.glob("*.json")):
        if is_test_config(path):
            continue
        data = read_json(path)
        visual_id = data.get("PortraitID") or f"monster_{data['MonsterID']}_portrait"
        interference = data.get("GridInterference") or "None"
        source_facts = (
            f"配置表怪物：{data.get('Name', data['MonsterID'])}。层级 {data.get('Layer', '')}，"
            f"生命 {data.get('HP', '')}，背包干涉 {interference}。"
        )
        add_entry(
            entries,
            existing_map,
            new_entry(
                domain="monster",
                source_type="config",
                derive_rule="Monsters/*.json -> monster portrait",
                config_source=repo_path(path, project_root),
                config_id=data["MonsterID"],
                display_name=data.get("Name", data["MonsterID"]),
                asset_type="portrait",
                visual_id=visual_id,
                output_path=join_repo_path("UnityClient/Assets/Art/Approved/Monsters/Portraits", f"{visual_id}.png"),
                priority="P0",
                source_facts_cn=source_facts,
            ),
        )

        combat_visual_id = str(data.get("CombatVisualID") or "").strip()
        if combat_visual_id:
            combat_facts = (
                f"配置表怪物战斗实体：{data.get('Name', data['MonsterID'])}。层级 {data.get('Layer', '')}，"
                f"生命 {data.get('HP', '')}，用于 Formal V1 战斗舞台站位。"
            )
            add_entry(
                entries,
                existing_map,
                new_entry(
                    domain="monster",
                    source_type="config",
                    derive_rule="Monsters/*.json CombatVisualID -> monster combat sprite",
                    config_source=repo_path(path, project_root),
                    config_id=data["MonsterID"],
                    display_name=f"{data.get('Name', data['MonsterID'])}战斗实体",
                    asset_type="combat_sprite",
                    visual_id=combat_visual_id,
                    output_path=join_repo_path("UnityClient/Assets/Art/Approved/Monsters/Combat", f"{combat_visual_id}.png"),
                    priority="P0",
                    source_facts_cn=combat_facts,
                ),
            )


def scan_dungeons(config_root: Path, project_root: Path, existing_map: Dict[str, Dict[str, Any]], entries: List[Dict[str, Any]]) -> None:
    dungeon_dir = config_root / "Dungeons"
    if not dungeon_dir.exists():
        return

    node_types: set[str] = set()

    for path in sorted(dungeon_dir.glob("*.json")):
        if is_test_config(path):
            continue
        data = read_json(path)
        for node in data.get("NodePool", []):
            node_type = node.get("NodeType")
            if node_type:
                node_types.add(node_type)
        if data.get("BossNode"):
            node_types.add("BossNode")
        end_node = data.get("EndNode")
        if isinstance(end_node, dict) and end_node.get("NodeType"):
            node_types.add(str(end_node["NodeType"]))

        layer_id = data.get("LayerID")
        visual_id = f"bg_dungeon_layer_{layer_id}"
        source_facts = (
            f"配置表深渊层：{data.get('Name', f'Layer {layer_id}')}。层级 {layer_id}，"
            f"每节点 SAN 消耗 {data.get('SANCostPerNode', '')}，期望节点数 {data.get('ExpectedNodeCount', '')}。"
        )
        add_entry(
            entries,
            existing_map,
            new_entry(
                domain="background",
                source_type="derived",
                derive_rule="Dungeons/*.json LayerID/Name -> dungeon layer background",
                config_source=repo_path(path, project_root),
                config_id=f"layer_{layer_id}",
                display_name=data.get("Name", f"Layer {layer_id}"),
                asset_type="background",
                visual_id=visual_id,
                output_path=join_repo_path("UnityClient/Assets/Art/Approved/Backgrounds/Dungeon", f"{visual_id}.png"),
                priority="P1",
                source_facts_cn=source_facts,
            ),
        )

    node_map = {
        "CombatNode": {
            "VisualID": "node_combat_icon",
            "DisplayName": "战斗节点",
            "Facts": "由 Dungeon NodePool 中的 CombatNode 推导，需要一个普通战斗节点资产。",
        },
        "SafeRoomNode": {
            "VisualID": "node_safe_room_icon",
            "DisplayName": "安全区节点",
            "Facts": "由 Dungeon NodePool 中的 SafeRoomNode 推导，需要一个安全区节点资产。",
        },
        "BossNode": {
            "VisualID": "node_boss_icon",
            "DisplayName": "首领节点",
            "Facts": "由 Dungeon BossNode 字段推导，需要一个首领节点资产。",
        },
        "StairsNode": {
            "VisualID": "node_stairs_icon",
            "DisplayName": "阶梯节点",
            "Facts": "由 Dungeon EndNode 字段推导，需要一个通往下一层的阶梯节点资产。",
        },
    }

    for node_type in sorted(node_types):
        spec = node_map.get(
            node_type,
            {
                "VisualID": f"node_{node_type.lower()}_icon",
                "DisplayName": node_type,
                "Facts": f"由 Dungeon 节点类型 {node_type} 推导，需要一个地图节点资产。",
            },
        )
        add_entry(
            entries,
            existing_map,
            new_entry(
                domain="node",
                source_type="derived",
                derive_rule="Dungeons/*.json NodePool/BossNode -> map node icon",
                config_source=repo_path(dungeon_dir, project_root),
                config_id=node_type,
                display_name=spec["DisplayName"],
                asset_type="icon",
                visual_id=spec["VisualID"],
                output_path=join_repo_path("UnityClient/Assets/Art/Approved/Nodes/Icons", f"{spec['VisualID']}.png"),
                priority="P0",
                source_facts_cn=spec["Facts"],
            ),
        )


def scan_prosthetics(config_root: Path, project_root: Path, existing_map: Dict[str, Dict[str, Any]], entries: List[Dict[str, Any]]) -> None:
    prosthetic_dir = config_root / "Prosthetics"
    if not prosthetic_dir.exists():
        return

    for path in sorted(prosthetic_dir.glob("*.json")):
        if is_test_config(path):
            continue
        data = read_json(path)
        visual_id = f"prosthetic_{data['ProstheticID']}_icon"
        effect_type = prop(prop(data, "PassiveEffect", {}), "EffectType", "")
        source_facts = (
            f"配置表义体：{data.get('Name', data['ProstheticID'])}。"
            f"槽位 {data.get('SlotType', '')}，被动效果 {effect_type}。"
        )
        add_entry(
            entries,
            existing_map,
            new_entry(
                domain="prosthetic",
                source_type="config",
                derive_rule="Prosthetics/*.json -> prosthetic icon",
                config_source=repo_path(path, project_root),
                config_id=data["ProstheticID"],
                display_name=data.get("Name", data["ProstheticID"]),
                asset_type="icon",
                visual_id=visual_id,
                output_path=join_repo_path("UnityClient/Assets/Art/Approved/Prosthetics/Icons", f"{visual_id}.png"),
                priority="P1",
                source_facts_cn=source_facts,
            ),
        )


def scan_chassis(config_root: Path, project_root: Path, existing_map: Dict[str, Dict[str, Any]], entries: List[Dict[str, Any]]) -> None:
    chassis_dir = config_root / "Chassis"
    if not chassis_dir.exists():
        return

    for path in sorted(chassis_dir.glob("*.json")):
        if is_test_config(path):
            continue
        data = read_json(path)
        visual_id = f"chassis_{data['ChassisID']}_frame"
        source_facts = (
            f"配置表底盘：{data['ChassisID']}。等级 {data.get('Level', '')}，"
            f"网格 {data.get('GridWidth', '')}x{data.get('GridHeight', '')}。"
        )
        add_entry(
            entries,
            existing_map,
            new_entry(
                domain="chassis",
                source_type="config",
                derive_rule="Chassis/*.json -> chassis frame",
                config_source=repo_path(path, project_root),
                config_id=data["ChassisID"],
                display_name=data["ChassisID"],
                asset_type="frame",
                visual_id=visual_id,
                output_path=join_repo_path("UnityClient/Assets/Art/Approved/Chassis", f"{visual_id}.png"),
                priority="P1",
                source_facts_cn=source_facts,
            ),
        )


def scan_dolls(config_root: Path, project_root: Path, existing_map: Dict[str, Dict[str, Any]], entries: List[Dict[str, Any]]) -> None:
    doll_dir = config_root / "Dolls"
    if not doll_dir.exists():
        return

    for path in sorted(doll_dir.glob("*.json")):
        if is_test_config(path):
            continue
        data = read_json(path)
        visual_id = f"{data['DollID']}_stand"
        source_facts = (
            f"配置表魔偶：{data.get('Name', data['DollID'])}。"
            f"初始底盘 {data.get('DefaultChassisID', '')}，初始物品数量 {len(data.get('InitialItems', []))}。"
        )
        add_entry(
            entries,
            existing_map,
            new_entry(
                domain="doll",
                source_type="config",
                derive_rule="Dolls/*.json -> doll standing visual",
                config_source=repo_path(path, project_root),
                config_id=data["DollID"],
                display_name=data.get("Name", data["DollID"]),
                asset_type="stand",
                visual_id=visual_id,
                output_path=join_repo_path("UnityClient/Assets/Art/Approved/Dolls", f"{visual_id}.png"),
                priority="P1",
                source_facts_cn=source_facts,
            ),
        )


def add_preset_assets(
    project_root: Path,
    preset_entries: Any,
    existing_map: Dict[str, Dict[str, Any]],
    entries: List[Dict[str, Any]],
) -> None:
    if isinstance(preset_entries, Path):
        if not preset_entries.exists():
            return
        seed_entries = read_json(preset_entries).get("Entries", [])
        preset_source = preset_entries
    else:
        seed_entries = preset_entries
        preset_source = project_root / "美术文档/art_requirements_seed.json"
    if not isinstance(seed_entries, list):
        raise ValueError("Preset seed Entries must be a list")

    for index, data in enumerate(seed_entries, start=1):
        if not isinstance(data, dict):
            raise ValueError(f"Preset seed entry #{index} must be an object.")
        visual_id = str(data.get("VisualID", "")).strip()
        if not visual_id:
            raise ValueError(f"Preset seed entry #{index} missing VisualID.")

        extra_fields = {
            "PresetCategory": data.get("PresetCategory"),
            "Screen": data.get("Screen"),
            "Usage": data.get("Usage"),
            "ProgramReference": data.get("ProgramReference"),
            "ProductionProfile": data.get("ProductionProfile"),
            "AssetSetID": data.get("AssetSetID"),
            "AssetID": data.get("AssetID"),
            "SetRole": data.get("SetRole"),
            "SourceAssets": data.get("SourceAssets"),
            "PoseSpec": data.get("PoseSpec"),
            "PresentationGroup": data.get("PresentationGroup"),
        }
        source_owned_fields = {key for key in extra_fields if key in data}
        add_entry(
            entries,
            existing_map,
            new_entry(
                domain=str(data.get("Domain", "ui")),
                source_type="preset",
                derive_rule=str(data.get("DeriveRule", "art requirements seed preset")),
                config_source=str(data.get("ConfigSource", repo_path(preset_source, project_root))),
                config_id=str(data.get("ConfigID", visual_id)),
                display_name=str(data.get("DisplayName", visual_id)),
                asset_type=str(data.get("AssetType", "icon")),
                visual_id=visual_id,
                output_path=str(data.get("OutputPath", join_repo_path("UnityClient/Assets/Art/Approved/UI", f"{visual_id}.png"))),
                priority=str(data.get("Priority", "P2")),
                source_facts_cn=str(data.get("SourceFactsCN", f"预置资产：{visual_id}。")),
                extra_fields=extra_fields,
            ),
            source_owned_fields,
        )


def make_markdown(manifest: Dict[str, Any]) -> str:
    entries = manifest["Entries"]
    status_counts: Dict[str, int] = {}
    for entry in entries:
        status_counts[entry["Status"]] = status_counts.get(entry["Status"], 0) + 1

    lines = [
        "# 视觉资产 Manifest",
        "",
        "> **定位：** 由 `tools/美术工具/Update-ArtManifest.ps1` 根据最新配置表、配置推导项和预置美术需求增量生成。第一步只填资产来源与配置事实，中文审阅描述、英文提示词、英文负面词和结构化规格在第二步补全。",
        f"> **配置来源：** `{manifest['ConfigRoot']}`",
        "",
        "## 状态流转",
        "",
        "`todo -> prompted -> generated -> selected -> approved -> registered -> validated`，废弃项标记为 `rejected` 或 `deprecated`。",
        "",
        "## 汇总",
        "",
        "| Domain | Count |",
        "|---|---:|",
    ]

    counts: Dict[str, int] = {}
    for entry in entries:
        counts[entry["Domain"]] = counts.get(entry["Domain"], 0) + 1
    for domain in sorted(counts):
        lines.append(f"| `{domain}` | {counts[domain]} |")

    lines.extend(
        [
            "",
            "## 资产列表",
            "",
            "| Domain | ConfigID | 名称 | 类型 | VisualID | 优先级 | 状态 |",
            "|---|---|---|---|---|---|---|",
        ]
    )
    for entry in entries:
        lines.append(
            f"| `{entry['Domain']}` | `{entry['ConfigID']}` | {entry['DisplayName']} | "
            f"`{entry['AssetType']}` | `{entry['VisualID']}` | {entry['Priority']} | `{entry['Status']}` |"
        )

    lines.extend(["", "## 下一步", ""])
    if status_counts.get("todo", 0) > 0:
        lines.append("1. 对 `Status=todo` 的新增项补全 `PromptCN`、`PromptEN`、`NegativePromptEN` 和结构化 `Spec`。")
        lines.append("2. 完成后运行 `tools/美术工具/Generate-ArtPrompts.ps1` 或人工审阅提示词。")
    elif status_counts.get("prompted", 0) > 0:
        lines.append("1. 对 `Status=prompted` 的条目按批次生成图片。")
        lines.append("2. 生成后填写 `BatchID` 和 `RawPath`，并将状态改为 `generated`。")
    elif status_counts.get("generated", 0) > 0:
        lines.append("1. 对 `Status=generated` 的批次进行预处理和筛选。")
        lines.append("2. 更新 `SelectedPath`、`ApprovedPath` 与状态。")
    elif status_counts.get("approved", 0) > 0:
        lines.append("1. 将 `Approved` 素材登记到 `VisualAssetRegistry`。")
        lines.append("2. 游戏内验证后更新 `RegistryStatus` 和 `Status=validated`。")
    else:
        lines.append("当前没有待处理资产。")
    lines.append("")
    return "\n".join(lines)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Update visual asset manifest from P3 config JSON files.")
    parser.add_argument("--config-root", default="UnityClient/Assets/StreamingAssets/Configs")
    parser.add_argument("--manifest-path", default="美术文档/_generated/art_manifest.json")
    parser.add_argument("--markdown-path", default="美术文档/_generated/视觉资产Manifest.md")
    parser.add_argument("--preset-path", default="美术文档/art_requirements_seed.json")
    parser.add_argument("--no-system-assets", action="store_true")
    parser.add_argument("--dry-run", action="store_true")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    project_root = Path.cwd()
    config_root = (project_root / args.config_root).resolve()
    manifest_path = (project_root / args.manifest_path).resolve()
    markdown_path = (project_root / args.markdown_path).resolve()
    preset_path = (project_root / args.preset_path).resolve()

    if not config_root.exists():
        raise FileNotFoundError(f"Config root not found: {config_root}")

    existing_manifest: Dict[str, Any] = {}
    if manifest_path.exists():
        existing_manifest = read_json(manifest_path)
    seed = read_json(preset_path)
    seed_asset_sets = validate_seed_asset_sets(seed.get("AssetSets", {}))
    existing_entries = existing_manifest.get("Entries", [])
    existing_map: Dict[str, Dict[str, Any]] = {}
    for entry in existing_entries:
        if not isinstance(entry, dict) or not entry.get("VisualID"):
            continue
        visual_id = str(entry["VisualID"])
        current = existing_map.get(visual_id)
        if current is None or primary_source_rank(entry) < primary_source_rank(current):
            existing_map[visual_id] = entry

    entries: List[Dict[str, Any]] = []
    scan_items(config_root, project_root, existing_map, entries)
    scan_monsters(config_root, project_root, existing_map, entries)
    scan_dungeons(config_root, project_root, existing_map, entries)
    scan_prosthetics(config_root, project_root, existing_map, entries)
    scan_chassis(config_root, project_root, existing_map, entries)
    scan_dolls(config_root, project_root, existing_map, entries)
    if not args.no_system_assets:
        add_preset_assets(project_root, seed.get("Entries", []), existing_map, entries)

    entries = canonicalize_manifest_entries(entries)
    validate_manifest_uniqueness(entries)

    current_ids = {entry["VisualID"] for entry in entries}
    for old_entry in existing_entries:
        if not isinstance(old_entry, dict):
            continue
        visual_id = old_entry.get("VisualID")
        if visual_id and visual_id not in current_ids:
            stale = dict(old_entry)
            stale["Status"] = "deprecated"
            entries.append(normalize_entry_workspace_paths(stale))

    entries.sort(key=lambda item: (item["Domain"], item["Priority"], item["ConfigID"], item["AssetType"], item["VisualID"]))
    manifest = {
        "Version": max(3, int(existing_manifest.get("Version", 1) or 1)),
        "ConfigRoot": repo_path(config_root, project_root),
        "StatusFlow": STATUS_FLOW,
        "Entries": entries,
    }
    if existing_manifest.get("ArtStyle"):
        manifest["ArtStyle"] = existing_manifest["ArtStyle"]
    if existing_manifest.get("ArtStyleCatalog"):
        manifest["ArtStyleCatalog"] = existing_manifest["ArtStyleCatalog"]
    manifest["AssetSets"] = merge_seed_asset_sets(seed_asset_sets, existing_manifest.get("AssetSets", {}))

    if args.dry_run:
        print(f"Manifest dry-run: {repo_path(manifest_path, project_root)}")
        print(f"Entries: {len(entries)}")
        print("No files changed.")
        return 0

    manifest_path.parent.mkdir(parents=True, exist_ok=True)
    manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    markdown_path.parent.mkdir(parents=True, exist_ok=True)
    markdown_path.write_text(make_markdown(manifest), encoding="utf-8")

    print(f"Manifest updated: {repo_path(manifest_path, project_root)}")
    print(f"Markdown updated: {repo_path(markdown_path, project_root)}")
    print(f"Entries: {len(entries)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
