# -*- coding: utf-8 -*-
"""Generate Formal V2 UI layout design boards.

These images are deterministic art-side design boards, not runtime assets and
not AI concept art. They are meant for structure review before active UI
specification migration.
"""

from __future__ import annotations

import argparse
import json
import math
import random
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable

from PIL import Image, ImageDraw, ImageFont


SCRIPT_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = SCRIPT_DIR.parents[1]
DEFAULT_OUT_DIR = "美术文档/ui_design/formal_v2/design_boards"
WIDTH = 1920
HEIGHT = 1080


PALETTE = {
    "ink": "#26333d",
    "ink_soft": "#49606a",
    "paper": "#e7d7b4",
    "paper_2": "#d8c49a",
    "paper_dark": "#b08f62",
    "brass": "#c79642",
    "brass_dark": "#8d622e",
    "ember": "#c96f49",
    "rose": "#a65359",
    "teal": "#487a78",
    "green": "#5f8f65",
    "blue": "#526e92",
    "violet": "#6e5f8f",
    "shadow": "#1c2027",
    "danger": "#8f3f38",
    "primary": "#d7903b",
    "secondary": "#7d8a78",
}


@dataclass(frozen=True)
class Zone:
    x: int
    y: int
    w: int
    h: int
    label: str
    note: str = ""
    kind: str = "panel"
    accent: str = "brass"


@dataclass(frozen=True)
class BoardSpec:
    screen_id: str
    title: str
    doc: str
    goal: str
    scene: str
    zones: tuple[Zone, ...]
    motif: str = "room"
    companion: bool = False


def repo_path(path: Path) -> str:
    try:
        return path.resolve().relative_to(PROJECT_ROOT.resolve()).as_posix()
    except ValueError:
        return path.as_posix()


def font_path() -> str | None:
    candidates = [
        Path("C:/Windows/Fonts/msyh.ttc"),
        Path("C:/Windows/Fonts/simhei.ttf"),
        Path("C:/Windows/Fonts/simsun.ttc"),
        Path("C:/Windows/Fonts/arial.ttf"),
    ]
    for item in candidates:
        if item.exists():
            return str(item)
    return None


FONT_PATH = font_path()


def load_font(size: int, bold: bool = False) -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    if FONT_PATH:
        return ImageFont.truetype(FONT_PATH, size=size)
    return ImageFont.load_default()


FONT_TITLE = load_font(46, True)
FONT_SUBTITLE = load_font(28)
FONT_LABEL = load_font(25, True)
FONT_NOTE = load_font(21)
FONT_SMALL = load_font(18)


def hex_to_rgb(value: str) -> tuple[int, int, int]:
    value = value.lstrip("#")
    return int(value[0:2], 16), int(value[2:4], 16), int(value[4:6], 16)


def blend(a: str, b: str, t: float) -> tuple[int, int, int]:
    ar, ag, ab = hex_to_rgb(a)
    br, bg, bb = hex_to_rgb(b)
    return (
        int(ar + (br - ar) * t),
        int(ag + (bg - ag) * t),
        int(ab + (bb - ab) * t),
    )


def wrap_text(draw: ImageDraw.ImageDraw, text: str, font: ImageFont.ImageFont, max_width: int) -> list[str]:
    if not text:
        return []
    lines: list[str] = []
    for paragraph in text.split("\n"):
        current = ""
        for char in paragraph:
            candidate = current + char
            bbox = draw.textbbox((0, 0), candidate, font=font)
            if bbox[2] - bbox[0] <= max_width or not current:
                current = candidate
            else:
                lines.append(current)
                current = char
        if current:
            lines.append(current)
    return lines


def draw_text_block(
    draw: ImageDraw.ImageDraw,
    xy: tuple[int, int],
    text: str,
    font: ImageFont.ImageFont,
    fill: str,
    max_width: int,
    line_gap: int = 6,
    max_lines: int | None = None,
) -> int:
    x, y = xy
    lines = wrap_text(draw, text, font, max_width)
    if max_lines is not None and len(lines) > max_lines:
        lines = lines[:max_lines]
        if lines:
            lines[-1] = lines[-1].rstrip("，。；、 ") + "..."
    for line in lines:
        draw.text((x, y), line, font=font, fill=fill)
        bbox = draw.textbbox((x, y), line, font=font)
        y += bbox[3] - bbox[1] + line_gap
    return y


def draw_background(draw: ImageDraw.ImageDraw, rng: random.Random) -> None:
    for y in range(HEIGHT):
        t = y / max(1, HEIGHT - 1)
        color = blend("#31424a", "#d4b679", t * 0.74)
        draw.line([(0, y), (WIDTH, y)], fill=color)

    for _ in range(1400):
        x = rng.randrange(WIDTH)
        y = rng.randrange(HEIGHT)
        r = rng.choice([1, 1, 1, 2])
        base = rng.choice(["#f3e5bd", "#8aa19a", "#b9884a", "#ffffff"])
        alpha = rng.randrange(20, 60)
        color = (*hex_to_rgb(base), alpha)
        draw.ellipse((x, y, x + r, y + r), fill=color)

    draw.rectangle((0, 0, WIDTH, HEIGHT), outline="#563f2a", width=6)
    draw.rectangle((24, 24, WIDTH - 24, HEIGHT - 24), outline="#a5793b", width=2)


def draw_motif(draw: ImageDraw.ImageDraw, motif: str) -> None:
    if motif in {"map", "layer"}:
        for i in range(7):
            x = 320 + i * 185
            y = 220 + int(math.sin(i * 0.8) * 80) + i * 70
            draw.line((x, y, x + 150, y + 45), fill="#8d622e", width=8)
            draw.ellipse((x - 18, y - 18, x + 18, y + 18), fill="#e7d7b4", outline="#8d622e", width=5)
        draw.arc((220, 130, 1580, 980), 205, 330, fill="#49606a", width=8)
    elif motif in {"combat", "loot"}:
        draw.rounded_rectangle((180, 205, 760, 680), 36, fill="#5c6c62", outline="#c79642", width=5)
        draw.rounded_rectangle((1160, 205, 1740, 680), 36, fill="#66565d", outline="#c79642", width=5)
        draw.ellipse((340, 355, 565, 645), fill="#dbc79b", outline="#8d622e", width=6)
        draw.ellipse((1345, 300, 1600, 640), fill="#8f5b55", outline="#5d352e", width=7)
    elif motif in {"shop", "board"}:
        draw.rounded_rectangle((220, 180, 1700, 850), 44, fill="#8d6840", outline="#c79642", width=8)
        for x in range(300, 1580, 210):
            draw.line((x, 215, x + 80, 800), fill="#6b4d2f", width=4)
        draw.rectangle((260, 680, 1660, 830), fill="#b08f62", outline="#6b4d2f", width=5)
    elif motif in {"doll", "room"}:
        draw.rounded_rectangle((210, 180, 1710, 860), 48, fill="#b99060", outline="#74512f", width=7)
        draw.rectangle((280, 220, 590, 500), fill="#9fc0b0", outline="#74512f", width=6)
        draw.ellipse((830, 310, 1080, 705), fill="#e3c6a3", outline="#7b5a40", width=7)
        draw.rectangle((1220, 260, 1580, 720), fill="#8d6840", outline="#5d3f27", width=6)
    elif motif == "event":
        draw.rounded_rectangle((260, 175, 1660, 870), 46, fill="#2f3841", outline="#c79642", width=7)
        draw.rounded_rectangle((380, 255, 1540, 790), 32, fill="#e7d7b4", outline="#8d622e", width=6)
    else:
        draw.rounded_rectangle((250, 180, 1670, 850), 52, fill="#987654", outline="#c79642", width=8)
        draw.ellipse((780, 300, 1110, 710), fill="#d9c09b", outline="#755c38", width=7)


def zone_color(kind: str, accent: str) -> tuple[str, str]:
    fills = {
        "main": "#ead9b7",
        "scene": "#d7c291",
        "panel": "#e2d2ac",
        "list": "#dbcaa5",
        "detail": "#e9ddc2",
        "status": "#c9d6be",
        "inventory": "#c7d0b8",
        "action": "#f0c06f",
        "danger": "#d49a8f",
        "modal": "#ead9c0",
        "tab": "#d2c4ad",
    }
    outlines = {
        "main": PALETTE.get(accent, PALETTE["brass"]),
        "scene": "#7c5d37",
        "panel": "#a77737",
        "list": "#8d622e",
        "detail": "#8d622e",
        "status": "#5f7d63",
        "inventory": "#58765b",
        "action": "#9a5b24",
        "danger": "#7b3830",
        "modal": "#9a6d33",
        "tab": "#7c6b55",
    }
    return fills.get(kind, "#e2d2ac"), outlines.get(kind, PALETTE.get(accent, PALETTE["brass"]))


def draw_zone(draw: ImageDraw.ImageDraw, zone: Zone, index: int) -> None:
    fill, outline = zone_color(zone.kind, zone.accent)
    shadow = (zone.x + 8, zone.y + 10, zone.x + zone.w + 8, zone.y + zone.h + 10)
    draw.rounded_rectangle(shadow, 18, fill="#00000055")
    draw.rounded_rectangle((zone.x, zone.y, zone.x + zone.w, zone.y + zone.h), 18, fill=fill, outline=outline, width=4)
    draw.rectangle((zone.x + 16, zone.y + 16, zone.x + 48, zone.y + 48), fill=outline)
    draw.text((zone.x + 57, zone.y + 15), f"{index}. {zone.label}", font=FONT_LABEL, fill=PALETTE["ink"])
    can_draw_note = not (zone.kind == "action" and zone.h <= 135)
    if zone.note and can_draw_note:
        draw_text_block(
            draw,
            (zone.x + 26, zone.y + 60),
            zone.note,
            FONT_NOTE,
            PALETTE["ink_soft"],
            zone.w - 52,
            line_gap=5,
            max_lines=max(2, (zone.h - 70) // 31),
        )
    if zone.kind == "inventory":
        cell = max(34, min(72, min(zone.w // 5, zone.h // 4)))
        sx = zone.x + zone.w - cell * 4 - 28
        sy = zone.y + zone.h - cell * 3 - 24
        for row in range(3):
            for col in range(4):
                x = sx + col * cell
                y = sy + row * cell
                draw.rounded_rectangle((x, y, x + cell - 8, y + cell - 8), 8, fill="#efe2bd", outline="#6f835e", width=2)
    if zone.kind == "action":
        bar_top = zone.y + max(58, zone.h - 50)
        draw.rounded_rectangle(
            (zone.x + 24, bar_top, zone.x + zone.w - 24, zone.y + zone.h - 16),
            14,
            fill="#c96f49",
            outline="#7b3c26",
            width=3,
        )
    if zone.kind == "danger":
        draw.line((zone.x + 22, zone.y + zone.h - 30, zone.x + zone.w - 22, zone.y + 30), fill="#7b3830", width=3)


def draw_header(draw: ImageDraw.ImageDraw, spec: BoardSpec) -> None:
    draw.rounded_rectangle((60, 54, 1860, 156), 24, fill="#e7d7b4", outline="#8d622e", width=4)
    title = f"{spec.title}  |  {spec.screen_id}"
    draw.text((90, 70), title, font=FONT_TITLE, fill=PALETTE["ink"])
    flag = "设计拆分图，不是 active ScreenID" if spec.companion else "Formal V2 active 界面设计图"
    draw.text((1290, 78), flag, font=FONT_NOTE, fill=PALETTE["rose"] if spec.companion else PALETTE["teal"])
    draw_text_block(draw, (92, 126), f"目标：{spec.goal}", FONT_SMALL, PALETTE["ink_soft"], 1680, line_gap=2, max_lines=1)


def draw_footer(draw: ImageDraw.ImageDraw, spec: BoardSpec) -> None:
    draw.rounded_rectangle((60, 944, 1860, 1026), 20, fill="#efe0bd", outline="#8d622e", width=3)
    draw_text_block(draw, (88, 962), f"场景隐喻：{spec.scene}", FONT_NOTE, PALETTE["ink"], 1250, line_gap=4, max_lines=2)
    draw.text((1480, 966), f"Design doc: {spec.doc}", font=FONT_SMALL, fill=PALETTE["ink_soft"])
    draw.text((1480, 995), "Not runtime asset / Not Manifest", font=FONT_SMALL, fill=PALETTE["danger"])


def render_board(spec: BoardSpec, path: Path, overwrite: bool) -> None:
    if path.exists() and not overwrite:
        return
    rng = random.Random(spec.screen_id)
    image = Image.new("RGBA", (WIDTH, HEIGHT), "#ffffff")
    draw = ImageDraw.Draw(image, "RGBA")
    draw_background(draw, rng)
    draw_motif(draw, spec.motif)
    draw_header(draw, spec)
    for index, zone in enumerate(spec.zones, start=1):
        draw_zone(draw, zone, index)
    draw_footer(draw, spec)
    path.parent.mkdir(parents=True, exist_ok=True)
    image.convert("RGB").save(path, format="PNG", optimize=True)


def z(x: int, y: int, w: int, h: int, label: str, note: str, kind: str = "panel", accent: str = "brass") -> Zone:
    return Zone(x, y, w, h, label, note, kind, accent)


def build_specs() -> list[BoardSpec]:
    return [
        BoardSpec(
            "workshop_main",
            "工坊主界面",
            "01_workshop_main_v2.md",
            "把局外主界面从按钮菜单改成以魔偶为中心的安心房间。",
            "家一样的工坊房间，深渊入口、工作室门和账本入口都是空间热点。",
            (
                z(145, 220, 430, 300, "深渊入口", "唯一最高权重主入口：出发 / 下潜。像门、升降机或窗外深渊，不是普通按钮。", "action"),
                z(705, 210, 510, 540, "魔偶与房间中心", "魔偶、床铺、灯光和情绪状态是视觉中心；互动入口从角色热点进入。", "main", "rose"),
                z(1290, 240, 380, 250, "工作室入口", "进入背包、底盘、义体、维护的空间门。", "panel"),
                z(1290, 540, 380, 180, "账本 / 市场角落", "低权重入口，只给摘要，不展开完整列表。", "detail"),
                z(120, 770, 670, 120, "轻状态条", "金币、日期、月租、风险用图标化短信息保留。", "status", "green"),
            ),
            "room",
        ),
        BoardSpec(
            "workshop_studio",
            "工作室拆分界面",
            "01_workshop_main_v2.md",
            "承接背包、改造椅、维护、义体和底盘等重操作。",
            "同一个温暖工坊工作室，左侧是背包，右侧魔偶坐在机械改造椅。",
            (
                z(100, 235, 420, 500, "背包 / 材料区", "左侧固定 100x100 玩法格；装饰只做容器，不改变格子规则。", "inventory", "green"),
                z(610, 250, 520, 420, "工作托盘", "当前选中的维护、义体或底盘方案放在工作台中央。", "main"),
                z(1210, 210, 520, 520, "魔偶改造椅", "魔偶状态、槽位和改造反馈集中在右侧实体区域。", "scene", "rose"),
                z(590, 750, 650, 95, "模式标签", "维护 / 义体 / 底盘作为同一工作室内切换，不是独立大场景。", "tab"),
                z(1310, 760, 360, 110, "执行主行动", "根据模式显示唯一主按钮，材料不足时给短提示。", "action"),
            ),
            "room",
            True,
        ),
        BoardSpec(
            "combat_hud",
            "战斗界面",
            "02_combat_hud_v2.md",
            "从状态堆叠改为左右实体战斗舞台 + 底部背包指令区。",
            "横版战斗舞台，左边人偶，右边敌人，敌人血条和意图贴近脚下。",
            (
                z(120, 240, 470, 360, "玩家人偶舞台", "保留站位、护盾、HP/SAN 短状态；少文字。", "scene", "teal"),
                z(1280, 235, 500, 380, "敌方实体舞台", "敌人实体、脚下血条、意图图标和目标环集中。", "scene", "rose"),
                z(610, 270, 560, 300, "VFX 走廊", "攻击、命中、破盾等反馈穿过中轴，不被面板遮挡。", "main"),
                z(495, 720, 930, 170, "底部居中背包", "背包格是回合指令来源，避免按钮挡住拖拽路径。", "inventory", "green"),
                z(1480, 770, 300, 95, "结束回合", "默认唯一主按钮，普通攻击不再常驻抢权重。", "action"),
            ),
            "combat",
        ),
        BoardSpec(
            "inventory_loot",
            "战利品清点",
            "03_inventory_loot_v2.md",
            "半透明战斗场景上叠清点层，中央背包，奖励散落在外。",
            "战后现场仍可见，玩家像在地上清点战利品，而不是打开表格。",
            (
                z(620, 250, 680, 430, "中央背包", "背包是核心决策对象；奖励拖入或点击放入。", "inventory", "green"),
                z(150, 250, 360, 230, "散落奖励 A", "奖励像地面物件围绕背包，不默认长列表。", "scene"),
                z(1380, 285, 360, 230, "散落奖励 B", "不同稀有度用图标和小徽记区分。", "scene"),
                z(210, 720, 390, 120, "容量 / 风险摘要", "只显示背包压力、未拾取风险和短提示。", "status"),
                z(1330, 725, 360, 120, "确认带出", "唯一主行动；放弃未拾取归入危险确认。", "action"),
            ),
            "loot",
        ),
        BoardSpec(
            "dungeon_map",
            "深渊地图",
            "04_dungeon_map_v2.md",
            "地图和节点成为视觉中心，只保留必要按钮。",
            "手绘路线图铺在桌面或羊皮纸上，节点是探索决策，不是按钮列表。",
            (
                z(190, 210, 1250, 600, "地图 / 节点网络", "路线、节点、锁定、当前路径是主视觉；详情不常驻占屏。", "main", "teal"),
                z(1490, 260, 300, 190, "极简节点提示", "仅展示图标、风险、奖励短摘要。", "detail"),
                z(1490, 490, 300, 130, "当前队伍状态", "HP/SAN、容量、撤离风险用短条显示。", "status"),
                z(1490, 680, 300, 110, "进入节点", "唯一主行动。", "action"),
                z(105, 835, 560, 70, "底部工具", "返回、缩放、图例图标化，低权重。", "tab"),
            ),
            "map",
        ),
        BoardSpec(
            "settlement",
            "战斗结算",
            "05_settlement_v2.md",
            "用报告式结算表达结果、收益损失、状态变化和下一步。",
            "战斗报告放在桌面上，胜败徽记、带出物和损伤记录一眼分层。",
            (
                z(210, 230, 360, 300, "结果徽记", "胜利、HP 战败、SAN 崩溃等用大徽记和色调区分。", "main", "rose"),
                z(630, 220, 540, 360, "收益 / 损失卡", "只保留关键物品和金币变化；长明细折叠。", "detail"),
                z(1230, 220, 430, 360, "状态变化", "HP、SAN、磨损、侵蚀用短条和图标表达。", "status"),
                z(400, 655, 1080, 130, "短时间线", "最多 3 条关键事件，不做系统日志墙。", "list"),
                z(1285, 825, 330, 90, "返回工坊 / 进账本", "根据结果唯一化下一步主行动。", "action"),
            ),
            "board",
        ),
        BoardSpec(
            "maintenance_panel",
            "维护子面板",
            "06_maintenance_panel_v2.md",
            "作为 workshop_studio 内维护模式，而不是独立诊所场景。",
            "共用工作室底图，护理舱、诊断板和方案托盘从同一房间切换出来。",
            (
                z(125, 230, 410, 420, "诊断板", "磨损、侵蚀、下潜许可和最大风险按优先级显示。", "status", "green"),
                z(610, 220, 520, 420, "护理舱 / 魔偶", "魔偶仍是右侧或中右侧实体，不切到陌生房间。", "scene", "rose"),
                z(1180, 250, 500, 330, "维护方案卡", "修复、净化、许可检查是可比较方案卡。", "list"),
                z(250, 720, 960, 105, "材料 / 金币 token", "用图标和短文字显示缺口，不做长表。", "detail"),
                z(1310, 735, 340, 105, "执行维护", "唯一主按钮；暂缓维护降级且高风险二次确认。", "action"),
            ),
            "room",
        ),
        BoardSpec(
            "prosthetic_panel",
            "义体子面板",
            "07_prosthetic_panel_v2.md",
            "在 workshop_studio 中处理义体柜、工作托盘和魔偶槽位。",
            "左侧义体柜，中间托盘，右侧魔偶改造椅，共用工作室空间。",
            (
                z(115, 220, 430, 500, "义体柜", "列表只做选择摘要，不在每行塞 Craft / Equip。", "list"),
                z(620, 250, 470, 370, "选中义体托盘", "展示外观、材料、效果和槽位适配。", "main"),
                z(1190, 215, 500, 510, "魔偶槽位", "槽位在实体旁可视化，强调改造结果。", "scene", "rose"),
                z(620, 705, 470, 110, "材料缺口", "短 token 行，缺口清楚但不压屏。", "detail"),
                z(1285, 765, 330, 95, "制造 / 装备", "选中后统一主行动。", "action"),
            ),
            "room",
        ),
        BoardSpec(
            "chassis_upgrade_panel",
            "底盘升级子面板",
            "08_chassis_upgrade_panel_v2.md",
            "用蓝图桌和当前 / 下一底盘大对比表达升级决策。",
            "工作室蓝图桌摊开，当前底盘与下一底盘像工程图并列。",
            (
                z(155, 250, 470, 340, "当前底盘", "展示格局和容量，不做背包拖拽。", "inventory", "teal"),
                z(720, 230, 520, 390, "下一底盘蓝图", "升级后的格局、容量变化和解锁点是视觉重点。", "main"),
                z(1330, 260, 350, 300, "材料 / 蓝图条件", "材料 token、前置许可和缺口提示。", "detail"),
                z(310, 705, 800, 105, "变化摘要", "容量、槽位、风险的前后对比。", "status"),
                z(1285, 740, 330, 105, "执行升级", "唯一主行动；材料追踪是次行动。", "action"),
            ),
            "room",
        ),
        BoardSpec(
            "sell_panel",
            "小镇商店 / 市场",
            "09_sell_panel_v2.md",
            "从工坊出货中解耦，定位为小镇商店买卖交易。",
            "温暖小镇柜台、货架、商人账本；不是工坊估价台。",
            (
                z(120, 240, 360, 480, "Buy / Sell 标签", "双标签存在，但默认只突出当前交易方向。", "tab"),
                z(560, 220, 520, 480, "商店货架", "商品像货架物件，不是电子表格。", "list"),
                z(1160, 240, 480, 330, "选中货物托盘", "价格、条件、声望或折扣在托盘中展示。", "detail"),
                z(1160, 610, 480, 105, "价格反馈", "涨跌、亏损、禁售风险短条。", "status"),
                z(1290, 760, 320, 95, "买入 / 卖出", "唯一主行动；Buy All / Sell All 折叠。", "action"),
            ),
            "shop",
        ),
        BoardSpec(
            "shop_staging",
            "营业前摆货",
            "10_shop_staging_v2.md",
            "以店面陈列台为中心，收敛出货分配、订单和黑市。",
            "工坊店面开张前，把货物摆上中央陈列台，侧抽屉承接订单和黑市。",
            (
                z(105, 230, 410, 520, "仓库库存", "左侧选择可摆货物，保留容量和筛选。", "inventory", "green"),
                z(610, 250, 570, 400, "中央陈列台", "被摆出的商品是视觉中心。", "main"),
                z(1260, 235, 420, 230, "订单侧抽屉", "订单渠道降级为侧抽屉，不抢主流程。", "detail"),
                z(1260, 510, 420, 180, "黑市侧箱", "风险明显，但默认折叠。", "danger"),
                z(1300, 760, 330, 100, "开始营业", "唯一主行动。", "action"),
            ),
            "shop",
        ),
        BoardSpec(
            "business_settlement",
            "营业结算演出",
            "11_business_settlement_v2.md",
            "把营业反馈做成小演出，再进入每日账单。",
            "店铺前台有顾客流和成交反馈，完整账单留给下一界面。",
            (
                z(180, 245, 560, 410, "顾客流 / 店面", "表现有顾客进出和成交爆点，不做完整收支表。", "scene"),
                z(820, 230, 430, 420, "成交高光", "金币增长、售出数量和最佳交易。", "main"),
                z(1330, 250, 330, 320, "短摘要 feed", "最多几条成交 / 未售出 / 风险提示。", "list"),
                z(330, 720, 760, 100, "未售出 / 风险", "只保留经营决策必要的风险摘要。", "status"),
                z(1280, 735, 340, 100, "进入账单", "唯一主行动，进入 daily_bill_report。", "action"),
            ),
            "shop",
        ),
        BoardSpec(
            "daily_bill_report",
            "每日账单",
            "12_daily_bill_report_v2.md",
            "用打开账本和月租压力轨表达日结，而不是表格墙。",
            "账本摊开，今日结论、收入支出和月租压力在纸页上分区。",
            (
                z(260, 230, 520, 470, "收入页", "销售、订单和其他收入以短卡归纳。", "detail"),
                z(845, 230, 520, 470, "支出页", "维护、采购、租金和债务压力。", "detail"),
                z(1410, 260, 250, 360, "今日结论", "一句摘要和关键数字，帮助决定明天做什么。", "main"),
                z(330, 735, 850, 95, "月租压力轨", "倒计时、欠款和风险用进度轨表达。", "status"),
                z(1300, 750, 330, 95, "结束当天", "唯一主行动；延后付款是危险折叠项。", "action"),
            ),
            "board",
        ),
        BoardSpec(
            "layer_select",
            "下潜层选择",
            "13_layer_select_v2.md",
            "让层级选择像站在深渊入口前，而不是弹窗列表。",
            "垂直深渊剖面、发光停靠点、锁链封印和右侧整备简报。",
            (
                z(155, 210, 690, 610, "深渊剖面地图", "可进入层发光，锁定层被雾气或锁链遮住。", "main", "teal"),
                z(925, 250, 430, 360, "选中层简报", "风险、推荐整备和主要奖励短摘要。", "detail"),
                z(1405, 270, 300, 230, "整备检查", "HP/SAN、容量、许可等短条。", "status"),
                z(925, 665, 430, 100, "锁定提示", "给解锁方向，不像普通 disabled 按钮。", "panel"),
                z(1405, 710, 300, 95, "开始下潜", "唯一主行动。", "action"),
            ),
            "layer",
        ),
        BoardSpec(
            "safe_room",
            "安全屋",
            "14_safe_room_v2.md",
            "把休整、整理、撤离和继续深入组织成安全营地决策。",
            "深渊洞穴中的温暖小营地，营火、铺盖、补给箱和安静灯光。",
            (
                z(580, 230, 560, 360, "安全营地中心", "营火和补给箱给安全感，UI 融入场景。", "scene", "green"),
                z(150, 620, 510, 190, "背包整理垫", "背包区域低位展开，仍遵守格子尺寸。", "inventory", "green"),
                z(1240, 240, 420, 260, "休整状态", "HP/SAN、疲劳、恢复收益短条。", "status"),
                z(1230, 555, 430, 150, "继续 / 撤离决策", "继续深入与撤离清楚区分。", "detail"),
                z(1320, 760, 320, 90, "继续深入", "主行动；撤离作为次行动保留。", "action"),
            ),
            "room",
        ),
        BoardSpec(
            "stairs_room",
            "阶梯房间",
            "15_stairs_room_v2.md",
            "把下一层风险、撤离和深入决策集中到下降口。",
            "地下石阶和黄铜升降门，下面是冷雾，上方仍有玩家灯光。",
            (
                z(680, 200, 560, 520, "下降口 / 阶梯", "视觉焦点是向下路径，危险感高于安全屋。", "scene", "violet"),
                z(145, 250, 430, 300, "下一层简报", "层级风险、特殊规则和推荐整备。", "detail"),
                z(1320, 250, 360, 300, "携带风险", "背包容量、负重、损失预估。", "status"),
                z(310, 735, 820, 115, "底部背包工作台", "最后整理，不展开过多功能按钮。", "inventory", "green"),
                z(1300, 740, 320, 100, "深入下一层", "主行动；撤离是较小次行动。", "action"),
            ),
            "layer",
        ),
        BoardSpec(
            "order_board",
            "订单委托板",
            "16_order_board_v2.md",
            "把订单从业务列表改成公告板上的委托合同。",
            "木质公告板、羊皮纸合同、蜡封、奖励印章和截止日标记。",
            (
                z(135, 220, 500, 520, "合同钉板", "左侧钉着可选订单，只显示摘要和势力标记。", "list"),
                z(725, 210, 620, 520, "选中合同", "目标物、截止日、奖励、风险在一张大合同上。", "main"),
                z(1390, 260, 300, 220, "奖励 / 缺口", "奖励印章和提交缺口短提示。", "detail"),
                z(1390, 525, 300, 120, "势力关系", "声望、信任或背叛风险短条。", "status"),
                z(1320, 745, 320, 95, "接取 / 提交", "唯一主行动；放弃订单是危险折叠。", "action"),
            ),
            "board",
        ),
        BoardSpec(
            "rumor_board",
            "传闻行情板",
            "17_rumor_board_v2.md",
            "把价格波动和传闻变成情报桌，而不是行情表。",
            "桌面上有纸条、地图、红线、收据和黄铜小灯。",
            (
                z(140, 230, 490, 450, "传闻纸条", "左侧纸条只做选择摘要，不显示完整算法。", "list"),
                z(705, 220, 510, 380, "涨跌物品标签", "价格上升 / 下降用商品标签和箭头表达。", "main"),
                z(1285, 240, 390, 310, "选中传闻详情", "来源、可信度、推荐出售时机。", "detail"),
                z(720, 650, 500, 110, "推荐计划卡", "把传闻转成下一步经营建议。", "status"),
                z(1320, 740, 320, 95, "加入计划", "唯一主行动。", "action"),
            ),
            "board",
        ),
        BoardSpec(
            "faction_shop",
            "势力商店",
            "18_faction_shop_v2.md",
            "通过势力柜台、声望账本和货架区分正规与黑市。",
            "小镇势力柜台，官方货架明亮，黑市侧帘阴影更重。",
            (
                z(120, 245, 390, 360, "势力柜台", "势力徽记、声望章和信任等级集中。", "status"),
                z(575, 225, 545, 450, "商品货架", "货物像展示品而不是电子商城卡片。", "list"),
                z(1190, 245, 420, 300, "选中商品托盘", "价格、条件、库存、声望门槛。", "detail"),
                z(1280, 585, 330, 130, "黑市风险帘", "黑市风险视觉区分，不和正规购买同权重。", "danger"),
                z(1290, 760, 320, 95, "购买", "唯一主行动；黑市动作二次确认。", "action"),
            ),
            "shop",
        ),
        BoardSpec(
            "doll_interaction",
            "魔偶互动",
            "19_doll_interaction_v2.md",
            "把互动从功能菜单改成围绕魔偶的情感场景。",
            "温暖房间中魔偶安静坐着，礼物、茶杯、护理工具自然放置。",
            (
                z(730, 230, 500, 470, "魔偶中心舞台", "角色不被按钮覆盖，是情绪反馈核心。", "scene", "rose"),
                z(190, 275, 380, 300, "互动工具环", "触摸、对话、赠礼、护理以图标环绕，不堆文字按钮。", "tab"),
                z(1300, 275, 360, 300, "礼物 / 话题托盘", "当前可用礼物和话题作为托盘选择。", "list"),
                z(470, 735, 980, 105, "反馈对白", "短反馈气泡，避免长文本墙。", "detail"),
                z(1335, 760, 300, 85, "确认互动", "需要确认时才出现主行动。", "action"),
            ),
            "doll",
        ),
        BoardSpec(
            "scenario_event",
            "剧情事件",
            "20_scenario_event_v2.md",
            "把事件做成叠在当前场景上的清晰故事卡。",
            "背景场景压暗，中间羊皮纸事件卡，选择和结果分层。",
            (
                z(210, 245, 390, 360, "事件焦点", "角色头像、势力徽记或关键物件。", "scene", "rose"),
                z(680, 235, 790, 260, "对白阅读区", "大而干净的阅读面，不被按钮围住。", "main"),
                z(680, 535, 790, 170, "选择卡", "最多 2-3 个选项，后果短提示。", "list"),
                z(680, 740, 480, 80, "结果印章条", "系统结果用短章和图标表达。", "status"),
                z(1240, 735, 300, 95, "继续", "唯一主行动；跳过是低权重小入口。", "action"),
            ),
            "event",
        ),
        BoardSpec(
            "doll_room",
            "魔偶房间",
            "21_doll_room_v2.md",
            "把长期状态、纪念物和日记融入一个安静生活房间。",
            "像视觉日记一样的房间：窗、床、桌、纪念架和魔偶待机中心。",
            (
                z(760, 250, 470, 440, "魔偶待机中心", "魔偶是房间情绪中心，不像仓库展示架。", "scene", "rose"),
                z(175, 250, 390, 240, "窗外状态 / 日记桌", "天气、日期、短日记自然嵌入房间。", "detail"),
                z(1280, 230, 380, 430, "纪念物架", "纪念物像摆件、挂件或贴纸，不是仓库槽。", "list"),
                z(410, 725, 980, 95, "低声反馈", "观察反馈短、轻，不打断房间感。", "detail"),
                z(1430, 760, 240, 80, "返回", "低权重动作，避免把房间变菜单。", "action"),
            ),
            "doll",
        ),
    ]


def write_readme(out_dir: Path, generated: list[dict[str, str]]) -> None:
    lines = [
        "---",
        "id: art_ui_formal_v2_design_boards",
        "title: Formal V2 UI 结构设计图",
        "type: art",
        "role: 美术",
        "domain: ui_design",
        "status: draft",
        "source_of_truth: false",
        "related:",
        "  - 美术文档/ui_design/formal_v2/README.md",
        "  - 美术文档/ui_design/formal_v2/00_formal_v2_ux_ui_overview.md",
        "  - 美术文档/ui_design/formal_v2/concepts/README.md",
        "  - tools/美术工具/README.md",
        "last_verified: 2026-06-02",
        "update_rule: 新增或替换 Formal V2 UI 结构设计图时同步本文件。",
        "---",
        "",
        "# Formal V2 UI 结构设计图",
        "",
        "本目录存放 Formal V2 的结构设计图。它们用于评审界面布局、视觉重心、主行动和信息层级，不是运行时素材，不进入 `UnityClient/Assets/Art/Approved`，不写入 Manifest，也不作为程序接入规格。",
        "",
        "这些图与 `../concepts/` 的 AI 概念参考图分工不同：`concepts/` 主要看氛围和画面方向；本目录主要看功能结构和迁移前的 UI 骨架。",
        "",
        "## 当前文件",
        "",
        "| 文件 | ScreenID | 对应文档 | 说明 |",
        "|---|---|---|---|",
    ]
    for item in generated:
        lines.append(
            f"| `{item['file']}` | `{item['screen_id']}` | `{item['doc']}` | {item['title']} |"
        )
    lines.extend(
        [
            "",
            "## 使用规则",
            "",
            "1. 只用于 Formal V2 草案评审和 active 迁移前沟通。",
            "2. 图中文字和控件命名是结构标注，不是最终 UI 文案。",
            "3. 若用户确认某个界面，仍需先更新 active `screen_layouts.json` 并通过 `Validate-UIDesign.ps1`。",
            "4. 程序侧不得直接按本目录 PNG 接入 Unity；正式接入口仍是 active UI 规格和美术交接清单。",
        ]
    )
    (out_dir / "README.md").write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--out-dir", default=DEFAULT_OUT_DIR)
    parser.add_argument("--overwrite", action="store_true")
    args = parser.parse_args()

    out_dir = Path(args.out_dir)
    if not out_dir.is_absolute():
        out_dir = PROJECT_ROOT / out_dir
    out_dir.mkdir(parents=True, exist_ok=True)

    generated: list[dict[str, str]] = []
    for spec in build_specs():
        filename = f"{spec.screen_id}_formal_v2_design_board.png"
        out_path = out_dir / filename
        render_board(spec, out_path, overwrite=args.overwrite)
        generated.append(
            {
                "screen_id": spec.screen_id,
                "title": spec.title,
                "doc": spec.doc,
                "file": filename,
                "path": repo_path(out_path),
                "companion": "true" if spec.companion else "false",
            }
        )
        print(f"[BOARD] {spec.screen_id} -> {repo_path(out_path)}")

    payload = {
        "Version": 1,
        "GeneratedAt": "2026-06-02",
        "Purpose": "Formal V2 UI structure design boards; not runtime assets.",
        "Boards": generated,
    }
    (out_dir / "formal_v2_design_boards.json").write_text(
        json.dumps(payload, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    write_readme(out_dir, generated)
    print(f"[DONE] boards={len(generated)} out={repo_path(out_dir)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
