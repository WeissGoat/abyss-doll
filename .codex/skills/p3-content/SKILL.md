---
name: p3-content
description: Use when adding or changing Project P3 game content or numbers through config - items and their grid shapes, monsters and intents, rewards and drop tables, dungeon layers and nodes, chassis, prosthetics, crafting, maintenance, economy (rent, prices), dolls, narrative nodes and lines. Covers editing 配置表(JSON), syncing to StreamingAssets, validating, balancing with the autoplay simulation, and checking the result in game.
---

# P3 Content

内容和数值都在 `配置表(JSON)/`，一个条目一个文件。只有需要新机制时才改代码。

## 步骤

1. **读规则**：先读该类目的 `配置表(JSON)/<类目>/README.md`（字段和取值），再挑一两个同类现有条目当模板。数值基准见 `数值模型设计/00_基准价值与空间本位模型.md`（1 格 = 100 金币 = 10 DPS），相关系统规则见 `设计文档/GDD/` 和 `设计文档/规则卡/`。
2. **改或新增 JSON**：
   - ID 全局唯一，小写加下划线，沿用类目前缀：物品 `gear_` / `loot_` / `con_` / `mat_` / `trade_` / `order_`，奖励 `reward_`，底盘 `chassis_`，义体 `pros_`，配方 `craft_`，维护 `maint_`，深渊层 `layer_<n>`。
   - 引用别的配置时用对方的 ID（掉落引用 `RewardID`、物品效果引用效果类型等），并确认被引用的条目存在。
   - 物品的 `Grid.Shape` 是格子坐标列表，坐标数必须等于 `Grid.GridCost`；`BaseValue / GridCost` 要落在 README 写的价值带里。
   - `IconID` 等美术引用可以先指向已有素材；需要新素材时走 `p3-art`。
3. **同步**：`.\tools\config\Sync-Configs.ps1 -Clean`（把配置源复制到 `UnityClient/Assets/StreamingAssets/Configs`）。
4. **校验**：用 `p3-program-validation` 跑 `ConfigValidationSmokeTest.Run`。改了战斗、掉落、背包或经济时，再跑相关 smoke，例如 `CombatLootDropTest`、`DungeonSettlementTest`、`InventoryInteractionServiceSmokeTest`、`TownEconomyOverviewServiceSmokeTest`。
5. **平衡**：会影响循环节奏的改动（掉落、价格、房租、伤害、HP / SAN 消耗）跑一次跑局模拟 `MVPAutoplaytestTest.Run`，对比 `UnityClient/Logs/MVPAutoplaytestReport.json` 里的通关率、下潜次数、收入和升级次数。
6. **进游戏看**：用 `p3-playtest` 到达相关界面截图，例如物品在背包里的形状和数值显示、掉落是否出现、价格和房租提示。
7. **新字段或新机制**：同步更新该类目的 README；确认配置加载（`UnityClient/Assets/Scripts/Core/ConfigManager.cs`）和校验（`Core/ConfigValidator.cs`）读得到新字段。新的效果类型在 `Scripts/Effects/Implementations/` 加一个继承 `EffectBase` 的类，`EffectFactory` 会通过反射自动注册。

## 硬规则

- 只改 `配置表(JSON)/`，不改 `UnityClient/Assets/StreamingAssets/Configs`。原因：那是同步副本，下次同步就被覆盖。
- 删除或改名 ID 前，在全仓库搜索引用（配置、代码、剧情配置、美术 Manifest）。原因：断掉的引用要到运行时才报错。
