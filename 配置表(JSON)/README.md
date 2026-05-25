---
id: config_configuration_overview
title: 配置表数据总览说明 (Configuration Overview)
type: config
role: 策划
domain: config_data
status: active
source_of_truth: true
related:
  - 开发文档/00_自动化测试框架与流程指南.md
  - 开发文档/15_P0配置Validator与自动验收底座需求.md
  - tools/config/README.md
  - 数值模型设计/00_基准价值与空间本位模型.md
  - 配置表(JSON)/Chassis/README.md
  - 配置表(JSON)/CraftingRecipes/README.md
  - 配置表(JSON)/Dolls/README.md
  - 配置表(JSON)/Dungeons/README.md
  - 配置表(JSON)/Effects/README.md
  - 配置表(JSON)/Economy/README.md
  - 配置表(JSON)/Factions/README.md
  - 配置表(JSON)/Items/README.md
  - 配置表(JSON)/Monsters/README.md
  - 配置表(JSON)/Orders/README.md
  - 配置表(JSON)/Prosthetics/README.md
  - 配置表(JSON)/Rewards/README.md
  - 配置表(JSON)/Rumors/README.md
  - 设计文档/GDD_00_系统关联总图.md
  - 版本规划/_archive/mvp_2026-05/03_mvp数值要求.md
  - 版本规划/_archive/mvp_2026-05/05_MVP需要补充的配置调整.md
  - agent_status/design.md
  - 知识库/views/design.md
  - 知识库/views/program.md
last_verified: 2026-05-25
update_rule: 修改配置字段、数据源规则或表间引用时同步本文件。
---

# 配置表数据总览说明 (Configuration Overview)

> 本目录存放了游戏最小可玩版本（MVP）所需的全部 JSON 格式实体配置文件。
> 所有的数值投放严格遵循《数值模型设计》中的“1格(CV) = 100金币 = 10DPS”基准。

## 目录结构与模块划分

为了方便版本管理与数据解耦，我们采用了**“一物一文件”**的结构，并按以下实体大类进行了文件夹拆分。点击进入各个文件夹，可以查看对应实体配置字段的详细说明表格。

| 文件夹目录 | 存放实体类型 | 核心作用与定位 | 对应系统 |
| :--- | :--- | :--- | :--- |
| **[`/Dolls`](./Dolls/README.md)** | 人偶 (Doll) | 玩家在局内操控的载体档案，包含基础的三维属性和初始状态上限。 | 养成系统 |
| **[`/Chassis`](./Chassis/README.md)** | 底盘 (Chassis) | 决定局内可用**背包网格大小与形状**的核心装备。 | 养成系统 |
| **[`/Items`](./Items/README.md)** | 局内物品 (Item) | 包含武器、防具、战利品、消耗品等所有**需要放在网格里**的物品。是整个数值验证的核心。 | 物品系统 / 经济系统 |
| **[`/Monsters`](./Monsters/README.md)** | 怪物 (Monster) | 深渊中的敌对实体，包含血量、伤害，以及特有的“网格干涉技能”（如锁格子、塞垃圾）。 | 战斗系统 |
| **[`/Rewards`](./Rewards/README.md)** | 奖励表 (Reward) | 统一配置战斗掉落、节点奖励、事件奖励与任务奖励。调用方只引用 `RewardID`。 | 奖励系统 / 掉落系统 |
| **[`/Dungeons`](./Dungeons/README.md)** | 深渊层数 (Dungeon) | 决定每一层地图的长度（节点数）、走一步掉多少SAN，以及怪物和Boss的刷新池。 | 探索系统 |
| **[`/Prosthetics`](./Prosthetics/README.md)** | 义体插件 (Prosthetic) | 局外装备的被动词条插件。**不占背包网格**，改变规则或提供全局增幅。 | 养成系统 |
| **[`/CraftingRecipes`](./CraftingRecipes/README.md)**| 制造配方 (Crafting) | 工坊中制造义体或升级底盘所需的金币与素材清单。 | 经济系统 / 养成系统 |
| **`/Maintenance`** | 维护方案 (Maintenance) | 局外维护方案，定义金币 / 材料成本以及磨损、侵蚀、HP、SAN 修复效果。 | 养成系统 / 下潜许可 |
| **[`/Economy`](./Economy/README.md)** | 小镇经济 (Economy) | 定义日 / 周 / 月周期、月租曲线、欠债利息、轻债阈值和典当折扣。 | 经济压力 / 小镇循环 |
| **[`/Effects`](./Effects/README.md)** | 效果字典 (Effect) | 定义游戏中所有可被物品、怪物、义体调用的基础效果（如加血、叠盾、乘伤）及其参数规范。 | 战斗系统 / 核心框架 |

## 全局通用规范
1. **命名规范：** 所有的 ID (如 `ConfigID`, `MonsterID`) 必须全局唯一，推荐使用小写字母加下划线（如 `gear_rusty_dagger`）。
2. **热更新友好：** 这些纯数据 JSON 文件可以直接通过服务端的 CDN 下发进行热更新和数值微调，无需重新打包客户端。
3. **Unity 运行时副本：** 本目录是版本源；`UnityClient/Assets/StreamingAssets/Configs` 是由脚本生成的运行时副本，不直接纳入 Git、不作为知识库事实来源、不手写维护。进入 Unity、运行配置加载测试或刷新美术 Manifest 前，先执行：

```powershell
.\tools\config\Sync-Configs.ps1 -Clean
```

## 源数据与副本边界

- 策划、数值和程序契约变更以本目录 JSON 与 README 为准。
- Unity 运行时只读取同步后的 `UnityClient/Assets/StreamingAssets/Configs`，该目录内容可以被脚本清理和重建。
- 如果运行时副本与本目录不一致，先同步配置；不要直接修改运行时副本来修复问题。
- 文档索引只收录本目录配置说明，不收录运行时副本说明，避免同一份配置出现两个事实来源。
