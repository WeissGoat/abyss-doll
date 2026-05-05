# -*- coding: utf-8 -*-
"""Fill bilingual art descriptions and structured specs for manifest entries."""

from __future__ import annotations

import argparse
import json
import re
from pathlib import Path
from typing import Any, Dict, Tuple


STYLE_EN = (
    "subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, "
    "brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow"
)

STYLE_CN = "地底奇幻冒险感，童话式好奇与深渊危险并存，结合蒸汽朋克机械、黄铜铜件、磨损金属、暖色灯光和微弱生物荧光。"

ITEM_EN: Dict[str, str] = {
    "con_cheap_sedative": "small cheap sedative ampoule or disposable syringe, worn metal safety sleeve, cool blue liquid, compact readable silhouette",
    "con_repair_kit": "compact mechanical repair kit, folded tools, small hose, repair fluid canister, subtle blue-white repair glow, compact readable silhouette",
    "gear_chainsaw_sword": "heavy chainsaw greatsword, broad metal blade with exposed chain teeth, oil stains, red warning accents, riveted scrap-metal parts, massive silhouette",
    "gear_charge_pistol": "compact energy pistol, short barrel, exposed coil, capacitor tube, blue glowing power core, clear horizontal silhouette",
    "gear_iron_armor": "patched iron chest armor, rough metal plates, rivets, leather straps, welding marks, heavy square silhouette",
    "gear_rusty_dagger": "short rusty dagger, chipped blade, wrapped handle, corroded edge, worn low-tier weapon silhouette",
    "gear_tactical_blade": "long tactical blade, narrow sharp blade, black handle, worn industrial metal, subtle cold-blue energy grooves, vertical silhouette",
    "gear_wooden_shield": "small makeshift wooden shield, old planks, metal rim, rivets, broken straps, fragile defensive silhouette",
    "loot_gear_scrap": "broken gear with loose screws and small metal scraps, ordinary mechanical salvage, simple single-object silhouette",
    "loot_rusty_coil": "rusty copper coil with iron core, loose wire ends, corroded casing, vertical mechanical component silhouette",
    "loot_toxic_filter": "polluted industrial filter cartridge, cracked casing, purple-green toxic residue, corroded metal, square silhouette",
    "mat_core_tier1": "small power core module, metal casing, stable blue-gold glowing core, heavy valuable machine part, strong centered silhouette",
}

ITEM_CN: Dict[str, str] = {
    "con_cheap_sedative": "小型廉价镇静剂图标，可表现为玻璃安瓿或一次性注射器，带磨损金属保护套和冷蓝药液，轮廓紧凑。",
    "con_repair_kit": "便携修复剂图标，小型机械维修包，包含折叠工具、胶管、修补液罐和蓝白修复微光。",
    "gear_chainsaw_sword": "重型链锯大剑图标，宽大金属剑身、外露链齿、油污、红色警示细节和铆接废铁结构。",
    "gear_charge_pistol": "充能手枪图标，短枪身、外露线圈、电容管和蓝色发光动力核心，横向轮廓清楚。",
    "gear_iron_armor": "铁片装甲图标，拼接胸甲、粗糙铁片、铆钉、皮带和焊痕，整体厚重。",
    "gear_rusty_dagger": "生锈短剑图标，缺口刀刃、缠布握柄、腐蚀边缘和低级旧武器气质。",
    "gear_tactical_blade": "战术长刀图标，狭长锋利刀身、黑色握柄、旧工业金属和少量冷蓝能量刻线。",
    "gear_wooden_shield": "木制小盾图标，旧木板、金属包边、铆钉和破损绑带，表现临时拼装感。",
    "loot_gear_scrap": "废旧齿轮材料图标，破损齿轮、螺丝和少量金属碎片，单体清晰。",
    "loot_rusty_coil": "生锈线圈材料图标，铜线圈、铁芯、断线和锈蚀外壳。",
    "loot_toxic_filter": "污染滤芯图标，工业滤芯破裂外壳、紫绿色污染残留和腐蚀金属。",
    "mat_core_tier1": "一阶动力核心图标，小型金属核心装置，内部有稳定蓝金色发光核心。",
}

MONSTER_EN: Dict[str, str] = {
    "mob_scavenger_bug": "small scavenger insect creature, scrap-metal shell plates, pincer mouthparts, tiny mechanical fragments attached, low-threat silhouette",
    "elite_scrap_guard": "heavy scrap-metal guardian, bulky welded helmet and shoulder armor, single red sensor eye, defensive intimidating silhouette",
    "mob_acid_slime": "acidic slime creature, semi-transparent corrosive body, trapped metal debris and bubbles inside, purple-green glow, readable blob silhouette",
    "elite_mutant_amalgam": "mutant amalgam creature, fused organic mass and broken machinery, multiple asymmetrical limbs, purple-green contamination marks, exposed metal bones",
}

MONSTER_CN: Dict[str, str] = {
    "mob_scavenger_bug": "拾荒虫头像，小型地底昆虫感，废铁甲壳、钳状口器和附着的机械碎片，威胁较低但不滑稽。",
    "elite_scrap_guard": "废铁守卫头像，焊接废铁头盔和肩甲，单眼红色感应器，厚重守门人压迫感。",
    "mob_acid_slime": "酸液软体头像，半透明腐蚀软泥，内部有金属碎片和气泡，紫绿色酸液发光。",
    "elite_mutant_amalgam": "畸变融合体头像，有机组织与破损机械融合，多肢不对称，紫绿色污染痕迹和外露金属骨架。",
}

NODE_EN: Dict[str, str] = {
    "CombatNode": "combat map node symbol, crossed blade marks or claw scratches, sharp aggressive shape, high contrast",
    "SafeRoomNode": "safe-room map node symbol, small shelter lamp or repair beacon inside a protective circle, calm readable shape",
    "BossNode": "boss map node symbol, heavy warning emblem, sealed gate icon or large cracked eye-shaped mark, ominous high-contrast shape",
}

NODE_CN: Dict[str, str] = {
    "CombatNode": "战斗节点图标，用交叉刀痕、爪痕或破损武器徽记表达危险。",
    "SafeRoomNode": "安全区节点图标，用庇护灯、维修灯或保护圆环表达休整。",
    "BossNode": "首领节点图标，用重型警告徽记、封闭门禁或裂隙眼形标记表达压迫感。",
}

PROSTHETIC_EN: Dict[str, str] = {
    "pros_cooling_system": "prosthetic cooling-system module, heat sink fins, coolant tubes, tiny pressure gauge, cold blue stabilizing light, compact machine part",
    "pros_power_arm": "prosthetic power-arm module, hydraulic joint, reinforced piston, mechanical fist connector, orange-red power cable, compact machine part",
}

PROSTHETIC_CN: Dict[str, str] = {
    "pros_cooling_system": "稳压散热插件图标，散热鳍片、冷却管线、小压力表和冷蓝稳定光。",
    "pros_power_arm": "动力臂增幅插件图标，液压关节、强化活塞、机械拳臂接口和橙红动力线。",
}

CHASSIS_EN: Dict[str, str] = {
    "chassis_lv1_basic": "basic backpack chassis frame, old workshop metal border, screws, worn corners, simple mechanical base plate, open center area",
    "chassis_lv2_expanded": "upgraded backpack chassis frame, sturdier metal border, reinforced side bars, upgrade connectors, precise mechanical details, open center area",
}

CHASSIS_CN: Dict[str, str] = {
    "chassis_lv1_basic": "基础背包底盘框架，旧工坊金属边框、螺丝、磨损边角和简洁机械底板，中间留空。",
    "chassis_lv2_expanded": "升级背包底盘框架，更坚固的金属边框、加固侧条、升级接口和精密机械细节，中间留空。",
}

BACKGROUND_EN: Dict[str, str] = {
    "combat": "side-scrolling battle arena background, empty industrial floor across the foreground, broken pipes, abandoned metal platform, dark vertical cavern fog in the midground, wide negative space on left and right",
    "dungeon_map": "dark route-map background texture, low visual noise, cracked stone, old brass pipes, faint mine lamps, deep vertical cavern feeling, large negative space",
    "layer_1": "shallow underground industrial passage, old metal walls, broken cables, faint warm lamps, light cavern mist, low danger atmosphere, wide empty floor",
    "layer_2": "polluted mining zone, corroded mine tunnel, purple-green toxic liquid, broken mining machines, acid haze, dim work lights, dangerous atmosphere",
    "workshop": "small mechanical repair workshop, workbench, hanging crane arm, tool wall, parts boxes, old fluorescent lamps, brass pipes, large negative space on both sides",
}

BACKGROUND_CN: Dict[str, str] = {
    "combat": "横版战斗背景，前景为空旷工业地面，中景有废弃金属平台、断裂管线和暗色洞穴雾气，左右留负空间。",
    "dungeon_map": "路线图底纹背景，低噪声暗色画面，裂石、旧黄铜管线、微弱矿灯和纵深洞穴感，大量负空间。",
    "layer_1": "浅层区域背景，废弃地下工业通道、旧金属墙、破损电缆、微弱暖灯和薄雾，危险感较低。",
    "layer_2": "污染矿带背景，腐蚀矿道、紫绿色毒液、破损采矿设备、酸雾和昏暗工作灯。",
    "workshop": "工坊整备背景，小型机械维修工坊，工作台、吊臂、工具墙、零件箱、旧灯管和黄铜管线，两侧留负空间。",
}

UI_EN = {
    "missing_sprite": "missing asset placeholder icon, simple broken-image symbol, dark base shape, red warning corner mark, clean readable silhouette",
}

UI_CN = {
    "missing_sprite": "缺失资源占位图，破损图片符号、暗色底形和红色警示角标，清楚但不刺眼。",
}

DOLL_EN = {
    "doll_proto_0": "humanoid mechanical doll, slender but durable body, exposed mechanical joints, repair marks, old workshop parts, cool glowing core light, calm neutral stance",
}

DOLL_CN = {
    "doll_proto_0": "原型机·零立绘，人形机械魔偶，纤细但坚固，外露机械关节、维修痕迹、旧工坊零件和冷色核心灯，安静中性站姿。",
}

NEGATIVE = {
    "item": "text, letters, numbers, watermark, logo, signature, busy background, cropped object, photorealistic hand, real person, clean plastic toy, multiple copies",
    "monster": "text, letters, numbers, watermark, logo, signature, busy background, cute mascot, friendly smile, excessive gore, cropped head, full environment scene, photorealistic animal photo",
    "node": "text, letters, numbers, watermark, logo, signature, busy background, tiny details, multiple symbols, photorealistic object, low contrast",
    "prosthetic": "text, letters, numbers, watermark, logo, signature, busy background, real human limb, medical advertisement style, cropped object, clean plastic product",
    "chassis": "text, letters, numbers, watermark, logo, signature, busy background, baked grid numbers, UI text, closed solid plate, cluttered center",
    "doll": "text, letters, numbers, watermark, logo, signature, photorealistic human, sexy pose, exaggerated expression, cropped feet, cropped head, busy background",
    "background": "text, letters, numbers, watermark, logo, signature, main character, large foreground creature, UI panels, buttons, high contrast noise, bright daylight",
    "ui": "text, letters, numbers, watermark, logo, signature, busy background, photorealistic photo, tiny details",
}

SPEC = {
    "item": {
        "Format": "png",
        "Width": 512,
        "Height": 512,
        "Background": "transparent",
        "AlphaRequired": True,
        "SafePaddingPercent": 10,
        "Composition": "centered single object",
        "PostProcess": ["resize", "trim_transparent_edges", "fit_safe_padding"],
        "PreviewSize": 64,
    },
    "monster": {
        "Format": "png",
        "Width": 1024,
        "Height": 1024,
        "Background": "transparent_or_simple_dark",
        "AlphaRequired": False,
        "SafePaddingPercent": 8,
        "Composition": "bust portrait, front or three-quarter view",
        "PostProcess": ["crop_square", "resize"],
        "PreviewSize": 160,
    },
    "node": {
        "Format": "png",
        "Width": 512,
        "Height": 512,
        "Background": "transparent",
        "AlphaRequired": True,
        "SafePaddingPercent": 12,
        "Composition": "centered high-contrast symbol",
        "PostProcess": ["resize", "trim_transparent_edges", "fit_safe_padding"],
        "PreviewSize": 64,
    },
    "prosthetic": {
        "Format": "png",
        "Width": 512,
        "Height": 512,
        "Background": "transparent",
        "AlphaRequired": True,
        "SafePaddingPercent": 10,
        "Composition": "centered single module",
        "PostProcess": ["resize", "trim_transparent_edges", "fit_safe_padding"],
        "PreviewSize": 80,
    },
    "chassis": {
        "Format": "png",
        "Width": 1024,
        "Height": 1024,
        "Background": "transparent",
        "AlphaRequired": True,
        "SafePaddingPercent": 6,
        "Composition": "open-center mechanical frame",
        "PostProcess": ["resize", "trim_transparent_edges"],
        "PreviewSize": 256,
    },
    "doll": {
        "Format": "png",
        "Width": 1024,
        "Height": 1536,
        "Background": "transparent",
        "AlphaRequired": True,
        "SafePaddingPercent": 6,
        "Composition": "full-body standing pose",
        "PostProcess": ["crop_portrait", "resize", "fit_safe_padding"],
        "PreviewSize": 256,
    },
    "background": {
        "Format": "png",
        "Width": 1920,
        "Height": 1080,
        "Background": "opaque_environment",
        "AlphaRequired": False,
        "SafePaddingPercent": 0,
        "Composition": "wide environment with negative space",
        "PostProcess": ["crop_16_9", "resize"],
        "PreviewSize": 320,
    },
    "ui": {
        "Format": "png",
        "Width": 512,
        "Height": 512,
        "Background": "transparent",
        "AlphaRequired": True,
        "SafePaddingPercent": 12,
        "Composition": "centered placeholder symbol",
        "PostProcess": ["resize", "fit_safe_padding"],
        "PreviewSize": 64,
    },
}

FORBIDDEN_PATTERNS = [
    "魔偶深渊",
    "来自深渊",
    "Made in Abyss",
    "搜打撤",
    "Unity",
    "UGUI",
    "游戏",
    "绘制",
    "战斗敌人卡片",
    "可读性",
    "站位",
]


def lookup(domain: str, config_id: str, english: bool) -> str:
    maps = {
        ("item", True): ITEM_EN,
        ("item", False): ITEM_CN,
        ("monster", True): MONSTER_EN,
        ("monster", False): MONSTER_CN,
        ("node", True): NODE_EN,
        ("node", False): NODE_CN,
        ("prosthetic", True): PROSTHETIC_EN,
        ("prosthetic", False): PROSTHETIC_CN,
        ("chassis", True): CHASSIS_EN,
        ("chassis", False): CHASSIS_CN,
        ("background", True): BACKGROUND_EN,
        ("background", False): BACKGROUND_CN,
        ("ui", True): UI_EN,
        ("ui", False): UI_CN,
        ("doll", True): DOLL_EN,
        ("doll", False): DOLL_CN,
    }
    return maps.get((domain, english), {}).get(config_id, "single readable game asset" if english else "单个清晰可读的美术资产。")


def prompt_for(entry: Dict[str, Any]) -> tuple[str, str, str, Dict[str, Any]]:
    domain = entry["Domain"]
    config_id = entry["ConfigID"]
    detail_en = lookup(domain, config_id, True)
    detail_cn = lookup(domain, config_id, False)

    if domain == "background":
        prompt_en = f"{STYLE_EN}, environment background, {detail_en}, wide composition, atmospheric depth, low visual noise, balanced lighting, no characters, no text"
    elif domain == "monster":
        prompt_en = f"{STYLE_EN}, creature portrait, {detail_en}, front or three-quarter view, head and upper body, strong silhouette, simple background, dramatic rim light, no text"
    elif domain == "doll":
        prompt_en = f"{STYLE_EN}, full-body character concept art, {detail_en}, neutral standing pose, full figure, clear silhouette, transparent background, no text"
    elif domain == "node":
        prompt_en = f"{STYLE_EN}, minimal map icon, {detail_en}, centered symbol, bold silhouette, high contrast, transparent background, no text"
    elif domain == "chassis":
        prompt_en = f"{STYLE_EN}, mechanical frame asset, {detail_en}, rectangular frame, open center, clean silhouette, transparent background, no text"
    elif domain == "prosthetic":
        prompt_en = f"{STYLE_EN}, prosthetic machine module icon, {detail_en}, centered single object, clean silhouette, transparent background, no text"
    elif domain == "ui":
        prompt_en = f"{STYLE_EN}, UI placeholder icon, {detail_en}, centered symbol, high contrast, transparent background, no text"
    else:
        prompt_en = f"{STYLE_EN}, game item icon, {detail_en}, centered single object, clean readable silhouette, transparent background, no text"

    prompt_cn = f"{STYLE_CN}{detail_cn}"
    return prompt_cn, prompt_en, NEGATIVE.get(domain, NEGATIVE["item"]), dict(SPEC.get(domain, SPEC["item"]))


def contains_forbidden_text(text: str, allow_cjk: bool = False) -> bool:
    lowered = text.lower()
    if not allow_cjk and re.search(r"[\u4e00-\u9fff]", text):
        return True
    return any(pattern.lower() in lowered for pattern in FORBIDDEN_PATTERNS)


def spec_is_legacy(spec: Any) -> bool:
    return not isinstance(spec, dict) or not spec.get("Width") or not spec.get("Height") or not spec.get("Format")


def should_fill(entry: Dict[str, Any], overwrite: bool) -> bool:
    if entry.get("Status") == "deprecated":
        return False
    if overwrite:
        return True
    return (
        entry.get("Status") == "todo"
        or not entry.get("PromptCN")
        or not entry.get("PromptEN")
        or not entry.get("NegativePromptEN")
        or spec_is_legacy(entry.get("Spec"))
        or contains_forbidden_text(str(entry.get("PromptEN", "")))
        or contains_forbidden_text(str(entry.get("NegativePromptEN", "")))
    )


def format_spec(spec: Any) -> str:
    if not isinstance(spec, dict):
        return str(spec).replace("|", "/")
    return json.dumps(spec, ensure_ascii=False, separators=(",", ":")).replace("|", "/")


def make_prompt_markdown(manifest: Dict[str, Any]) -> str:
    lines = [
        "# AI Image Prompt List",
        "",
        "> Generated by `美术工具/Generate-ArtPrompts.ps1`. `PromptEN` is for image-generation tools; `PromptCN` is for human review.",
        "",
        "| Domain | ConfigID | VisualID | Status | PromptCN | PromptEN | NegativePromptEN | Spec |",
        "|---|---|---|---|---|---|---|---|",
    ]
    for entry in manifest["Entries"]:
        prompt_cn = str(entry.get("PromptCN", "")).replace("|", "/")
        prompt_en = str(entry.get("PromptEN", "")).replace("|", "/")
        negative = str(entry.get("NegativePromptEN", "")).replace("|", "/")
        spec = format_spec(entry.get("Spec", {}))
        lines.append(
            f"| `{entry['Domain']}` | `{entry['ConfigID']}` | `{entry['VisualID']}` | "
            f"`{entry['Status']}` | {prompt_cn} | {prompt_en} | {negative} | `{spec}` |"
        )
    lines.append("")
    return "\n".join(lines)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Generate bilingual AI art prompt data for manifest entries.")
    parser.add_argument("--manifest-path", default="美术文档/_generated/art_manifest.json")
    parser.add_argument("--prompt-markdown-path", default="美术文档/_generated/AI绘图提示词清单.md")
    parser.add_argument("--overwrite", action="store_true")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    root = Path.cwd()
    manifest_path = (root / args.manifest_path).resolve()
    prompt_markdown_path = (root / args.prompt_markdown_path).resolve()

    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    manifest["ArtStyle"] = {
        "NameCN": "地底奇幻冒险 + 蒸汽朋克",
        "ReferenceCN": "类似来自深渊的奇幻探索感，但不在 AI 提示词中直接引用作品名。",
        "PromptStyleEN": STYLE_EN,
        "PromptStyleCN": STYLE_CN,
    }

    changed = 0
    skipped = 0
    violations = []
    for entry in manifest["Entries"]:
        if should_fill(entry, args.overwrite):
            prompt_cn, prompt_en, negative_en, spec = prompt_for(entry)
            entry["PromptCN"] = prompt_cn
            entry["PromptEN"] = prompt_en
            entry["NegativePromptEN"] = negative_en
            entry["Spec"] = spec
            entry["Status"] = "prompted"
            changed += 1
        else:
            skipped += 1

        if contains_forbidden_text(str(entry.get("PromptEN", ""))):
            violations.append(f"{entry['VisualID']}:PromptEN")
        if contains_forbidden_text(str(entry.get("NegativePromptEN", ""))):
            violations.append(f"{entry['VisualID']}:NegativePromptEN")

    if violations:
        raise RuntimeError(f"Forbidden AI prompt text remains: {sorted(set(violations))}")

    manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    prompt_markdown_path.parent.mkdir(parents=True, exist_ok=True)
    prompt_markdown_path.write_text(make_prompt_markdown(manifest), encoding="utf-8")

    print(f"Prompt fields updated: {changed}")
    print(f"Entries skipped: {skipped}")
    print(f"Manifest: {manifest_path.relative_to(root).as_posix()}")
    print(f"Prompt markdown: {prompt_markdown_path.relative_to(root).as_posix()}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
