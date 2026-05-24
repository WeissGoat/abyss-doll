# 视觉资产 Manifest

> **定位：** 由 `tools/美术工具/Update-ArtManifest.ps1` 根据最新配置表、配置推导项和预置美术需求增量生成。第一步只填资产来源与配置事实，中文审阅描述、英文提示词、英文负面词和结构化规格在第二步补全。
> **配置来源：** `UnityClient/Assets/StreamingAssets/Configs`

## 状态流转

`todo -> prompted -> generated -> selected -> approved -> registered -> validated`，废弃项标记为 `rejected` 或 `deprecated`。

## 汇总

| Domain | Count |
|---|---:|
| `background` | 10 |
| `chassis` | 2 |
| `doll` | 2 |
| `item` | 13 |
| `monster` | 8 |
| `node` | 4 |
| `prosthetic` | 2 |
| `ui` | 43 |

## 资产列表

| Domain | ConfigID | 名称 | 类型 | VisualID | 优先级 | 状态 |
|---|---|---|---|---|---|---|
| `background` | `combat` | 通用战斗背景 | `background` | `bg_combat_abyss` | P1 | `approved` |
| `background` | `dungeon_map` | 深渊路线图背景 | `background` | `bg_dungeon_map` | P1 | `approved` |
| `background` | `layer_1` | 浅层区域 | `background` | `bg_dungeon_layer_1` | P1 | `approved` |
| `background` | `layer_2` | 污染矿带 | `background` | `bg_dungeon_layer_2` | P1 | `approved` |
| `background` | `safe_room` | 安全屋房间背景 | `background` | `bg_safe_room` | P1 | `approved` |
| `background` | `stairs_room` | 阶梯房间背景 | `background` | `bg_stairs_room` | P1 | `approved` |
| `background` | `workshop` | 工坊整备背景 | `background` | `bg_workshop_day` | P1 | `approved` |
| `background` | `layer_select` | 层选择入口背景 | `background` | `bg_layer_select` | P2 | `approved` |
| `background` | `settlement_defeat` | 战败结算背景 | `background` | `bg_settlement_defeat` | P2 | `approved` |
| `background` | `settlement_victory` | 撤离成功结算背景 | `background` | `bg_settlement_victory` | P2 | `approved` |
| `chassis` | `chassis_lv1_basic` | chassis_lv1_basic | `frame` | `chassis_chassis_lv1_basic_frame` | P1 | `approved` |
| `chassis` | `chassis_lv2_expanded` | chassis_lv2_expanded | `frame` | `chassis_chassis_lv2_expanded_frame` | P1 | `approved` |
| `doll` | `doll_proto_0` | 原型机·零 | `stand` | `doll_proto_0_stand` | P1 | `approved` |
| `doll` | `doll_proto_0_test` | 原型机·零（测试） | `stand` | `doll_proto_0_test_stand` | P1 | `deprecated` |
| `item` | `con_cheap_sedative` | 廉价镇静剂 | `icon` | `item_con_cheap_sedative_icon` | P0 | `approved` |
| `item` | `con_repair_kit` | 便携修复剂 | `icon` | `item_con_repair_kit_icon` | P0 | `approved` |
| `item` | `gear_chainsaw_sword` | 链锯大剑 | `icon` | `item_gear_chainsaw_sword_icon` | P0 | `approved` |
| `item` | `gear_charge_pistol` | 充能手枪 | `icon` | `item_gear_charge_pistol_icon` | P0 | `approved` |
| `item` | `gear_iron_armor` | 铁片装甲 | `icon` | `item_gear_iron_armor_icon` | P0 | `approved` |
| `item` | `gear_rusty_dagger` | 生锈短剑 | `icon` | `item_gear_rusty_dagger_icon` | P0 | `approved` |
| `item` | `gear_tactical_blade` | 战术长刀 | `icon` | `item_gear_tactical_blade_icon` | P0 | `approved` |
| `item` | `gear_wooden_shield` | 木制小盾 | `icon` | `item_gear_wooden_shield_icon` | P0 | `approved` |
| `item` | `loot_gear_scrap` | 废旧齿轮 | `icon` | `item_loot_gear_scrap_icon` | P0 | `approved` |
| `item` | `loot_rusty_coil` | 生锈线圈 | `icon` | `item_loot_rusty_coil_icon` | P0 | `approved` |
| `item` | `loot_toxic_filter` | 污染滤芯 | `icon` | `item_loot_toxic_filter_icon` | P0 | `approved` |
| `item` | `mat_core_tier1` | 一阶动力核心 | `icon` | `item_mat_core_tier1_icon` | P0 | `approved` |
| `item` | `mat_core_tier2` | 二阶污染核心 | `icon` | `item_mat_core_tier1_icon` | P0 | `approved` |
| `monster` | `elite_mutant_amalgam` | 畸变融合体战斗实体 | `combat_sprite` | `monster_elite_mutant_amalgam_combat` | P0 | `approved` |
| `monster` | `elite_mutant_amalgam` | 畸变融合体 | `portrait` | `monster_elite_mutant_amalgam_portrait` | P0 | `approved` |
| `monster` | `elite_scrap_guard` | 废铁守卫 (守门人)战斗实体 | `combat_sprite` | `monster_elite_scrap_guard_combat` | P0 | `approved` |
| `monster` | `elite_scrap_guard` | 废铁守卫 (守门人) | `portrait` | `monster_elite_scrap_guard_portrait` | P0 | `approved` |
| `monster` | `mob_acid_slime` | 酸液软体战斗实体 | `combat_sprite` | `monster_mob_acid_slime_combat` | P0 | `approved` |
| `monster` | `mob_acid_slime` | 酸液软体 | `portrait` | `monster_mob_acid_slime_portrait` | P0 | `approved` |
| `monster` | `mob_scavenger_bug` | 拾荒虫战斗实体 | `combat_sprite` | `monster_mob_scavenger_bug_combat` | P0 | `approved` |
| `monster` | `mob_scavenger_bug` | 拾荒虫 | `portrait` | `monster_mob_scavenger_bug_portrait` | P0 | `approved` |
| `node` | `BossNode` | 首领节点 | `icon` | `node_boss_icon` | P0 | `approved` |
| `node` | `CombatNode` | 战斗节点 | `icon` | `node_combat_icon` | P0 | `approved` |
| `node` | `SafeRoomNode` | 安全区节点 | `icon` | `node_safe_room_icon` | P0 | `approved` |
| `node` | `StairsNode` | 阶梯节点 | `icon` | `node_stairs_icon` | P0 | `approved` |
| `prosthetic` | `pros_cooling_system` | 稳压散热插件 | `icon` | `prosthetic_pros_cooling_system_icon` | P1 | `approved` |
| `prosthetic` | `pros_power_arm` | 动力臂增幅插件 | `icon` | `prosthetic_pros_power_arm_icon` | P1 | `approved` |
| `ui` | `missing_sprite` | 缺失占位图 | `icon` | `ui_missing_sprite` | P0 | `approved` |
| `ui` | `button_primary` | 主按钮 | `button` | `ui_button_primary` | P1 | `approved` |
| `ui` | `button_secondary` | 次按钮 | `button` | `ui_button_secondary` | P1 | `approved` |
| `ui` | `combat_ap_pip` | 行动点圆点 | `icon` | `ui_combat_ap_pip` | P1 | `approved` |
| `ui` | `combat_enemy_card` | 敌人卡片框 | `frame` | `ui_combat_enemy_card` | P1 | `approved` |
| `ui` | `combat_enemy_card_selected` | 敌人选中卡片框 | `frame` | `ui_combat_enemy_card_selected` | P1 | `approved` |
| `ui` | `combat_entity_shadow` | 战斗实体脚底阴影 | `shadow` | `ui_combat_entity_shadow` | P1 | `approved` |
| `ui` | `combat_status_bar_hp` | 生命状态条 | `bar` | `ui_combat_status_bar_hp` | P1 | `approved` |
| `ui` | `combat_status_bar_shield` | 护盾状态条 | `bar` | `ui_combat_status_bar_shield` | P1 | `approved` |
| `ui` | `combat_target_ring` | 战斗目标选择光环 | `ring` | `ui_combat_target_ring` | P1 | `approved` |
| `ui` | `icon_bill` | 账单图标 | `icon` | `ui_icon_bill` | P1 | `approved` |
| `ui` | `icon_maintenance` | 维护状态图标 | `icon` | `ui_icon_maintenance` | P1 | `approved` |
| `ui` | `icon_warning` | 警告图标 | `icon` | `ui_icon_warning` | P1 | `approved` |
| `ui` | `inventory_chassis_panel` | 背包底盘面板 | `panel` | `ui_inventory_chassis_panel` | P1 | `approved` |
| `ui` | `inventory_slot_available` | 背包可用格 | `slot` | `ui_inventory_slot_available` | P1 | `approved` |
| `ui` | `inventory_slot_hover` | 背包悬停格 | `slot` | `ui_inventory_slot_hover` | P1 | `approved` |
| `ui` | `inventory_slot_invalid` | 背包不可放置反馈格 | `slot` | `ui_inventory_slot_invalid` | P1 | `approved` |
| `ui` | `inventory_slot_locked` | 背包锁定格 | `slot` | `ui_inventory_slot_locked` | P1 | `approved` |
| `ui` | `inventory_slot_valid` | 背包可放置反馈格 | `slot` | `ui_inventory_slot_valid` | P1 | `approved` |
| `ui` | `loot_drop_zone` | 战利品掉落区 | `panel` | `ui_loot_drop_zone` | P1 | `approved` |
| `ui` | `loot_pickup_panel` | 战利品拾取面板 | `panel` | `ui_loot_pickup_panel` | P1 | `approved` |
| `ui` | `panel_info` | 小信息面板 | `panel` | `ui_panel_info` | P1 | `approved` |
| `ui` | `panel_main` | 主弹窗面板 | `panel` | `ui_panel_main` | P1 | `approved` |
| `ui` | `button_danger` | 危险按钮 | `button` | `ui_button_danger` | P2 | `approved` |
| `ui` | `combat_turn_banner` | 回合提示条 | `banner` | `ui_combat_turn_banner` | P2 | `approved` |
| `ui` | `dungeon_node_plate` | 地图节点底板 | `frame` | `ui_dungeon_node_plate` | P2 | `approved` |
| `ui` | `dungeon_route_line` | 地图路线连接线 | `divider` | `ui_dungeon_route_line` | P2 | `approved` |
| `ui` | `icon_black_market` | 黑市渠道图标 | `icon` | `ui_icon_black_market` | P2 | `todo` |
| `ui` | `icon_deadline` | 截止日图标 | `icon` | `ui_icon_deadline` | P2 | `todo` |
| `ui` | `icon_equipped` | 已装备图标 | `icon` | `ui_icon_equipped` | P2 | `approved` |
| `ui` | `icon_faction` | 势力图标 | `icon` | `ui_icon_faction` | P2 | `todo` |
| `ui` | `icon_locked` | 锁定图标 | `icon` | `ui_icon_locked` | P2 | `approved` |
| `ui` | `icon_money` | 金币图标 | `icon` | `ui_icon_money` | P2 | `approved` |
| `ui` | `icon_order` | 订单图标 | `icon` | `ui_icon_order` | P2 | `todo` |
| `ui` | `icon_price_down` | 价格下跌图标 | `icon` | `ui_icon_price_down` | P2 | `todo` |
| `ui` | `icon_price_up` | 价格上涨图标 | `icon` | `ui_icon_price_up` | P2 | `todo` |
| `ui` | `icon_rumor` | 传闻图标 | `icon` | `ui_icon_rumor` | P2 | `todo` |
| `ui` | `icon_shop_channel` | 出货渠道图标 | `icon` | `ui_icon_shop_channel` | P2 | `todo` |
| `ui` | `list_row_normal` | 列表行底板 | `panel` | `ui_list_row_normal` | P2 | `approved` |
| `ui` | `list_row_selected` | 选中列表行底板 | `panel` | `ui_list_row_selected` | P2 | `approved` |
| `ui` | `settlement_defeat_panel` | 战败结算面板 | `panel` | `ui_settlement_defeat_panel` | P2 | `approved` |
| `ui` | `settlement_victory_panel` | 撤离成功结算面板 | `panel` | `ui_settlement_victory_panel` | P2 | `approved` |
| `ui` | `title_divider` | 标题装饰线 | `divider` | `ui_title_divider` | P2 | `approved` |

## 下一步

1. 对 `Status=todo` 的新增项补全 `PromptCN`、`PromptEN`、`NegativePromptEN` 和结构化 `Spec`。
2. 完成后运行 `tools/美术工具/Generate-ArtPrompts.ps1` 或人工审阅提示词。
