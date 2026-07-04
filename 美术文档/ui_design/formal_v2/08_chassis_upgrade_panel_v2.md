---
id: art_ui_formal_v2_chassis_upgrade_panel
title: Chassis Upgrade Panel Formal V2 方案
type: art
role: 美术
domain: ui_design
status: draft
source_of_truth: false
related:
  - 美术文档/ui_design/formal_v2/README.md
  - 美术文档/ui_design/formal_v2/00_formal_v2_ux_ui_overview.md
  - 美术文档/ui_design/formal_v1/chassis_upgrade_panel_v1.md
last_verified: 2026-06-01
update_rule: 编写或确认 chassis_upgrade_panel Formal V2 详细方案时同步本文件。
---

# Chassis Upgrade Panel Formal V2 方案

> **状态：** Formal V2 设计草案。本文用于确认 `chassis_upgrade_panel` 的正式化方向；确认并迁移到 active `screen_layouts.json` 前，不作为程序或素材生成规格。

---

## 1. Formal V1 问题

`chassis_upgrade_panel` Formal V1 已覆盖当前底盘、下一底盘、材料、蓝图和升级按钮，但仍偏工程信息面板：

| 问题 | 表现 | V2 处理 |
|---|---|---|
| 当前 / 下一底盘像数据卡 | 两个预览面板并排，但缺少“升级工程”仪式感 | 改成中央蓝图桌，当前和下一底盘作为两张大蓝图板。 |
| 材料和变化分散 | 容量变化、材料缺口、前置条件在多个区块 | 把变化摘要压缩成底部 delta 条，材料集中成少量 token。 |
| 升级按钮像普通操作 | 主行动视觉权重不足 | 右下只保留一个升级主行动，禁用时显示首个阻塞原因。 |
| 背包容量变化不够直观 | 玩家需要读字段 | 使用格局轮廓或容量徽记表示变化。 |

---

## 2. 玩家目标

玩家进入底盘升级界面时，目标是：

1. 看懂当前底盘和下一底盘的差异。
2. 判断升级是否值得现在做。
3. 确认金币、材料和蓝图是否满足。
4. 执行升级或追踪缺口材料。

---

## 3. Formal V2 体验定位

`chassis_upgrade_panel` 是 `workshop_studio` 内的“底盘 / 蓝图模式页”。

```text
workshop_studio
  -> 切换到底盘模式
  -> 蓝图桌
  -> 当前底盘
  -> 下一底盘
  -> 容量 / 负载变化
  -> 材料 token
  -> 升级确认
```

视觉目标：

* 复用 `workshop_studio` 的工作室底图和魔偶改造椅，只把中央工作台切换为蓝图桌。
* 玩家一眼看到从当前底盘到下一底盘的方向。
* 蓝图、刻度尺、格局轮廓、符文章和材料 token 形成手作工程感。
* 信息密度低，优先突出升级收益和首个阻塞项。
* 不显示完整可拖拽背包，只显示底盘预览。

---

## 4. 主结构草案

参考分辨率 `1920x1080`。

```text
┌────────────────────────────────────────────────────────────┐
│ Header: chassis title / blueprint state / close            │
│                                                            │
│ Current Blueprint        Upgrade Arrow       Next Blueprint │
│ current grid/capacity                       next grid       │
│                                                            │
│ Delta Strip: slots / load / dive readiness                 │
│ Material Tokens                         Primary Action     │
└────────────────────────────────────────────────────────────┘
```

| ZoneID | 建议位置 | 作用 |
|---|---|---|
| `studio_background` | `0,0 1920x1080` | 复用 `workshop_studio` 背景，底盘模式只替换蓝图桌和对比面板。 |
| `blueprint_table` | `180,130 1560x820` | 主工作台。 |
| `current_chassis_blueprint` | `260,250 520x430` | 当前底盘、容量、格局轮廓。 |
| `upgrade_arrow_plate` | `820,330 280x220` | 升级方向、蓝图状态、首个阻塞项。 |
| `next_chassis_blueprint` | `1140,250 520x430` | 下一底盘、容量提升、解锁格局。 |
| `chassis_delta_strip` | `260,720 900x120` | 容量、负载、下潜许可变化。 |
| `chassis_cost_tokens` | `1180,710 260x140` | 材料和金币 token。 |
| `chassis_action_panel` | `1460,710 200x140` | Upgrade / Track / Close。 |

---

## 5. 信息层级

默认阅读顺序：

1. 当前底盘和下一底盘差异。
2. 升级带来的容量 / 负载变化。
3. 是否满足蓝图、金币和材料。
4. 能否升级，不能升级时首个原因。
5. 追踪材料或返回。

不默认展示：

* 完整材料公式。
* 复杂数值推导。
* 多个未来底盘分支。
* 可拖拽背包格。

---

## 6. 行动层级

| 层级 | 行动 | 表达 |
|---|---|---|
| Primary | `UpgradeChassis` | 唯一主按钮。 |
| Secondary | 追踪材料、返回工作室 | 小按钮。 |
| Tertiary | 查看底盘详情、查看来源 | 折叠详情。 |
| Danger | 放弃当前升级计划 | 默认不需要常驻。 |

---

## 7. 按钮合并 / 降级 / 隐藏策略

| 当前入口 | V2 策略 |
|---|---|
| Upgrade | 保留为唯一主行动。 |
| Track Materials | 降级为缺口状态下的次行动。 |
| Postpone | 合并为 Close / 返回，不做危险大按钮。 |
| 材料行按钮 | 不常驻。材料 token 只负责查看详情。 |
| 当前 / 下一底盘详情 | 默认摘要，点击蓝图板展开。 |

---

## 8. 场景隐喻

底盘升级界面像同一个工作室里切换出的蓝图桌：

* 当前底盘在左，下一底盘在右，中间有刻度尺、符文章和方向箭头。
* 桌面有少量材料、核心、螺丝、蓝图章。
* 底盘格局是可读轮廓，不是可操作背包。
* 魔偶改造椅可以保留在右后方或弱化为背景，保持与 `workshop_studio` 的空间连续性。
* 色调保持温暖奇幻，避免冷色科幻面板。

---

## 9. 程序迁移影响

确认后建议层级：

```text
ChassisUpgradePanel
  StudioBackground
  StudioModeTabs
  BlueprintTable
  CurrentChassisBlueprint
  UpgradeArrowPlate
  NextChassisBlueprint
  ChassisDeltaStrip
  ChassisCostTokens
  ChassisActionPanel
```

关键绑定：

| 绑定 | 来源 |
|---|---|
| 当前底盘 | Doll / Chassis 只读快照。 |
| 下一底盘 | 配置和解锁规则。 |
| 容量 / 负载变化 | ChassisUpgradePreviewService。 |
| 材料 / 蓝图 / 金币缺口 | Crafting / Inventory / Economy 检查。 |
| 升级执行 | 领域服务，UI 不直接切换底盘。 |

---

## 10. 素材需求变化

第一版复用：

| VisualID | 用法 |
|---|---|
| `bg_workshop_studio` / `bg_workshop_day` | 工作室背景，优先复用 `workshop_studio`。 |
| `ui_inventory_chassis_panel` | 底盘蓝图承托。 |
| `chassis_*_icon` / `chassis_chassis_*` | 当前 / 下一底盘。 |
| `ui_icon_chassis_upgrade` | 升级方向和主标题。 |
| `ui_icon_blueprint` | 蓝图状态。 |
| `ui_icon_material_need` / `ui_icon_money` | 缺口和费用。 |
| `ui_panel_main` / `ui_panel_info` | 蓝图桌与摘要区。 |

可能新增但不立即跑图：

| 候选 VisualID | 说明 |
|---|---|
| `ui_chassis_blueprint_table` | 蓝图桌主皮肤。 |
| `ui_chassis_upgrade_arrow_plate` | 当前到下一底盘的箭头 / 章。 |
| `ui_chassis_delta_strip` | 容量变化条。 |

---

## 10.1 概念图状态

旧 `chassis_upgrade_panel_formal_v2_concept.png` 已归档到 `concepts/archive/2026-06-01_workshop_studio_and_shop_semantics/`。归档原因：画面把底盘升级做成独立蓝图大场景，但当前方向应是 `workshop_studio` 内的可切换底盘 / 蓝图模式页。

后续重新出图时，提示词必须明确：

* same cozy Japanese anime subterranean fantasy workshop studio as workshop_studio
* chassis blueprint mode overlay
* shared workshop background and doll modification chair
* central blueprint table with current and next chassis
* no separate blueprint room, no standalone engineering hall

---

## 11. UX 验收标准

运行时截图需要满足：

1. 3 秒内能看出这是底盘升级界面。
2. 当前底盘和下一底盘对比是视觉中心。
3. 升级收益和首个阻塞项清楚可读。
4. 不出现可误解为可拖拽背包的真实玩法格。
5. 默认最高权重行动只有 Upgrade。
6. 材料缺口以 token 和短文本表达，不形成长表格。

---

## 12. 用户确认问题

建议确认以下 3 点：

1. `chassis_upgrade_panel` 是否采用“蓝图桌 + 当前 / 下一底盘大对比”的结构。
2. 是否只展示底盘格局预览，不在该界面提供背包拖拽。
3. 是否把材料追踪降级为次行动，Upgrade 保持唯一主行动。
