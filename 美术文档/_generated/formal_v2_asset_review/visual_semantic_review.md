# Formal V2 资产语义 / 风格复核

生成时间：`2026-06-09T04:35:00+08:00`

## 结论

这 58 个待程序登记的 Formal V2 Approved 素材，静态技术预检已全部通过：PNG、Unity `.meta`、`formal_ai_v2`、尺寸和 alpha 都符合 Manifest SourceSpec。

美术侧结论是：**允许程序继续登记 / 接入这 58 个 VisualID**。但“技术可交接”不等于“最终视觉完成”。当前最明显的问题是物品图标同质化偏高，很多都读成暗金发光灯具 / 容器；怪物 combat sprite 相对可用，怪物 portrait 中有若干黑底小主体，运行时小头像框内可能不清晰。

## 分层口径

- `ok_for_current_v2`：当前可进入程序接入和运行时验收。
- `watch_runtime_readability`：不阻塞接入，但运行时截图必须重点看小尺寸可读性。
- `secondary_replacement_candidate`：不阻塞接入，但建议进入后续 NovelAI 二次质量替换候选。

## 重点风险

- 物品图标：23 个里有较多“发光黄铜容器 / 灯具”轮廓，消耗品、装备、战利品和交易品之间的视觉差异不足。
- 怪物战斗图：整体比头像强，适合先进入运行时站位和缩放验收。
- 怪物头像：部分是黑底小光点或主体过小，技术上合规，但头像框可读性风险较高。

## 二次替换候选

| VisualID | 风险 | 说明 | 建议 |
|---|---|---|---|
| `item_con_purifying_salt_icon` | secondary | 像灯具或矿灯，不像净化盐 | 重出白色盐晶、玻璃小瓶、符纸封口或散落晶盐 |
| `item_gear_corroded_bulwark_icon` | secondary | 像发光灯筒，不像腐蚀盾墙 / 防具 | 重出锈蚀盾牌、厚重护甲片或腐蚀孔洞 |
| `item_gear_mycelium_cloak_icon` | secondary | 斗篷 / 菌丝披覆语义弱 | 重出布料披风、菌丝边缘、孢子纤维和肩扣 |
| `item_gear_spore_lance_icon` | secondary | 不像长枪，武器方向不明确 | 重出细长枪尖、孢子囊刃、杆柄和侧向武器构图 |
| `item_loot_acid_gland_icon` | secondary | 像灯笼或容器，不像酸腺 | 重出半透明腺体、酸液泡、腐蚀液滴 |
| `item_mat_core_tier2_fragment_icon` | secondary | 核心碎片语义弱，像小台灯 | 重出破裂核心碎片、机械内核残片和缺口边缘 |
| `monster_boss_spore_foundry_portrait` | secondary | 几乎是黑底光圈，角色识别度不足 | 重出半身头部 / 孢子熔炉结构，增加可读轮廓 |
| `monster_mob_acid_slime_mature_portrait` | secondary | 主体过小且像单个灯点 | 重出胶质半透明主体、酸泡和流动轮廓 |
| `monster_mob_echo_pilgrim_portrait` | secondary | 主体太小，黑底占比过大 | 重出更大的朝圣者头肩剪影和提灯结构 |

## 运行时重点复核

| VisualID | 风险 | 说明 |
|---|---|---|
| `item_con_solvent_spray_icon` | watch | 喷雾语义不够明显，小尺寸可能像普通容器 |
| `item_loot_spore_amber_icon` | watch | 琥珀感可用，但仍偏灯具 |
| `item_mat_core_tier3_seed_icon` | watch | 核心种子可用，但与灯具族群接近 |
| `item_trade_luminous_fungus_icon` | watch | 蘑菇帽存在，但主体偏黑 |
| `monster_mob_shell_grafter_portrait` | watch | 轮廓有趣但主体偏小 |
| `monster_mob_soul_midge_swarm_portrait` | watch | 氛围合适，但小光点可能导致头像框辨识弱 |
| `monster_mob_spore_archer_portrait` | watch | 主体靠下且偏小，弓箭语义不明显 |

## 下一步

1. 程序可以继续登记当前 58 个 VisualID。
2. 程序登记后，美术侧用 ArtAcceptance 截图判断 `watch_runtime_readability` 是否真的影响界面。
3. 若运行时截图证明影响明显，再按 `secondary_replacement_candidate` 建立下一批 NovelAI 二次替换，不并发，继续单图串行。
