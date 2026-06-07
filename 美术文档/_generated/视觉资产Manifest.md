# 视觉资产 Manifest

> **定位：** 由 `tools/美术工具/Update-ArtManifest.ps1` 根据最新配置表、配置推导项和预置美术需求增量生成。第一步只填资产来源与配置事实，中文审阅描述、英文提示词、英文负面词和结构化规格在第二步补全。
> **配置来源：** `UnityClient/Assets/StreamingAssets/Configs`

## 状态流转

`todo -> prompted -> generated -> selected -> approved -> registered -> validated`，废弃项标记为 `rejected` 或 `deprecated`。

## 汇总

| Domain | Count |
|---|---:|
| `background` | 11 |
| `chassis` | 5 |
| `doll` | 2 |
| `faction` | 5 |
| `item` | 28 |
| `memento` | 8 |
| `monster` | 26 |
| `node` | 8 |
| `order` | 29 |
| `prosthetic` | 8 |
| `rumor` | 8 |
| `ui` | 88 |

## 资产列表

| Domain | ConfigID | 名称 | 类型 | VisualID | 优先级 | 状态 |
|---|---|---|---|---|---|---|
| `background` | `combat` | 通用战斗背景 | `background` | `bg_combat_abyss` | P1 | `approved` |
| `background` | `dungeon_map` | 深渊路线图背景 | `background` | `bg_dungeon_map` | P1 | `approved` |
| `background` | `layer_1` | 浅渊回廊 | `background` | `bg_dungeon_layer_1` | P1 | `approved` |
| `background` | `layer_2` | 腐蚀甬道 | `background` | `bg_dungeon_layer_2` | P1 | `approved` |
| `background` | `safe_room` | 安全屋房间背景 | `background` | `bg_safe_room` | P1 | `approved` |
| `background` | `stairs_room` | 阶梯房间背景 | `background` | `bg_stairs_room` | P1 | `approved` |
| `background` | `workshop` | 工坊整备背景 | `background` | `bg_workshop_day` | P1 | `approved` |
| `background` | `doll_room_attic` | 人偶阁楼房间背景 | `background` | `bg_doll_room_attic` | P2 | `approved` |
| `background` | `layer_select` | 层选择入口背景 | `background` | `bg_layer_select` | P2 | `approved` |
| `background` | `settlement_defeat` | 战败结算背景 | `background` | `bg_settlement_defeat` | P2 | `approved` |
| `background` | `settlement_victory` | 撤离成功结算背景 | `background` | `bg_settlement_victory` | P2 | `approved` |
| `chassis` | `chassis_lv1_basic` | chassis_lv1_basic | `frame` | `chassis_chassis_lv1_basic_frame` | P1 | `approved` |
| `chassis` | `chassis_lv2_expanded` | chassis_lv2_expanded | `frame` | `chassis_chassis_lv2_expanded_frame` | P1 | `approved` |
| `chassis` | `chassis_bulwark_carrier` | 重载承运底盘图标 | `icon` | `chassis_bulwark_carrier_icon` | P2 | `approved` |
| `chassis` | `chassis_compact_raider` | 轻装掠行底盘图标 | `icon` | `chassis_compact_raider_icon` | P2 | `approved` |
| `chassis` | `chassis_standard_frame` | 标准工坊底盘图标 | `icon` | `chassis_standard_frame_icon` | P2 | `approved` |
| `doll` | `doll_proto_0` | 原型机·零 | `stand` | `doll_proto_0_stand` | P1 | `approved` |
| `doll` | `doll_proto_0_test` | 原型机·零（测试） | `stand` | `doll_proto_0_test_stand` | P1 | `deprecated` |
| `faction` | `faction_adventurer_guild` | 冒险者公会徽章 | `icon` | `faction_adventurer_guild_icon` | P2 | `approved` |
| `faction` | `faction_alchemy_guild` | 炼金公会徽章 | `icon` | `faction_alchemy_guild_icon` | P2 | `approved` |
| `faction` | `faction_black_market` | 黑市徽章 | `icon` | `faction_black_market_icon` | P2 | `approved` |
| `faction` | `faction_mage_tower` | 法师塔徽章 | `icon` | `faction_mage_tower_icon` | P2 | `approved` |
| `faction` | `faction_mechanic_workshop` | 机械工坊徽章 | `icon` | `faction_mechanic_workshop_icon` | P2 | `approved` |
| `item` | `con_cheap_sedative` | 廉价镇静剂 | `icon` | `item_con_cheap_sedative_icon` | P0 | `approved` |
| `item` | `con_repair_kit` | 便携修复剂 | `icon` | `item_con_repair_kit_icon` | P0 | `approved` |
| `item` | `con_solvent_spray` | 溶剂喷雾 | `icon` | `item_con_solvent_spray_icon` | P0 | `todo` |
| `item` | `con_stabilizer_ampoule` | 稳定安瓿 | `icon` | `item_con_stabilizer_ampoule_icon` | P0 | `todo` |
| `item` | `gear_chain_hook` | 链钩短枪 | `icon` | `item_gear_chain_hook_icon` | P0 | `todo` |
| `item` | `gear_chainsaw_sword` | 链锯大剑 | `icon` | `item_gear_chainsaw_sword_icon` | P0 | `approved` |
| `item` | `gear_charge_pistol` | 充能手铳 | `icon` | `item_gear_charge_pistol_icon` | P0 | `approved` |
| `item` | `gear_corroded_bulwark` | 腐蚀壁盾 | `icon` | `item_gear_corroded_bulwark_icon` | P0 | `todo` |
| `item` | `gear_cracked_iron_armor` | 裂铁胸甲 | `icon` | `item_gear_iron_armor_icon` | P0 | `approved` |
| `item` | `gear_iron_armor` | 铁片装甲 | `icon` | `item_gear_iron_armor_icon` | P0 | `approved` |
| `item` | `gear_plank_shield_l1` | 拼板护盾 | `icon` | `item_gear_wooden_shield_icon` | P0 | `approved` |
| `item` | `gear_rusty_dagger` | 生锈短剑 | `icon` | `item_gear_rusty_dagger_icon` | P0 | `approved` |
| `item` | `gear_tactical_blade` | 战术长刀 | `icon` | `item_gear_tactical_blade_icon` | P0 | `approved` |
| `item` | `gear_wooden_shield` | 木制小盾 | `icon` | `item_gear_wooden_shield_icon` | P0 | `approved` |
| `item` | `loot_acid_gland` | 酸腺囊 | `icon` | `item_loot_acid_gland_icon` | P0 | `todo` |
| `item` | `loot_crystal_scale` | 晶化鳞片 | `icon` | `item_loot_crystal_scale_icon` | P0 | `todo` |
| `item` | `loot_gear_scrap` | 废旧齿轮 | `icon` | `item_loot_gear_scrap_icon` | P0 | `approved` |
| `item` | `loot_rusty_coil` | 生锈线圈 | `icon` | `item_loot_rusty_coil_icon` | P0 | `approved` |
| `item` | `loot_toxic_filter` | 污染滤芯 | `icon` | `item_loot_toxic_filter_icon` | P0 | `approved` |
| `item` | `loot_warped_plate` | 扭曲装甲片 | `icon` | `item_loot_warped_plate_icon` | P0 | `todo` |
| `item` | `mat_core_tier1` | 一阶动力核心 | `icon` | `item_mat_core_tier1_icon` | P0 | `approved` |
| `item` | `mat_core_tier2` | 二阶污染核心 | `icon` | `item_mat_core_tier1_icon` | P0 | `approved` |
| `item` | `mat_core_tier2_fragment` | 二阶机核碎片 | `icon` | `item_mat_core_tier2_fragment_icon` | P0 | `todo` |
| `item` | `order_live_spore_cage` | 活孢子笼 | `icon` | `item_order_live_spore_cage_icon` | P0 | `todo` |
| `item` | `trade_cracked_relic` | 裂纹圣牌 | `icon` | `item_loot_toxic_filter_icon` | P0 | `approved` |
| `item` | `trade_luminous_fungus` | 夜光菌簇 | `icon` | `item_trade_luminous_fungus_icon` | P0 | `todo` |
| `item` | `trade_miner_lamp` | 矿工提灯 | `icon` | `item_loot_rusty_coil_icon` | P0 | `approved` |
| `item` | `trade_sealed_relic_box` | 封存遗物匣 | `icon` | `item_trade_sealed_relic_box_icon` | P0 | `todo` |
| `memento` | `memento_boss1_lamp` | 第一层矿灯纪念物 | `prop` | `memento_boss1_lamp` | P2 | `approved` |
| `memento` | `memento_first_chassis_frame` | 首次底盘纪念物 | `prop` | `memento_first_chassis_frame` | P2 | `approved` |
| `memento` | `memento_first_prosthetic_case` | 首次义体纪念物 | `prop` | `memento_first_prosthetic_case` | P2 | `approved` |
| `memento` | `memento_first_repair_patch` | 首次修补纪念物 | `prop` | `memento_first_repair_patch` | P2 | `approved` |
| `memento` | `memento_layer2_corrosion_vial` | 第二层腐蚀样本纪念物 | `prop` | `memento_layer2_corrosion_vial` | P2 | `approved` |
| `memento` | `memento_miracle_burn_mark` | 奇迹灼痕纪念物 | `prop` | `memento_miracle_burn_mark` | P2 | `approved` |
| `memento` | `memento_return_mark` | 归还痕迹纪念物 | `prop` | `memento_return_mark` | P2 | `approved` |
| `memento` | `memento_san_collapse_blanket` | SAN崩溃安抚纪念物 | `prop` | `memento_san_collapse_blanket` | P2 | `approved` |
| `monster` | `boss_gatekeeper_mk1` | 一层守门机 MK1战斗实体 | `combat_sprite` | `monster_boss_gatekeeper_mk1_combat` | P0 | `todo` |
| `monster` | `boss_gatekeeper_mk1` | 一层守门机 MK1 | `portrait` | `monster_boss_gatekeeper_mk1_portrait` | P0 | `todo` |
| `monster` | `boss_spore_foundry` | 孢殖熔炉战斗实体 | `combat_sprite` | `monster_boss_spore_foundry_combat` | P0 | `todo` |
| `monster` | `boss_spore_foundry` | 孢殖熔炉 | `portrait` | `monster_boss_spore_foundry_portrait` | P0 | `todo` |
| `monster` | `elite_crystal_bulwark` | 晶壁守卫战斗实体 | `combat_sprite` | `monster_elite_crystal_bulwark_combat` | P0 | `todo` |
| `monster` | `elite_crystal_bulwark` | 晶壁守卫 | `portrait` | `monster_elite_crystal_bulwark_portrait` | P0 | `todo` |
| `monster` | `elite_mutant_amalgam` | 畸变融合体战斗实体 | `combat_sprite` | `monster_elite_mutant_amalgam_combat` | P0 | `approved` |
| `monster` | `elite_mutant_amalgam` | 畸变融合体 | `portrait` | `monster_elite_mutant_amalgam_portrait` | P0 | `approved` |
| `monster` | `elite_scrap_guard` | 废铁守卫战斗实体 | `combat_sprite` | `monster_elite_scrap_guard_combat` | P0 | `approved` |
| `monster` | `elite_scrap_guard` | 废铁守卫 | `portrait` | `monster_elite_scrap_guard_portrait` | P0 | `approved` |
| `monster` | `mob_acid_slime` | 酸液软体战斗实体 | `combat_sprite` | `monster_mob_acid_slime_combat` | P0 | `approved` |
| `monster` | `mob_acid_slime` | 酸液软体 | `portrait` | `monster_mob_acid_slime_portrait` | P0 | `approved` |
| `monster` | `mob_acid_slime_mature` | 成熟酸液软体战斗实体 | `combat_sprite` | `monster_mob_acid_slime_mature_combat` | P0 | `todo` |
| `monster` | `mob_acid_slime_mature` | 成熟酸液软体 | `portrait` | `monster_mob_acid_slime_mature_portrait` | P0 | `todo` |
| `monster` | `mob_crystal_guard` | 结晶守卫战斗实体 | `combat_sprite` | `monster_mob_crystal_guard_combat` | P0 | `todo` |
| `monster` | `mob_crystal_guard` | 结晶守卫 | `portrait` | `monster_mob_crystal_guard_portrait` | P0 | `todo` |
| `monster` | `mob_lost_miner_echo` | 迷失矿工回声战斗实体 | `combat_sprite` | `monster_mob_lost_miner_echo_combat` | P0 | `todo` |
| `monster` | `mob_lost_miner_echo` | 迷失矿工回声 | `portrait` | `monster_mob_lost_miner_echo_portrait` | P0 | `todo` |
| `monster` | `mob_rust_cultivator` | 锈蚀培植者战斗实体 | `combat_sprite` | `monster_mob_rust_cultivator_combat` | P0 | `todo` |
| `monster` | `mob_rust_cultivator` | 锈蚀培植者 | `portrait` | `monster_mob_rust_cultivator_portrait` | P0 | `todo` |
| `monster` | `mob_rust_hound` | 锈蚀猎犬战斗实体 | `combat_sprite` | `monster_mob_rust_hound_combat` | P0 | `todo` |
| `monster` | `mob_rust_hound` | 锈蚀猎犬 | `portrait` | `monster_mob_rust_hound_portrait` | P0 | `todo` |
| `monster` | `mob_scavenger_bug` | 拾荒虫战斗实体 | `combat_sprite` | `monster_mob_scavenger_bug_combat` | P0 | `approved` |
| `monster` | `mob_scavenger_bug` | 拾荒虫 | `portrait` | `monster_mob_scavenger_bug_portrait` | P0 | `approved` |
| `monster` | `mob_soul_midge_swarm` | 窃魂虫群战斗实体 | `combat_sprite` | `monster_mob_soul_midge_swarm_combat` | P0 | `todo` |
| `monster` | `mob_soul_midge_swarm` | 窃魂虫群 | `portrait` | `monster_mob_soul_midge_swarm_portrait` | P0 | `todo` |
| `node` | `BossNode` | 首领节点 | `icon` | `node_boss_icon` | P0 | `approved` |
| `node` | `CombatNode` | 战斗节点 | `icon` | `node_combat_icon` | P0 | `approved` |
| `node` | `EventNode` | EventNode | `icon` | `node_eventnode_icon` | P0 | `approved` |
| `node` | `HazardNode` | HazardNode | `icon` | `node_hazardnode_icon` | P0 | `approved` |
| `node` | `RestStopNode` | RestStopNode | `icon` | `node_reststopnode_icon` | P0 | `approved` |
| `node` | `SafeRoomNode` | 安全区节点 | `icon` | `node_safe_room_icon` | P0 | `approved` |
| `node` | `StairsNode` | 阶梯节点 | `icon` | `node_stairs_icon` | P0 | `approved` |
| `node` | `TreasureNode` | TreasureNode | `icon` | `node_treasurenode_icon` | P0 | `approved` |
| `order` | `order_alchemy_antitoxin_contract` | 抗毒供应订单图标 | `icon` | `order_alchemy_antitoxin_contract_icon` | P2 | `approved` |
| `order` | `order_alchemy_market_forecast` | 炼金市场预测订单图标 | `icon` | `order_alchemy_market_forecast_icon` | P2 | `approved` |
| `order` | `order_alchemy_purification_batch` | 炼金净化批量订单图标 | `icon` | `order_alchemy_purification_batch_icon` | P2 | `approved` |
| `order` | `order_alchemy_spore_sample_fast` | 限时孢子样本订单图标 | `icon` | `order_alchemy_spore_sample_fast_icon` | P2 | `approved` |
| `order` | `order_black_bound_core_betrayal` | 黑市绑定核心背叛订单图标 | `icon` | `order_black_bound_core_betrayal_icon` | P2 | `approved` |
| `order` | `order_black_forbidden_relic_buyout` | 黑市违禁遗物收购订单图标 | `icon` | `order_black_forbidden_relic_buyout_icon` | P2 | `approved` |
| `order` | `order_black_live_sample_no_questions` | 黑市活体样本收购订单图标 | `icon` | `order_black_live_sample_no_questions_icon` | P2 | `approved` |
| `order` | `order_black_smuggled_route_key` | 黑市走私路线钥匙订单图标 | `icon` | `order_black_smuggled_route_key_icon` | P2 | `approved` |
| `order` | `order_guild_layer1_map_rubbing` | 第一层拓图报告订单图标 | `icon` | `order_guild_layer1_map_rubbing_icon` | P2 | `approved` |
| `order` | `order_guild_layer2_route_report` | 第二层路线报告订单图标 | `icon` | `order_guild_layer2_route_report_icon` | P2 | `approved` |
| `order` | `order_guild_safezone_signal` | 安全区信号订单图标 | `icon` | `order_guild_safezone_signal_icon` | P2 | `approved` |
| `order` | `order_mage_boss_core_research` | 法师塔 Boss 核心研究订单图标 | `icon` | `order_mage_boss_core_research_icon` | P2 | `approved` |
| `order` | `order_mage_corroded_memory_stone` | 腐蚀记忆石订单图标 | `icon` | `order_mage_corroded_memory_stone_icon` | P2 | `approved` |
| `order` | `order_mage_live_slime_sample` | 活体史莱姆样本订单图标 | `icon` | `order_mage_live_slime_sample_icon` | P2 | `approved` |
| `order` | `order_mage_unidentified_relic` | 未鉴定遗物订单图标 | `icon` | `order_mage_unidentified_relic_icon` | P2 | `approved` |
| `order` | `order_status_boss_core_mutex` | 订单状态-Boss 核心互斥图标 | `icon` | `order_status_boss_core_mutex_icon` | P2 | `approved` |
| `order` | `order_status_deadline_warning` | 订单状态-期限警告图标 | `icon` | `order_status_deadline_warning_icon` | P2 | `approved` |
| `order` | `order_status_order_bound` | 订单状态-绑定物图标 | `icon` | `order_status_order_bound_icon` | P2 | `approved` |
| `order` | `order_status_perishable` | 订单状态-易腐图标 | `icon` | `order_status_perishable_icon` | P2 | `approved` |
| `order` | `order_type_black_market_betrayal` | 订单类型-黑市背叛图标 | `icon` | `order_type_black_market_betrayal_icon` | P2 | `approved` |
| `order` | `order_type_exploration_report` | 订单类型-探索报告图标 | `icon` | `order_type_exploration_report_icon` | P2 | `approved` |
| `order` | `order_type_large_cargo` | 订单类型-大型货物图标 | `icon` | `order_type_large_cargo_icon` | P2 | `approved` |
| `order` | `order_type_live_capture` | 订单类型-活体捕获图标 | `icon` | `order_type_live_capture_icon` | P2 | `approved` |
| `order` | `order_type_procurement` | 订单类型-采购图标 | `icon` | `order_type_procurement_icon` | P2 | `approved` |
| `order` | `order_type_target_item` | 订单类型-指定物图标 | `icon` | `order_type_target_item_icon` | P2 | `approved` |
| `order` | `order_workshop_boss_core_claim` | 工坊 Boss 核心索取订单图标 | `icon` | `order_workshop_boss_core_claim_icon` | P2 | `approved` |
| `order` | `order_workshop_furnace_core_large` | 大型炉芯订单图标 | `icon` | `order_workshop_furnace_core_large_icon` | P2 | `approved` |
| `order` | `order_workshop_layer2_ore_batch` | 第二层矿石批量订单图标 | `icon` | `order_workshop_layer2_ore_batch_icon` | P2 | `approved` |
| `order` | `order_workshop_scrap_guard_plate` | 废料守卫装甲板订单图标 | `icon` | `order_workshop_scrap_guard_plate_icon` | P2 | `approved` |
| `prosthetic` | `pros_cooling_system` | 稳压散热插件 | `icon` | `prosthetic_pros_cooling_system_icon` | P1 | `approved` |
| `prosthetic` | `pros_power_arm` | 动力臂增幅插件 | `icon` | `prosthetic_pros_power_arm_icon` | P1 | `approved` |
| `prosthetic` | `prosthetic_anchor_left_arm` | 锚定左臂图标 | `icon` | `prosthetic_anchor_left_arm_icon` | P2 | `approved` |
| `prosthetic` | `prosthetic_charge_coil_arm` | 蓄能线圈臂图标 | `icon` | `prosthetic_charge_coil_arm_icon` | P2 | `approved` |
| `prosthetic` | `prosthetic_focus_lens` | 裂隙聚焦镜图标 | `icon` | `prosthetic_focus_lens_icon` | P2 | `approved` |
| `prosthetic` | `prosthetic_mender_spine` | 修复脊索图标 | `icon` | `prosthetic_mender_spine_icon` | P2 | `approved` |
| `prosthetic` | `prosthetic_salvage_fingertips` | 精密拾荒指图标 | `icon` | `prosthetic_salvage_fingertips_icon` | P2 | `approved` |
| `prosthetic` | `prosthetic_san_regulator_core` | SAN稳态调节核图标 | `icon` | `prosthetic_san_regulator_core_icon` | P2 | `approved` |
| `rumor` | `rumor_contraband_night_channel` | 黑市夜间窗口传闻图标 | `icon` | `rumor_contraband_night_channel_icon` | P2 | `approved` |
| `rumor` | `rumor_corrosion_sample_premium` | 腐蚀样本溢价传闻图标 | `icon` | `rumor_corrosion_sample_premium_icon` | P2 | `approved` |
| `rumor` | `rumor_faction_medical_request` | 医疗势力需求传闻图标 | `icon` | `rumor_faction_medical_request_icon` | P2 | `approved` |
| `rumor` | `rumor_low_layer_bulk_buy` | 低层统购传闻图标 | `icon` | `rumor_low_layer_bulk_buy_icon` | P2 | `approved` |
| `rumor` | `rumor_origin_stone_demand` | 源石需求传闻图标 | `icon` | `rumor_origin_stone_demand_icon` | P2 | `approved` |
| `rumor` | `rumor_scrap_workshop_shortage` | 工坊废料短缺传闻图标 | `icon` | `rumor_scrap_workshop_shortage_icon` | P2 | `approved` |
| `rumor` | `rumor_slime_crash` | 史莱姆跌价传闻图标 | `icon` | `rumor_slime_crash_icon` | P2 | `approved` |
| `rumor` | `rumor_weapon_collector_visit` | 武器藏家到访传闻图标 | `icon` | `rumor_weapon_collector_visit_icon` | P2 | `approved` |
| `ui` | `missing_sprite` | 缺失占位图 | `icon` | `ui_missing_sprite` | P0 | `approved` |
| `ui` | `button_primary` | 主按钮 | `button` | `ui_button_primary` | P1 | `approved` |
| `ui` | `button_secondary` | 次按钮 | `button` | `ui_button_secondary` | P1 | `approved` |
| `ui` | `combat_ap_pip` | 行动点圆点 | `icon` | `ui_combat_ap_pip` | P1 | `approved` |
| `ui` | `combat_enemy_card` | 敌人卡片框 | `frame` | `ui_combat_enemy_card` | P1 | `approved` |
| `ui` | `combat_enemy_card_selected` | 敌人选中卡片框 | `frame` | `ui_combat_enemy_card_selected` | P1 | `approved` |
| `ui` | `combat_entity_shadow` | 战斗实体脚底阴影 | `shadow` | `ui_combat_entity_shadow` | P1 | `approved` |
| `ui` | `combat_feedback_hit` | 命中反馈符号 | `effect_overlay` | `ui_combat_feedback_hit` | P1 | `approved` |
| `ui` | `combat_feedback_shield_break` | 破盾反馈符号 | `effect_overlay` | `ui_combat_feedback_shield_break` | P1 | `approved` |
| `ui` | `combat_grid_lock_marker` | 封格覆盖标记 | `marker` | `ui_combat_grid_lock_marker` | P1 | `approved` |
| `ui` | `combat_intent_add_junk` | 塞包意图图标 | `icon` | `ui_combat_intent_add_junk` | P1 | `approved` |
| `ui` | `combat_intent_attack` | 攻击意图图标 | `icon` | `ui_combat_intent_attack` | P1 | `approved` |
| `ui` | `combat_intent_buff` | 增益意图图标 | `icon` | `ui_combat_intent_buff` | P1 | `approved` |
| `ui` | `combat_intent_charge` | 蓄力意图图标 | `icon` | `ui_combat_intent_charge` | P1 | `approved` |
| `ui` | `combat_intent_debuff` | 弱化意图图标 | `icon` | `ui_combat_intent_debuff` | P1 | `approved` |
| `ui` | `combat_intent_defend` | 防御意图图标 | `icon` | `ui_combat_intent_defend` | P1 | `approved` |
| `ui` | `combat_intent_grid_lock` | 封格意图图标 | `icon` | `ui_combat_intent_grid_lock` | P1 | `approved` |
| `ui` | `combat_intent_move_item` | 移位意图图标 | `icon` | `ui_combat_intent_move_item` | P1 | `approved` |
| `ui` | `combat_intent_san_pressure` | SAN压力意图图标 | `icon` | `ui_combat_intent_san_pressure` | P1 | `approved` |
| `ui` | `combat_intent_unknown` | 未知意图图标 | `icon` | `ui_combat_intent_unknown` | P1 | `approved` |
| `ui` | `combat_junk_preview_marker` | 塞包预告标记 | `marker` | `ui_combat_junk_preview_marker` | P1 | `approved` |
| `ui` | `combat_status_bar_hp` | 生命状态条 | `bar` | `ui_combat_status_bar_hp` | P1 | `approved` |
| `ui` | `combat_status_bar_shield` | 护盾状态条 | `bar` | `ui_combat_status_bar_shield` | P1 | `approved` |
| `ui` | `combat_status_corrosion` | 腐蚀状态图标 | `icon` | `ui_combat_status_corrosion` | P1 | `approved` |
| `ui` | `combat_status_curse` | 诅咒状态图标 | `icon` | `ui_combat_status_curse` | P1 | `approved` |
| `ui` | `combat_status_stun` | 眩晕状态图标 | `icon` | `ui_combat_status_stun` | P1 | `approved` |
| `ui` | `combat_target_ring` | 战斗目标选择光环 | `ring` | `ui_combat_target_ring` | P1 | `approved` |
| `ui` | `icon_bill` | 账单图标 | `icon` | `ui_icon_bill` | P1 | `approved` |
| `ui` | `icon_corruption_purify` | 侵蚀净化图标 | `icon` | `ui_icon_corruption_purify` | P1 | `approved` |
| `ui` | `icon_debt_rent` | 月租债务图标 | `icon` | `ui_icon_debt_rent` | P1 | `approved` |
| `ui` | `icon_dive_permit` | 下潜许可图标 | `icon` | `ui_icon_dive_permit` | P1 | `approved` |
| `ui` | `icon_expense` | 支出图标 | `icon` | `ui_icon_expense` | P1 | `approved` |
| `ui` | `icon_income` | 收入图标 | `icon` | `ui_icon_income` | P1 | `approved` |
| `ui` | `icon_maintenance` | 维护状态图标 | `icon` | `ui_icon_maintenance` | P1 | `approved` |
| `ui` | `icon_warning` | 警告图标 | `icon` | `ui_icon_warning` | P1 | `approved` |
| `ui` | `icon_wear_repair` | 磨损修复图标 | `icon` | `ui_icon_wear_repair` | P1 | `approved` |
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
| `ui` | `settlement_outcome_hp_defeat` | 结算结果-HP 战败徽记 | `icon` | `ui_settlement_outcome_hp_defeat` | P1 | `approved` |
| `ui` | `settlement_outcome_hp_san_defeat` | 结算结果-HP+SAN 复合战败徽记 | `icon` | `ui_settlement_outcome_hp_san_defeat` | P1 | `approved` |
| `ui` | `settlement_outcome_party_wipe` | 结算结果-队伍溃败徽记 | `icon` | `ui_settlement_outcome_party_wipe` | P1 | `approved` |
| `ui` | `settlement_outcome_san_collapse` | 结算结果-SAN 崩溃徽记 | `icon` | `ui_settlement_outcome_san_collapse` | P1 | `approved` |
| `ui` | `settlement_outcome_victory` | 结算结果-胜利徽记 | `icon` | `ui_settlement_outcome_victory` | P1 | `approved` |
| `ui` | `button_danger` | 危险按钮 | `button` | `ui_button_danger` | P2 | `approved` |
| `ui` | `combat_turn_banner` | 回合提示条 | `banner` | `ui_combat_turn_banner` | P2 | `approved` |
| `ui` | `dungeon_node_plate` | 地图节点底板 | `frame` | `ui_dungeon_node_plate` | P2 | `approved` |
| `ui` | `dungeon_route_line` | 地图路线连接线 | `divider` | `ui_dungeon_route_line` | P2 | `approved` |
| `ui` | `icon_black_market` | 黑市渠道图标 | `icon` | `ui_icon_black_market` | P2 | `approved` |
| `ui` | `icon_blueprint` | 蓝图图标 | `icon` | `ui_icon_blueprint` | P2 | `approved` |
| `ui` | `icon_business_settlement` | 营业结算图标 | `icon` | `ui_icon_business_settlement` | P2 | `approved` |
| `ui` | `icon_chassis_upgrade` | 底盘升级图标 | `icon` | `ui_icon_chassis_upgrade` | P2 | `approved` |
| `ui` | `icon_customer` | 顾客图标 | `icon` | `ui_icon_customer` | P2 | `approved` |
| `ui` | `icon_deadline` | 截止日图标 | `icon` | `ui_icon_deadline` | P2 | `approved` |
| `ui` | `icon_diary` | 日记图标 | `icon` | `ui_icon_diary` | P2 | `approved` |
| `ui` | `icon_equipped` | 已装备图标 | `icon` | `ui_icon_equipped` | P2 | `approved` |
| `ui` | `icon_event` | 剧本事件图标 | `icon` | `ui_icon_event` | P2 | `approved` |
| `ui` | `icon_faction` | 势力图标 | `icon` | `ui_icon_faction` | P2 | `approved` |
| `ui` | `icon_gift` | 赠礼图标 | `icon` | `ui_icon_gift` | P2 | `approved` |
| `ui` | `icon_locked` | 锁定图标 | `icon` | `ui_icon_locked` | P2 | `approved` |
| `ui` | `icon_lore` | Lore 档案图标 | `icon` | `ui_icon_lore` | P2 | `approved` |
| `ui` | `icon_material_need` | 材料缺口图标 | `icon` | `ui_icon_material_need` | P2 | `approved` |
| `ui` | `icon_memento` | 纪念物图标 | `icon` | `ui_icon_memento` | P2 | `approved` |
| `ui` | `icon_money` | 金币图标 | `icon` | `ui_icon_money` | P2 | `approved` |
| `ui` | `icon_order` | 订单图标 | `icon` | `ui_icon_order` | P2 | `approved` |
| `ui` | `icon_price_down` | 价格下跌图标 | `icon` | `ui_icon_price_down` | P2 | `approved` |
| `ui` | `icon_price_up` | 价格上涨图标 | `icon` | `ui_icon_price_up` | P2 | `approved` |
| `ui` | `icon_reputation` | 声望等级图标 | `icon` | `ui_icon_reputation` | P2 | `approved` |
| `ui` | `icon_rumor` | 传闻图标 | `icon` | `ui_icon_rumor` | P2 | `approved` |
| `ui` | `icon_sale_spark` | 成交爆点图标 | `effect_overlay` | `ui_icon_sale_spark` | P2 | `approved` |
| `ui` | `icon_shop_channel` | 出货渠道图标 | `icon` | `ui_icon_shop_channel` | P2 | `approved` |
| `ui` | `icon_skip` | 跳过摘要图标 | `icon` | `ui_icon_skip` | P2 | `approved` |
| `ui` | `icon_talk` | 对话图标 | `icon` | `ui_icon_talk` | P2 | `approved` |
| `ui` | `icon_touch` | 触摸图标 | `icon` | `ui_icon_touch` | P2 | `approved` |
| `ui` | `icon_trust` | 黑市信任图标 | `icon` | `ui_icon_trust` | P2 | `approved` |
| `ui` | `list_row_normal` | 列表行底板 | `panel` | `ui_list_row_normal` | P2 | `approved` |
| `ui` | `list_row_selected` | 选中列表行底板 | `panel` | `ui_list_row_selected` | P2 | `approved` |
| `ui` | `room_memento_slot` | 纪念物展示槽 | `slot` | `ui_room_memento_slot` | P2 | `approved` |
| `ui` | `settlement_defeat_panel` | 战败结算面板 | `panel` | `ui_settlement_defeat_panel` | P2 | `approved` |
| `ui` | `settlement_victory_panel` | 撤离成功结算面板 | `panel` | `ui_settlement_victory_panel` | P2 | `approved` |
| `ui` | `title_divider` | 标题装饰线 | `divider` | `ui_title_divider` | P2 | `approved` |

## 下一步

1. 对 `Status=todo` 的新增项补全 `PromptCN`、`PromptEN`、`NegativePromptEN` 和结构化 `Spec`。
2. 完成后运行 `tools/美术工具/Generate-ArtPrompts.ps1` 或人工审阅提示词。
