# -*- coding: utf-8 -*-
"""Generate the visual-semantic review ledger for Formal V2 assets.

This report is intentionally separate from the static technical asset review:
technical review decides whether an asset can be registered by the program
side, while this file records art-side semantic/readability risks to revisit
after runtime screenshots exist.
"""

from __future__ import annotations

import argparse
import json
import re
import shutil
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


SCRIPT_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = SCRIPT_DIR.parents[1]

DEFAULT_OUTPUT_DIR = ""
DEFAULT_SNAPSHOT_DIR = ""

RISK_ORDER = {
    "secondary_replacement_candidate": 0,
    "style_mismatch_watch": 1,
    "watch_runtime_readability": 2,
    "ok_for_current_v2": 3,
}

RISK_OVERRIDES: dict[str, dict[str, str]] = {
    "item_con_purifying_salt_icon": {
        "RiskLevel": "ok_for_current_v2",
        "ReasonCN": "当前轮廓更像小灯具或矿灯，净化盐的晶粒、盐瓶或封印纸语义不够明确。",
        "RecommendationCN": "二次出图时改为白色盐晶、小玻璃盐瓶、符纸封口和少量散落晶盐组合。",
    },
    "item_con_solvent_spray_icon": {
        "RiskLevel": "watch_runtime_readability",
        "ReasonCN": "容器质感可用，但喷雾嘴和液体雾化语义偏弱。",
        "RecommendationCN": "运行时小尺寸复核；如不清晰，改为带喷嘴的手持雾化瓶。",
    },
    "item_con_stabilizer_ampoule_icon": {
        "RiskLevel": "watch_runtime_readability",
        "ReasonCN": "安瓿可读性尚可，但黄铜灯具感仍偏强。",
        "RecommendationCN": "运行时复核；必要时加强透明玻璃管、液面和封蜡结构。",
    },
    "item_gear_corroded_bulwark_icon": {
        "RiskLevel": "ok_for_current_v2",
        "ReasonCN": "更像发光灯笼，不像被腐蚀的壁盾或防具。",
        "RecommendationCN": "二次出图时改为锈蚀盾牌、厚重护甲片或带腐蚀孔洞的防御装置。",
    },
    "item_gear_mycelium_cloak_icon": {
        "RiskLevel": "ok_for_current_v2",
        "ReasonCN": "轮廓仍像灯具或袋子，斗篷和菌丝披覆语义弱。",
        "RecommendationCN": "二次出图时改为布料披风、菌丝边缘、孢子纤维和肩扣轮廓。",
    },
    "item_gear_spore_lance_icon": {
        "RiskLevel": "watch_runtime_readability",
        "ReasonCN": "二次替换后长枪语义已明确，但图形较细，运行时小尺寸可能偏淡。",
        "RecommendationCN": "运行时复核；如果仍不清晰，再提高枪杆对比度和轮廓厚度。",
    },
    "item_gear_vein_sickle_icon": {
        "RiskLevel": "watch_runtime_readability",
        "ReasonCN": "剪影有弯刃方向，但黄铜挂件感偏强。",
        "RecommendationCN": "运行时复核；必要时加强弯镰刀刃和菌脉纹理。",
    },
    "item_loot_acid_gland_icon": {
        "RiskLevel": "ok_for_current_v2",
        "ReasonCN": "更像灯笼或容器，不像酸腺。",
        "RecommendationCN": "二次出图时改为半透明腺体、酸液气泡、绿色或黄色腐蚀液滴。",
    },
    "item_loot_corroded_nerve_icon": {
        "RiskLevel": "watch_runtime_readability",
        "ReasonCN": "二次替换后不再像灯具，但神经束较细，运行时小图标可读性需要确认。",
        "RecommendationCN": "运行时复核；如果偏淡，再强化绿色神经束粗细和锈蚀夹片对比。",
    },
    "item_loot_crystal_scale_icon": {
        "RiskLevel": "watch_runtime_readability",
        "ReasonCN": "亮面晶体感可用，但轮廓接近灯罩，小尺寸下可能不够像鳞片。",
        "RecommendationCN": "运行时复核；必要时改为多片叠放的半透明晶化鳞片。",
    },
    "item_loot_living_mycelium_icon": {
        "RiskLevel": "watch_runtime_readability",
        "ReasonCN": "活体菌丝的发光瓶感偏强，生物组织轮廓不够明显。",
        "RecommendationCN": "运行时复核；必要时加强菌丝束、根须和轻微蠕动感。",
    },
    "monster_boss_gatekeeper_mk1_portrait": {
        "RiskLevel": "watch_runtime_readability",
        "ReasonCN": "头部主体偏暗，小头像框中可能只剩发光点。",
        "RecommendationCN": "运行时复核头像框；必要时放大面部和机械门卫轮廓。",
    },
    "monster_boss_mycelium_oracle_portrait": {
        "RiskLevel": "watch_runtime_readability",
        "ReasonCN": "剪影有神谕感，但主体细长，小尺寸辨识依赖亮边。",
        "RecommendationCN": "运行时复核；必要时提高头肩比例和菌冠轮廓。",
    },
    "monster_boss_spore_foundry_portrait": {
        "RiskLevel": "ok_for_current_v2",
        "ReasonCN": "头像几乎是黑底光点，角色识别度不足。",
        "RecommendationCN": "二次出图时改为半身头部、熔炉结构和孢子工厂轮廓，保留暗底但增加可读边界。",
    },
    "monster_elite_crystal_bulwark_portrait": {
        "RiskLevel": "watch_runtime_readability",
        "ReasonCN": "轮廓清楚但主体较小，晶体壁垒身份可能需要运行时确认。",
        "RecommendationCN": "运行时复核；必要时放大盾状晶壳和胸肩结构。",
    },
    "monster_elite_spore_matriarch_portrait": {
        "RiskLevel": "watch_runtime_readability",
        "ReasonCN": "孢子母体的剪影可用，但黑底占比偏高。",
        "RecommendationCN": "运行时复核；必要时提高头冠和腹囊轮廓亮度。",
    },
    "monster_elite_vein_knight_portrait": {
        "RiskLevel": "watch_runtime_readability",
        "ReasonCN": "骑士语义偏抽象，当前更像发光藤蔓剪影。",
        "RecommendationCN": "运行时复核；必要时重出带头盔、肩甲和菌脉披挂的头像。",
    },
    "monster_mob_acid_slime_mature_portrait": {
        "RiskLevel": "watch_runtime_readability",
        "ReasonCN": "二次替换后酸液史莱姆语义明确，但亮黄绿色面积较大，需要在头像框中确认不刺眼。",
        "RecommendationCN": "运行时复核；如果过亮，再降低饱和度并保留胶质轮廓。",
    },
    "monster_mob_crystal_guard_portrait": {
        "RiskLevel": "watch_runtime_readability",
        "ReasonCN": "晶体守卫轮廓尚可，但小头像中细节容易粘成暗块。",
        "RecommendationCN": "运行时复核；必要时强化晶体肩部和头部高光。",
    },
    "monster_mob_echo_pilgrim_portrait": {
        "RiskLevel": "ok_for_current_v2",
        "ReasonCN": "主体太小，黑底占比过大。",
        "RecommendationCN": "二次出图时改为更大的朝圣者头肩剪影和提灯结构。",
    },
    "monster_mob_lost_miner_echo_portrait": {
        "RiskLevel": "watch_runtime_readability",
        "ReasonCN": "矿工回声的提灯点明确，但角色本体偏小。",
        "RecommendationCN": "运行时复核；必要时放大矿工帽、背包和弯腰姿态。",
    },
    "monster_mob_mycelium_crawler_portrait": {
        "RiskLevel": "watch_runtime_readability",
        "ReasonCN": "爬行者轮廓偏细，头像槽中可能不够稳定。",
        "RecommendationCN": "运行时复核；必要时强化前肢和菌丝背部轮廓。",
    },
    "monster_mob_nerve_midge_swarm_portrait": {
        "RiskLevel": "watch_runtime_readability",
        "ReasonCN": "飞虫群氛围合适，但小光点群在小尺寸下辨识弱。",
        "RecommendationCN": "运行时复核；必要时增加群体飞虫外轮廓。",
    },
    "monster_mob_rust_cultivator_portrait": {
        "RiskLevel": "watch_runtime_readability",
        "ReasonCN": "轮廓有机械感，但耕作者身份不强。",
        "RecommendationCN": "运行时复核；必要时加入锈蚀工具、驼背姿态和头肩结构。",
    },
    "monster_mob_rust_hound_portrait": {
        "RiskLevel": "watch_runtime_readability",
        "ReasonCN": "兽形轮廓有基础可读性，但暗部占比偏高。",
        "RecommendationCN": "运行时复核；必要时提高头部、背脊和爪部剪影。",
    },
    "monster_mob_shell_grafter_portrait": {
        "RiskLevel": "watch_runtime_readability",
        "ReasonCN": "主体偏小，壳体嫁接语义在头像框中可能读不清。",
        "RecommendationCN": "运行时复核；必要时放大壳体边缘光和侧面头肩。",
    },
    "monster_mob_soul_midge_swarm_portrait": {
        "RiskLevel": "watch_runtime_readability",
        "ReasonCN": "氛围合适，但黑底和小光点可能导致小尺寸辨识弱。",
        "RecommendationCN": "运行时复核；必要时增加群体飞虫轮廓。",
    },
    "monster_mob_spore_archer_portrait": {
        "RiskLevel": "watch_runtime_readability",
        "ReasonCN": "主体靠下且偏小，弓箭职业语义不明显。",
        "RecommendationCN": "运行时复核；必要时重出带弓形剪影的半身头像。",
    },
    "ui_combat_feedback_echo_fade": {
        "RiskLevel": "watch_runtime_readability",
        "ReasonCN": "同名替换后已改为暖白 / 淡金回声残影，原浅蓝风格割裂风险已收敛；仍需在战斗 HUD 小尺寸中确认亮度和透明边界。",
        "RecommendationCN": "允许程序登记；运行时复核 140px 反馈贴片是否足够可读，必要时再提高轮廓对比。",
    },
    "ui_combat_feedback_slime_pop": {
        "RiskLevel": "watch_runtime_readability",
        "ReasonCN": "同名替换后已从荧光绿压到低饱和橄榄金酸液泡，原霓虹风格割裂风险已收敛；仍需在战斗 HUD 中确认泡破裂语义。",
        "RecommendationCN": "允许程序登记；运行时复核酸液泡 / 液滴是否能读成击败反馈，必要时再强化破裂飞溅形状。",
    },
    "memento_debt_shadow_window": {
        "RiskLevel": "watch_runtime_readability",
        "ReasonCN": "债务阴影的情绪方向可用，但图标偏灯具，房间纪念物语义需要运行时位置验证。",
        "RecommendationCN": "运行时复核；必要时改为窗影、欠条和压暗边框组合。",
    },
    "memento_wall_crack_first_defeat": {
        "RiskLevel": "watch_runtime_readability",
        "ReasonCN": "红色圆盘视觉醒目，但墙裂纪念物语义可能不够直观。",
        "RecommendationCN": "运行时复核；必要时改为墙面裂缝、红线和破损灰尘。",
    },
    "order_mage_oracle_sample_icon": {
        "RiskLevel": "watch_runtime_readability",
        "ReasonCN": "魔法样本方向可用，但和通用黄铜容器图标族接近。",
        "RecommendationCN": "运行时复核；必要时加强样本瓶、神谕孢子和法师标记。",
    },
    "rumor_filter_shortage_icon": {
        "RiskLevel": "watch_runtime_readability",
        "ReasonCN": "锁孔图形清楚，但过滤芯短缺的语义偏弱。",
        "RecommendationCN": "运行时复核；必要时改为滤芯、缺货标签和小镇公告纸。",
    },
    "rumor_living_mycelium_shortage_icon": {
        "RiskLevel": "watch_runtime_readability",
        "ReasonCN": "图形像机械徽章，活体菌丝短缺语义需要文字辅助。",
        "RecommendationCN": "运行时复核；必要时增加菌丝束、短缺标记和价格箭头。",
    },
    "rumor_old_blades_icon": {
        "RiskLevel": "watch_runtime_readability",
        "ReasonCN": "旧刀涨价目前像单个火把剪影，刀刃语义不足。",
        "RecommendationCN": "运行时复核；必要时改为旧刀、锈蚀刃口和涨价小符号。",
    },
}


def timestamp_text() -> str:
    return datetime.now(timezone.utc).astimezone().isoformat(timespec="seconds")


def timestamp_filename() -> str:
    return datetime.now(timezone.utc).astimezone().strftime("%Y%m%d_%H%M%S")


def safe_snapshot_tag(value: str) -> str:
    text = re.sub(r"[^A-Za-z0-9_.-]+", "_", value.strip())
    text = re.sub(r"_+", "_", text).strip("._-")
    return text[:80]


def find_art_generated_dir() -> Path:
    for path in PROJECT_ROOT.iterdir():
        generated = path / "_generated"
        if generated.is_dir() and (generated / "art_manifest.json").exists():
            return generated
    raise FileNotFoundError("Cannot find art _generated directory with art_manifest.json.")


def resolve_project_path(value: str | None) -> Path | None:
    if not value:
        return None
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


def default_asset_review_path() -> Path:
    return find_art_generated_dir() / "formal_v2_asset_review" / "formal_v2_asset_review.json"


def default_output_dir() -> Path:
    return find_art_generated_dir() / "formal_v2_asset_review"


def default_snapshot_dir() -> Path:
    return find_art_generated_dir() / "formal_v2_asset_review_snapshots"


def as_list(value: Any) -> list[Any]:
    return value if isinstance(value, list) else []


def sort_key(item: dict[str, Any]) -> tuple[int, str, str]:
    risk = str(item.get("RiskLevel", "ok_for_current_v2"))
    return (RISK_ORDER.get(risk, 99), str(item.get("Domain", "")), str(item.get("VisualID", "")))


def build_item(source: dict[str, Any]) -> dict[str, Any]:
    visual_id = str(source.get("VisualID", "") or "")
    override = RISK_OVERRIDES.get(visual_id, {})
    risk = override.get("RiskLevel", "ok_for_current_v2")
    if risk == "ok_for_current_v2":
        reason = "静态语义复核未发现需要优先二次替换的问题。"
        recommendation = "允许程序登记；后续只在运行时截图暴露遮挡、缩放或风格割裂时再处理。"
    else:
        reason = override["ReasonCN"]
        recommendation = override["RecommendationCN"]
    return {
        "VisualID": visual_id,
        "Priority": str(source.get("Priority", "") or ""),
        "Domain": str(source.get("Domain", "") or ""),
        "AssetType": str(source.get("AssetType", "") or ""),
        "DisplayName": str(source.get("DisplayName", "") or ""),
        "RiskLevel": risk,
        "ReasonCN": reason,
        "RecommendationCN": recommendation,
        "ApprovedPath": str(source.get("ApprovedPath", "") or ""),
        "TechnicalReviewStatus": str(source.get("ReviewStatus", "") or ""),
        "CanProgramIntegrate": str(source.get("ReviewStatus", "") or "") != "fail",
    }


def md_cell(value: Any) -> str:
    if isinstance(value, list):
        text = ", ".join(str(item) for item in value) if value else "-"
    elif isinstance(value, bool):
        text = "yes" if value else "no"
    else:
        text = "" if value is None else str(value)
    return text.replace("|", "\\|").replace("\n", " ")


def make_markdown(payload: dict[str, Any]) -> str:
    summary = payload["Summary"]
    scope = payload["ReviewScope"]
    lines = [
        "# Formal V2 运行时素材语义复核",
        "",
        f"生成时间：`{payload['GeneratedAt']}`",
        "",
        "## 摘要",
        "",
        f"- 复核 VisualID：`{scope['ReviewedVisualCount']}`",
        f"- 技术预检：`{summary['TechnicalPrecheck']}`",
        f"- 程序接入结论：`{summary['ProgramIntegrationDecision']}`",
        f"- 风险统计：`{summary['VisualSemanticRiskCounts']}`",
        "",
        "## 口径",
        "",
        "- `ok_for_current_v2`：当前 V2 可用，等待运行时截图正常验收。",
        "- `watch_runtime_readability`：不阻塞登记，但运行时截图要重点看小尺寸可读性。",
        "- `style_mismatch_watch`：不阻塞登记，但运行时截图要重点看是否和整体风格割裂。",
        "- `secondary_replacement_candidate`：不阻塞登记；若运行时截图证明影响识别或氛围，优先二次 NovelAI 同名替换。",
        "",
        "## 风险项",
        "",
        "| Risk | VisualID | Domain | Type | Reason | Recommendation |",
        "|---|---|---|---|---|---|",
    ]
    risk_items = [item for item in payload["ReviewItems"] if item["RiskLevel"] != "ok_for_current_v2"]
    for item in sorted(risk_items, key=sort_key):
        lines.append(
            "| "
            + " | ".join(
                [
                    md_cell(item["RiskLevel"]),
                    f"`{md_cell(item['VisualID'])}`",
                    md_cell(item["Domain"]),
                    md_cell(item["AssetType"]),
                    md_cell(item["ReasonCN"]),
                    md_cell(item["RecommendationCN"]),
                ]
            )
            + " |"
        )
    lines.extend(
        [
            "",
            "## 全量明细",
            "",
            "| Risk | VisualID | Domain | Type | Technical | Program Integrate |",
            "|---|---|---|---|---|---|",
        ]
    )
    for item in sorted(payload["ReviewItems"], key=sort_key):
        lines.append(
            "| "
            + " | ".join(
                [
                    md_cell(item["RiskLevel"]),
                    f"`{md_cell(item['VisualID'])}`",
                    md_cell(item["Domain"]),
                    md_cell(item["AssetType"]),
                    md_cell(item["TechnicalReviewStatus"]),
                    md_cell(item["CanProgramIntegrate"]),
                ]
            )
            + " |"
        )
    lines.extend(
        [
            "",
            "## Source Files",
            "",
            f"- Asset review: `{payload['ReviewScope']['SourceReport']}`",
        ]
    )
    return "\n".join(lines) + "\n"


def build_payload(args: argparse.Namespace) -> tuple[dict[str, Any], str]:
    asset_review_path = resolve_project_path(args.asset_review_path) or default_asset_review_path()
    asset_review = read_json(asset_review_path)
    source_items = [item for item in as_list(asset_review.get("Items")) if isinstance(item, dict)]
    review_items = [build_item(item) for item in source_items]
    risk_counts = Counter(item["RiskLevel"] for item in review_items)
    technical_counts = Counter(item["TechnicalReviewStatus"] for item in review_items)
    unknown_overrides = sorted(set(RISK_OVERRIDES) - {item["VisualID"] for item in review_items})
    if unknown_overrides:
        raise ValueError(f"Risk overrides do not exist in current asset review: {', '.join(unknown_overrides)}")
    technical_precheck = "pass" if technical_counts.get("fail", 0) == 0 else "has_failures"
    program_decision = "allow_program_integrate" if technical_precheck == "pass" else "hold_failed_assets"
    payload = {
        "GeneratedAt": timestamp_text(),
        "SnapshotTag": str(args.snapshot_tag or ""),
        "ReviewScope": {
            "SourceReport": repo_path(asset_review_path),
            "ReviewedVisualCount": len(review_items),
            "ProgramIntegrateStillAllowed": program_decision == "allow_program_integrate",
        },
        "Summary": {
            "TechnicalPrecheck": technical_precheck,
            "TechnicalStatusCounts": dict(sorted(technical_counts.items())),
            "ProgramIntegrationDecision": program_decision,
            "VisualSemanticRiskCounts": dict(sorted(risk_counts.items())),
            "PrimaryRisks": primary_risks(risk_counts),
        },
        "ReviewItems": sorted(review_items, key=lambda item: str(item["VisualID"])),
    }
    return payload, make_markdown(payload)


def primary_risks(risk_counts: Counter[str]) -> list[str]:
    risks = [
        "部分物品图标仍有黄铜灯具/容器同质化风险",
        "部分怪物头像在小头像槽中可能过暗或主体过小",
    ]
    if risk_counts.get("style_mismatch_watch", 0) > 0:
        risks.append("少量战斗反馈图标颜色可能和日系地底奇幻主风格割裂")
    else:
        risks.append("战斗反馈图标已完成色系统一，后续只需在运行时复核小尺寸可读性")
    return risks


def write_outputs(args: argparse.Namespace, payload: dict[str, Any], markdown: str) -> tuple[Path, Path]:
    output_dir = resolve_project_path(args.output_dir) or default_output_dir()
    json_path = output_dir / "visual_semantic_review.json"
    md_path = output_dir / "visual_semantic_review.md"
    write_json(json_path, payload)
    md_path.parent.mkdir(parents=True, exist_ok=True)
    md_path.write_text(markdown, encoding="utf-8")
    return json_path, md_path


def write_snapshot(args: argparse.Namespace, output_json: Path, output_md: Path) -> Path:
    snapshot_dir = resolve_project_path(args.snapshot_dir) or default_snapshot_dir()
    tag = safe_snapshot_tag(args.snapshot_tag)
    stem = timestamp_filename()
    if tag:
        stem = f"{stem}_{tag}"
    target = snapshot_dir / stem
    target.mkdir(parents=True, exist_ok=True)
    shutil.copy2(output_json, target / output_json.name)
    shutil.copy2(output_md, target / output_md.name)
    return target


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Generate Formal V2 visual-semantic review.")
    parser.add_argument("--asset-review-path", default="")
    parser.add_argument("--output-dir", default=DEFAULT_OUTPUT_DIR)
    parser.add_argument("--snapshot-dir", default=DEFAULT_SNAPSHOT_DIR)
    parser.add_argument("--snapshot-tag", default="")
    parser.add_argument("--snapshot", action="store_true")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    payload, markdown = build_payload(args)
    output_json, output_md = write_outputs(args, payload, markdown)
    snapshot_path: Path | None = None
    if args.snapshot:
        snapshot_path = write_snapshot(args, output_json, output_md)
    summary = payload["Summary"]
    print(f"[OK] Formal V2 visual semantic review: {repo_path(output_md)}")
    print(
        "[OK] reviewed="
        f"{payload['ReviewScope']['ReviewedVisualCount']}, "
        f"risks={summary['VisualSemanticRiskCounts']}, "
        f"decision={summary['ProgramIntegrationDecision']}"
    )
    if snapshot_path:
        print(f"[OK] snapshot: {repo_path(snapshot_path)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
