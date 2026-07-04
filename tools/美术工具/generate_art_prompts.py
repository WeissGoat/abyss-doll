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
    "Japanese anime-inspired 2D game art, subterranean fantasy adventure, whimsical yet ominous, "
    "soft cel-shaded painterly rendering, clean linework, clear saturated accents, "
    "airy fantasy atmosphere, luminous cave flora, low visual noise"
)

STYLE_CN = "日系二次元地底奇幻冒险感，童话式好奇与深渊危险并存，干净线条、柔和赛璐珞阴影、明亮但不轻飘的色彩和微弱生物荧光。"

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
    "loot_rusty_coil": "rusty electromagnetic coil icon, stacked reddish old wire windings around a dark iron rod, two loose wire ends, corroded ceramic insulator base, clearly a machine part, not a lantern, not a lamp",
    "loot_toxic_filter": "polluted industrial filter cartridge icon, cracked square metal filter frame, dark pleated mesh, purple-green toxic residue leaking from the grid, clearly a replaceable filter part, not a jar, not a lamp",
    "mat_core_tier1": "small power core module icon, compact oval metal casing, exposed gear ring, stable blue-gold glowing energy cell in the center, heavy valuable machine part, not a lantern, not a lamp",
    "mat_core_tier2": "polluted tier two power core icon, compact cracked metal core module, purple-green corruption veins around a blue-gold energy cell, heavy machine part silhouette, not a lantern, not a lamp",
    "con_anchor_charm": "anchor charm consumable icon, small dark-metal anchor-shaped talisman tied with worn red cord, protective seal paper, tiny blue stabilizing glow, compact readable silhouette",
    "con_purifying_salt": "purifying salt icon, small clear glass vial filled with coarse white salt crystals, a few loose salt grains, paper seal and thin red cord, cool blue cleansing sparkle, not a lantern, not a lamp, clean compact silhouette",
    "con_solvent_spray": "solvent spray consumable icon, handheld dark-metal spray bottle with clear nozzle, small pressure gauge, pale cleaning mist plume, blue-green solvent liquid, clear spray-can silhouette",
    "con_stabilizer_ampoule": "stabilizer ampoule consumable icon, slim glass ampoule inside a small dark-metal safety frame, blue-purple calming fluid, tiny cork seal, readable medicine silhouette",
    "gear_chain_hook": "chain hook gear icon, heavy iron hook attached to short dark chain links, worn grip ring, rust scratches, strong curved hook silhouette",
    "gear_corroded_bulwark": "corroded bulwark shield icon, thick rusted tower shield plate, pitted holes, green corrosion stains, reinforced dark rim, battered defensive silhouette, no handle lamp shape",
    "gear_mycelium_cloak": "mycelium cloak icon, folded cloth cloak with pale fungal fiber edges, mushroom-thread embroidery, small shoulder clasp, soft cream and moss colors, flowing fabric silhouette",
    "gear_spore_lance": "spore lance weapon icon, long slender spear shaft with sharp lance tip, swollen spore pod below the blade, fungal thorns, diagonal weapon silhouette, clearly a spear",
    "gear_vein_sickle": "vein sickle weapon icon, crescent sickle blade with red vein-like channels, short wrapped handle, small dark joint, sharp curved weapon silhouette",
    "loot_acid_gland": "acid gland loot icon, translucent organic gland sac filled with yellow-green acid bubbles, dripping corrosive liquid, soft membrane veins, biological pouch silhouette",
    "loot_corroded_nerve": "corroded nerve loot icon, twisted organic nerve strand threaded with green corrosion crystals, wet fibrous texture, small dark sample clamp, readable coiled strand silhouette",
    "loot_crystal_scale": "crystal scale loot icon, single translucent blue-violet scale shard, chipped mineral edge, faint inner glow, small paper sample tag, clean angular silhouette",
    "loot_living_mycelium": "living mycelium loot icon, pale fungal root bundle curling around a tiny dark ring, soft spores, threadlike fibers, readable organic bundle silhouette",
    "loot_spore_amber": "spore amber loot icon, irregular honey amber resin shard with visible trapped spores inside, chipped fossil-resin edges, tiny moss flecks, clearly a gemstone-like amber chunk, not a lantern, not a lamp",
    "loot_vein_plate": "vein plate loot icon, flat broken biological armor plate, red vein channels crossing the surface, chipped dark mounting pins, dark organic shell texture, clearly a plate shard, not a lantern",
    "loot_warped_plate": "warped plate loot icon, bent corroded metal armor plate, twisted torn edge, purple-green stains, broken rivets, flattened damaged metal silhouette, not a lantern, not a vessel",
    "mat_core_tier2_fragment": "tier two broken machine-core fragment icon, jagged cracked blue-gold energy core shard, angular mechanical casing pieces, exposed tiny gears and wires, clearly a broken core fragment, not an intact lamp",
    "mat_core_tier3_seed": "tier three machine-core seed icon, seed-shaped mechanical core embryo inside a translucent crystal shell, tiny root-like wires, blue-gold energy seams, compact valuable core silhouette, not a lantern",
    "order_contested_spore_core": "contested spore core item icon, glowing fungal core clamped between two dark claim hooks, red wax risk seal on the side, spores suspended inside the core, unique round core silhouette, not a lantern",
    "order_live_spore_cage": "live spore cage item icon, small square dark-metal capture cage with visible bars, floating green spores inside, glass safety tube on one side, tiny latch, clearly a containment cage, not a teapot",
    "trade_luminous_fungus": "luminous fungus trade item icon, cluster of small glowing cave mushrooms growing from a short root base, pale blue-green caps, soft spores, readable mushroom cluster silhouette, not a lamp",
    "trade_miner_lamp": "old miner headlamp trade item icon, compact worn dark-metal helmet lamp with cracked glass lens and short leather strap, mining tool silhouette, not a tall lantern, not a coil",
    "trade_cracked_relic": "cracked relic medallion trade item icon, broken stone holy medallion with a visible central crack, dark rim, faint purple curse glow from the crack, flat relic silhouette, not a filter jar",
    "trade_sealed_relic_box": "sealed relic box trade item icon, small dark lockbox wrapped with red cord and wax seal, old corner plates, square chest silhouette, mysterious relic glow leaking from seams, not a lantern",
    "trade_singing_fossil": "singing fossil trade item icon, spiral shell fossil stone with dark tuning fork charm, faint blue sound-wave rings around the shell, chipped stone texture, readable fossil silhouette, not a lamp",
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
    "loot_rusty_coil": "生锈线圈材料图标，红褐旧导线线圈、铁芯、断线和锈蚀外壳。",
    "loot_toxic_filter": "污染滤芯图标，工业滤芯破裂外壳、紫绿色污染残留和腐蚀金属。",
    "mat_core_tier1": "一阶动力核心图标，小型金属核心装置，内部有稳定蓝金色发光核心。",
}

MONSTER_EN: Dict[str, str] = {
    "mob_scavenger_bug": "small scavenger insect creature, scrap-metal shell plates, pincer mouthparts, tiny mechanical fragments attached, low-threat silhouette",
    "elite_scrap_guard": "heavy scrap-metal guardian, bulky welded helmet and shoulder armor, single red sensor eye, defensive intimidating silhouette",
    "mob_acid_slime": "acidic slime creature, semi-transparent corrosive body, trapped metal debris and bubbles inside, purple-green glow, readable blob silhouette",
    "elite_mutant_amalgam": "mutant amalgam creature, fused organic mass and broken machinery, multiple asymmetrical limbs, purple-green contamination marks, exposed metal bones",
    "boss_gatekeeper_mk1": "massive mechanical gatekeeper monster, dark iron armored body, heavy shield arms, single red sensor eye, old gate-lock machinery, imposing guardian silhouette",
    "boss_mycelium_oracle": "mycelium oracle boss monster, tall fungal priest-like creature, crown of pale mushrooms, dangling root tendrils, glowing blue-green spores, mysterious readable silhouette",
    "boss_spore_foundry": "large spore-forge monster portrait, upper body fills most of the frame, fungal furnace body, glowing molten core in the chest, heavy dark vents, mushroom growths, clear head-and-shoulder silhouette, readable face-like focal point",
    "elite_crystal_bulwark": "elite crystal bulwark monster, bulky creature covered in blue crystal plates and dark braces, shield-like shoulders, heavy defensive silhouette",
    "elite_spore_matriarch": "elite spore matriarch monster, large fungal brood mother, layered mushroom cap crown, egg-like spore sacs, many root tendrils, commanding silhouette",
    "elite_vein_knight": "elite vein knight monster, armored knight-like creature with dark organic armor, red vein channels, corroded blade arm, dark metal fragments, sharp warrior silhouette",
    "mob_crystal_guard": "crystal guard monster, medium cave sentinel with blue crystal armor shards, squat armored body, dark restraint bands, clear defensive silhouette",
    "mob_lost_miner_echo": "lost miner echo monster, ghostly miner figure with cracked helmet light, torn work clothes, dark pickaxe fragment, pale blue echo glow, melancholy silhouette",
    "mob_mycelium_crawler": "mycelium crawler monster, low crawling fungal creature, pale root legs, mushroom caps on the back, damp cave body, clear low silhouette",
    "mob_nerve_midge_swarm": "nerve midge swarm monster, clustered glowing insects forming one readable cloud, red nerve-thread trails, small dark debris caught inside, swarm silhouette",
    "mob_rust_cultivator": "rust cultivator monster, hunched cave farmer-like creature grown into rusty tools, corroded hoe arm, fungus basket shell, earthy silhouette",
    "mob_rust_hound": "rust hound monster, lean mechanical beast with rusty metal ribs, sharp snout, red sensor eye, four-legged hunting stance, clear animal silhouette",
    "mob_acid_slime_mature": "mature acid slime portrait, large semi-transparent gelatinous body fills the frame, visible acid bubbles, suspended corroded metal scraps, sagging slime folds, yellow-green glow, clear blob silhouette",
    "mob_echo_pilgrim": "echo pilgrim monster portrait, hooded cave wanderer upper body fills the frame, faceless dark hood, small blue crystal charm held close to the chest, torn cloth, dark keepsakes, clear head-and-shoulder silhouette",
    "mob_shell_grafter": "shell grafter monster, hunched creature grafting broken shells and metal plates onto its body, asymmetrical carapace, small tool-like claws, readable upper-body silhouette",
    "mob_soul_midge_swarm": "soul midge swarm monster, cloud of pale blue ghostly midges forming a large face-like silhouette, tiny wings and soft spirit glow, readable swarm mass",
    "mob_spore_archer": "spore archer monster, fungal humanoid archer, curved bow grown from mushroom wood, spore quiver, hood of mushroom caps, clear ranged-attacker silhouette",
}

MONSTER_CN: Dict[str, str] = {
    "mob_scavenger_bug": "拾荒虫头像，小型地底昆虫感，废铁甲壳、钳状口器和附着的机械碎片，威胁较低但不滑稽。",
    "elite_scrap_guard": "废铁守卫头像，焊接废铁头盔和肩甲，单眼红色感应器，厚重守门人压迫感。",
    "mob_acid_slime": "酸液软体头像，半透明腐蚀软泥，内部有金属碎片和气泡，紫绿色酸液发光。",
    "elite_mutant_amalgam": "畸变融合体头像，有机组织与破损机械融合，多肢不对称，紫绿色污染痕迹和外露金属骨架。",
}

NODE_EN: Dict[str, str] = {
    "CombatNode": "combat map node symbol, crossed blade marks or claw scratches, sharp aggressive shape, high contrast",
    "SafeRoomNode": "safe-room map node symbol, small glowing shelter crystal or repair beacon inside a protective circle, calm readable shape",
    "BossNode": "boss map node symbol, heavy warning emblem, sealed gate icon or large cracked eye-shaped mark, ominous high-contrast shape",
    "StairsNode": "stairs map node symbol, descending stone stairway or metal ladder opening, clear downward gateway shape, high contrast",
    "EventNode": "event map node symbol, small rune-marked scroll and question-mark-shaped torn ribbon, mysterious but calm silhouette, high contrast",
    "HazardNode": "hazard map node symbol, triangular stone warning plate with cracked toxic crystal, sharp danger silhouette, high contrast",
    "RestStopNode": "rest stop map node symbol, small glowing shelter crystal and folded blanket inside a protective rune circle, calm shelter silhouette, high contrast",
    "TreasureNode": "treasure map node symbol, small locked supply chest with carved clasp and soft crystal glow, readable reward silhouette, high contrast",
}

NODE_CN: Dict[str, str] = {
    "CombatNode": "战斗节点图标，用交叉刀痕、爪痕或破损武器徽记表达危险。",
    "SafeRoomNode": "安全区节点图标，用庇护晶石、维修信标或保护圆环表达休整。",
    "BossNode": "首领节点图标，用重型警告徽记、封闭门禁或裂隙眼形标记表达压迫感。",
    "StairsNode": "阶梯节点图标，用向下台阶、竖井入口或金属梯口表达进入下一层的通道感。",
    "EventNode": "事件节点图标，用符文卷轴和问号形破布表达未知事件，轮廓神秘但不危险。",
    "HazardNode": "危险节点图标，用三角石质警示牌和开裂污染水晶表达环境危害。",
    "RestStopNode": "休整节点图标，用庇护晶石和折叠毯子置于符文保护环中表达短暂庇护。",
    "TreasureNode": "宝箱节点图标，用带雕刻锁扣的补给箱和柔和晶石光表达奖励。",
}

PROSTHETIC_EN: Dict[str, str] = {
    "pros_cooling_system": "prosthetic cooling-system module, heat sink fins, coolant tubes, tiny pressure gauge, cold blue stabilizing light, compact machine part",
    "pros_power_arm": "prosthetic power-arm module, hydraulic joint, reinforced piston, mechanical fist connector, orange-red power cable, compact machine part",
    "prosthetic_anchor_left_arm": "mechanical anchor left-arm module, clamp fingers, reinforced wrist brace, small grid-lock stabilizer pins, heavy dark hinge, compact machine part",
    "prosthetic_charge_coil_arm": "charge-coil arm module, exposed reddish wire coil, compact capacitor core, reinforced forearm casing, blue-violet overload spark, compact machine part",
    "prosthetic_focus_lens": "focus-lens sensor module, round dark optic, layered glass lens, tiny scanning fins, soft blue detection glow, compact machine part",
    "prosthetic_mender_spine": "mender-spine prosthetic module, articulated spinal rail, small repair injectors, pale blue repair pulse, compact machine part",
    "prosthetic_salvage_fingertips": "precision salvage fingertip module, tiny articulated dark-metal fingers, small cutting tips, material scanner dot, compact machine part",
    "prosthetic_san_regulator_core": "SAN regulator core module, sealed dark pump, pressure gauge, small crystal stabilizer, blue-purple calming pulse, compact machine part",
}

PROSTHETIC_CN: Dict[str, str] = {
    "pros_cooling_system": "稳压散热插件图标，散热鳍片、冷却管线、小压力表和冷蓝稳定光。",
    "pros_power_arm": "动力臂增幅插件图标，液压关节、强化活塞、机械拳臂接口和橙红动力线。",
    "prosthetic_anchor_left_arm": "锚定左臂图标，机械夹爪、加固腕箍、背包格稳定插针和厚重暗色铰链，轮廓紧凑。",
    "prosthetic_charge_coil_arm": "蓄能线圈臂图标，外露红褐旧导线线圈、紧凑电容核心、加固前臂外壳和蓝紫过载火花。",
    "prosthetic_focus_lens": "裂隙聚焦镜图标，圆形暗色光学镜、层叠玻璃镜片、小型扫描鳍片和柔和蓝色侦测光。",
    "prosthetic_mender_spine": "修复脊索图标，分节脊柱导轨、小型修复注射器和浅蓝修复脉冲，轮廓紧凑。",
    "prosthetic_salvage_fingertips": "精密拾荒指图标，小型分节暗色金属手指、细切割尖端和材料扫描光点。",
    "prosthetic_san_regulator_core": "SAN 稳态调节核图标，密封暗色泵、小压力表、小型水晶稳压器和蓝紫安定脉冲。",
}

CHASSIS_EN: Dict[str, str] = {
    "chassis_lv1_basic": "basic backpack chassis frame, old workshop metal border, screws, worn corners, simple mechanical base plate, open center area",
    "chassis_lv2_expanded": "upgraded backpack chassis frame, sturdier metal border, reinforced side bars, upgrade connectors, precise mechanical details, open center area",
    "chassis_bulwark_carrier": "heavy carrier backpack chassis icon, broad reinforced cargo frame, thick side rails, anchor plates, sturdy dark corner blocks, clean readable silhouette",
    "chassis_compact_raider": "compact raider backpack chassis icon, light narrow frame, scout sensor fins, rounded extraction hooks, agile dark side rails, clean readable silhouette",
    "chassis_standard_frame": "standard workshop backpack chassis icon, balanced rectangular frame, simple dark rails, core mounting sockets, stable baseline silhouette",
}

CHASSIS_CN: Dict[str, str] = {
    "chassis_lv1_basic": "基础背包底盘框架，旧工坊金属边框、螺丝、磨损边角和简洁机械底板，中间留空。",
    "chassis_lv2_expanded": "升级背包底盘框架，更坚固的金属边框、加固侧条、升级接口和精密机械细节，中间留空。",
    "chassis_bulwark_carrier": "重载承运底盘图标，宽大的加固货架、厚侧轨、锚定板和结实暗色角块，轮廓稳定。",
    "chassis_compact_raider": "轻装掠行底盘图标，轻窄框架、侦察传感鳍片、圆形撤离挂钩和灵活暗色侧轨。",
    "chassis_standard_frame": "标准工坊底盘图标，均衡矩形框、简洁暗色导轨、核心安装插槽和稳定基准轮廓。",
}

MEMENTO_EN: Dict[str, str] = {
    "memento_boss1_lamp": "small old mining lamp keepsake, dark cage, cool blue flame core, scratched handle, quiet memory object, clean readable silhouette",
    "memento_first_chassis_frame": "miniature dark body-frame keepsake, small rectangular workshop chassis sample, tiny mounting sockets, worn screws, clean readable silhouette",
    "memento_first_prosthetic_case": "small opened prosthetic storage case, padded dark interior, dark corner clips, old paper wrapping without text, clean readable silhouette",
    "memento_first_repair_patch": "first repair patch keepsake, stitched cloth strip on a tiny dark plate, blue-white repair glow, worn screw corners, clean readable silhouette",
    "memento_layer2_corrosion_vial": "sealed corrosion sample vial keepsake, purple-green liquid, dark clamp frame, tiny filter cap, clean readable silhouette",
    "memento_miracle_burn_mark": "small charred dark token keepsake, circular scorch mark, faint blue-gold afterglow, cracked rim, clean readable silhouette",
    "memento_return_mark": "homecoming mark keepsake, small worn dark tag with repaired red thread and soft blue glow, clean readable silhouette",
    "memento_san_collapse_blanket": "folded recovery blanket keepsake, muted fabric roll with dark pin, pale blue calming glow, clean readable silhouette",
    "memento_blackmarket_letter": "sealed black-market letter keepsake, folded dark parchment packet, red wax seal, dark corner clip, no readable writing, clean readable silhouette",
    "memento_debt_shadow_window": "small cracked window-frame keepsake, dark debt notice shadow behind frosted glass, red wax pin, dark nail corners, no readable writing, clean readable silhouette",
    "memento_first_return_tag": "first return luggage tag keepsake, worn dark tag, repaired red cord loop, tiny stitched cloth strip, no readable writing, clean readable silhouette",
    "memento_low_san_blanket": "folded calming blanket keepsake, soft muted cloth roll, pale blue stitched patch, small dark safety pin, clean readable silhouette",
    "memento_pawn_empty_tag": "empty pawn-shop tag keepsake, blank paper price tag, snapped string loop, tiny coin dent, no readable writing, clean readable silhouette",
    "memento_truth_red_thread": "truth clue keepsake, bright red thread stretched between three dark evidence pins on a small cork-backed plate, clear red thread focus, no readable writing, clean readable silhouette",
    "memento_wall_crack_first_defeat": "first defeat wall-crack keepsake, small broken plaster shard, jagged dark crack, dark repair staple, muted red warning dust, clean readable silhouette",
}

MEMENTO_CN: Dict[str, str] = {
    "memento_boss1_lamp": "第一层首领后的矿灯纪念物，小型旧矿灯、暗色护笼、冷蓝灯芯和磨损提手，轮廓清楚。",
    "memento_first_chassis_frame": "首次底盘纪念物，小型暗色身体框样品、矩形工坊底盘、安装插槽和旧螺丝，轮廓清楚。",
    "memento_first_prosthetic_case": "首次义体纪念物，打开的小型义体收纳盒、深色软垫、暗色护角和无文字旧包装纸，轮廓清楚。",
    "memento_first_repair_patch": "首次修补纪念物，缝合布条固定在小暗色铭板上，带蓝白修复微光和磨损螺丝角，轮廓清楚。",
    "memento_layer2_corrosion_vial": "第二层腐蚀净化纪念物，密封腐蚀样本瓶、紫绿色液体、暗色夹架和小滤帽，轮廓清楚。",
    "memento_miracle_burn_mark": "首次奇迹纪念物，小型焦黑暗色圆牌、环形灼痕、蓝金余辉和开裂边缘，轮廓清楚。",
    "memento_return_mark": "高压路线归还纪念物，小型磨损暗色吊牌、修补红线和柔和蓝光，表达“回来了”的痕迹。",
    "memento_san_collapse_blanket": "首次 SAN 崩溃安抚纪念物，折叠的恢复毯、小暗色别针和浅蓝安定微光，轮廓清楚。",
}

RUMOR_EN: Dict[str, str] = {
    "rumor_acid_gland_shortage": "acid gland shortage rumor emblem, translucent green acid gland sac beside a cracked supply marker, small shortage notch, clean readable silhouette",
    "rumor_contraband_night_channel": "black-market night-channel rumor emblem, dark mask, hidden contraband crate, tiny crescent moon shape, muted red risk seal, clean readable silhouette",
    "rumor_corrosion_sample_premium": "corrosion sample premium rumor emblem, sealed purple-green vial beside stacked gold coins, small danger glow, clean readable silhouette",
    "rumor_crystal_scale_contract": "crystal scale contract rumor emblem, blue iridescent scale shards stacked beside a dark contract clamp, tiny coin sparkle, no readable paper, clean readable silhouette",
    "rumor_faction_medical_request": "medical faction request rumor emblem, dark medical supply case, pale blue purification droplet, small green signal spark, clean readable silhouette",
    "rumor_filter_shortage": "filter shortage rumor emblem, cracked round air-filter cartridge with pleated dark mesh, red warning triangle, missing filter slot silhouette, clean readable silhouette",
    "rumor_living_mycelium_shortage": "living mycelium shortage rumor emblem, pale glowing fungal root bundle in a small dark tray, dry broken strands, small shortage marker, clean readable silhouette",
    "rumor_low_layer_bulk_buy": "low-layer bulk purchase rumor emblem, small cargo crate filled with common stones and coins, cool market signal glow, clean readable silhouette",
    "rumor_miner_lamps": "miner lamp demand rumor emblem, three tiny mining headlamp silhouettes grouped on a dark supply tray, one lamp unlit, clean readable silhouette",
    "rumor_old_blades": "old blades demand rumor emblem, crossed worn short blades on a dark rack, chipped edges, muted collector coin, clean readable silhouette",
    "rumor_oracle_fossil_collector": "oracle fossil collector rumor emblem, spiral fossil stone held by dark research calipers, blue-purple oracle spark, clean readable silhouette",
    "rumor_origin_stone_demand": "origin stone demand rumor emblem, blue-gold crystal above stacked gold coins, small upward market arrow, clean readable silhouette",
    "rumor_scrap_workshop_shortage": "workshop scrap shortage rumor emblem, cracked gear and loose screws beside a small warning marker, clean readable silhouette",
    "rumor_slime_crash": "slime price crash rumor emblem, green slime droplet above a gold coin with a red downward marker, clean readable silhouette",
    "rumor_spore_amber_jeweler": "spore amber jeweler rumor emblem, honey-colored amber nugget with trapped glowing spores, dark jeweler loupe ring, clean readable silhouette",
    "rumor_weapon_collector_visit": "weapon collector visit rumor emblem, polished blade on a small dark display stand with collector coin sparkle, clean readable silhouette",
}

RUMOR_CN: Dict[str, str] = {
    "rumor_contraband_night_channel": "黑市夜间窗口传闻图标，暗色面具、隐藏违禁品箱、小月牙和低饱和红色风险封印，轮廓清楚。",
    "rumor_corrosion_sample_premium": "腐蚀样本溢价传闻图标，密封紫绿色样本瓶、金币和小型危险微光，轮廓清楚。",
    "rumor_faction_medical_request": "医疗势力需求传闻图标，暗色医疗补给箱、浅蓝净化液滴和绿色信号火花，轮廓清楚。",
    "rumor_low_layer_bulk_buy": "低层统购传闻图标，小型货箱中装有常见石块和硬币，带冷色市场信号光，轮廓清楚。",
    "rumor_origin_stone_demand": "源石需求传闻图标，蓝金晶石置于金币上方，带小型上涨箭头，轮廓清楚。",
    "rumor_scrap_workshop_shortage": "工坊废料短缺传闻图标，开裂齿轮、散落螺丝和小警告标记，轮廓清楚。",
    "rumor_slime_crash": "史莱姆跌价传闻图标，绿色软泥液滴压在金币上，带红色下行标记，轮廓清楚。",
    "rumor_weapon_collector_visit": "武器藏家到访传闻图标，展示架上的抛光刀刃和藏家硬币火花，轮廓清楚。",
}

FACTION_EN: Dict[str, str] = {
    "faction_adventurer_guild": "adventurer guild faction crest, compass star, blue route crystal, route-marker notches, reliable exploration emblem, clean readable silhouette",
    "faction_alchemy_guild": "alchemy guild faction crest, glass flask, purification droplet, leaf-like filter fins, calm green-blue glow, clean readable silhouette",
    "faction_black_market": "black market faction crest, dark half-mask, hidden red seal, locked contraband clasp, risky secretive emblem, clean readable silhouette",
    "faction_mage_tower": "mage tower faction crest, tall stone tower silhouette, floating blue-purple crystal, research lens ring, clean readable silhouette",
    "faction_mechanic_workshop": "mechanic workshop faction crest, dark gear, crossed wrench and piston, sturdy engineering badge, blue-white worklight, clean readable silhouette",
}

FACTION_CN: Dict[str, str] = {
    "faction_adventurer_guild": "冒险者公会势力徽章，罗盘星、蓝色路线晶石和路线刻痕，表达可靠探索支持，轮廓清楚。",
    "faction_alchemy_guild": "炼金公会势力徽章，玻璃烧瓶、净化液滴和叶片状滤片，带绿蓝安定微光，轮廓清楚。",
    "faction_black_market": "黑市势力徽章，暗色半面具、隐藏红色封印和违禁品锁扣，表达高风险秘密渠道，轮廓清楚。",
    "faction_mage_tower": "法师塔势力徽章，石质高塔剪影、蓝紫悬浮水晶和研究镜环，轮廓清楚。",
    "faction_mechanic_workshop": "机械工坊势力徽章，暗色齿轮、交叉扳手与活塞，带蓝白工坊光，轮廓清楚。",
}

ORDER_EN: Dict[str, str] = {
    "order_alchemy_antitoxin_contract": "antitoxin supply contract emblem, dark medicine case, green filter vial, sealed supply clasp, clean readable silhouette",
    "order_alchemy_market_forecast": "market forecast contract emblem, small dark chart frame, coin stack, green alchemy flask, clean readable silhouette",
    "order_alchemy_purification_batch": "purification batch contract emblem, grouped glass vials in a dark rack, blue-green cleansing glow, clean readable silhouette",
    "order_alchemy_spore_sample_fast": "urgent spore sample contract emblem, sealed spore vial, small hourglass, fragile green glow, clean readable silhouette",
    "order_black_bound_core_betrayal": "secret-market betrayal contract emblem, dark mask, broken red wax seal, unique glowing core, clean readable silhouette",
    "order_black_contested_core_buyout": "contested core buyout contract emblem, one cracked glowing mechanical core gripped by two opposing dark claim hooks, muted red risk seal, clean readable silhouette",
    "order_black_forbidden_relic_buyout": "forbidden relic buyout contract emblem, locked dark relic box, red risk seal, stacked gold coins, clean readable silhouette",
    "order_black_live_sample_no_questions": "secret live-sample buyout emblem, dark capture jar, hidden mask mark, muted red trust token, clean readable silhouette",
    "order_black_smuggled_route_key": "smuggled route key contract emblem, dark route key, hidden map notch, red warning spark, clean readable silhouette",
    "order_blackmarket_relic_box": "black-market relic box contract emblem, small locked blackened relic chest with reinforced corners, red wax risk seal, no paper, no lamp, clean readable silhouette",
    "order_crystal_scale_batch": "crystal scale batch contract emblem, several blue crystal scales stacked in a shallow dark tray, tiny supply clasp, clean readable silhouette",
    "order_guild_layer1_map_rubbing": "exploration report contract emblem, charcoal map rubbing sheet, dark compass pin, blue route spark, clean readable silhouette",
    "order_guild_layer2_route_report": "route report contract emblem, folded cavern route map, three dark node pins, small blue path glow, clean readable silhouette",
    "order_guild_safezone_signal": "safe-zone signal contract emblem, dark signal flare canister, shelter crystal, green beacon spark, clean readable silhouette",
    "order_mage_boss_core_research": "rare core research contract emblem, blue-purple crystal core held by dark lens rings, clean readable silhouette",
    "order_mage_corroded_memory_stone": "corroded memory stone contract emblem, purple-green cracked stone, dark research clamp, faint echo glow, clean readable silhouette",
    "order_mage_live_slime_sample": "live slime sample contract emblem, glass capture jar with green slime inside, dark lid, blue research glow, clean readable silhouette",
    "order_mage_oracle_sample": "oracle sample contract emblem, blue-purple fossil shard floating inside a dark research clamp ring, small measuring lens, clean readable silhouette",
    "order_mage_unidentified_relic": "unidentified relic contract emblem, wrapped unknown relic, dark magnifying lens, blue-purple question glow without any text, clean readable silhouette",
    "order_spore_cage_procurement": "spore cage procurement contract emblem, small dark specimen cage containing glowing fungal spores, locked latch, green bioluminescent mist, clean readable silhouette",
    "order_status_boss_core_mutex": "unique core exclusivity status emblem, one glowing core between two crossed dark claim hooks, clean readable silhouette",
    "order_status_deadline_warning": "deadline warning status emblem, dark hourglass, red warning spark, small fading sand glow, clean readable silhouette",
    "order_status_order_bound": "bound item status emblem, dark chain clasp around a sealed parcel, blue lock glow, clean readable silhouette",
    "order_status_perishable": "perishable status emblem, fragile green vial, wilting leaf mark, small hourglass, clean readable silhouette",
    "order_type_black_market_betrayal": "secret-market betrayal type emblem, dark mask, broken contract ribbon, muted red warning glow, clean readable silhouette",
    "order_type_exploration_report": "exploration report type emblem, folded route paper, dark compass needle, blue route spark, clean readable silhouette",
    "order_type_large_cargo": "large cargo type emblem, heavy dark crate with reinforced corners, 3-by-3 cargo silhouette blocks, clean readable silhouette",
    "order_type_live_capture": "live capture type emblem, glass capture jar, small air valve, green living glow, clean readable silhouette",
    "order_type_procurement": "procurement type emblem, supply crate, small checklist tags without text, gold coin token, clean readable silhouette",
    "order_type_target_item": "target item type emblem, single marked relic under dark focus brackets, blue target glow, clean readable silhouette",
    "order_workshop_boss_core_claim": "workshop core claim contract emblem, heavy glowing engine core, dark gear claws, blue-white worklight, clean readable silhouette",
    "order_workshop_furnace_core_large": "large furnace core contract emblem, reinforced 3-by-3 dark cargo crate, hot orange engine core, clean readable silhouette",
    "order_workshop_layer2_ore_batch": "ore batch contract emblem, three dark ore chunks in a dark tray, blue-white workshop stamp without text, clean readable silhouette",
    "order_workshop_scrap_guard_plate": "scrap guard plate contract emblem, dented armor plate, gear teeth, repair sparks, clean readable silhouette",
    "order_workshop_spore_core_frame": "workshop spore-core frame contract emblem, rectangular dark machine frame holding a green glowing spore core, gear clamps, clean readable silhouette",
}

ORDER_CN: Dict[str, str] = {
    "order_alchemy_antitoxin_contract": "抗毒供应订单图标，暗色药箱、绿色滤液瓶和封口供给扣，轮廓清楚。",
    "order_alchemy_market_forecast": "市场预测订单图标，小型暗色图表框、硬币堆和绿色炼金瓶，轮廓清楚。",
    "order_alchemy_purification_batch": "净化批量订单图标，暗色架上的成组玻璃瓶、蓝绿净化微光，轮廓清楚。",
    "order_alchemy_spore_sample_fast": "限时孢子样本订单图标，密封孢子瓶、小沙漏和脆弱绿色微光，轮廓清楚。",
    "order_black_bound_core_betrayal": "黑市核心背叛订单图标，暗色面具、破裂红蜡封和发光唯一核心，轮廓清楚。",
    "order_black_forbidden_relic_buyout": "违禁遗物收购订单图标，锁住的暗色遗物盒、红色风险封印和金币堆，轮廓清楚。",
    "order_black_live_sample_no_questions": "黑市活体样本收购订单图标，暗色捕获罐、隐藏面具标记和低饱和红色信任币，轮廓清楚。",
    "order_black_smuggled_route_key": "走私路线钥匙订单图标，暗色路线钥匙、隐藏地图刻痕和红色警示火花，轮廓清楚。",
    "order_guild_layer1_map_rubbing": "第一层拓图报告订单图标，炭拓地图纸、暗色罗盘针和蓝色路线火花，轮廓清楚。",
    "order_guild_layer2_route_report": "第二层路线报告订单图标，折叠洞穴路线图、三个暗色节点针和蓝色路径微光，轮廓清楚。",
    "order_guild_safezone_signal": "安全区信号订单图标，暗色信号筒、庇护晶石和绿色信标火花，轮廓清楚。",
    "order_mage_boss_core_research": "稀有核心研究订单图标，蓝紫晶体核心被暗色镜环固定，轮廓清楚。",
    "order_mage_corroded_memory_stone": "腐蚀记忆石订单图标，紫绿色裂石、暗色研究夹具和微弱回声光，轮廓清楚。",
    "order_mage_live_slime_sample": "活体史莱姆样本订单图标，玻璃捕获罐中有绿色软泥、暗色盖和蓝色研究光，轮廓清楚。",
    "order_mage_unidentified_relic": "未鉴定遗物订单图标，包裹的未知遗物、暗色放大镜和蓝紫疑问微光，不出现文字，轮廓清楚。",
    "order_status_boss_core_mutex": "唯一核心互斥状态图标，一个发光核心位于两只交叉暗色索取钩之间，轮廓清楚。",
    "order_status_deadline_warning": "期限警告状态图标，暗色沙漏、红色警示火花和正在流逝的微光沙，轮廓清楚。",
    "order_status_order_bound": "订单绑定状态图标，暗色链扣环绕封好的包裹，带蓝色锁定微光，轮廓清楚。",
    "order_status_perishable": "易腐状态图标，脆弱绿色小瓶、枯叶标记和小沙漏，轮廓清楚。",
    "order_type_black_market_betrayal": "黑市背叛类型图标，暗色面具、断裂契约绳和低饱和红色警示光，轮廓清楚。",
    "order_type_exploration_report": "探索报告类型图标，折叠路线纸、暗色罗盘针和蓝色路线火花，轮廓清楚。",
    "order_type_large_cargo": "大型货物类型图标，加固重型暗色货箱和 3x3 货物轮廓块，轮廓清楚。",
    "order_type_live_capture": "活体捕获类型图标，玻璃捕获罐、小气阀和绿色生命微光，轮廓清楚。",
    "order_type_procurement": "采购类型图标，补给箱、无文字小标签和金币，轮廓清楚。",
    "order_type_target_item": "指定目标物类型图标，单个遗物被暗色定位框包围，带蓝色目标微光，轮廓清楚。",
    "order_workshop_boss_core_claim": "工坊核心索取订单图标，沉重发光引擎核心、暗色齿轮夹爪和蓝白工坊光，轮廓清楚。",
    "order_workshop_furnace_core_large": "大型炉芯订单图标，加固 3x3 暗色货箱和炽热橙色引擎核心，轮廓清楚。",
    "order_workshop_layer2_ore_batch": "第二层矿石批量订单图标，暗色托盘中的三块暗色矿石和无文字蓝白工坊印记，轮廓清楚。",
    "order_workshop_scrap_guard_plate": "废料守卫装甲板订单图标，凹陷护甲板、齿轮齿和维修火花，轮廓清楚。",
}

BACKGROUND_EN: Dict[str, str] = {
    "combat": "side-scrolling subterranean battle stage background, empty readable foreground ground, broken stone platforms, luminous cave plants, dark vertical cavern fog in the midground, wide negative space on left and right",
    "dungeon_map": "large route-map background texture, low visual noise, cracked stone paths, glowing cave plants, deep vertical cavern feeling, large negative space",
    "layer_1": "shallow subterranean ruins and cave path, worn stone walls, scattered old tools, soft cave glow, light cavern mist, low danger atmosphere, wide empty floor",
    "layer_2": "polluted mining cavern, corroded stone tunnel, purple-green toxic liquid, abandoned tools, acid haze and bioluminescent danger glow",
    "layer_3": "vast underground fungal forest and grassland layer, long forward route through luminous moss fields, giant mushroom trunks, distant cliff terraces, deep map-like perspective, enough depth for a camera moving forward",
    "layer_select": "underground departure chamber, arched abyss entrance, illustrated layer route board, soft warning glow, empty central area for layer list, dark side walls, low visual noise",
    "safe_room": "quiet underground refuge room, small repair bench, blankets, medicine cabinet, calm safe floor area, soft cave light, low visual noise",
    "settlement_defeat": "failed expedition result backdrop, dim underground return bay, damaged gear crates, muted red warning glow, empty central area, somber mood",
    "settlement_victory": "successful evacuation result backdrop, underground lift exit, recovered supply crates, soft town-side light, empty central area, calm relief mood",
    "stairs_room": "deep stairwell chamber, descending stone stairs and simple railings, round hatch opening, warning marks, cavern darkness below, empty foreground floor",
    "workshop": "small doll repair room, workbench, gentle handmade tools, shelves, cloth and parts boxes, soft underground window glow, large negative space on both sides",
    "daily_bill": "quiet town accounting desk background, illustrated ledger, coin trays, rent notice board without readable text, soft shop interior light, clean center area for bill panels, low visual noise",
    "doll_room_attic": "small attic room for a doll, soft window light, narrow bed, old wooden floor, diary desk, memento shelf, window to a deep underground town glow, calm empty center floor, low visual noise",
    "shop_staging": "small town shipping counter background, wooden sorting table, empty item staging trays, soft market window light, cozy underground shop atmosphere, low visual noise",
    "town_shop": "cozy underground town shop interior background, shelves of wrapped goods, small scale, soft fantasy market colors, clean central counter area, low visual noise",
    "workshop_home_room": "peaceful doll home room background, cozy bed nook, memento shelf, soft underground window glow, open floor around the doll, low visual noise",
    "workshop_studio": "doll modification studio background, sturdy repair chair at center-right, handmade tool arms, workbench and parts drawers, clean left inventory area, safe room lighting, low visual noise",
}

BACKGROUND_CN: Dict[str, str] = {
    "combat": "横版地底战斗背景，前景为空旷可读地面，中景有断裂岩台、发光洞穴植物和暗色洞穴雾气，左右留负空间。",
    "dungeon_map": "路线图底纹背景，低噪声画面，裂石道路、发光洞穴植物和纵深洞穴感，大量负空间。",
    "layer_1": "浅层区域背景，地底浅层遗迹与洞穴道路、磨损石墙、散落旧工具、柔和洞穴光和薄雾，危险感较低。",
    "layer_2": "污染矿带背景，腐蚀石质矿道、紫绿色毒液、废弃工具、酸雾和生物荧光危险感。",
    "layer_select": "层选择入口背景，地下出发空间、拱形深渊入口、插画式层级路线牌、柔和警示光和空的中心列表区域。",
    "safe_room": "安全屋背景，安静的地下休整房间、小维修台、毯子、药柜和柔和洞穴光，前景留空。",
    "settlement_defeat": "战败结算背景，昏暗地下返回区、损坏装备箱、低饱和红色警示光和空的中心区域。",
    "settlement_victory": "撤离成功结算背景，地下升降出口、回收物资箱、柔和小镇侧光和空的中心区域。",
    "stairs_room": "阶梯房间背景，向下延伸的石阶和简洁栏杆、圆形舱口、警示标记和下方洞穴黑暗，前景留空。",
    "workshop": "工坊整备背景，小型人偶修复房间，工作台、手作工具、置物架、布料和零件箱、地下窗光，两侧留负空间。",
    "doll_room_attic": "人偶阁楼房间背景，柔和窗光、窄床、旧木地板、日记桌、纪念物架和地下小镇窗光，中间留空。",
}

UI_EN = {
    "missing_sprite": "missing asset placeholder icon, simple broken-image symbol, dark base shape, red warning corner mark, clean readable silhouette",
    "panel_main": "large modular fantasy interface panel frame, soft dark body, illustrated rim, empty center area, subtle inner shadow",
    "panel_info": "small modular information panel frame, soft dark plate, fantasy trim, gentle inset surface, empty center area, compact shape",
    "button_primary": "rectangular primary button skin, clear fantasy rim, dark center plate, bright cool highlight, empty label area",
    "button_secondary": "rectangular secondary button skin, subdued dark steel rim, cool blue-gray inset plate, empty label area",
    "button_danger": "rectangular warning button skin, soft dark plate, muted red accents, empty label area",
    "list_row_normal": "horizontal list row plate, thin fantasy slab, subtle light edge, empty center strip, low contrast",
    "list_row_selected": "horizontal selected list row plate, dark slab, brighter fantasy outline, soft blue edge glow, empty center strip",
    "title_divider": "thin decorative title divider, fantasy linework, small star or cave-gem accents, symmetrical horizontal ornament",
    "icon_money": "small coin-value symbol, stacked gold coins and tiny star stamp, clean silhouette",
    "icon_locked": "small lock symbol, dark old-metal padlock with a crystal shackle, clean silhouette",
    "icon_equipped": "small equipped check symbol, bright check mark over compact rune badge, clean silhouette",
    "icon_maintenance": "small maintenance symbol, crossed old wrench and tiny crystal gauge over a worn gear badge, clean silhouette",
    "icon_wear_repair": "wear repair symbol, cracked gear being patched by a tiny wrench, blue-white repair glow, clean silhouette",
    "icon_corruption_purify": "corruption purification symbol, purple-green droplet inside a crystal filter ring being cleansed by pale blue light, clean silhouette",
    "icon_dive_permit": "dive permit readiness symbol, carved departure gate badge with green-blue signal crystal and check mark, clean silhouette",
    "icon_bill": "small invoice symbol, folded parchment slip with coin stamp and tiny rune mark, clean silhouette",
    "icon_income": "income symbol, stacked gold coins rising beside a small green upward spark, tiny star stamp, clean silhouette",
    "icon_expense": "expense symbol, gold coin leaving a small worn ledger tray, muted red downward marker, clean silhouette",
    "icon_debt_rent": "rent debt pressure symbol, folded invoice pinned by an old key and red wax seal, small coin mark, clean silhouette",
    "icon_warning": "small warning symbol, triangular stone hazard plate with subtle red enamel edge, clean silhouette",
    "icon_chassis_upgrade": "chassis upgrade symbol, carved upward arrow rising from a reinforced backpack frame badge, small gear teeth, clean silhouette",
    "icon_blueprint": "blueprint unlock symbol, folded blue technical plan with rune corner clips and tiny stamp, clean silhouette",
    "icon_material_need": "material shortage symbol, cracked supply crate with a small red warning marker and missing part silhouette, clean silhouette",
    "icon_customer": "customer flow symbol, small visitor silhouette beside a shop counter badge, simple profile shape, clean silhouette",
    "icon_sale_spark": "sale feedback burst symbol, bright coin spark with small radial rays and translucent green-blue impact flare, clean silhouette",
    "icon_business_settlement": "business settlement symbol, small shop counter with coin tray and tiny stamp, clean silhouette",
    "icon_shop_channel": "ordinary shop or export channel symbol, small counter stall or shipping tray badge, clean silhouette",
    "icon_black_market": "black market risk symbol, dark mask or hidden contraband crate badge, muted red accent, clean silhouette",
    "icon_order": "commission order symbol, pinned request slip or stamped parcel badge, clean silhouette",
    "icon_faction": "faction reputation symbol, heraldic rune crest with subtle gear motif, clean silhouette",
    "icon_reputation": "reputation rank symbol, laurel rune crest with small progress notches, clean silhouette",
    "icon_trust": "black market trust symbol, half-hidden handshake badge with muted red seal, clean silhouette",
    "icon_deadline": "deadline and time pressure symbol, small hourglass or clock badge with crystal sand, clean silhouette",
    "icon_rumor": "rumor and intel symbol, folded note with small listening horn or whisper mark, clean silhouette",
    "icon_price_up": "price increase symbol, bright upward arrow over coin and gear badge, clean silhouette",
    "icon_price_down": "price decrease symbol, muted downward arrow over coin and gear badge, clean silhouette",
    "icon_touch": "touch interaction symbol, small gloved hand hovering over a glowing pulse ring, clean silhouette",
    "icon_talk": "talk interaction symbol, speech bubble with tiny rune rivets, clean silhouette",
    "icon_gift": "gift interaction symbol, small wrapped parcel with cloth ribbon and tiny gear tag, clean silhouette",
    "icon_memento": "memento memory symbol, small locket or keepsake gear charm with soft blue glow, clean silhouette",
    "icon_event": "scenario event symbol, small theater curtain badge with bright exclamation mark, clean silhouette",
    "icon_lore": "lore archive symbol, old cloth-bound book and small crystal bookmark, clean silhouette",
    "icon_skip": "skip summary symbol, fast-forward double arrow over a small note sheet, clean silhouette",
    "icon_diary": "diary and memory log symbol, small cloth-bound notebook with ribbon bookmark and tiny gear clasp, clean silhouette",
    "room_memento_slot": "memento display slot, small empty wooden shelf frame with soft inset backing and tiny screw corners, clean silhouette",
    "inventory_slot_available": "square inventory grid slot, dark recessed tile, thin fantasy rim, empty center, subtle bevel",
    "inventory_slot_locked": "square locked inventory grid slot, dark sealed metal tile, diagonal brace, tiny lock emblem, empty center",
    "inventory_slot_hover": "square inventory grid slot hover state, dark recessed tile, brighter fantasy rim, soft blue outline, empty center",
    "inventory_slot_valid": "square inventory grid slot valid placement state, dark recessed tile, green-blue edge glow, clean empty center",
    "inventory_slot_invalid": "square inventory grid slot invalid placement state, dark recessed tile, muted red edge warning, clean empty center",
    "inventory_chassis_panel": "large backpack grid support panel, mechanical base plate, open central grid area, dark rails, worn screws, low visual clutter",
    "loot_pickup_panel": "large loot pickup panel frame, dark fantasy-workshop frame, carved corner clamps, empty list area, subtle worn texture",
    "loot_drop_zone": "loot drop zone tray, shallow old-metal basin, worn fantasy rim, faint grid texture, empty center area",
    "combat_enemy_card": "enemy portrait card frame, square dark frame, carved clamps, small status sockets, empty portrait window",
    "combat_enemy_card_selected": "selected enemy portrait card frame, square dark frame, brighter rune clamps, green-blue target glow, empty portrait window",
    "combat_status_bar_hp": "horizontal health bar skin, dark track, muted red fill channel, carved end caps, empty center channel",
    "combat_status_bar_shield": "horizontal shield bar skin, dark track, cool blue fill channel, carved end caps, empty center channel",
    "combat_ap_pip": "small action point pip, compact blue energy bead, circular fantasy token, clean silhouette",
    "combat_turn_banner": "wide turn banner frame, dark ribbon, carved end caps, empty center area, subtle blue-violet glow",
    "combat_entity_shadow": "soft transparent elliptical floor shadow, subtle dark edge, centered empty interior, no hard border",
    "combat_target_ring": "elliptical target selection ring, thin rune outline, soft blue-violet glow, transparent center",
    "combat_intent_attack": "attack warning emblem, sharp red claw slash over a small gear badge, angular impact shape, clean silhouette",
    "combat_intent_defend": "defensive warning emblem, compact shield plate with blue edge light and tiny rivets, clean silhouette",
    "combat_intent_buff": "empowerment emblem, upward bright arrow wrapped by green-blue energy coil, clean silhouette",
    "combat_intent_debuff": "weakening status emblem, one single bold downward arrow icon, arrowhead pointing down at the bottom, vertical shaft above the arrowhead, cracked purple enamel fill with dark outline, small broken gear badge behind it, flat simple silhouette",
    "combat_intent_grid_lock": "sealed square tile emblem, crossed dark clamps over a grid cell, muted red lock glow, clean silhouette",
    "combat_intent_add_junk": "clutter warning emblem, cracked scrap chunk falling into a small metal tray, green toxic spark, clean silhouette",
    "combat_intent_move_item": "displacement warning emblem, four bright direction arrows around a small crate, clean silhouette",
    "combat_intent_san_pressure": "mental pressure emblem, cracked blue-purple crystal inside a dark pressure gauge ring, clean silhouette",
    "combat_intent_charge": "charge-up warning emblem, glowing capacitor coil and compressed spring, blue-violet energy buildup, clean silhouette",
    "combat_intent_unknown": "unknown action emblem, obscured dark eye badge under a small torn veil, clean silhouette",
    "combat_status_corrosion": "corrosion status emblem, green acid droplet eating into a dark plate, clean silhouette",
    "combat_status_curse": "curse status emblem, dark purple cracked talisman plate wrapped by small dark wires, clean silhouette",
    "combat_status_stun": "stun status emblem, tilted gear with blue spark burst, clean silhouette",
    "combat_feedback_echo_fade": "echo fade feedback overlay, pale ivory and blue-violet spirit afterimage arc dissolving into sparse dust motes, soft translucent glow, low saturation, no lamp, no object body, clean readable effect shape",
    "combat_feedback_hit": "hit feedback overlay, sharp translucent red-orange impact burst with bright spark fragments, transparent center gaps",
    "combat_feedback_scrap_break": "scrap break feedback overlay, cracked rusty armor plate bursting into angular metal shards and bright sparks, transparent gaps, no lamp, clean readable effect shape",
    "combat_feedback_shield_break": "shield break feedback overlay, cracked blue shield shards with bright spark fragments, transparent gaps",
    "combat_feedback_slime_pop": "slime pop feedback overlay, desaturated olive yellow acid bubble bursting into rounded splash droplets, honey amber highlights, murky transparent liquid, low saturation, absolutely no lime green or neon color, transparent center gaps, no bottle, no lamp, clean readable effect shape",
    "combat_grid_lock_marker": "square cell overlay marker, crossed dark clamp frame with muted red warning glow, transparent center",
    "combat_junk_preview_marker": "square cell preview marker, faint green toxic stain and small scrap warning outline, transparent center",
    "dungeon_node_plate": "round map node backing plate, dark stone disk, rune ring, small carved marks, empty center",
    "dungeon_route_line": "thin map route connector line, glowing carved route strip with small rune ticks, horizontal tileable strip",
    "settlement_victory_panel": "large evacuation success panel frame, dark fantasy panel body, clean illustrated trim, subtle green-blue signal light, empty center area",
    "settlement_defeat_panel": "large defeat result panel frame, dark damaged panel body, muted red warning trims, cracked carved corners, empty center area",
    "settlement_outcome_victory": "victory result emblem, glowing rescue beacon above a small recovered supply crate, green-blue signal spark, clean silhouette",
    "settlement_outcome_hp_defeat": "physical defeat result emblem, cracked heart gauge with muted red warning glow, broken armor shard, clean silhouette",
    "settlement_outcome_san_collapse": "mental collapse result emblem, cracked blue-purple crystal inside a dark pressure ring, pale cold pulse, clean silhouette",
    "settlement_outcome_hp_san_defeat": "combined defeat result emblem, split dark badge with cracked heart gauge and fractured blue-purple crystal, clean silhouette",
    "settlement_outcome_party_wipe": "party collapse result emblem, fallen cloth banner beside a broken crystal beacon and muted red warning spark, clean silhouette",
}

UI_CN = {
    "missing_sprite": "缺失资源占位图，破损图片符号、暗色底形和红色警示角标，清楚但不刺眼。",
    "panel_main": "主弹窗面板皮肤，大型奇幻界面面板，柔和暗色主体、插画式边框和空的中心区域。",
    "panel_info": "小信息面板皮肤，柔和暗色板、奇幻细边、内凹表面和紧凑的空白内容区。",
    "button_primary": "主按钮皮肤，清晰奇幻边框、暗色中心板、明亮冷色高光，中间不带文字。",
    "button_secondary": "次按钮皮肤，低调深色边框、冷蓝灰内嵌板，中间不带文字。",
    "button_danger": "危险按钮皮肤，暗色界面板、低饱和红色警示细节和插画式边钉，中间不带文字。",
    "list_row_normal": "普通列表行底板，横向柔和暗色薄板、细奇幻边线、低对比空白条。",
    "list_row_selected": "选中列表行底板，横向柔和暗色板、较亮奇幻外轮廓和冷色边缘光。",
    "title_divider": "标题装饰线，细符文线、晶点点缀和对称横向装饰。",
    "icon_money": "金币价值图标，叠放金币和小星形印记，轮廓清楚。",
    "icon_locked": "锁定图标，暗色旧工艺挂锁和水晶锁梁，轮廓清楚。",
    "icon_equipped": "已装备图标，明亮确认标记叠在小符文徽章上，轮廓清楚。",
    "icon_maintenance": "维护图标，交叉旧扳手和小水晶压力表叠在磨损齿轮徽章上，轮廓清楚。",
    "icon_wear_repair": "磨损修复图标，开裂齿轮被小扳手修补，带蓝白修复微光，轮廓清楚。",
    "icon_corruption_purify": "侵蚀净化图标，紫绿色污染液滴置于水晶滤环中，被浅蓝净化光清除，轮廓清楚。",
    "icon_dive_permit": "下潜许可图标，雕刻出发闸门徽章、绿蓝信号晶石和确认标记，轮廓清楚。",
    "icon_bill": "账单图标，折叠羊皮纸票据、硬币印章和小符文标记，轮廓清楚。",
    "icon_income": "收入图标，叠放金币旁有绿色上升火花和小星形印记，轮廓清楚。",
    "icon_expense": "支出图标，金币从旧账本托盘滑出，带低饱和红色下行标记，轮廓清楚。",
    "icon_debt_rent": "月租债务图标，折叠账单被旧钥匙和红蜡封压住，带小硬币标记，轮廓清楚。",
    "icon_warning": "警告图标，三角石质危险牌和低饱和红色珐琅边，轮廓清楚。",
    "icon_chassis_upgrade": "底盘升级图标，雕刻上箭头从加固背包框徽章中升起，带小齿轮齿，轮廓清楚。",
    "icon_blueprint": "蓝图解锁图标，折叠蓝色技术图纸、符文角夹和小印章，轮廓清楚。",
    "icon_material_need": "材料缺口图标，开裂补给箱、小红色警告标记和缺失零件剪影，轮廓清楚。",
    "icon_customer": "顾客图标，小型访客剪影搭配店铺柜台徽章，侧影简洁，轮廓清楚。",
    "icon_sale_spark": "成交爆点图标，金币火花、放射状短光线和半透明绿蓝冲击光，轮廓清楚。",
    "icon_business_settlement": "营业结算图标，小型店铺柜台、硬币托盘和小印章，轮廓清楚。",
    "icon_shop_channel": "普通出货渠道图标，小型柜台或运输托盘徽章，轮廓清楚。",
    "icon_black_market": "黑市风险图标，暗色面具或隐藏违禁品箱徽章，带低饱和红色点缀，轮廓清楚。",
    "icon_order": "订单委托图标，钉住的委托单或盖章包裹徽章，轮廓清楚。",
    "icon_faction": "势力声望图标，纹章符文徽记并带少量齿轮语义，轮廓清楚。",
    "icon_reputation": "声望等级图标，月桂符文徽章和小型进度刻痕，轮廓清楚。",
    "icon_trust": "黑市信任图标，半隐藏的握手徽章和低饱和红色封印，轮廓清楚。",
    "icon_deadline": "截止日和时间压力图标，小型沙漏或时钟徽章，内部有晶石砂，轮廓清楚。",
    "icon_rumor": "传闻情报图标，折叠纸条搭配小听筒或低语符号，轮廓清楚。",
    "icon_price_up": "涨价图标，明亮上箭头叠在硬币和齿轮徽章上，轮廓清楚。",
    "icon_price_down": "降价图标，低饱和下箭头叠在硬币和齿轮徽章上，轮廓清楚。",
    "icon_touch": "触摸交互图标，戴手套的手悬停在发光脉冲环上，轮廓清楚。",
    "icon_talk": "对话交互图标，对话气泡和小符文铆钉，轮廓清楚。",
    "icon_gift": "赠礼图标，小包裹、布艺缎带和齿轮标签，轮廓清楚。",
    "icon_memento": "纪念物/记忆图标，小吊坠或齿轮纪念物，带柔和蓝光，轮廓清楚。",
    "icon_event": "剧本事件图标，小剧场帷幕徽章和明亮感叹号，轮廓清楚。",
    "icon_lore": "Lore/档案图标，布面旧书和小水晶书签，轮廓清楚。",
    "icon_skip": "跳过摘要图标，快进双箭头叠在小纸条上，轮廓清楚。",
    "icon_diary": "日记和记忆记录图标，布面小笔记本、缎带书签和小齿轮锁扣，轮廓清楚。",
    "room_memento_slot": "纪念物展示槽，空的木质小展示架、柔和内嵌底板和小螺丝角，轮廓清楚。",
    "inventory_slot_available": "背包可用格，方形暗色内凹格、细奇幻边框、空中心和轻微倒角。",
    "inventory_slot_locked": "背包锁定格，封闭暗金属格、斜向加固条、小锁符号和空中心。",
    "inventory_slot_hover": "背包悬停格，较亮奇幻边框和柔和蓝色外轮廓。",
    "inventory_slot_valid": "背包可放置反馈格，绿蓝色边缘光和干净空中心。",
    "inventory_slot_invalid": "背包不可放置反馈格，低饱和红色警示边缘和干净空中心。",
    "inventory_chassis_panel": "背包底盘面板，大型机械底板、开放网格中心区、暗色导轨、磨损螺丝和低噪声纹理。",
    "loot_pickup_panel": "战利品拾取面板，暗色奇幻工坊框体、雕刻角夹、空列表区和轻微磨损纹理。",
    "loot_drop_zone": "战利品掉落区，浅旧金属托盘、磨损奇幻边、淡网格纹理和空中心区域。",
    "combat_enemy_card": "敌人卡片框，方形暗色框、雕刻夹具、小状态插槽和空头像窗。",
    "combat_enemy_card_selected": "敌人选中卡片框，较亮符文夹具、绿蓝目标光和空头像窗。",
    "combat_status_bar_hp": "生命状态条皮肤，暗色轨道、低饱和红色填充槽和雕刻端盖。",
    "combat_status_bar_shield": "护盾状态条皮肤，暗色轨道、冷蓝填充槽和雕刻端盖。",
    "combat_ap_pip": "行动点圆点，小型蓝色能量珠、圆形奇幻代币，轮廓清楚。",
    "combat_turn_banner": "回合提示条，宽暗色条、雕刻端头、空中心和淡蓝紫光。",
    "combat_entity_shadow": "战斗实体脚底阴影，柔和透明椭圆、暗色边缘、中心干净，不做硬边框。",
    "combat_target_ring": "目标选择光环，椭圆形符文线框、柔和蓝紫光、中心透明。",
    "combat_intent_attack": "攻击意图图标，红色爪痕斩击叠在小齿轮徽章上，轮廓尖锐清楚。",
    "combat_intent_defend": "防御意图图标，小型盾牌、蓝色边缘光和细铆钉，轮廓清楚。",
    "combat_intent_buff": "增益意图图标，向上明亮箭头缠绕绿蓝能量线圈，轮廓清楚。",
    "combat_intent_debuff": "弱化意图图标，单个粗大的向下箭头，箭头尖明确朝下且位于底部，上方有竖直箭杆，紫色开裂珐琅填充、暗色描边，背后可有小型破损齿轮徽章。",
    "combat_intent_grid_lock": "封格意图图标，被交叉暗色夹具封住的方格，带低饱和红色锁定光。",
    "combat_intent_add_junk": "塞包意图图标，破裂废料块落入小金属托盘，带绿色污染火花。",
    "combat_intent_move_item": "移位意图图标，小箱子周围有四向明亮箭头，轮廓清楚。",
    "combat_intent_san_pressure": "SAN 压力意图图标，开裂蓝紫水晶位于暗色压力表环中，轮廓清楚。",
    "combat_intent_charge": "蓄力意图图标，发光电容线圈和压缩弹簧，蓝紫能量聚集。",
    "combat_intent_unknown": "未知意图图标，暗色眼形徽章被小块破布遮住，轮廓清楚。",
    "combat_status_corrosion": "腐蚀状态图标，绿色酸液滴腐蚀暗色板，轮廓清楚。",
    "combat_status_curse": "诅咒状态图标，暗紫开裂符牌被细暗线缠绕，轮廓清楚。",
    "combat_status_stun": "眩晕状态图标，倾斜齿轮和蓝色电火花爆点，轮廓清楚。",
    "combat_feedback_hit": "命中反馈覆盖图，半透明红橙冲击爆点和明亮火花碎片，保留透明空隙。",
    "combat_feedback_shield_break": "破盾反馈覆盖图，开裂蓝色护盾碎片和明亮火花碎片，保留透明空隙。",
    "combat_grid_lock_marker": "封格覆盖标记，方形格覆盖层、交叉暗色夹具和低饱和红色警示光，中间透明。",
    "combat_junk_preview_marker": "塞包预告标记，方形格预览层、淡绿色污染痕和小废料警示轮廓，中间透明。",
    "dungeon_node_plate": "地图节点底板，圆形暗色石盘、符文环、小雕刻痕和空中心。",
    "dungeon_route_line": "地图路线连接线，发光雕刻路线条、小符文刻度和横向可平铺条。",
    "settlement_victory_panel": "撤离成功结算面板，暗色奇幻面板主体、干净插画式边、微弱绿蓝信号灯和空中心。",
    "settlement_defeat_panel": "战败结算面板，暗色破损面板、低饱和红色警示边、破裂雕刻角和空中心。",
    "settlement_outcome_victory": "胜利结果徽记，发光救援信标、小型回收补给箱和绿蓝信号火花，轮廓清楚。",
    "settlement_outcome_hp_defeat": "HP 战败结果徽记，开裂生命表、低饱和红色警示光和破损护甲碎片，轮廓清楚。",
    "settlement_outcome_san_collapse": "SAN 崩溃结果徽记，开裂蓝紫水晶置于暗色压力环内，带浅冷色脉冲，轮廓清楚。",
    "settlement_outcome_hp_san_defeat": "HP+SAN 复合战败徽记，分裂暗色徽章中同时包含开裂生命表和破碎蓝紫水晶，轮廓清楚。",
    "settlement_outcome_party_wipe": "队伍溃败结果徽记，倒下的布旗、破损水晶信标和低饱和红色警示火花，轮廓清楚。",
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
    "memento": "text, letters, numbers, watermark, logo, signature, busy background, human hand, full room scene, inventory icon frame, UI panel, multiple copies",
    "rumor": "text, letters, numbers, watermark, logo, signature, busy background, UI panel, paper with readable writing, multiple copies, photorealistic product shot",
    "faction": "text, letters, numbers, watermark, logo, signature, busy background, national flag, real-world heraldry, photorealistic medal, multiple copies",
    "order": "text, letters, numbers, watermark, logo, signature, busy background, UI panel, readable paperwork, multiple copies, photorealistic product shot, human hands",
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
    "chassis_icon": make_spec(
        width=512,
        height=512,
        background="transparent",
        alpha_required=True,
        display_width=96,
        display_height=96,
        safe_padding=10,
        subject_min=0.72,
        subject_max=0.86,
        composition="centered mechanical chassis badge icon",
        post_process=["resize", "trim_transparent_edges", "fit_safe_padding"],
        preview_size=96,
    ),
    "memento": make_spec(
        width=512,
        height=512,
        background="transparent",
        alpha_required=True,
        display_width=120,
        display_height=120,
        safe_padding=10,
        subject_min=0.70,
        subject_max=0.86,
        composition="centered small room keepsake prop",
        post_process=["resize", "trim_transparent_edges", "fit_safe_padding"],
        preview_size=120,
    ),
    "rumor": make_spec(
        width=512,
        height=512,
        background="transparent",
        alpha_required=True,
        display_width=72,
        display_height=72,
        safe_padding=10,
        subject_min=0.70,
        subject_max=0.86,
        composition="centered market rumor emblem",
        post_process=["resize", "trim_transparent_edges", "fit_safe_padding"],
        preview_size=72,
    ),
    "faction": make_spec(
        width=512,
        height=512,
        background="transparent",
        alpha_required=True,
        display_width=88,
        display_height=88,
        safe_padding=10,
        subject_min=0.72,
        subject_max=0.88,
        composition="centered faction crest emblem",
        post_process=["resize", "trim_transparent_edges", "fit_safe_padding"],
        preview_size=88,
    ),
    "order": make_spec(
        width=512,
        height=512,
        background="transparent",
        alpha_required=True,
        display_width=72,
        display_height=72,
        safe_padding=10,
        subject_min=0.70,
        subject_max=0.86,
        composition="centered contract or order emblem",
        post_process=["resize", "trim_transparent_edges", "fit_safe_padding"],
        preview_size=72,
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

SETTLEMENT_OUTCOME_EMBLEM_SPEC = make_spec(
    width=512,
    height=512,
    background="transparent",
    alpha_required=True,
    display_width=96,
    display_height=96,
    safe_padding=10,
    subject_min=0.68,
    subject_max=0.84,
    composition="centered combat outcome emblem",
    post_process=["resize", "trim_transparent_edges", "fit_safe_padding"],
    preview_size=96,
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
    "combat_feedback_echo_fade": COMBAT_FEEDBACK_OVERLAY_SPEC,
    "combat_feedback_hit": COMBAT_FEEDBACK_OVERLAY_SPEC,
    "combat_feedback_scrap_break": COMBAT_FEEDBACK_OVERLAY_SPEC,
    "combat_feedback_shield_break": COMBAT_FEEDBACK_OVERLAY_SPEC,
    "combat_feedback_slime_pop": COMBAT_FEEDBACK_OVERLAY_SPEC,
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
    "settlement_outcome_victory": SETTLEMENT_OUTCOME_EMBLEM_SPEC,
    "settlement_outcome_hp_defeat": SETTLEMENT_OUTCOME_EMBLEM_SPEC,
    "settlement_outcome_san_collapse": SETTLEMENT_OUTCOME_EMBLEM_SPEC,
    "settlement_outcome_hp_san_defeat": SETTLEMENT_OUTCOME_EMBLEM_SPEC,
    "settlement_outcome_party_wipe": SETTLEMENT_OUTCOME_EMBLEM_SPEC,
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

STALE_PROMPT_FRAGMENTS = [
    "downward broken dark arrow with cold purple haze",
    "cracked downward dark arrow combined with a purple falling triangle",
]

NEGATIVE_BY_CONFIG = {
    "combat_feedback_echo_fade": (
        "text, letters, numbers, watermark, logo, signature, busy background, photorealistic photo, tiny details, "
        "lantern, lamp, torch, candle, bottle, vase, jar, full character, portrait, solid opaque object, UI panel, "
        "electric blue, cyan, blue neon, blue flame, turquoise, aqua, cold blue glow, dense particle cloud"
    ),
    "combat_feedback_scrap_break": (
        "text, letters, numbers, watermark, logo, signature, busy background, photorealistic photo, tiny details, "
        "lantern, lamp, torch, candle, bottle, vase, jar, intact shield, full weapon, UI panel, paper"
    ),
    "combat_feedback_slime_pop": (
        "text, letters, numbers, watermark, logo, signature, busy background, photorealistic photo, tiny details, "
        "lantern, lamp, torch, candle, bottle, flask, potion bottle, vase, jar, full creature body, UI panel, "
        "neon green, fluorescent green, lime green, bright green, radioactive glow, cyan, blue, slime monster body"
    ),
    "memento_truth_red_thread": (
        "text, letters, numbers, watermark, logo, signature, busy background, human hand, full room scene, "
        "lantern, lamp, torch, candle, bottle, vase, jar, loose random rope, no red thread, readable paper"
    ),
    "order_blackmarket_relic_box": (
        "text, letters, numbers, watermark, logo, signature, busy background, UI panel, readable paperwork, multiple copies, "
        "lantern, lamp, torch, candle, bottle, vase, jar, open empty box, treasure pile, human hands"
    ),
    "rumor_filter_shortage": (
        "text, letters, numbers, watermark, logo, signature, busy background, UI panel, paper with readable writing, multiple copies, "
        "lantern, lamp, torch, candle, bottle, flask, vase, jar, mask, coin only, full machine"
    ),
    "con_purifying_salt": (
        "text, letters, numbers, watermark, logo, signature, busy background, multiple copies, cropped object, "
        "lantern, lamp, torch, candle, oil lamp, glowing metal vessel, potion bottle only, empty bottle, weapon, shield"
    ),
    "gear_corroded_bulwark": (
        "text, letters, numbers, watermark, logo, signature, busy background, multiple copies, cropped object, "
        "lantern, lamp, torch, glowing metal vessel, bottle, spear, sword, clean polished shield, round coin"
    ),
    "gear_mycelium_cloak": (
        "text, letters, numbers, watermark, logo, signature, busy background, multiple copies, cropped object, "
        "lantern, lamp, metal vessel, bottle, armor plate, weapon, human model, full character, clean plastic raincoat"
    ),
    "gear_spore_lance": (
        "text, letters, numbers, watermark, logo, signature, busy background, multiple copies, cropped object, "
        "lantern, lamp, glowing metal vessel, bottle, shield, cloak, short dagger, staff only, blunt club"
    ),
    "loot_acid_gland": (
        "text, letters, numbers, watermark, logo, signature, busy background, multiple copies, cropped object, "
        "lantern, lamp, torch, metal vessel, glass lantern, potion bottle only, clean fruit, jewel, mechanical core"
    ),
    "loot_rusty_coil": (
        "text, letters, numbers, watermark, logo, signature, busy background, multiple copies, cropped object, "
        "lantern, lamp, oil lamp, miner lamp, torch, candle, glowing metal vessel, vase, jar, bottle, teapot, "
        "coin, gem, intact power core, helmet, full machine, decorative ornament"
    ),
    "loot_toxic_filter": (
        "text, letters, numbers, watermark, logo, signature, busy background, multiple copies, cropped object, "
        "lantern, lamp, oil lamp, torch, candle, glowing metal vessel, vase, jar, bottle, flask, teapot, "
        "gas mask, helmet, coin, gem, intact machine core, decorative container"
    ),
    "mat_core_tier1": (
        "text, letters, numbers, watermark, logo, signature, busy background, multiple copies, cropped object, "
        "lantern, lamp, oil lamp, torch, candle, glowing metal vessel, vase, jar, bottle, flask, teapot, "
        "coin only, gem only, helmet lamp, decorative pendant, full machine"
    ),
    "mat_core_tier2": (
        "text, letters, numbers, watermark, logo, signature, busy background, multiple copies, cropped object, "
        "lantern, lamp, oil lamp, torch, candle, glowing metal vessel, vase, jar, bottle, flask, teapot, "
        "coin only, gem only, helmet lamp, decorative pendant, clean pristine core"
    ),
    "loot_spore_amber": (
        "text, letters, numbers, watermark, logo, signature, busy background, multiple copies, cropped object, "
        "lantern, lamp, oil lamp, torch, candle, glowing metal vessel, vase, jar, bottle, flask, teapot, "
        "mushroom cluster only, coin, full fossil shell, decorative lamp, intact container"
    ),
    "loot_vein_plate": (
        "text, letters, numbers, watermark, logo, signature, busy background, multiple copies, cropped object, "
        "lantern, lamp, oil lamp, torch, candle, glowing metal vessel, vase, jar, bottle, flask, teapot, "
        "round shield, full armor suit, coin, gem, mushroom, decorative lamp"
    ),
    "loot_warped_plate": (
        "text, letters, numbers, watermark, logo, signature, busy background, multiple copies, cropped object, "
        "lantern, lamp, oil lamp, torch, candle, glowing metal vessel, vase, jar, bottle, flask, teapot, "
        "round shield, full armor suit, coin, gem, decorative container, polished clean plate"
    ),
    "mat_core_tier2_fragment": (
        "text, letters, numbers, watermark, logo, signature, busy background, multiple copies, cropped object, "
        "lantern, lamp, torch, glowing metal vessel, intact round core, coin, gem only, potion bottle"
    ),
    "mat_core_tier3_seed": (
        "text, letters, numbers, watermark, logo, signature, busy background, multiple copies, cropped object, "
        "lantern, lamp, oil lamp, torch, candle, glowing metal vessel, vase, jar, bottle, flask, teapot, "
        "plant seed only, mushroom, coin, gem only, decorative pendant, full machine"
    ),
    "order_contested_spore_core": (
        "text, letters, numbers, watermark, logo, signature, busy background, multiple copies, cropped object, "
        "lantern, lamp, oil lamp, torch, candle, glowing metal vessel, vase, jar, bottle, flask, teapot, "
        "mushroom cluster only, coin pile, full contract paper, decorative ornament"
    ),
    "order_live_spore_cage": (
        "text, letters, numbers, watermark, logo, signature, busy background, multiple copies, cropped object, "
        "lantern, lamp, oil lamp, torch, candle, glowing metal vessel, vase, jar, bottle, flask, teapot, "
        "bird cage, animal, full contract paper, open empty cage, decorative lamp"
    ),
    "trade_luminous_fungus": (
        "text, letters, numbers, watermark, logo, signature, busy background, multiple copies, cropped object, "
        "lantern, lamp, oil lamp, torch, candle, glowing metal vessel, vase, jar, bottle, flask, teapot, "
        "single light bulb, crystal lamp, full cave scene, coin pile, mechanical part"
    ),
    "trade_miner_lamp": (
        "text, letters, numbers, watermark, logo, signature, busy background, multiple copies, cropped object, "
        "tall lantern, oil lantern, candle, torch, vase, jar, bottle, teapot, coil, filter, mushroom, full helmet, full character"
    ),
    "trade_cracked_relic": (
        "text, letters, numbers, watermark, logo, signature, busy background, multiple copies, cropped object, "
        "lantern, lamp, oil lamp, torch, candle, glowing metal vessel, vase, jar, bottle, flask, teapot, "
        "filter cartridge, coil, mushroom, coin pile, full statue, readable paper"
    ),
    "trade_sealed_relic_box": (
        "text, letters, numbers, watermark, logo, signature, busy background, multiple copies, cropped object, "
        "lantern, lamp, oil lamp, torch, candle, glowing metal vessel, vase, jar, bottle, flask, teapot, "
        "open treasure chest, coin pile, full contract paper, suitcase, decorative lamp"
    ),
    "trade_singing_fossil": (
        "text, letters, numbers, watermark, logo, signature, busy background, multiple copies, cropped object, "
        "lantern, lamp, oil lamp, torch, candle, glowing metal vessel, vase, jar, bottle, flask, teapot, "
        "mushroom, coin, full musical instrument, microphone, decorative lamp, readable paper"
    ),
    "boss_spore_foundry": (
        "text, letters, numbers, watermark, logo, signature, busy background, tiny subject, mostly black image, "
        "empty dark background, single light ring, full environment scene, cute mascot, friendly smile, cropped head, excessive gore"
    ),
    "mob_acid_slime_mature": (
        "text, letters, numbers, watermark, logo, signature, busy background, tiny subject, mostly black image, "
        "single glowing dot, lantern, lamp, empty dark background, cute mascot, friendly smile, cropped creature, photorealistic animal photo"
    ),
    "mob_echo_pilgrim": (
        "text, letters, numbers, watermark, logo, signature, busy background, tiny subject, mostly black image, "
        "single glowing dot, empty dark background, full body far away, human portrait, cute mascot, friendly smile, cropped head"
    ),
    "combat_intent_debuff": (
        "text, letters, numbers, watermark, logo, signature, busy background, photorealistic photo, tiny details, "
        "upward arrow, arrowhead pointing up, triangle only, purple triangle, diamond shape, rhombus, bottle, flask, torch, staff, wand, spear, flame, lantern"
    ),
}


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
        ("memento", True): MEMENTO_EN,
        ("memento", False): MEMENTO_CN,
        ("rumor", True): RUMOR_EN,
        ("rumor", False): RUMOR_CN,
        ("faction", True): FACTION_EN,
        ("faction", False): FACTION_CN,
        ("order", True): ORDER_EN,
        ("order", False): ORDER_CN,
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
        if asset_type == "icon" or visual_id.endswith("_icon"):
            prompt_en = f"{STYLE_EN}, mechanical chassis badge icon, {detail_en}, centered single object, clean silhouette, transparent background, no text"
        else:
            prompt_en = f"{STYLE_EN}, mechanical frame asset, {detail_en}, rectangular frame, open center, clean silhouette, transparent background, no text"
    elif domain == "memento":
        prompt_en = f"{STYLE_EN}, small room keepsake prop, {detail_en}, centered single object, clean silhouette, transparent background, no text, no letters, no numbers"
    elif domain == "rumor":
        prompt_en = f"{STYLE_EN}, small market rumor emblem, {detail_en}, centered symbol, bold readable silhouette, transparent background, no text, no letters, no numbers"
    elif domain == "faction":
        prompt_en = f"{STYLE_EN}, faction crest emblem, {detail_en}, centered heraldic badge, bold readable silhouette, transparent background, no text, no letters, no numbers"
    elif domain == "order":
        prompt_en = f"{STYLE_EN}, compact contract emblem, {detail_en}, centered single symbol, bold readable silhouette, transparent background, no text, no letters, no numbers"
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
    elif domain == "chassis" and (asset_type == "icon" or visual_id.endswith("_icon")):
        spec = SPEC["chassis_icon"]
    else:
        spec = SPEC.get(domain, SPEC["item"])
    negative = NEGATIVE_BY_CONFIG.get(config_id, NEGATIVE.get(domain, NEGATIVE["item"]))
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
    if entry.get("Status") == "deprecated" and not overwrite:
        return False
    if overwrite:
        return True
    prompt_en = str(entry.get("PromptEN", ""))
    config_id = str(entry.get("ConfigID", ""))
    has_stale_negative = config_id in NEGATIVE_BY_CONFIG and str(entry.get("NegativePromptEN", "")) != NEGATIVE_BY_CONFIG[config_id]
    return (
        entry.get("Status") == "todo"
        or not entry.get("PromptCN")
        or not entry.get("PromptEN")
        or not entry.get("NegativePromptEN")
        or has_stale_negative
        or spec_is_legacy(entry.get("Spec"))
        or "single readable game asset" in prompt_en
        or any(fragment in prompt_en for fragment in STALE_PROMPT_FRAGMENTS)
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
        "NameCN": "日系二次元地底奇幻冒险",
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
            original_status = str(entry.get("Status", ""))
            prompt_cn, prompt_en, negative_en, spec = prompt_for(entry)
            entry["PromptCN"] = prompt_cn
            entry["PromptEN"] = prompt_en
            entry["NegativePromptEN"] = negative_en
            entry["Spec"] = spec
            if original_status in {"", "todo"}:
                entry["Status"] = "prompted"
            else:
                entry["Status"] = original_status
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
