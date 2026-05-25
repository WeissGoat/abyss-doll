---
id: config_dolls_readme
title: 人偶基础档案配置字段说明 (Dolls Config)
type: config
role: 策划
domain: config_dolls
status: active
source_of_truth: true
related:
  - 开发文档/数据与实体定义/02_人偶与状态实体.md
  - 配置表(JSON)/README.md
  - 配置表(JSON)/Effects/README.md
  - 设计文档/GDD_08_人偶养成子模块全案.md
  - 设计文档/05_局外成长与维护规则卡.md
  - 设计文档/GDD_12_人偶交互管理器.md
  - 设计文档/GDD_03_人偶实体对象与好感双轨机制.md
  - 设计文档/GDD_11_人偶房间与视觉叙事系统.md
  - 设计文档/GDD_09_标签与特质系统.md
  - 设计文档/06_标签与特质规则卡.md
  - 设计文档/08_人偶核心状态与好感双轨规则卡.md
  - 设计文档/09_人偶交互事件与反馈规则卡.md
  - 设计文档/11_人偶房间布局与视觉叙事规则卡.md
  - 设计文档/36_局外成长正式配置承接审计.md
  - 设计文档/37_局外成长正式配置落地设计.md
  - 设计文档/38_局外成长正式配置实现任务拆分.md
  - 设计文档/39_局外成长正式配置ID锁定与冲突检查.md
  - 设计文档/40_局外成长底盘正式配置落地设计.md
  - 设计文档/41_局外成长效果特质正式配置落地设计.md
  - 设计文档/44_局外成长人偶特质房间正式配置落地设计.md
  - 设计文档/45_局外成长正式配置Validator与固定验收样例.md
last_verified: 2026-05-25
update_rule: 修改配置字段、数据源规则或表间引用时同步本文件。
---

# 人偶基础档案配置字段说明 (Dolls Config)

> 本目录定义可操作人偶的静态基础档案。正式版中，`Dolls` 不保存当前 HP / SAN / Bond / 磨损 / 侵蚀等运行时状态；这些状态属于存档实例。
>
> 当前 JSON 仍可能保留 MVP 字段。正式配置落地时按本文补字段；程序尚未支持的字段先作为策划 / Validator warning，不把正式口径降级为旧字段。

## 正式职责

| 职责 | 说明 |
|---|---|
| 静态身份 | 定义人偶 ID、基础档案、默认房间和默认构筑入口。 |
| 基础上限 | 定义 HP / SAN / Bond 曲线等初始边界，不保存当前值。 |
| 状态规则入口 | 引用情绪、SAN 阈值、Bond 阈值、奇迹和崩溃策略。 |
| 交互入口 | 引用允许的触摸 / 对话 / 赠礼 / 保养 / 特殊交互和反馈池。 |
| 房间入口 | 引用房间偏好、待机规则、房间反馈和默认展示。 |
| 表现 fallback | 所有表情、动作和房间表现必须有 fallback。 |

## 字段说明

| 字段名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| `DollID` | string | 是 | 人偶全局唯一 ID。 |
| `DisplayName` | string | 是 | 玩家可见名称。旧字段 `Name` 仅作为兼容别名。 |
| `BaseProfileID` | string | 是 | 基础档案 ID，用于剧情、UI 和本地化。 |
| `DefaultRoomID` | string | 是 | 默认房间配置 ID。 |
| `BaseMaxHP` | int | 是 | 初始 HP 上限。旧 `Stats.HP_Max` 可兼容映射。 |
| `BaseMaxSAN` | int | 是 | 初始 SAN 上限。旧 `Stats.SAN_Max` 可兼容映射。 |
| `BaseBondCurveID` | string | 是 | Bond 曲线 ID。 |
| `DefaultChassisID` | string | 是 | 初始底盘 ID，必须存在于 `Chassis`。 |
| `InitialUnlockedBlueprints` | string[] | 否 | 初始已解锁图纸。 |
| `InitialLoadoutRef` | string | 否 | 初始装备 / 物品配置引用；不要长期把 `InitialItems` 混在基础档案。 |
| `EmotionStateSet` | string | 是 | 情绪状态集合 ID。 |
| `BondThresholds` | object/string | 是 | Bond 阈值配置或引用。 |
| `SanThresholds` | object/string | 是 | SAN 阈值配置或引用。 |
| `MiraclePolicyID` | string | 是 | 奇迹裁决策略 ID。 |
| `SanBreakPolicyID` | string | 是 | SAN 崩溃裁决策略 ID。 |
| `LikeTags` | string[] | 否 | 偏好标签，用于赠礼 / 互动反馈。 |
| `DislikeTags` | string[] | 否 | 抗拒标签。 |
| `AllowedInteractionRefs` | string[] | 是 | 允许的交互配置 ID。 |
| `FeedbackSetRefs` | string[] | 是 | 反馈池引用。 |
| `RoomPreferenceTags` | string[] | 否 | 房间偏好标签。 |
| `DefaultIdlePoseRules` | object[] | 否 | 房间待机规则。 |
| `RoomFeedbackPools` | string[] | 否 | 房间反馈池。 |
| `ExpressionVisualIDs` | object | 是 | 表情资源引用。 |
| `MotionIDs` | object | 否 | 动作资源引用。 |
| `FallbackVisualID` | string | 是 | 表现资源缺失时的 fallback。 |

## 运行时字段边界

以下字段不得写入静态 `Dolls` 配置：

| 运行时字段 | 归属 |
|---|---|
| `CurrentHP`、`CurrentSAN` | 人偶运行时实例 / 存档。 |
| `WearLevel`、`CorruptionLevel` | 人偶运行时实例 / 维护系统。 |
| `BondValue`、`BondLevel`、`EmotionState` | 人偶运行时实例 / 关系系统。 |
| `TraitInstanceIDs`、`DynamicStateTags` | 存档 / 经历日志。 |
| `MaintenanceHistory`、`RoomMemoryState` | 存档 / 长期记忆。 |

## 兼容字段

| 旧字段 | 正式字段 |
|---|---|
| `Name` | `DisplayName` |
| `Stats.HP_Max` | `BaseMaxHP` |
| `Stats.SAN_Max` | `BaseMaxSAN` |
| `Stats.Power` / `Compute` / `Charm` | 可迁移为 `BaseProfileID` 或后续属性曲线；不再作为唯一成长口径。 |

## 本轮目标

| 模块 | 目标 |
|---|---|
| 静态档案 | 明确 `DollID`、默认底盘、默认房间、基础 HP / SAN / Bond 曲线和表现 fallback。 |
| 交互反馈 | 接入 8 条反馈 ID，包括低 SAN 抗拒、战败道歉、赠礼、维护和 Bond 里程碑。 |
| 房间记忆 | 接入 8 个房间纪念物候选，记录维护、底盘、义体、崩溃恢复、Boss 和撤离经历。 |

## Validator 建议

| ValidatorID | 检查 |
|---|---|
| `CFG-DOLL-001` | `DollID`、基础上限、默认底盘、默认房间和 fallback 资源必填。 |
| `CFG-DOLL-002` | 静态配置不得保存当前 HP / SAN / Bond / 磨损 / 侵蚀等运行时状态。 |
| `CFG-DOLL-003` | `DefaultChassisID`、交互引用、反馈池和房间引用必须存在或明确标记为占位 warning。 |
| `CFG-DOLL-004` | SAN、Bond、奇迹和崩溃策略必须能追到规则文档或配置。 |

## 完成口径

README 字段口径完成只代表 `GROWTH-README` 的文档侧完成，不代表人偶 JSON 已完成。`设计文档/44_局外成长人偶特质房间正式配置落地设计.md` 是 `GROWTH-DOLL-TRAIT-ROOM` 的 JSON 修改前字段级设计，也不代表 JSON 已完成。JSON 修改后必须回写 `agent_status/design.md` 的配置状态和证据入口；如影响长期节点门禁，再通知 PM 更新 `版本规划/09_正式版核心纵切开发路线.md`。
