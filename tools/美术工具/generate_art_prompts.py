# -*- coding: utf-8 -*-
"""Fill bilingual art descriptions and structured specs for manifest entries."""

from __future__ import annotations

import argparse
import copy
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
    "StairsNode": "stairs map node symbol, descending stone stairway or metal ladder opening, clear downward gateway shape, high contrast",
    "EventNode": "event map node symbol, small brass scroll and question-mark-shaped torn ribbon, mysterious but calm silhouette, high contrast",
    "HazardNode": "hazard map node symbol, triangular brass warning plate with cracked toxic crystal, sharp danger silhouette, high contrast",
    "RestStopNode": "rest stop map node symbol, small warm lantern and folded blanket inside a protective brass ring, calm shelter silhouette, high contrast",
    "TreasureNode": "treasure map node symbol, small locked brass supply chest with gear clasp and soft gold glow, readable reward silhouette, high contrast",
}

NODE_CN: Dict[str, str] = {
    "CombatNode": "战斗节点图标，用交叉刀痕、爪痕或破损武器徽记表达危险。",
    "SafeRoomNode": "安全区节点图标，用庇护灯、维修灯或保护圆环表达休整。",
    "BossNode": "首领节点图标，用重型警告徽记、封闭门禁或裂隙眼形标记表达压迫感。",
    "StairsNode": "阶梯节点图标，用向下台阶、竖井入口或金属梯口表达进入下一层的通道感。",
    "EventNode": "事件节点图标，用黄铜卷轴和问号形破布表达未知事件，轮廓神秘但不危险。",
    "HazardNode": "危险节点图标，用三角黄铜警示牌和开裂污染水晶表达环境危害。",
    "RestStopNode": "休整节点图标，用暖色小灯和折叠毯子置于黄铜保护环中表达短暂庇护。",
    "TreasureNode": "宝箱节点图标，用带齿轮锁扣的黄铜补给箱和柔和金光表达奖励。",
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
    "layer_select": "underground departure gate chamber, arched mine entrance, brass route board, old warning lamps, empty central area for layer list, dark side walls, low visual noise",
    "safe_room": "quiet underground refuge room, small repair bench, warm lanterns, blankets, brass pipes, medicine cabinet, calm empty floor area, low visual noise",
    "settlement_defeat": "failed expedition result backdrop, dim underground return bay, damaged gear crates, broken lanterns, muted red warning lamps, empty central area, somber mood",
    "settlement_victory": "successful evacuation result backdrop, underground lift exit, warm town-side lamps, brass cargo scale, recovered supply crates, empty central area, calm relief mood",
    "stairs_room": "deep stairwell chamber, descending stone stairs and metal ladder rails, round hatch opening, old warning lamps, cavern darkness below, empty foreground floor",
    "workshop": "small mechanical repair workshop, workbench, hanging crane arm, tool wall, parts boxes, old fluorescent lamps, brass pipes, large negative space on both sides",
    "doll_room_attic": "small attic room for a mechanical doll, warm repair lamp, narrow bed, old wooden floor, brass pipes, diary desk, memento shelf, window to a deep underground town glow, calm empty center floor, low visual noise",
}

BACKGROUND_CN: Dict[str, str] = {
    "combat": "横版战斗背景，前景为空旷工业地面，中景有废弃金属平台、断裂管线和暗色洞穴雾气，左右留负空间。",
    "dungeon_map": "路线图底纹背景，低噪声暗色画面，裂石、旧黄铜管线、微弱矿灯和纵深洞穴感，大量负空间。",
    "layer_1": "浅层区域背景，废弃地下工业通道、旧金属墙、破损电缆、微弱暖灯和薄雾，危险感较低。",
    "layer_2": "污染矿带背景，腐蚀矿道、紫绿色毒液、破损采矿设备、酸雾和昏暗工作灯。",
    "layer_select": "层选择入口背景，地下出发闸门、拱形矿洞入口、黄铜路线牌、旧警示灯和空的中心列表区域。",
    "safe_room": "安全屋背景，安静的地下休整房间、小维修台、暖灯、毯子、黄铜管线和药柜，前景留空。",
    "settlement_defeat": "战败结算背景，昏暗地下返回区、损坏装备箱、破裂灯具、低饱和红色警示灯和空的中心区域。",
    "settlement_victory": "撤离成功结算背景，地下升降出口、温暖小镇侧灯光、黄铜货秤、回收物资箱和空的中心区域。",
    "stairs_room": "阶梯房间背景，向下延伸的石阶和金属梯栏、圆形舱口、旧警示灯和下方洞穴黑暗，前景留空。",
    "workshop": "工坊整备背景，小型机械维修工坊，工作台、吊臂、工具墙、零件箱、旧灯管和黄铜管线，两侧留负空间。",
    "doll_room_attic": "人偶阁楼房间背景，暖色维修灯、窄床、旧木地板、黄铜管线、日记桌、纪念物架和地下小镇窗光，中间留空。",
}

UI_EN = {
    "missing_sprite": "missing asset placeholder icon, simple broken-image symbol, dark base shape, red warning corner mark, clean readable silhouette",
    "panel_main": "large modular interface panel frame, dark worn metal body, brass corner plates, thin rivets, empty center area, subtle inner shadow",
    "panel_info": "small modular information panel frame, dark metal plate, brass trim, soft inset surface, empty center area, compact shape",
    "button_primary": "rectangular primary button skin, sturdy brass rim, dark metal center plate, soft amber highlight, empty label area",
    "button_secondary": "rectangular secondary button skin, subdued dark steel rim, cool blue-gray inset plate, empty label area",
    "button_danger": "rectangular warning button skin, worn dark metal, muted red enamel accents, brass rivets, empty label area",
    "list_row_normal": "horizontal list row plate, thin dark metal slab, subtle brass edge, empty center strip, low contrast",
    "list_row_selected": "horizontal selected list row plate, dark metal slab, brighter brass outline, soft amber edge glow, empty center strip",
    "title_divider": "thin decorative title divider, brass pipe line, small gear accents, symmetrical horizontal ornament",
    "icon_money": "small coin-value symbol, stacked brass coins and tiny gear stamp, clean silhouette",
    "icon_locked": "small lock symbol, brass padlock with worn metal shackle, clean silhouette",
    "icon_equipped": "small equipped check symbol, brass check mark over compact gear badge, clean silhouette",
    "icon_maintenance": "small maintenance symbol, crossed brass wrench and tiny pressure gauge over a worn gear badge, clean silhouette",
    "icon_wear_repair": "wear repair symbol, cracked brass gear being patched by a tiny wrench, blue-white repair glow, clean silhouette",
    "icon_corruption_purify": "corruption purification symbol, purple-green droplet inside a brass filter ring being cleansed by pale blue light, clean silhouette",
    "icon_dive_permit": "dive permit readiness symbol, brass departure gate badge with green signal lamp and check mark, clean silhouette",
    "icon_bill": "small invoice symbol, folded brass-edged paper slip with coin stamp and tiny gear mark, clean silhouette",
    "icon_income": "income symbol, stacked brass coins rising beside a small green upward spark, tiny gear stamp, clean silhouette",
    "icon_expense": "expense symbol, brass coin leaving a small worn ledger tray, muted red downward marker, clean silhouette",
    "icon_debt_rent": "rent debt pressure symbol, folded invoice pinned by a brass key and red wax seal, small coin mark, clean silhouette",
    "icon_warning": "small warning symbol, triangular brass hazard plate with subtle red enamel edge, clean silhouette",
    "icon_chassis_upgrade": "chassis upgrade symbol, brass upward arrow rising from a reinforced backpack frame badge, small gear teeth, clean silhouette",
    "icon_blueprint": "blueprint unlock symbol, folded blue technical plan with brass corner clips and tiny gear stamp, clean silhouette",
    "icon_material_need": "material shortage symbol, cracked supply crate with a small brass warning marker and missing part silhouette, clean silhouette",
    "icon_customer": "customer flow symbol, small brass visitor silhouette beside a shop counter badge, simple profile shape, clean silhouette",
    "icon_sale_spark": "sale feedback burst symbol, bright brass coin spark with small radial rays and translucent amber impact flare, clean silhouette",
    "icon_business_settlement": "business settlement symbol, small brass shop counter with coin tray and tiny gear stamp, clean silhouette",
    "icon_shop_channel": "ordinary shop or export channel symbol, small brass counter stall or shipping tray badge, clean silhouette",
    "icon_black_market": "black market risk symbol, dark brass mask or hidden contraband crate badge, muted red accent, clean silhouette",
    "icon_order": "commission order symbol, pinned request slip or stamped parcel badge, clean silhouette",
    "icon_faction": "faction reputation symbol, brass heraldic gear crest, clean silhouette",
    "icon_reputation": "reputation rank symbol, brass laurel gear crest with small progress notches, clean silhouette",
    "icon_trust": "black market trust symbol, half-hidden brass handshake badge with muted red seal, clean silhouette",
    "icon_deadline": "deadline and time pressure symbol, small brass hourglass or clock badge, clean silhouette",
    "icon_rumor": "rumor and intel symbol, folded note with small listening horn or whisper mark, clean silhouette",
    "icon_price_up": "price increase symbol, brass upward arrow over coin and gear badge, clean silhouette",
    "icon_price_down": "price decrease symbol, brass downward arrow over coin and gear badge, clean silhouette",
    "icon_touch": "touch interaction symbol, small gloved hand hovering over a brass pulse ring, clean silhouette",
    "icon_talk": "talk interaction symbol, brass speech bubble with tiny gear rivets, clean silhouette",
    "icon_gift": "gift interaction symbol, small wrapped parcel with brass ribbon and tiny gear tag, clean silhouette",
    "icon_memento": "memento memory symbol, small locket or keepsake gear charm with soft blue glow, clean silhouette",
    "icon_event": "scenario event symbol, small theater curtain badge with brass exclamation mark, clean silhouette",
    "icon_lore": "lore archive symbol, old brass-edged book and small crystal bookmark, clean silhouette",
    "icon_skip": "skip summary symbol, fast-forward brass double arrow over a small note sheet, clean silhouette",
    "icon_diary": "diary and memory log symbol, small brass-edged notebook with ribbon bookmark and tiny gear clasp, clean silhouette",
    "room_memento_slot": "memento display slot, small empty brass shelf frame with soft inset backing and tiny screw corners, clean silhouette",
    "inventory_slot_available": "square inventory grid slot, dark metal recessed tile, thin brass rim, empty center, subtle bevel",
    "inventory_slot_locked": "square locked inventory grid slot, dark sealed metal tile, diagonal brace, tiny lock emblem, empty center",
    "inventory_slot_hover": "square inventory grid slot hover state, dark recessed tile, brighter brass rim, soft amber outline, empty center",
    "inventory_slot_valid": "square inventory grid slot valid placement state, dark recessed tile, green-blue edge glow, clean empty center",
    "inventory_slot_invalid": "square inventory grid slot invalid placement state, dark recessed tile, muted red edge warning, clean empty center",
    "inventory_chassis_panel": "large backpack grid support panel, mechanical base plate, open central grid area, brass rails, worn screws, low visual clutter",
    "loot_pickup_panel": "large loot pickup panel frame, dark workshop metal, brass corner clamps, empty list area, subtle industrial texture",
    "loot_drop_zone": "loot drop zone tray, shallow metal basin, worn brass rim, faint grid texture, empty center area",
    "combat_enemy_card": "enemy portrait card frame, square dark metal frame, brass clamps, small status sockets, empty portrait window",
    "combat_enemy_card_selected": "selected enemy portrait card frame, square dark metal frame, brighter brass clamps, amber target glow, empty portrait window",
    "combat_status_bar_hp": "horizontal health bar skin, dark metal track, muted red fill channel, brass end caps, empty center channel",
    "combat_status_bar_shield": "horizontal shield bar skin, dark metal track, cool blue fill channel, brass end caps, empty center channel",
    "combat_ap_pip": "small action point pip, compact brass-and-blue energy bead, circular mechanical token, clean silhouette",
    "combat_turn_banner": "wide turn banner frame, dark metal ribbon, brass pipe ends, empty center area, subtle amber glow",
    "combat_entity_shadow": "soft transparent elliptical floor shadow, subtle dark edge, centered empty interior, no hard border",
    "combat_target_ring": "elliptical target selection ring, thin brass mechanical outline, soft amber glow, transparent center",
    "combat_intent_attack": "attack warning emblem, sharp red claw slash over a small brass gear badge, angular impact shape, clean silhouette",
    "combat_intent_defend": "defensive warning emblem, compact brass shield plate with blue edge light and tiny rivets, clean silhouette",
    "combat_intent_buff": "empowerment emblem, upward brass arrow wrapped by warm golden energy coil, clean silhouette",
    "combat_intent_debuff": "weakening emblem, downward broken brass arrow with cold purple haze, clean silhouette",
    "combat_intent_grid_lock": "sealed square tile emblem, crossed brass clamps over a dark grid cell, muted red lock glow, clean silhouette",
    "combat_intent_add_junk": "clutter warning emblem, cracked scrap chunk falling into a small metal tray, green toxic spark, clean silhouette",
    "combat_intent_move_item": "displacement warning emblem, four brass direction arrows around a small crate, clean silhouette",
    "combat_intent_san_pressure": "mental pressure emblem, cracked blue-purple crystal inside a brass pressure gauge ring, clean silhouette",
    "combat_intent_charge": "charge-up warning emblem, glowing capacitor coil and compressed spring, amber energy buildup, clean silhouette",
    "combat_intent_unknown": "unknown action emblem, obscured dark brass eye badge under a small torn veil, clean silhouette",
    "combat_status_corrosion": "corrosion status emblem, green acid droplet eating into a brass plate, clean silhouette",
    "combat_status_curse": "curse status emblem, dark purple cracked talisman plate wrapped by small brass wires, clean silhouette",
    "combat_status_stun": "stun status emblem, tilted brass gear with blue spark burst, clean silhouette",
    "combat_feedback_hit": "hit feedback overlay, sharp translucent red-orange impact burst with brass spark fragments, transparent center gaps",
    "combat_feedback_shield_break": "shield break feedback overlay, cracked blue shield shards with brass spark fragments, transparent gaps",
    "combat_grid_lock_marker": "square cell overlay marker, crossed brass clamp frame with muted red warning glow, transparent center",
    "combat_junk_preview_marker": "square cell preview marker, faint green toxic stain and small scrap warning outline, transparent center",
    "dungeon_node_plate": "round map node backing plate, dark metal disk, brass ring, small screw marks, empty center",
    "dungeon_route_line": "thin map route connector line, brass pipe segment with small rivets, horizontal tileable strip",
    "settlement_victory_panel": "large evacuation success panel frame, dark metal body, warm brass trim, subtle green-blue signal light, empty center area",
    "settlement_defeat_panel": "large defeat result panel frame, dark damaged metal body, muted red warning trims, cracked brass corners, empty center area",
}

UI_CN = {
    "missing_sprite": "缺失资源占位图，破损图片符号、暗色底形和红色警示角标，清楚但不刺眼。",
    "panel_main": "主弹窗面板皮肤，大型模块化界面面板，暗色旧金属主体、黄铜角片、细铆钉和空的中心区域。",
    "panel_info": "小信息面板皮肤，暗金属板、黄铜细边、内凹表面和紧凑的空白内容区。",
    "button_primary": "主按钮皮肤，矩形黄铜边框、暗色金属中心板、柔和琥珀高光，中间不带文字。",
    "button_secondary": "次按钮皮肤，低调暗钢边框、冷蓝灰内嵌板，中间不带文字。",
    "button_danger": "危险按钮皮肤，旧暗金属、低饱和红色珐琅警示细节和黄铜铆钉，中间不带文字。",
    "list_row_normal": "普通列表行底板，横向暗金属薄板、黄铜细边、低对比空白条。",
    "list_row_selected": "选中列表行底板，横向暗金属板、较亮黄铜外轮廓和柔和琥珀边缘光。",
    "title_divider": "标题装饰线，黄铜管线、小齿轮点缀和对称横向装饰。",
    "icon_money": "金币价值图标，叠放黄铜硬币和小齿轮印记，轮廓清楚。",
    "icon_locked": "锁定图标，黄铜挂锁和磨损金属锁梁，轮廓清楚。",
    "icon_equipped": "已装备图标，黄铜确认标记叠在小齿轮徽章上，轮廓清楚。",
    "icon_maintenance": "维护图标，交叉黄铜扳手和小压力表叠在磨损齿轮徽章上，轮廓清楚。",
    "icon_wear_repair": "磨损修复图标，开裂黄铜齿轮被小扳手修补，带蓝白修复微光，轮廓清楚。",
    "icon_corruption_purify": "侵蚀净化图标，紫绿色污染液滴置于黄铜滤环中，被浅蓝净化光清除，轮廓清楚。",
    "icon_dive_permit": "下潜许可图标，黄铜出发闸门徽章、绿色信号灯和确认标记，轮廓清楚。",
    "icon_bill": "账单图标，带黄铜边的折叠票据、硬币印章和小齿轮标记，轮廓清楚。",
    "icon_income": "收入图标，叠放黄铜硬币旁有绿色上升火花和小齿轮印记，轮廓清楚。",
    "icon_expense": "支出图标，黄铜硬币从旧账本托盘滑出，带低饱和红色下行标记，轮廓清楚。",
    "icon_debt_rent": "月租债务图标，折叠账单被黄铜钥匙和红蜡封压住，带小硬币标记，轮廓清楚。",
    "icon_warning": "警告图标，三角黄铜危险牌和低饱和红色珐琅边，轮廓清楚。",
    "icon_chassis_upgrade": "底盘升级图标，黄铜上箭头从加固背包框徽章中升起，带小齿轮齿，轮廓清楚。",
    "icon_blueprint": "蓝图解锁图标，折叠蓝色技术图纸、黄铜角夹和小齿轮印章，轮廓清楚。",
    "icon_material_need": "材料缺口图标，开裂补给箱、小黄铜警告标记和缺失零件剪影，轮廓清楚。",
    "icon_customer": "顾客图标，小型黄铜访客剪影搭配店铺柜台徽章，侧影简洁，轮廓清楚。",
    "icon_sale_spark": "成交爆点图标，黄铜硬币火花、放射状短光线和半透明琥珀冲击光，轮廓清楚。",
    "icon_business_settlement": "营业结算图标，小型黄铜店铺柜台、硬币托盘和小齿轮印章，轮廓清楚。",
    "icon_shop_channel": "普通出货渠道图标，小型黄铜柜台或运输托盘徽章，轮廓清楚。",
    "icon_black_market": "黑市风险图标，暗黄铜面具或隐藏违禁品箱徽章，带低饱和红色点缀，轮廓清楚。",
    "icon_order": "订单委托图标，钉住的委托单或盖章包裹徽章，轮廓清楚。",
    "icon_faction": "势力声望图标，黄铜纹章齿轮徽记，轮廓清楚。",
    "icon_reputation": "声望等级图标，黄铜月桂齿轮徽章和小型进度刻痕，轮廓清楚。",
    "icon_trust": "黑市信任图标，半隐藏的黄铜握手徽章和低饱和红色封印，轮廓清楚。",
    "icon_deadline": "截止日和时间压力图标，小型黄铜沙漏或时钟徽章，轮廓清楚。",
    "icon_rumor": "传闻情报图标，折叠纸条搭配小听筒或低语符号，轮廓清楚。",
    "icon_price_up": "涨价图标，黄铜上箭头叠在硬币和齿轮徽章上，轮廓清楚。",
    "icon_price_down": "降价图标，黄铜下箭头叠在硬币和齿轮徽章上，轮廓清楚。",
    "icon_touch": "触摸交互图标，戴手套的手悬停在黄铜脉冲环上，轮廓清楚。",
    "icon_talk": "对话交互图标，黄铜对话气泡和小齿轮铆钉，轮廓清楚。",
    "icon_gift": "赠礼图标，小包裹、黄铜缎带和齿轮标签，轮廓清楚。",
    "icon_memento": "纪念物/记忆图标，小吊坠或齿轮纪念物，带柔和蓝光，轮廓清楚。",
    "icon_event": "剧本事件图标，小剧场帷幕徽章和黄铜感叹号，轮廓清楚。",
    "icon_lore": "Lore/档案图标，黄铜边旧书和小水晶书签，轮廓清楚。",
    "icon_skip": "跳过摘要图标，黄铜快进双箭头叠在小纸条上，轮廓清楚。",
    "icon_diary": "日记和记忆记录图标，黄铜边小笔记本、缎带书签和小齿轮锁扣，轮廓清楚。",
    "room_memento_slot": "纪念物展示槽，空的黄铜小展示架、柔和内嵌底板和小螺丝角，轮廓清楚。",
    "inventory_slot_available": "背包可用格，方形暗金属内凹格、细黄铜边框、空中心和轻微倒角。",
    "inventory_slot_locked": "背包锁定格，封闭暗金属格、斜向加固条、小锁符号和空中心。",
    "inventory_slot_hover": "背包悬停格，较亮黄铜边框和柔和琥珀外轮廓。",
    "inventory_slot_valid": "背包可放置反馈格，绿蓝色边缘光和干净空中心。",
    "inventory_slot_invalid": "背包不可放置反馈格，低饱和红色警示边缘和干净空中心。",
    "inventory_chassis_panel": "背包底盘面板，大型机械底板、开放网格中心区、黄铜导轨、磨损螺丝和低噪声纹理。",
    "loot_pickup_panel": "战利品拾取面板，暗色工坊金属、黄铜角夹、空列表区和轻微工业纹理。",
    "loot_drop_zone": "战利品掉落区，浅金属托盘、磨损黄铜边、淡网格纹理和空中心区域。",
    "combat_enemy_card": "敌人卡片框，方形暗金属框、黄铜夹具、小状态插槽和空头像窗。",
    "combat_enemy_card_selected": "敌人选中卡片框，较亮黄铜夹具、琥珀目标光和空头像窗。",
    "combat_status_bar_hp": "生命状态条皮肤，暗金属轨道、低饱和红色填充槽和黄铜端盖。",
    "combat_status_bar_shield": "护盾状态条皮肤，暗金属轨道、冷蓝填充槽和黄铜端盖。",
    "combat_ap_pip": "行动点圆点，小型黄铜与蓝色能量珠、圆形机械代币，轮廓清楚。",
    "combat_turn_banner": "回合提示条，宽暗金属条、黄铜管线端头、空中心和淡琥珀光。",
    "combat_entity_shadow": "战斗实体脚底阴影，柔和透明椭圆、暗色边缘、中心干净，不做硬边框。",
    "combat_target_ring": "目标选择光环，椭圆形黄铜机械线框、柔和琥珀光、中心透明。",
    "combat_intent_attack": "攻击意图图标，红色爪痕斩击叠在小黄铜齿轮徽章上，轮廓尖锐清楚。",
    "combat_intent_defend": "防御意图图标，小型黄铜盾牌、蓝色边缘光和细铆钉，轮廓清楚。",
    "combat_intent_buff": "增益意图图标，向上黄铜箭头缠绕暖色能量线圈，轮廓清楚。",
    "combat_intent_debuff": "弱化意图图标，断裂向下黄铜箭头和冷紫色雾气，轮廓清楚。",
    "combat_intent_grid_lock": "封格意图图标，被交叉黄铜夹具封住的暗色方格，带低饱和红色锁定光。",
    "combat_intent_add_junk": "塞包意图图标，破裂废料块落入小金属托盘，带绿色污染火花。",
    "combat_intent_move_item": "移位意图图标，小箱子周围有四向黄铜箭头，轮廓清楚。",
    "combat_intent_san_pressure": "SAN 压力意图图标，开裂蓝紫水晶位于黄铜压力表环中，轮廓清楚。",
    "combat_intent_charge": "蓄力意图图标，发光电容线圈和压缩弹簧，琥珀能量聚集。",
    "combat_intent_unknown": "未知意图图标，暗黄铜眼形徽章被小块破布遮住，轮廓清楚。",
    "combat_status_corrosion": "腐蚀状态图标，绿色酸液滴腐蚀黄铜板，轮廓清楚。",
    "combat_status_curse": "诅咒状态图标，暗紫开裂符牌被细黄铜线缠绕，轮廓清楚。",
    "combat_status_stun": "眩晕状态图标，倾斜黄铜齿轮和蓝色电火花爆点，轮廓清楚。",
    "combat_feedback_hit": "命中反馈覆盖图，半透明红橙冲击爆点和黄铜火花碎片，保留透明空隙。",
    "combat_feedback_shield_break": "破盾反馈覆盖图，开裂蓝色护盾碎片和黄铜火花碎片，保留透明空隙。",
    "combat_grid_lock_marker": "封格覆盖标记，方形格覆盖层、交叉黄铜夹具和低饱和红色警示光，中间透明。",
    "combat_junk_preview_marker": "塞包预告标记，方形格预览层、淡绿色污染痕和小废料警示轮廓，中间透明。",
    "dungeon_node_plate": "地图节点底板，圆形暗金属盘、黄铜环、小螺丝痕和空中心。",
    "dungeon_route_line": "地图路线连接线，黄铜管线段、小铆钉和横向可平铺条。",
    "settlement_victory_panel": "撤离成功结算面板，暗金属主体、暖黄铜边、微弱绿蓝信号灯和空中心。",
    "settlement_defeat_panel": "战败结算面板，暗色破损金属、低饱和红色警示边、破裂黄铜角和空中心。",
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


def make_spec(
    *,
    width: int,
    height: int,
    background: str,
    alpha_required: bool,
    display_width: int,
    display_height: int,
    safe_padding: int,
    subject_min: float,
    subject_max: float,
    post_process: list[str],
    preview_size: int,
    fit_mode: str = "contain",
    pivot: str = "center",
    anchor: str = "center",
    composition: str = "",
    safe_area: str | None = None,
    baseline_percent: int | None = None,
    nine_slice: Dict[str, Any] | None = None,
) -> Dict[str, Any]:
    composition_spec: Dict[str, Any] = {
        "SafePaddingPercent": safe_padding,
        "SubjectOccupancyMin": subject_min,
        "SubjectOccupancyMax": subject_max,
        "Anchor": anchor,
    }
    if composition:
        composition_spec["Composition"] = composition
    if safe_area:
        composition_spec["SafeArea"] = safe_area
    if baseline_percent is not None:
        composition_spec["BaselinePercent"] = baseline_percent

    process_spec: Dict[str, Any] = {
        "PostProcess": post_process,
        "PreviewSize": preview_size,
    }
    if nine_slice:
        process_spec["NineSlice"] = nine_slice

    return {
        "SourceSpec": {
            "Format": "png",
            "Width": width,
            "Height": height,
            "Background": background,
            "AlphaRequired": alpha_required,
        },
        "DisplaySpec": {
            "ReferenceResolution": "1920x1080",
            "DisplayWidth": display_width,
            "DisplayHeight": display_height,
            "Unit": "ui_px",
            "FitMode": fit_mode,
            "Pivot": pivot,
        },
        "CompositionSpec": composition_spec,
        "ProcessSpec": process_spec,
    }


def nine_slice(left: int, right: int, top: int, bottom: int) -> Dict[str, Any]:
    return {
        "Enabled": True,
        "Border": {
            "Left": left,
            "Right": right,
            "Top": top,
            "Bottom": bottom,
        },
    }


SPEC = {
    "item": make_spec(
        width=512,
        height=512,
        background="transparent",
        alpha_required=True,
        display_width=64,
        display_height=64,
        safe_padding=10,
        subject_min=0.74,
        subject_max=0.84,
        composition="centered single object",
        post_process=["resize", "trim_transparent_edges", "fit_safe_padding"],
        preview_size=64,
    ),
    "monster": make_spec(
        width=1024,
        height=1024,
        background="transparent_or_simple_dark",
        alpha_required=False,
        display_width=320,
        display_height=320,
        safe_padding=8,
        subject_min=0.78,
        subject_max=0.92,
        composition="bust portrait, front or three-quarter view",
        post_process=["crop_square", "resize"],
        preview_size=160,
    ),
    "monster_combat": make_spec(
        width=1024,
        height=1024,
        background="transparent",
        alpha_required=True,
        display_width=360,
        display_height=420,
        safe_padding=6,
        subject_min=0.74,
        subject_max=0.90,
        pivot="bottom_center",
        anchor="bottom_center",
        baseline_percent=92,
        composition="full-body battle stance, side or three-quarter side view",
        post_process=["trim_transparent_edges", "resize", "fit_safe_padding"],
        preview_size=220,
    ),
    "node": make_spec(
        width=512,
        height=512,
        background="transparent",
        alpha_required=True,
        display_width=80,
        display_height=80,
        safe_padding=12,
        subject_min=0.68,
        subject_max=0.80,
        composition="centered high-contrast symbol",
        post_process=["resize", "trim_transparent_edges", "fit_safe_padding"],
        preview_size=64,
    ),
    "prosthetic": make_spec(
        width=512,
        height=512,
        background="transparent",
        alpha_required=True,
        display_width=80,
        display_height=80,
        safe_padding=10,
        subject_min=0.72,
        subject_max=0.84,
        composition="centered single module",
        post_process=["resize", "trim_transparent_edges", "fit_safe_padding"],
        preview_size=80,
    ),
    "chassis": make_spec(
        width=1024,
        height=1024,
        background="transparent",
        alpha_required=True,
        display_width=512,
        display_height=512,
        safe_padding=6,
        subject_min=0.82,
        subject_max=0.94,
        composition="open-center mechanical frame",
        post_process=["resize", "trim_transparent_edges"],
        preview_size=256,
    ),
    "doll": make_spec(
        width=1024,
        height=1536,
        background="transparent",
        alpha_required=True,
        display_width=420,
        display_height=720,
        safe_padding=6,
        subject_min=0.86,
        subject_max=0.94,
        pivot="bottom_center",
        anchor="bottom_center",
        baseline_percent=94,
        composition="full-body standing pose",
        post_process=["crop_portrait", "resize", "fit_safe_padding"],
        preview_size=256,
    ),
    "background": make_spec(
        width=1920,
        height=1080,
        background="opaque_environment",
        alpha_required=False,
        display_width=1920,
        display_height=1080,
        fit_mode="cover",
        safe_padding=0,
        subject_min=0.0,
        subject_max=1.0,
        safe_area="center_4_3",
        composition="wide environment with negative space",
        post_process=["crop_16_9", "resize"],
        preview_size=320,
    ),
    "ui": make_spec(
        width=512,
        height=512,
        background="transparent",
        alpha_required=True,
        display_width=64,
        display_height=64,
        safe_padding=12,
        subject_min=0.68,
        subject_max=0.80,
        composition="centered placeholder symbol",
        post_process=["resize", "fit_safe_padding"],
        preview_size=64,
    ),
}

COMBAT_INTENT_ICON_SPEC = make_spec(
    width=512,
    height=512,
    background="transparent",
    alpha_required=True,
    display_width=64,
    display_height=64,
    safe_padding=10,
    subject_min=0.68,
    subject_max=0.84,
    composition="centered combat intent emblem",
    post_process=["resize", "trim_transparent_edges", "fit_safe_padding"],
    preview_size=64,
)

COMBAT_STATUS_ICON_SPEC = make_spec(
    width=512,
    height=512,
    background="transparent",
    alpha_required=True,
    display_width=48,
    display_height=48,
    safe_padding=10,
    subject_min=0.68,
    subject_max=0.84,
    composition="centered compact status emblem",
    post_process=["resize", "trim_transparent_edges", "fit_safe_padding"],
    preview_size=56,
)

COMBAT_FEEDBACK_OVERLAY_SPEC = make_spec(
    width=512,
    height=512,
    background="transparent",
    alpha_required=True,
    display_width=140,
    display_height=140,
    safe_padding=8,
    subject_min=0.62,
    subject_max=0.90,
    composition="centered transient combat feedback burst with transparent gaps",
    post_process=["resize", "preserve_transparency"],
    preview_size=128,
)

COMBAT_GRID_MARKER_SPEC = make_spec(
    width=256,
    height=256,
    background="transparent",
    alpha_required=True,
    display_width=100,
    display_height=100,
    safe_padding=6,
    subject_min=0.76,
    subject_max=0.96,
    composition="square inventory grid overlay marker with transparent center",
    post_process=["resize", "preserve_transparency"],
    preview_size=100,
)

UI_SPEC_BY_CONFIG: Dict[str, Dict[str, Any]] = {
    "missing_sprite": SPEC["ui"],
    "panel_main": make_spec(
        width=1024,
        height=768,
        background="transparent",
        alpha_required=True,
        display_width=960,
        display_height=640,
        safe_padding=5,
        subject_min=0.88,
        subject_max=0.98,
        fit_mode="stretch",
        composition="large empty-center nine-slice panel frame",
        post_process=["resize", "preserve_transparency", "check_nine_slice_edges"],
        preview_size=320,
        nine_slice=nine_slice(96, 96, 96, 96),
    ),
    "panel_info": make_spec(
        width=768,
        height=384,
        background="transparent",
        alpha_required=True,
        display_width=480,
        display_height=240,
        safe_padding=5,
        subject_min=0.88,
        subject_max=0.98,
        fit_mode="stretch",
        composition="compact empty-center nine-slice panel frame",
        post_process=["resize", "preserve_transparency", "check_nine_slice_edges"],
        preview_size=240,
        nine_slice=nine_slice(64, 64, 64, 64),
    ),
    "button_primary": make_spec(
        width=512,
        height=160,
        background="transparent",
        alpha_required=True,
        display_width=220,
        display_height=64,
        safe_padding=4,
        subject_min=0.88,
        subject_max=0.98,
        fit_mode="stretch",
        composition="rectangular label-free nine-slice button",
        post_process=["resize", "preserve_transparency", "check_nine_slice_edges"],
        preview_size=160,
        nine_slice=nine_slice(72, 72, 48, 48),
    ),
    "button_secondary": make_spec(
        width=512,
        height=160,
        background="transparent",
        alpha_required=True,
        display_width=220,
        display_height=64,
        safe_padding=4,
        subject_min=0.88,
        subject_max=0.98,
        fit_mode="stretch",
        composition="rectangular label-free nine-slice button",
        post_process=["resize", "preserve_transparency", "check_nine_slice_edges"],
        preview_size=160,
        nine_slice=nine_slice(72, 72, 48, 48),
    ),
    "button_danger": make_spec(
        width=512,
        height=160,
        background="transparent",
        alpha_required=True,
        display_width=220,
        display_height=64,
        safe_padding=4,
        subject_min=0.88,
        subject_max=0.98,
        fit_mode="stretch",
        composition="rectangular label-free nine-slice warning button",
        post_process=["resize", "preserve_transparency", "check_nine_slice_edges"],
        preview_size=160,
        nine_slice=nine_slice(72, 72, 48, 48),
    ),
    "list_row_normal": make_spec(
        width=1024,
        height=128,
        background="transparent",
        alpha_required=True,
        display_width=720,
        display_height=72,
        safe_padding=4,
        subject_min=0.88,
        subject_max=0.98,
        fit_mode="stretch",
        composition="horizontal nine-slice list row plate",
        post_process=["resize", "preserve_transparency", "check_nine_slice_edges"],
        preview_size=200,
        nine_slice=nine_slice(80, 80, 36, 36),
    ),
    "list_row_selected": make_spec(
        width=1024,
        height=128,
        background="transparent",
        alpha_required=True,
        display_width=720,
        display_height=72,
        safe_padding=4,
        subject_min=0.88,
        subject_max=0.98,
        fit_mode="stretch",
        composition="horizontal nine-slice selected list row plate",
        post_process=["resize", "preserve_transparency", "check_nine_slice_edges"],
        preview_size=200,
        nine_slice=nine_slice(80, 80, 36, 36),
    ),
    "title_divider": make_spec(
        width=1024,
        height=128,
        background="transparent",
        alpha_required=True,
        display_width=720,
        display_height=48,
        safe_padding=8,
        subject_min=0.70,
        subject_max=0.92,
        composition="thin horizontal decorative divider",
        post_process=["resize", "preserve_transparency"],
        preview_size=200,
    ),
    "icon_money": SPEC["ui"],
    "icon_locked": SPEC["ui"],
    "icon_equipped": SPEC["ui"],
    "icon_maintenance": SPEC["ui"],
    "icon_wear_repair": SPEC["ui"],
    "icon_corruption_purify": SPEC["ui"],
    "icon_dive_permit": SPEC["ui"],
    "icon_bill": SPEC["ui"],
    "icon_income": SPEC["ui"],
    "icon_expense": SPEC["ui"],
    "icon_debt_rent": SPEC["ui"],
    "icon_warning": SPEC["ui"],
    "icon_chassis_upgrade": SPEC["ui"],
    "icon_blueprint": SPEC["ui"],
    "icon_material_need": SPEC["ui"],
    "icon_customer": SPEC["ui"],
    "icon_sale_spark": make_spec(
        width=512,
        height=512,
        background="transparent",
        alpha_required=True,
        display_width=120,
        display_height=120,
        safe_padding=8,
        subject_min=0.62,
        subject_max=0.90,
        composition="centered sale feedback burst with transparent gaps",
        post_process=["resize", "preserve_transparency"],
        preview_size=96,
    ),
    "icon_business_settlement": SPEC["ui"],
    "icon_shop_channel": SPEC["ui"],
    "icon_black_market": SPEC["ui"],
    "icon_order": SPEC["ui"],
    "icon_faction": SPEC["ui"],
    "icon_reputation": SPEC["ui"],
    "icon_trust": SPEC["ui"],
    "icon_deadline": SPEC["ui"],
    "icon_rumor": SPEC["ui"],
    "icon_price_up": SPEC["ui"],
    "icon_price_down": SPEC["ui"],
    "icon_touch": SPEC["ui"],
    "icon_talk": SPEC["ui"],
    "icon_gift": SPEC["ui"],
    "icon_memento": SPEC["ui"],
    "icon_event": SPEC["ui"],
    "icon_lore": SPEC["ui"],
    "icon_skip": SPEC["ui"],
    "icon_diary": SPEC["ui"],
    "room_memento_slot": make_spec(
        width=512,
        height=512,
        background="transparent",
        alpha_required=True,
        display_width=120,
        display_height=120,
        safe_padding=8,
        subject_min=0.68,
        subject_max=0.92,
        composition="single empty display slot frame",
        post_process=["resize", "preserve_transparency"],
        preview_size=96,
    ),
    "inventory_slot_available": make_spec(
        width=256,
        height=256,
        background="transparent",
        alpha_required=True,
        display_width=100,
        display_height=100,
        safe_padding=8,
        subject_min=0.80,
        subject_max=0.96,
        composition="square inventory slot tile",
        post_process=["resize", "preserve_transparency"],
        preview_size=100,
    ),
    "inventory_slot_locked": make_spec(
        width=256,
        height=256,
        background="transparent",
        alpha_required=True,
        display_width=100,
        display_height=100,
        safe_padding=8,
        subject_min=0.80,
        subject_max=0.96,
        composition="square locked inventory slot tile",
        post_process=["resize", "preserve_transparency"],
        preview_size=100,
    ),
    "inventory_slot_hover": make_spec(
        width=256,
        height=256,
        background="transparent",
        alpha_required=True,
        display_width=100,
        display_height=100,
        safe_padding=8,
        subject_min=0.80,
        subject_max=0.96,
        composition="square highlighted inventory slot tile",
        post_process=["resize", "preserve_transparency"],
        preview_size=100,
    ),
    "inventory_slot_valid": make_spec(
        width=256,
        height=256,
        background="transparent",
        alpha_required=True,
        display_width=100,
        display_height=100,
        safe_padding=8,
        subject_min=0.80,
        subject_max=0.96,
        composition="square valid-placement inventory slot tile",
        post_process=["resize", "preserve_transparency"],
        preview_size=100,
    ),
    "inventory_slot_invalid": make_spec(
        width=256,
        height=256,
        background="transparent",
        alpha_required=True,
        display_width=100,
        display_height=100,
        safe_padding=8,
        subject_min=0.80,
        subject_max=0.96,
        composition="square invalid-placement inventory slot tile",
        post_process=["resize", "preserve_transparency"],
        preview_size=100,
    ),
    "inventory_chassis_panel": make_spec(
        width=1024,
        height=1024,
        background="transparent",
        alpha_required=True,
        display_width=512,
        display_height=512,
        safe_padding=5,
        subject_min=0.86,
        subject_max=0.98,
        composition="large open-center backpack grid support panel",
        post_process=["resize", "preserve_transparency"],
        preview_size=256,
    ),
    "loot_pickup_panel": make_spec(
        width=1024,
        height=768,
        background="transparent",
        alpha_required=True,
        display_width=960,
        display_height=640,
        safe_padding=5,
        subject_min=0.88,
        subject_max=0.98,
        fit_mode="stretch",
        composition="large empty-center nine-slice loot panel frame",
        post_process=["resize", "preserve_transparency", "check_nine_slice_edges"],
        preview_size=320,
        nine_slice=nine_slice(96, 96, 96, 96),
    ),
    "loot_drop_zone": make_spec(
        width=768,
        height=512,
        background="transparent",
        alpha_required=True,
        display_width=420,
        display_height=300,
        safe_padding=6,
        subject_min=0.82,
        subject_max=0.96,
        composition="empty tray-like drop zone panel",
        post_process=["resize", "preserve_transparency"],
        preview_size=220,
    ),
    "combat_enemy_card": make_spec(
        width=768,
        height=768,
        background="transparent",
        alpha_required=True,
        display_width=360,
        display_height=360,
        safe_padding=5,
        subject_min=0.86,
        subject_max=0.98,
        composition="square portrait card frame with empty center",
        post_process=["resize", "preserve_transparency"],
        preview_size=220,
    ),
    "combat_enemy_card_selected": make_spec(
        width=768,
        height=768,
        background="transparent",
        alpha_required=True,
        display_width=360,
        display_height=360,
        safe_padding=5,
        subject_min=0.86,
        subject_max=0.98,
        composition="square selected portrait card frame with empty center",
        post_process=["resize", "preserve_transparency"],
        preview_size=220,
    ),
    "combat_status_bar_hp": make_spec(
        width=512,
        height=96,
        background="transparent",
        alpha_required=True,
        display_width=220,
        display_height=28,
        safe_padding=4,
        subject_min=0.86,
        subject_max=0.98,
        fit_mode="stretch",
        composition="horizontal status bar track",
        post_process=["resize", "preserve_transparency", "check_nine_slice_edges"],
        preview_size=160,
        nine_slice=nine_slice(48, 48, 28, 28),
    ),
    "combat_status_bar_shield": make_spec(
        width=512,
        height=96,
        background="transparent",
        alpha_required=True,
        display_width=220,
        display_height=28,
        safe_padding=4,
        subject_min=0.86,
        subject_max=0.98,
        fit_mode="stretch",
        composition="horizontal status bar track",
        post_process=["resize", "preserve_transparency", "check_nine_slice_edges"],
        preview_size=160,
        nine_slice=nine_slice(48, 48, 28, 28),
    ),
    "combat_ap_pip": make_spec(
        width=256,
        height=256,
        background="transparent",
        alpha_required=True,
        display_width=24,
        display_height=24,
        safe_padding=12,
        subject_min=0.64,
        subject_max=0.82,
        composition="small circular action point token",
        post_process=["resize", "trim_transparent_edges", "fit_safe_padding"],
        preview_size=48,
    ),
    "combat_turn_banner": make_spec(
        width=1024,
        height=256,
        background="transparent",
        alpha_required=True,
        display_width=520,
        display_height=96,
        safe_padding=5,
        subject_min=0.86,
        subject_max=0.98,
        fit_mode="stretch",
        composition="wide empty-center banner frame",
        post_process=["resize", "preserve_transparency", "check_nine_slice_edges"],
        preview_size=240,
        nine_slice=nine_slice(96, 96, 64, 64),
    ),
    "combat_entity_shadow": make_spec(
        width=512,
        height=256,
        background="transparent",
        alpha_required=True,
        display_width=300,
        display_height=90,
        safe_padding=8,
        subject_min=0.74,
        subject_max=0.94,
        composition="soft elliptical floor shadow",
        post_process=["resize", "preserve_transparency"],
        preview_size=160,
    ),
    "combat_target_ring": make_spec(
        width=512,
        height=256,
        background="transparent",
        alpha_required=True,
        display_width=360,
        display_height=110,
        safe_padding=8,
        subject_min=0.74,
        subject_max=0.96,
        composition="elliptical target selection ring with transparent center",
        post_process=["resize", "preserve_transparency"],
        preview_size=180,
    ),
    "combat_intent_attack": COMBAT_INTENT_ICON_SPEC,
    "combat_intent_defend": COMBAT_INTENT_ICON_SPEC,
    "combat_intent_buff": COMBAT_INTENT_ICON_SPEC,
    "combat_intent_debuff": COMBAT_INTENT_ICON_SPEC,
    "combat_intent_grid_lock": COMBAT_INTENT_ICON_SPEC,
    "combat_intent_add_junk": COMBAT_INTENT_ICON_SPEC,
    "combat_intent_move_item": COMBAT_INTENT_ICON_SPEC,
    "combat_intent_san_pressure": COMBAT_INTENT_ICON_SPEC,
    "combat_intent_charge": COMBAT_INTENT_ICON_SPEC,
    "combat_intent_unknown": COMBAT_INTENT_ICON_SPEC,
    "combat_status_corrosion": COMBAT_STATUS_ICON_SPEC,
    "combat_status_curse": COMBAT_STATUS_ICON_SPEC,
    "combat_status_stun": COMBAT_STATUS_ICON_SPEC,
    "combat_feedback_hit": COMBAT_FEEDBACK_OVERLAY_SPEC,
    "combat_feedback_shield_break": COMBAT_FEEDBACK_OVERLAY_SPEC,
    "combat_grid_lock_marker": COMBAT_GRID_MARKER_SPEC,
    "combat_junk_preview_marker": COMBAT_GRID_MARKER_SPEC,
    "dungeon_node_plate": make_spec(
        width=512,
        height=512,
        background="transparent",
        alpha_required=True,
        display_width=96,
        display_height=96,
        safe_padding=10,
        subject_min=0.70,
        subject_max=0.88,
        composition="round map node backing plate",
        post_process=["resize", "trim_transparent_edges", "fit_safe_padding"],
        preview_size=80,
    ),
    "dungeon_route_line": make_spec(
        width=512,
        height=128,
        background="transparent",
        alpha_required=True,
        display_width=200,
        display_height=16,
        safe_padding=6,
        subject_min=0.70,
        subject_max=0.96,
        composition="thin horizontal route connector strip",
        post_process=["resize", "preserve_transparency"],
        preview_size=160,
    ),
    "settlement_victory_panel": make_spec(
        width=1024,
        height=768,
        background="transparent",
        alpha_required=True,
        display_width=900,
        display_height=600,
        safe_padding=5,
        subject_min=0.88,
        subject_max=0.98,
        fit_mode="stretch",
        composition="large empty-center nine-slice success panel frame",
        post_process=["resize", "preserve_transparency", "check_nine_slice_edges"],
        preview_size=320,
        nine_slice=nine_slice(96, 96, 96, 96),
    ),
    "settlement_defeat_panel": make_spec(
        width=1024,
        height=768,
        background="transparent",
        alpha_required=True,
        display_width=900,
        display_height=600,
        safe_padding=5,
        subject_min=0.88,
        subject_max=0.98,
        fit_mode="stretch",
        composition="large empty-center nine-slice defeat panel frame",
        post_process=["resize", "preserve_transparency", "check_nine_slice_edges"],
        preview_size=320,
        nine_slice=nine_slice(96, 96, 96, 96),
    ),
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
    asset_type = str(entry.get("AssetType", "asset"))
    visual_id = str(entry.get("VisualID", ""))
    detail_en = lookup(domain, config_id, True)
    detail_cn = lookup(domain, config_id, False)
    is_monster_combat = domain == "monster" and (
        asset_type in {"combat_sprite", "battle_stand", "stand"} or visual_id.endswith("_combat")
    )

    if domain == "background":
        prompt_en = f"{STYLE_EN}, environment background, {detail_en}, wide composition, atmospheric depth, low visual noise, balanced lighting, no characters, no text"
    elif domain == "monster":
        if is_monster_combat:
            prompt_en = f"{STYLE_EN}, full-body creature illustration, {detail_en}, side or three-quarter side view, grounded stance, full figure visible, clear silhouette, transparent background, no text"
        else:
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
        ui_kind = {
            "panel": "modular 2D interface panel sprite",
            "button": "modular 2D interface button sprite",
            "slot": "modular 2D inventory slot sprite",
            "bar": "modular 2D status bar sprite",
            "banner": "modular 2D banner sprite",
            "divider": "modular 2D decorative divider sprite",
            "effect_overlay": "small 2D interface feedback overlay sprite",
            "frame": "modular 2D interface frame sprite",
            "icon": "small 2D interface icon sprite",
            "shadow": "soft 2D interface shadow sprite",
            "ring": "2D interface target ring sprite",
        }.get(asset_type, "modular 2D interface sprite")
        prompt_en = f"{STYLE_EN}, {ui_kind}, {detail_en}, transparent background, clean silhouette, no text, no letters, no numbers"
    else:
        prompt_en = f"{STYLE_EN}, game item icon, {detail_en}, centered single object, clean readable silhouette, transparent background, no text"

    if is_monster_combat:
        detail_cn = detail_cn.replace("头像", "战斗实体站姿").replace("立绘", "战斗实体站姿")
    prompt_cn = f"{STYLE_CN}{detail_cn}"
    if domain == "ui":
        spec = UI_SPEC_BY_CONFIG.get(config_id, SPEC["ui"])
    elif is_monster_combat:
        spec = SPEC["monster_combat"]
    else:
        spec = SPEC.get(domain, SPEC["item"])
    negative = NEGATIVE.get(domain, NEGATIVE["item"])
    if is_monster_combat:
        negative = (
            "text, letters, numbers, watermark, logo, signature, busy background, cute mascot, friendly smile, "
            "excessive gore, cropped head, cropped feet, portrait crop, bust portrait, full environment scene, photorealistic animal photo"
        )
    return prompt_cn, prompt_en, negative, copy.deepcopy(spec)


def contains_forbidden_text(text: str, allow_cjk: bool = False) -> bool:
    lowered = text.lower()
    if not allow_cjk and re.search(r"[\u4e00-\u9fff]", text):
        return True
    return any(pattern.lower() in lowered for pattern in FORBIDDEN_PATTERNS)


def has_source_spec(spec: Any) -> bool:
    if not isinstance(spec, dict):
        return False
    source = spec.get("SourceSpec")
    if isinstance(source, dict):
        return bool(source.get("Width") and source.get("Height") and source.get("Format"))
    return bool(spec.get("Width") and spec.get("Height") and spec.get("Format"))


def spec_is_legacy(spec: Any) -> bool:
    return not has_source_spec(spec)


def should_fill(entry: Dict[str, Any], overwrite: bool) -> bool:
    if entry.get("Status") == "deprecated":
        return False
    if overwrite:
        return True
    prompt_en = str(entry.get("PromptEN", ""))
    return (
        entry.get("Status") == "todo"
        or not entry.get("PromptCN")
        or not entry.get("PromptEN")
        or not entry.get("NegativePromptEN")
        or spec_is_legacy(entry.get("Spec"))
        or "single readable game asset" in prompt_en
        or contains_forbidden_text(prompt_en)
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
        "> Generated by `tools/美术工具/Generate-ArtPrompts.ps1`. `PromptEN` is for image-generation tools; `PromptCN` is for human review.",
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
    parser.add_argument("--refresh-spec", action="store_true", help="Refresh Spec for existing non-deprecated entries without changing their status.")
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
    spec_refreshed = 0
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
        elif args.refresh_spec and entry.get("Status") != "deprecated":
            _, _, _, spec = prompt_for(entry)
            if entry.get("Spec") != spec:
                entry["Spec"] = spec
                spec_refreshed += 1
            skipped += 1
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
    print(f"Specs refreshed: {spec_refreshed}")
    print(f"Entries skipped: {skipped}")
    print(f"Manifest: {manifest_path.relative_to(root).as_posix()}")
    print(f"Prompt markdown: {prompt_markdown_path.relative_to(root).as_posix()}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
