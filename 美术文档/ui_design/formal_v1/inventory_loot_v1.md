---
id: art_ui_formal_v1_inventory_loot
title: 战利品拾取界面 Formal V1
type: art
role: 美术
domain: ui_design
status: draft
source_of_truth: true
related:
  - 美术文档/ui_design/formal_v1/screen_structure_review.md
  - 美术文档/ui_design/formal_v2/03_inventory_loot_v2.md
last_verified: 2026-05-23
update_rule: 修改战利品拾取正式结构或程序迁移要求时同步本文件。
---

# 战利品拾取界面 Formal V1

> **目标：** 保留“左背包、右战利品”的取舍核心，但把它从功能弹窗升级为正式的战后清点界面：背包容量压力、掉落物价值、未拾取损失和确认行动都清楚表达。

---

## 1. MVP Baseline 问题

| 项目 | MVP 结构 | 问题 |
|---|---|---|
| 主面板 | 大弹窗承载全部内容 | 可用，但仪式感弱。 |
| 背包 | 左侧格子 | 正确，应保留。 |
| 战利品区 | 右上掉落区 | 需要更明确的安全边距和价值层级。 |
| 详情 | 右下文字区 | 与确认按钮关系弱。 |
| 未拾取损失 | 文本提示 | 应更明确表达“未拿走会丢弃”。 |

---

## 2. Formal V1 结构

```text
┌────────────────────────────────────────────────────────────┐
│ Battle Result / Loot Recovery                              │
│                                                            │
│  Current Backpack                  Loot Cache               │
│  100x100 grid                      draggable loot items     │
│                                                            │
│  Capacity / Chassis Summary        Item Detail / Value      │
│                                                            │
│              Unclaimed Warning + Confirm Continue           │
└────────────────────────────────────────────────────────────┘
```

### 主区域

| ZoneID | Rect | 目标 |
|---|---|---|
| `loot_background` | `0,0 1920x1080` | 战斗后暗化背景。 |
| `loot_modal` | `160,96 1600x888` | 主容器。 |
| `title_area` | `260,140 1400x92` | 战利品拾取标题和本场掉落摘要。 |
| `inventory_grid` | `260,260 640x600` | 玩家背包和底盘。 |
| `loot_cache` | `1010,250 560x420` | 待拾取战利品。 |
| `capacity_summary` | `260,875 640x80` | 背包容量、剩余格、警告。 |
| `item_detail` | `1010,700 560x150` | 当前物品名称、价值、用途。 |
| `confirm_area` | `1010,880 560x84` | 未拾取警告和确认按钮。 |

---

## 3. 设计口径

1. 左侧永远是玩家当前背包，是决策主视角。
2. 右侧是战利品缓存，表现“可拿走但空间有限”。
3. 未拾取物品会丢弃，要在确认区明确提示。
4. 物品详情和价值靠近确认区，帮助玩家做取舍。
5. 背包状态图继续使用 `valid/invalid/hover/locked`，不改变拖拽规则。

---

## 4. 可复用资源

| VisualID | 用途 |
|---|---|
| `bg_combat_abyss` | 暗化背景。 |
| `ui_loot_pickup_panel` | 主容器。 |
| `ui_loot_drop_zone` | 战利品缓存区域。 |
| `ui_inventory_chassis_panel` | 背包底盘。 |
| `ui_inventory_slot_*` | 背包格状态。 |
| `ui_panel_info` | 详情和容量信息。 |
| `ui_button_primary` | 确认继续。 |
| `ui_button_danger` | 放弃/丢弃类动作，如后续需要。 |

---

## 5. 程序迁移要求

现有 `CombatLootUIController` 可以保留业务逻辑，主要调整运行时创建的节点结构和锚点。

建议节点：

```text
CombatLootPanel_Runtime
  LootBackground_Image
  LootModal
    TitleArea
    InventoryAnchor
    LootCache
    CapacitySummary
    ItemDetailPanel
    ConfirmArea
```

---

## 6. 验收标准

1. 左侧背包、右侧战利品、底部确认区结构清晰。
2. 物品可从战利品区拖入背包。
3. 合法/非法格状态可见。
4. 未拾取会丢弃的提示可读。
5. 右侧战利品物品不贴边、不溢出主面板。
6. 背包格仍为 100x100。
