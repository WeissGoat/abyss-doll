---
id: art_ui_formal_v1_settlement
title: 结算界面 Formal V1
type: art
role: 美术
domain: ui_design
status: draft
source_of_truth: true
related:
  - 美术文档/ui_design/formal_v1/screen_structure_review.md
last_verified: 2026-05-23
update_rule: 修改结算界面正式结构或程序迁移要求时同步本文件。
---

# 结算界面 Formal V1

> **目标：** 把结算从结果弹窗升级为一次撤离/战败的仪式界面。胜利、失败、带出物、损失、收益和下一步压力要有清晰层级。

---

## 1. Formal V1 结构

```text
┌────────────────────────────────────────────────────────────┐
│ Victory / Defeat Background                                │
│                                                            │
│                 Result Emblem / Title                      │
│        Summary: depth, loot, value, losses, rent            │
│                                                            │
│  Brought Out Items / Lost Items / Value Breakdown           │
│                                                            │
│                Continue to Workshop                        │
└────────────────────────────────────────────────────────────┘
```

### 主区域

| ZoneID | Rect | 目标 |
|---|---|---|
| `settlement_background` | `0,0 1920x1080` | 胜利/失败背景。 |
| `result_card` | `500,150 920x780` | 主结算面板。 |
| `result_header` | `570,190 780x150` | 标题、结果徽记、标题分隔。 |
| `summary_area` | `600,340 720x170` | 深度、收益、损失、房租压力摘要。 |
| `loot_breakdown` | `600,530 720x300` | 带出/损失物品列表。 |
| `continue_action` | `790,850 340x80` | 返回工坊按钮。 |

---

## 2. 胜利与失败差异

| 内容 | 胜利 | 失败 |
|---|---|---|
| 背景 | `bg_settlement_victory` | `bg_settlement_defeat` |
| 面板 | `ui_settlement_victory_panel` | `ui_settlement_defeat_panel` |
| 重点 | 带出收益、下一次整备 | 损失、回收、压力 |
| 色彩 | 暖光、回收感 | 冷暗、破损感 |

---

## 3. 可复用资源

| VisualID | 用途 |
|---|---|
| `bg_settlement_victory` | 胜利背景。 |
| `bg_settlement_defeat` | 战败背景。 |
| `ui_settlement_victory_panel` | 胜利面板。 |
| `ui_settlement_defeat_panel` | 战败面板。 |
| `ui_title_divider` | 标题分隔。 |
| `ui_list_row_normal` | 物品/收益列表行。 |
| `ui_icon_money` | 金币/价值。 |
| `ui_button_primary` | 返回工坊。 |

---

## 4. 程序迁移要求

建议节点：

```text
SettlementPanel_Runtime
  SettlementBackground_Image
  ResultCard_Image
    ResultHeader
    TitleDivider_Image
    SummaryArea
    LootBreakdown
    Continue_Button
```

第一轮可以继续使用文本列表，后续再升级为物品图标行。

---

## 5. 验收标准

1. 胜利和失败截图背景、面板、标题至少有明确差异。
2. 结果标题、收益/损失摘要、返回工坊按钮可读。
3. 主面板不被背景抢占。
4. 按钮可点击，背景和面板不拦截按钮射线。
5. 文本不溢出主面板。
