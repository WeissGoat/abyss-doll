---
id: dev_15_p0_config_validator_acceptance
title: P0配置Validator与自动验收底座需求
type: dev
role: 程序
domain: config_validation
status: active
source_of_truth: true
related:
  - 版本规划/09_正式版核心纵切开发路线.md
  - 版本规划/11_纵切批次与需求文档承接矩阵.md
  - 开发文档/00_程序开发大纲.md
  - 开发文档/12_程序开发优化建议与重构路线.md
  - 开发文档/rules/13_编程规范与架构约定.md
  - 开发文档/14_Unity运行时美术自动验收方案.md
  - 开发文档/rules/09_视觉资源系统程序开发规范.md
  - 设计文档/delivery/15_P0主干配置表现验收承接清单.md
  - 设计文档/config/validation/35_前三层正式配置Validator与固定Seed验收样例.md
  - 设计文档/config/validation/45_局外成长正式配置Validator与固定验收样例.md
  - 配置表(JSON)/README.md
  - tools/config/README.md
  - tools/agent/README.md
  - agent_status/program.md
  - agent_status/design.md
  - agent_status/pm.md
  - PROJECT_STATUS.md
last_verified: 2026-05-24
update_rule: 调整 P0 配置校验范围、自动验收命令、报告格式、通过标准或门禁等级时同步本文件。
---

# P0配置Validator与自动验收底座需求

> **定位：** 本文档把 `P0 配置 / Validator / 验收底座` 翻译成程序可开发需求。它不定义玩法规则，不替代 GDD、规则卡或配置 README；它定义工具要检查什么、怎么报错、怎么一键运行、什么条件下算 P0 通过。

---

## 1. 背景与目标

正式版核心纵切已经不再以 MVP 临时验证为目标。后续每个系统都要按正式版标准落地，但如果配置字段、ID 引用、VisualID、地图 seed、怪物意图和验收入口仍依赖人工肉眼检查，后续开发会反复出现以下问题：

| 风险 | 后果 |
|---|---|
| 配置字段写错或旧字段残留 | 程序运行时 fallback，真实错误被隐藏。 |
| ID 引用失效 | 掉落、制造、订单、地图节点或视觉资源在运行时才断。 |
| 怪物意图 / 地图画像不可复现 | 策划无法稳定验收战斗压力和路线节奏。 |
| 美术 VisualID 缺失 | UI 或战斗截图出现空图、黑块、错误占位。 |
| 验收命令分散 | 不同 agent 各跑各的，提交前无法形成统一门禁。 |

P0 底座的目标是：

```text
配置源 -> 配置同步 -> ConfigValidator -> 核心 smoke tests -> UI / 美术校验 -> 统一报告
```

让后续 P1 背包战斗、P2 深渊地图、P3 局外成长和 P4 经济压力开发之前，先拥有一套能稳定发现配置与验收断点的工具链。

---

## 2. 范围

### 2.1 本文档覆盖

| 模块 | 覆盖内容 |
|---|---|
| `ConfigValidator` 扩展 | 配置结构、必填字段、枚举、ID 引用、VisualID、内容包引用、地图 seed、怪物意图。 |
| 一键 P0 验收命令 | 同步配置、运行配置校验、触发 Unity smoke test、调用 UI / 美术校验、汇总结果。 |
| 统一报告 | 输出机器可读 JSON 和人类可读 Markdown，区分 error / warning / info。 |
| 门禁标准 | 明确哪些问题必须阻塞提交，哪些可以记录为后续补强。 |
| P0 验收 seed | 固定少量可复现路径，覆盖第一层、第二层直达、战败损失和地图路线。 |

### 2.2 本文档不覆盖

| 不覆盖项 | 归属 |
|---|---|
| 具体物品、怪物、地图节点怎么设计 | `设计文档/delivery/15_P0主干配置表现验收承接清单.md` 和内容包。 |
| 背包旋转、物品生命周期、怪物意图规则 | 对应 GDD 和规则卡。 |
| Unity UI 视觉结构设计 | `美术文档/ui_design/screen_layouts.json` 和美术文档。 |
| 大量新内容生产 | `设计文档/content_packs/18_正式版内容生产规格.md`。 |
| 修复配置错误 | 对应配置 / 策划 / 程序任务；Validator 只发现问题，不静默修复。 |

---

## 3. 现有工具基线

| 工具 / 系统 | 当前作用 | P0 需要补强 |
|---|---|---|
| `tools/config/Sync-Configs.ps1` | 将 `配置表(JSON)` 同步到 Unity `StreamingAssets`。 | 纳入一键 P0 验收首步，失败时直接中断。 |
| `ConfigValidator` | 已承担部分配置校验。 | 扩大到 P0 主干字段、引用、VisualID、地图 seed、怪物意图和内容包引用。 |
| `tools/agent/Invoke-UnitySmokeTests.ps1` | 触发若干 Unity smoke test。 | 支持被 P0 一键命令调用，并输出可汇总状态。 |
| `tools/美术工具/Validate-UIDesign.ps1` | 校验 active UI 规格。 | 作为 P0 表现校验步骤之一。 |
| ArtAcceptance | 运行时截图验收与 UI 风险报告。 | 作为 P0/P1/P2 表现门禁，至少能被统一报告引用 latest 结果。 |
| `tools/docs/Validate-Docs.ps1` | 校验文档索引和双向关系。 | 作为文档变更任务的附加门禁，不作为玩法配置必跑项。 |
| `tools/agent/Invoke-AgentHealthCheck.ps1` | 开工前工作区风险检查。 | 一键 P0 验收可提示但不替代它。 |

---

## 4. 推荐命令

新增脚本：

```powershell
.\tools\agent\Invoke-P0Validation.ps1
```

建议参数：

| 参数 | 默认 | 说明 |
|---|---|---|
| `-SkipUnity` | false | 跳过 Unity smoke test，只跑静态配置和文档 / UI 规格校验。 |
| `-SkipArtAcceptance` | true | 默认不重跑截图验收，只读取 latest；需要时显式开启重跑。 |
| `-Strict` | false | warning 升级为失败，供提交前或候选版本门禁使用。 |
| `-OutputRoot` | `UnityClient/Logs/P0Validation/latest` | 输出报告目录。 |
| `-History` | false | 是否把本次结果复制到 history 时间戳目录。 |
| `-SeedProfile` | `p0_core` | 指定验收 seed 组。 |

基础执行顺序：

```text
1. tools/config/Sync-Configs.ps1 -Clean
2. ConfigValidator.ValidateAllConfigs()
3. Invoke-UnitySmokeTests.ps1
4. Validate-UIDesign.ps1
5. 读取 ArtAcceptance latest report
6. 汇总 P0Validation report
```

说明：

*   `-SkipUnity` 只能用于快速静态检查，不能作为提交前完整 P0 门禁。
*   `-SkipArtAcceptance` 默认 true 是为了避免每次配置校验都重跑截图；但报告必须显示 latest 截图时间和对应 active UI 规格是否过期。
*   如果 Unity 已被前台实例占用导致 batchmode 不能运行，报告应记录 `blocked_by_unity_instance`，不能伪装为通过。

---

## 5. ConfigValidator 校验范围

### 5.1 全局规则

| 编号 | 校验项 | 等级 | 说明 |
|---|---|---|---|
| `CFG-GLOBAL-001` | 所有配置 ID 唯一且非空 | error | 同目录内不允许重复 ID；跨目录引用必须带类型上下文。 |
| `CFG-GLOBAL-002` | 所有必填字段存在 | error | 按配置 README / schema / 代码实体共同定义。 |
| `CFG-GLOBAL-003` | 枚举值可解析 | error | 禁止业务逻辑靠裸字符串兜底。 |
| `CFG-GLOBAL-004` | 数值范围合法 | error / warning | 负数、0、超上限按字段语义判断。 |
| `CFG-GLOBAL-005` | 旧字段残留 | error | 已废弃字段不保留兼容路径。 |
| `CFG-GLOBAL-006` | 配置源和运行时副本一致 | error | 同步后 `StreamingAssets/Configs` 不应落后。 |

### 5.2 Items

事实来源：`GDD_01`、`GDD_06`、`02_物品背包旋转与生命周期规则卡`、`15_P0主干配置表现验收承接清单`。

| 编号 | 校验项 | 等级 |
|---|---|---|
| `CFG-ITEM-001` | `ItemID`、`DisplayName`、`Rarity`、`IconVisualID`、`DescriptionKey` 必填。 | error |
| `CFG-ITEM-002` | `ShapeCells` 至少 1 格，坐标连续且不重复。 | error |
| `CFG-ITEM-003` | `CanRotate=false` 时不允许配置多个旋转角。 | error |
| `CFG-ITEM-004` | `AllowedRotations` 只能使用项目允许角度，默认 `0/90/180/270`。 | error |
| `CFG-ITEM-005` | 旋转后占格不应产生非法重复格。 | error |
| `CFG-ITEM-006` | `Tags`、`RiskTags`、`PlacementTags` 必须来自标签白名单或标签配置。 | error |
| `CFG-ITEM-007` | 主动使用物品必须有 `ApCost`、`TargetRule`、`EffectRefs` 或可执行 payload。 | error |
| `CFG-ITEM-008` | 战斗效果、制造配方、掉落池、奖励引用存在。 | error |
| `CFG-ITEM-009` | `IconVisualID` 存在于 `VisualAssetRegistry` 或可接入资源清单。 | error |
| `CFG-ITEM-010` | 不可出售物必须说明硬阻止原因：委托物、剧情锁定或情感锚点。 | warning |
| `CFG-ITEM-011` | 绑定物必须配置战败损坏 / 保留规则。 | error |

### 5.3 Monsters

事实来源：`GDD_01`、`03_战斗回合与怪物意图规则卡`、`11_怪物AI与行动系统(MonsterActionAI).md`。

| 编号 | 校验项 | 等级 |
|---|---|---|
| `CFG-MON-001` | `MonsterID`、名称、层级范围、HP、基础意图组必填。 | error |
| `CFG-MON-002` | `IntentType` 必须来自合法枚举。 | error |
| `CFG-MON-003` | 每个意图必须有玩家可读文本 / 图标引用 / 目标说明。 | error |
| `CFG-MON-004` | 攻击意图必须有伤害、目标规则和行动时机。 | error |
| `CFG-MON-005` | 对包干涉意图必须有格子选择规则、影响范围和失败反馈。 | error |
| `CFG-MON-006` | 权重总和、冷却、前置条件不应导致无可用行动。 | error |
| `CFG-MON-007` | 多怪行动顺序可由速度、优先级或固定规则稳定排序。 | error |
| `CFG-MON-008` | 掉落池和奖励引用存在。 | error |
| `CFG-MON-009` | `CombatVisualID` 存在，并和美术资源接入口一致。 | error |
| `CFG-MON-010` | Boss / 精英必须配置至少一个区别于普通攻击的机制意图。 | warning |

### 5.4 Dungeons

事实来源：`GDD_02`、`15_P0主干配置表现验收承接清单`、`19/20/25` 内容包。

| 编号 | 校验项 | 等级 |
|---|---|---|
| `CFG-DUN-001` | `LayerID`、`LayerName`、`MapProfileID`、节点池、Boss、解锁条件必填。 | error |
| `CFG-DUN-002` | 地图行数、每行节点数、边数量在允许范围内。 | error |
| `CFG-DUN-003` | 节点类型权重总和合法，必出节点可被生成流程满足。 | error |
| `CFG-DUN-004` | Boss / SafeZone / Stairs 的固定规则与 GDD 不冲突。 | error |
| `CFG-DUN-005` | 节点池引用的战斗、事件、奖励、商店、安全区配置存在。 | error |
| `CFG-DUN-006` | `RunSeed + LayerID + MapProfileID` 可生成稳定摘要。 | error |
| `CFG-DUN-007` | 已通层直达配置不能要求玩家重新打一遍前一层。 | error |
| `CFG-DUN-008` | 安全区免费恢复 HP / SAN 的配置口径一致。 | error |
| `CFG-DUN-009` | 地图节点、背景、特殊房间 VisualID 存在或有明确 fallback。 | warning / error |
| `CFG-DUN-010` | 前三层内容包 `19/20/25` 的关键条目能被配置引用。 | warning |

### 5.5 Rewards

| 编号 | 校验项 | 等级 |
|---|---|---|
| `CFG-REWARD-001` | `RewardID` 唯一且可被掉落、订单、Boss、事件引用。 | error |
| `CFG-REWARD-002` | 奖励引用的 Item / Currency / Unlock / Reputation 存在。 | error |
| `CFG-REWARD-003` | Boss 保底奖励存在且不会引用后续未开放层级必需内容。 | error |
| `CFG-REWARD-004` | 奖励池权重合法，空池必须有明确原因。 | error |
| `CFG-REWARD-005` | 关键成长材料至少有一个可追溯来源。 | warning |

### 5.6 VisualID

| 编号 | 校验项 | 等级 |
|---|---|---|
| `CFG-VIS-001` | 配置引用的核心 VisualID 已登记。 | error |
| `CFG-VIS-002` | 物品图标、怪物战斗图、地图节点图标、关键 UI 图标不能缺失。 | error |
| `CFG-VIS-003` | draft UI 或后续批次资源可以 warning，但不得影响 active 界面。 | warning |
| `CFG-VIS-004` | Approved 资源存在但未登记时给出可操作提示。 | warning |
| `CFG-VIS-005` | 已登记资源文件丢失或 GUID 异常。 | error |

---

## 6. P0 验收 seed

新增固定 seed 组 `p0_core`。每个 seed 至少输出：

```text
LayerID / RunSeed / MapProfileID / NodeSummary / EncounterSummary / RewardSummary
```

| SeedID | 用途 | 最低期望 |
|---|---|---|
| `P0-L1-RUN-001` | 第一层基础闭环 | 普通战斗 -> 战利品拾取 -> Boss -> 安全区撤离可跑通。 |
| `P0-L2-DIRECT-001` | 第二层直达 | 已通第一层后可从第二层出发，不需要重打一层。 |
| `P0-L2-BAG-001` | 背包压力 | 至少出现一个对包干涉怪和一个需要旋转 / 舍弃的战利品。 |
| `P0-FAIL-001` | 战败损失 | 本轮新获物、绑定物、安全盒物品按规则分流。 |
| `P0-MAP-ROUTE-001` | 地图路线 | 至少有一个绕开精英或追逐奖励的路线选择。 |

这些 seed 不要求覆盖全部游戏内容，只要求成为回归测试的稳定锚点。若配置变动导致 seed 摘要变化，报告必须显示差异。

前三层正式配置的更细样例由策划侧 `设计文档/config/validation/35_前三层正式配置Validator与固定Seed验收样例.md` 承接。程序侧实现 P0 / 后续 Validator 时，应把本文的 `p0_core` seed 组视为底座，把 `35` 中的 `V-L1-*`、`V-L2-*`、`V-L3-*` 视为前三层正式配置扩展验收样例。

---

## 7. 报告格式

输出目录：

```text
UnityClient/Logs/P0Validation/latest/
  report.json
  report.md
  config_validation.json
  smoke_tests.json
  ui_validation.json
  art_acceptance_summary.json
```

`report.json` 最低字段：

```json
{
  "status": "Passed | Failed | Blocked",
  "startedAt": "2026-05-24T00:00:00+08:00",
  "durationMs": 0,
  "strict": false,
  "steps": [
    {
      "name": "ConfigValidator",
      "status": "Passed",
      "errorCount": 0,
      "warningCount": 0,
      "output": "UnityClient/Logs/P0Validation/latest/config_validation.json"
    }
  ],
  "errors": [],
  "warnings": [],
  "artAcceptanceLatest": {
    "reportPath": "UnityClient/Logs/ArtAcceptance/latest/report.json",
    "generatedAt": "",
    "isStaleAgainstActiveUi": false
  }
}
```

`report.md` 面向人阅读，至少包含：

1. 总状态。
2. 每一步通过 / 失败 / 阻塞。
3. Error 列表，按配置目录和 ID 分组。
4. Warning 列表，按优先级排序。
5. ArtAcceptance latest 是否过期。
6. 下一步建议：例如“补 `monster_xxx.CombatVisualID`”。

---

## 8. Error / Warning 分级

| 等级 | 含义 | 是否阻塞 |
|---|---|---|
| `error` | 会导致运行时断链、核心玩法错误、正式 active UI 缺图、规则无法复现。 | 是 |
| `warning` | 不阻塞当前运行，但会影响可读性、内容完整度、后续维护。 | `-Strict` 下阻塞 |
| `info` | 统计、建议或非当前批次提示。 | 否 |
| `blocked` | 工具无法运行，例如 Unity 被占用、配置文件无法读取。 | 是 |

不得把 `error` 自动降级为 runtime fallback。

---

## 9. 通过标准

P0 普通通过：

| 项 | 标准 |
|---|---|
| 配置同步 | 成功。 |
| ConfigValidator | `errorCount = 0`。 |
| Unity smoke tests | 必跑项通过，或明确 `-SkipUnity` 只作为快速检查。 |
| UI 规格校验 | active `screen_layouts.json` 校验通过。 |
| ArtAcceptance | latest report 可读取；若过期，普通模式 warning，Strict 模式失败。 |
| 报告 | `report.json` 和 `report.md` 成功输出。 |

P0 Strict 通过：

| 项 | 标准 |
|---|---|
| 所有普通通过条件 | 满足。 |
| warnings | `warningCount = 0` 或全部在允许白名单中。 |
| ArtAcceptance | latest 不过期，且无 errors。 |
| Seed 摘要 | 固定 seed 摘要稳定或差异被显式接受。 |

---

## 10. 开发拆分建议

### P0A：静态配置校验扩展

目标：

* 扩展 `ConfigValidator`，先覆盖 Items / Monsters / Dungeons / Rewards / VisualID 的必填、枚举、ID 引用和资源引用。

出口：

* 能输出结构化 `config_validation.json`。
* 人为制造 3-5 个错误配置时，Validator 能稳定报错。

### P0B：一键脚本与报告

目标：

* 新增 `tools/agent/Invoke-P0Validation.ps1`。
* 串联配置同步、ConfigValidator、Unity smoke test、UI 校验和 ArtAcceptance latest 摘要。

出口：

* 生成 `report.json` 和 `report.md`。
* Unity 被占用时返回 blocked，不误报通过。

### P0C：Seed 验收和内容包引用

目标：

* 固定 `p0_core` seed 组。
* 检查 `19/20/25` 内容包关键条目在配置中的落点和引用。
* 为 `设计文档/config/validation/35_前三层正式配置Validator与固定Seed验收样例.md` 中的前三层 seed 样例预留扩展入口。

出口：

* P0 seed 摘要可稳定输出。
* 关键内容包条目缺配置时给出 warning 或 error。

### P0D：Strict 门禁

目标：

* 提供提交前 / 候选版本前使用的 Strict 模式。
* 将 stale ArtAcceptance、warning 白名单和 seed 差异纳入门禁。

出口：

* `Invoke-P0Validation.ps1 -Strict` 可作为候选版本前检查命令。

---

## 11. 与后续功能的关系

P0 不是“暂停做玩法”，而是降低后续玩法返工概率。

| 后续功能 | 依赖 P0 的原因 |
|---|---|
| P1 背包战斗 | 旋转、物品生命周期、怪物意图和掉落都需要配置和 Validator 保底。 |
| P2 深渊地图 | 地图生成、层级直达、安全区、节点池和 seed 复现需要可机械检查。 |
| P3 局外成长 | 底盘、义体、配方、材料缺口和下潜许可需要 ID 引用和奖励来源校验。 |
| P4 经济压力 | 售价、账单、订单、奖励和材料目标需要跨配置引用一致。 |
| P5 表现支撑 | UI / 美术资源接入需要 VisualID 和截图验收进入常规门禁。 |
