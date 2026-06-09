# Formal V2 运行时素材语义复核

生成时间：`2026-06-09T12:37:44+08:00`

## 摘要

- 复核 VisualID：`77`
- 技术预检：`pass`
- 程序接入结论：`allow_program_integrate`
- 风险统计：`{'ok_for_current_v2': 47, 'watch_runtime_readability': 30}`

## 口径

- `ok_for_current_v2`：当前 V2 可用，等待运行时截图正常验收。
- `watch_runtime_readability`：不阻塞登记，但运行时截图要重点看小尺寸可读性。
- `style_mismatch_watch`：不阻塞登记，但运行时截图要重点看是否和整体风格割裂。
- `secondary_replacement_candidate`：不阻塞登记；若运行时截图证明影响识别或氛围，优先二次 NovelAI 同名替换。

## 风险项

| Risk | VisualID | Domain | Type | Reason | Recommendation |
|---|---|---|---|---|---|
| watch_runtime_readability | `item_con_solvent_spray_icon` | item | icon | 容器质感可用，但喷雾嘴和液体雾化语义偏弱。 | 运行时小尺寸复核；如不清晰，改为带喷嘴的手持雾化瓶。 |
| watch_runtime_readability | `item_con_stabilizer_ampoule_icon` | item | icon | 安瓿可读性尚可，但黄铜灯具感仍偏强。 | 运行时复核；必要时加强透明玻璃管、液面和封蜡结构。 |
| watch_runtime_readability | `item_gear_spore_lance_icon` | item | icon | 二次替换后长枪语义已明确，但图形较细，运行时小尺寸可能偏淡。 | 运行时复核；如果仍不清晰，再提高枪杆对比度和轮廓厚度。 |
| watch_runtime_readability | `item_gear_vein_sickle_icon` | item | icon | 剪影有弯刃方向，但黄铜挂件感偏强。 | 运行时复核；必要时加强弯镰刀刃和菌脉纹理。 |
| watch_runtime_readability | `item_loot_corroded_nerve_icon` | item | icon | 二次替换后不再像灯具，但神经束较细，运行时小图标可读性需要确认。 | 运行时复核；如果偏淡，再强化绿色神经束粗细和锈蚀夹片对比。 |
| watch_runtime_readability | `item_loot_crystal_scale_icon` | item | icon | 亮面晶体感可用，但轮廓接近灯罩，小尺寸下可能不够像鳞片。 | 运行时复核；必要时改为多片叠放的半透明晶化鳞片。 |
| watch_runtime_readability | `item_loot_living_mycelium_icon` | item | icon | 活体菌丝的发光瓶感偏强，生物组织轮廓不够明显。 | 运行时复核；必要时加强菌丝束、根须和轻微蠕动感。 |
| watch_runtime_readability | `memento_debt_shadow_window` | memento | icon | 债务阴影的情绪方向可用，但图标偏灯具，房间纪念物语义需要运行时位置验证。 | 运行时复核；必要时改为窗影、欠条和压暗边框组合。 |
| watch_runtime_readability | `memento_wall_crack_first_defeat` | memento | icon | 红色圆盘视觉醒目，但墙裂纪念物语义可能不够直观。 | 运行时复核；必要时改为墙面裂缝、红线和破损灰尘。 |
| watch_runtime_readability | `monster_boss_gatekeeper_mk1_portrait` | monster | portrait | 头部主体偏暗，小头像框中可能只剩发光点。 | 运行时复核头像框；必要时放大面部和机械门卫轮廓。 |
| watch_runtime_readability | `monster_boss_mycelium_oracle_portrait` | monster | portrait | 剪影有神谕感，但主体细长，小尺寸辨识依赖亮边。 | 运行时复核；必要时提高头肩比例和菌冠轮廓。 |
| watch_runtime_readability | `monster_elite_crystal_bulwark_portrait` | monster | portrait | 轮廓清楚但主体较小，晶体壁垒身份可能需要运行时确认。 | 运行时复核；必要时放大盾状晶壳和胸肩结构。 |
| watch_runtime_readability | `monster_elite_spore_matriarch_portrait` | monster | portrait | 孢子母体的剪影可用，但黑底占比偏高。 | 运行时复核；必要时提高头冠和腹囊轮廓亮度。 |
| watch_runtime_readability | `monster_elite_vein_knight_portrait` | monster | portrait | 骑士语义偏抽象，当前更像发光藤蔓剪影。 | 运行时复核；必要时重出带头盔、肩甲和菌脉披挂的头像。 |
| watch_runtime_readability | `monster_mob_acid_slime_mature_portrait` | monster | portrait | 二次替换后酸液史莱姆语义明确，但亮黄绿色面积较大，需要在头像框中确认不刺眼。 | 运行时复核；如果过亮，再降低饱和度并保留胶质轮廓。 |
| watch_runtime_readability | `monster_mob_crystal_guard_portrait` | monster | portrait | 晶体守卫轮廓尚可，但小头像中细节容易粘成暗块。 | 运行时复核；必要时强化晶体肩部和头部高光。 |
| watch_runtime_readability | `monster_mob_lost_miner_echo_portrait` | monster | portrait | 矿工回声的提灯点明确，但角色本体偏小。 | 运行时复核；必要时放大矿工帽、背包和弯腰姿态。 |
| watch_runtime_readability | `monster_mob_mycelium_crawler_portrait` | monster | portrait | 爬行者轮廓偏细，头像槽中可能不够稳定。 | 运行时复核；必要时强化前肢和菌丝背部轮廓。 |
| watch_runtime_readability | `monster_mob_nerve_midge_swarm_portrait` | monster | portrait | 飞虫群氛围合适，但小光点群在小尺寸下辨识弱。 | 运行时复核；必要时增加群体飞虫外轮廓。 |
| watch_runtime_readability | `monster_mob_rust_cultivator_portrait` | monster | portrait | 轮廓有机械感，但耕作者身份不强。 | 运行时复核；必要时加入锈蚀工具、驼背姿态和头肩结构。 |
| watch_runtime_readability | `monster_mob_rust_hound_portrait` | monster | portrait | 兽形轮廓有基础可读性，但暗部占比偏高。 | 运行时复核；必要时提高头部、背脊和爪部剪影。 |
| watch_runtime_readability | `monster_mob_shell_grafter_portrait` | monster | portrait | 主体偏小，壳体嫁接语义在头像框中可能读不清。 | 运行时复核；必要时放大壳体边缘光和侧面头肩。 |
| watch_runtime_readability | `monster_mob_soul_midge_swarm_portrait` | monster | portrait | 氛围合适，但黑底和小光点可能导致小尺寸辨识弱。 | 运行时复核；必要时增加群体飞虫轮廓。 |
| watch_runtime_readability | `monster_mob_spore_archer_portrait` | monster | portrait | 主体靠下且偏小，弓箭职业语义不明显。 | 运行时复核；必要时重出带弓形剪影的半身头像。 |
| watch_runtime_readability | `order_mage_oracle_sample_icon` | order | icon | 魔法样本方向可用，但和通用黄铜容器图标族接近。 | 运行时复核；必要时加强样本瓶、神谕孢子和法师标记。 |
| watch_runtime_readability | `rumor_filter_shortage_icon` | rumor | icon | 锁孔图形清楚，但过滤芯短缺的语义偏弱。 | 运行时复核；必要时改为滤芯、缺货标签和小镇公告纸。 |
| watch_runtime_readability | `rumor_living_mycelium_shortage_icon` | rumor | icon | 图形像机械徽章，活体菌丝短缺语义需要文字辅助。 | 运行时复核；必要时增加菌丝束、短缺标记和价格箭头。 |
| watch_runtime_readability | `rumor_old_blades_icon` | rumor | icon | 旧刀涨价目前像单个火把剪影，刀刃语义不足。 | 运行时复核；必要时改为旧刀、锈蚀刃口和涨价小符号。 |
| watch_runtime_readability | `ui_combat_feedback_echo_fade` | ui | icon | 同名替换后已改为暖白 / 淡金回声残影，原浅蓝风格割裂风险已收敛；仍需在战斗 HUD 小尺寸中确认亮度和透明边界。 | 允许程序登记；运行时复核 140px 反馈贴片是否足够可读，必要时再提高轮廓对比。 |
| watch_runtime_readability | `ui_combat_feedback_slime_pop` | ui | icon | 同名替换后已从荧光绿压到低饱和橄榄金酸液泡，原霓虹风格割裂风险已收敛；仍需在战斗 HUD 中确认泡破裂语义。 | 允许程序登记；运行时复核酸液泡 / 液滴是否能读成击败反馈，必要时再强化破裂飞溅形状。 |

## 全量明细

| Risk | VisualID | Domain | Type | Technical | Program Integrate |
|---|---|---|---|---|---|
| watch_runtime_readability | `item_con_solvent_spray_icon` | item | icon | pass | yes |
| watch_runtime_readability | `item_con_stabilizer_ampoule_icon` | item | icon | pass | yes |
| watch_runtime_readability | `item_gear_spore_lance_icon` | item | icon | pass | yes |
| watch_runtime_readability | `item_gear_vein_sickle_icon` | item | icon | pass | yes |
| watch_runtime_readability | `item_loot_corroded_nerve_icon` | item | icon | pass | yes |
| watch_runtime_readability | `item_loot_crystal_scale_icon` | item | icon | pass | yes |
| watch_runtime_readability | `item_loot_living_mycelium_icon` | item | icon | pass | yes |
| watch_runtime_readability | `memento_debt_shadow_window` | memento | icon | pass | yes |
| watch_runtime_readability | `memento_wall_crack_first_defeat` | memento | icon | pass | yes |
| watch_runtime_readability | `monster_boss_gatekeeper_mk1_portrait` | monster | portrait | pass | yes |
| watch_runtime_readability | `monster_boss_mycelium_oracle_portrait` | monster | portrait | pass | yes |
| watch_runtime_readability | `monster_elite_crystal_bulwark_portrait` | monster | portrait | pass | yes |
| watch_runtime_readability | `monster_elite_spore_matriarch_portrait` | monster | portrait | pass | yes |
| watch_runtime_readability | `monster_elite_vein_knight_portrait` | monster | portrait | pass | yes |
| watch_runtime_readability | `monster_mob_acid_slime_mature_portrait` | monster | portrait | pass | yes |
| watch_runtime_readability | `monster_mob_crystal_guard_portrait` | monster | portrait | pass | yes |
| watch_runtime_readability | `monster_mob_lost_miner_echo_portrait` | monster | portrait | pass | yes |
| watch_runtime_readability | `monster_mob_mycelium_crawler_portrait` | monster | portrait | pass | yes |
| watch_runtime_readability | `monster_mob_nerve_midge_swarm_portrait` | monster | portrait | pass | yes |
| watch_runtime_readability | `monster_mob_rust_cultivator_portrait` | monster | portrait | pass | yes |
| watch_runtime_readability | `monster_mob_rust_hound_portrait` | monster | portrait | pass | yes |
| watch_runtime_readability | `monster_mob_shell_grafter_portrait` | monster | portrait | pass | yes |
| watch_runtime_readability | `monster_mob_soul_midge_swarm_portrait` | monster | portrait | pass | yes |
| watch_runtime_readability | `monster_mob_spore_archer_portrait` | monster | portrait | pass | yes |
| watch_runtime_readability | `order_mage_oracle_sample_icon` | order | icon | pass | yes |
| watch_runtime_readability | `rumor_filter_shortage_icon` | rumor | icon | pass | yes |
| watch_runtime_readability | `rumor_living_mycelium_shortage_icon` | rumor | icon | pass | yes |
| watch_runtime_readability | `rumor_old_blades_icon` | rumor | icon | pass | yes |
| watch_runtime_readability | `ui_combat_feedback_echo_fade` | ui | icon | pass | yes |
| watch_runtime_readability | `ui_combat_feedback_slime_pop` | ui | icon | pass | yes |
| ok_for_current_v2 | `bg_daily_bill` | background | background | pass | yes |
| ok_for_current_v2 | `bg_dungeon_layer_3` | background | background | pass | yes |
| ok_for_current_v2 | `bg_shop_staging` | background | background | pass | yes |
| ok_for_current_v2 | `bg_town_shop` | background | background | pass | yes |
| ok_for_current_v2 | `bg_workshop_home_room` | background | background | pass | yes |
| ok_for_current_v2 | `bg_workshop_studio` | background | background | pass | yes |
| ok_for_current_v2 | `item_con_anchor_charm_icon` | item | icon | pass | yes |
| ok_for_current_v2 | `item_con_purifying_salt_icon` | item | icon | pass | yes |
| ok_for_current_v2 | `item_gear_chain_hook_icon` | item | icon | pass | yes |
| ok_for_current_v2 | `item_gear_corroded_bulwark_icon` | item | icon | pass | yes |
| ok_for_current_v2 | `item_gear_mycelium_cloak_icon` | item | icon | pass | yes |
| ok_for_current_v2 | `item_loot_acid_gland_icon` | item | icon | pass | yes |
| ok_for_current_v2 | `memento_blackmarket_letter` | memento | icon | pass | yes |
| ok_for_current_v2 | `memento_first_return_tag` | memento | icon | pass | yes |
| ok_for_current_v2 | `memento_low_san_blanket` | memento | icon | pass | yes |
| ok_for_current_v2 | `memento_pawn_empty_tag` | memento | icon | pass | yes |
| ok_for_current_v2 | `memento_truth_red_thread` | memento | icon | pass | yes |
| ok_for_current_v2 | `monster_boss_gatekeeper_mk1_combat` | monster | combat_sprite | pass | yes |
| ok_for_current_v2 | `monster_boss_mycelium_oracle_combat` | monster | combat_sprite | pass | yes |
| ok_for_current_v2 | `monster_boss_spore_foundry_combat` | monster | combat_sprite | pass | yes |
| ok_for_current_v2 | `monster_boss_spore_foundry_portrait` | monster | portrait | pass | yes |
| ok_for_current_v2 | `monster_elite_crystal_bulwark_combat` | monster | combat_sprite | pass | yes |
| ok_for_current_v2 | `monster_elite_spore_matriarch_combat` | monster | combat_sprite | pass | yes |
| ok_for_current_v2 | `monster_elite_vein_knight_combat` | monster | combat_sprite | pass | yes |
| ok_for_current_v2 | `monster_mob_acid_slime_mature_combat` | monster | combat_sprite | pass | yes |
| ok_for_current_v2 | `monster_mob_crystal_guard_combat` | monster | combat_sprite | pass | yes |
| ok_for_current_v2 | `monster_mob_echo_pilgrim_combat` | monster | combat_sprite | pass | yes |
| ok_for_current_v2 | `monster_mob_echo_pilgrim_portrait` | monster | portrait | pass | yes |
| ok_for_current_v2 | `monster_mob_lost_miner_echo_combat` | monster | combat_sprite | pass | yes |
| ok_for_current_v2 | `monster_mob_mycelium_crawler_combat` | monster | combat_sprite | pass | yes |
| ok_for_current_v2 | `monster_mob_nerve_midge_swarm_combat` | monster | combat_sprite | pass | yes |
| ok_for_current_v2 | `monster_mob_rust_cultivator_combat` | monster | combat_sprite | pass | yes |
| ok_for_current_v2 | `monster_mob_rust_hound_combat` | monster | combat_sprite | pass | yes |
| ok_for_current_v2 | `monster_mob_shell_grafter_combat` | monster | combat_sprite | pass | yes |
| ok_for_current_v2 | `monster_mob_soul_midge_swarm_combat` | monster | combat_sprite | pass | yes |
| ok_for_current_v2 | `monster_mob_spore_archer_combat` | monster | combat_sprite | pass | yes |
| ok_for_current_v2 | `order_black_contested_core_buyout_icon` | order | icon | pass | yes |
| ok_for_current_v2 | `order_blackmarket_relic_box_icon` | order | icon | pass | yes |
| ok_for_current_v2 | `order_crystal_scale_batch_icon` | order | icon | pass | yes |
| ok_for_current_v2 | `order_spore_cage_procurement_icon` | order | icon | pass | yes |
| ok_for_current_v2 | `order_workshop_spore_core_frame_icon` | order | icon | pass | yes |
| ok_for_current_v2 | `rumor_acid_gland_shortage_icon` | rumor | icon | pass | yes |
| ok_for_current_v2 | `rumor_crystal_scale_contract_icon` | rumor | icon | pass | yes |
| ok_for_current_v2 | `rumor_miner_lamps_icon` | rumor | icon | pass | yes |
| ok_for_current_v2 | `rumor_oracle_fossil_collector_icon` | rumor | icon | pass | yes |
| ok_for_current_v2 | `rumor_spore_amber_jeweler_icon` | rumor | icon | pass | yes |
| ok_for_current_v2 | `ui_combat_feedback_scrap_break` | ui | icon | pass | yes |

## Source Files

- Asset review: `美术文档/_generated/formal_v2_asset_review/formal_v2_asset_review.json`
