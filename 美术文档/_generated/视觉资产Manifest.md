# 视觉资产 Manifest

> **定位：** 由 `tools/美术工具/Update-ArtManifest.ps1` 根据最新配置表、配置推导项和预置美术需求增量生成。第一步只填资产来源与配置事实，中文审阅描述、英文提示词、英文负面词和结构化规格在第二步补全。
> **配置来源：** `UnityClient/Assets/StreamingAssets/Configs`

## 状态流转

`todo -> prompted -> generated -> selected -> approved -> registered -> validated`，废弃项标记为 `rejected` 或 `deprecated`。

## 汇总

| Domain | Count |
|---|---:|
| `background` | 5 |
| `chassis` | 2 |
| `doll` | 1 |
| `item` | 12 |
| `monster` | 4 |
| `node` | 3 |
| `prosthetic` | 2 |
| `ui` | 1 |

## 资产列表

| Domain | ConfigID | 名称 | 类型 | VisualID | 优先级 | 状态 |
|---|---|---|---|---|---|---|
| `background` | `combat` | 通用战斗背景 | `background` | `bg_combat_abyss` | P1 | `prompted` |
| `background` | `dungeon_map` | 深渊路线图背景 | `background` | `bg_dungeon_map` | P1 | `prompted` |
| `background` | `layer_1` | 浅层区域 | `background` | `bg_dungeon_layer_1` | P1 | `prompted` |
| `background` | `layer_2` | 污染矿带 | `background` | `bg_dungeon_layer_2` | P1 | `prompted` |
| `background` | `workshop` | 工坊整备背景 | `background` | `bg_workshop_day` | P1 | `prompted` |
| `chassis` | `chassis_lv1_basic` | chassis_lv1_basic | `frame` | `chassis_chassis_lv1_basic_frame` | P1 | `prompted` |
| `chassis` | `chassis_lv2_expanded` | chassis_lv2_expanded | `frame` | `chassis_chassis_lv2_expanded_frame` | P1 | `prompted` |
| `doll` | `doll_proto_0` | 原型机·零 | `stand` | `doll_proto_0_stand` | P1 | `prompted` |
| `item` | `con_cheap_sedative` | 廉价镇静剂 | `icon` | `item_con_cheap_sedative_icon` | P0 | `prompted` |
| `item` | `con_repair_kit` | 便携修复剂 | `icon` | `item_con_repair_kit_icon` | P0 | `prompted` |
| `item` | `gear_chainsaw_sword` | 链锯大剑 | `icon` | `item_gear_chainsaw_sword_icon` | P0 | `prompted` |
| `item` | `gear_charge_pistol` | 充能手枪 | `icon` | `item_gear_charge_pistol_icon` | P0 | `prompted` |
| `item` | `gear_iron_armor` | 铁片装甲 | `icon` | `item_gear_iron_armor_icon` | P0 | `prompted` |
| `item` | `gear_rusty_dagger` | 生锈短剑 | `icon` | `item_gear_rusty_dagger_icon` | P0 | `prompted` |
| `item` | `gear_tactical_blade` | 战术长刀 | `icon` | `item_gear_tactical_blade_icon` | P0 | `prompted` |
| `item` | `gear_wooden_shield` | 木制小盾 | `icon` | `item_gear_wooden_shield_icon` | P0 | `prompted` |
| `item` | `loot_gear_scrap` | 废旧齿轮 | `icon` | `item_loot_gear_scrap_icon` | P0 | `prompted` |
| `item` | `loot_rusty_coil` | 生锈线圈 | `icon` | `item_loot_rusty_coil_icon` | P0 | `prompted` |
| `item` | `loot_toxic_filter` | 污染滤芯 | `icon` | `item_loot_toxic_filter_icon` | P0 | `prompted` |
| `item` | `mat_core_tier1` | 一阶动力核心 | `icon` | `item_mat_core_tier1_icon` | P0 | `prompted` |
| `monster` | `elite_mutant_amalgam` | 畸变融合体 | `portrait` | `monster_elite_mutant_amalgam_portrait` | P0 | `prompted` |
| `monster` | `elite_scrap_guard` | 废铁守卫 (守门人) | `portrait` | `monster_elite_scrap_guard_portrait` | P0 | `prompted` |
| `monster` | `mob_acid_slime` | 酸液软体 | `portrait` | `monster_mob_acid_slime_portrait` | P0 | `prompted` |
| `monster` | `mob_scavenger_bug` | 拾荒虫 | `portrait` | `monster_mob_scavenger_bug_portrait` | P0 | `prompted` |
| `node` | `BossNode` | 首领节点 | `icon` | `node_boss_icon` | P0 | `prompted` |
| `node` | `CombatNode` | 战斗节点 | `icon` | `node_combat_icon` | P0 | `prompted` |
| `node` | `SafeRoomNode` | 安全区节点 | `icon` | `node_safe_room_icon` | P0 | `prompted` |
| `prosthetic` | `pros_cooling_system` | 稳压散热插件 | `icon` | `prosthetic_pros_cooling_system_icon` | P1 | `prompted` |
| `prosthetic` | `pros_power_arm` | 动力臂增幅插件 | `icon` | `prosthetic_pros_power_arm_icon` | P1 | `prompted` |
| `ui` | `missing_sprite` | 缺失占位图 | `icon` | `ui_missing_sprite` | P0 | `prompted` |

## 下一步

1. 对 `Status=prompted` 的条目按批次生成图片。
2. 生成后填写 `BatchID` 和 `RawPath`，并将状态改为 `generated`。
