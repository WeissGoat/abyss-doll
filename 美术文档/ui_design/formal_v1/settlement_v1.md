---
id: art_ui_formal_v1_settlement
title: 结算界面 Formal V1
type: art
role: 美术
domain: ui_design
status: active
source_of_truth: true
related:
  - 美术文档/ui_design/formal_v1/screen_structure_review.md
  - 美术文档/ui_design/formal_v2/05_settlement_v2.md
last_verified: 2026-05-25
update_rule: 修改结算界面正式结构或程序迁移要求时同步本文件。
---

# 结算界面 Formal V1

> **目标：** 把结算从结果弹窗升级为一次撤离 / 战斗结果报告界面。胜利、HP 战败、SAN 崩溃、复合战败、带出物、损失、收益和下一步压力要有清晰层级。

---

## 1. Formal V1 结构

```text
┌────────────────────────────────────────────────────────────┐
│ Victory / Defeat Background                                │
│                                                            │
│           Outcome Emblem / Title / Defeat Reason            │
│      Summary: HP/SAN outcome, loot, value, losses, rent     │
│                                                            │
│  Brought Out Items / Lost Items / Combat Breakdown          │
│                                                            │
│                Continue to Workshop                        │
└────────────────────────────────────────────────────────────┘
```

### 主区域

| ZoneID | Rect | 目标 |
|---|---|---|
| `settlement_background` | `0,0 1920x1080` | 胜利/失败背景。 |
| `result_card` | `500,150 920x780` | 主结算面板。 |
| `result_header` | `570,190 780x150` | 结果徽记、标题、失败原因、标题分隔。 |
| `summary_area` | `600,340 720x170` | 战斗结果快照、HP/SAN 结论、收益、损失、房租压力摘要。 |
| `loot_breakdown` | `600,530 720x300` | 带出/损失物品列表与战斗失败复盘。 |
| `continue_action` | `790,850 340x80` | 返回工坊按钮。 |

当前已写入 active `screen_layouts.json`，状态为 `active_spec / FormalV1`。程序和素材生成均以 active 规格为准。

---

## 2. 结果状态差异

| 结果 | 背景 / 面板 | 徽记 VisualID | 重点 |
|---|---|---|---|
| 胜利 | `bg_settlement_victory` / `ui_settlement_victory_panel` | `ui_settlement_outcome_victory` | 带出收益、下一次整备。 |
| HP 战败 | `bg_settlement_defeat` / `ui_settlement_defeat_panel` | `ui_settlement_outcome_hp_defeat` | 生命耗尽、损失、回收压力。 |
| SAN 崩溃 | `bg_settlement_defeat` / `ui_settlement_defeat_panel` | `ui_settlement_outcome_san_collapse` | 精神崩溃、后续维护和下潜许可风险。 |
| HP+SAN 复合战败 | `bg_settlement_defeat` / `ui_settlement_defeat_panel` | `ui_settlement_outcome_hp_san_defeat` | 复合失败、收益损失和状态惩罚同时可见。 |
| 队伍溃败 / 未知失败 | `bg_settlement_defeat` / `ui_settlement_defeat_panel` | `ui_settlement_outcome_party_wipe` | 队伍级失败、未知失败或兜底结果。 |

---

## 3. 可复用资源

| VisualID | 用途 |
|---|---|
| `bg_settlement_victory` | 胜利背景。 |
| `bg_settlement_defeat` | 战败背景。 |
| `ui_settlement_victory_panel` | 胜利面板。 |
| `ui_settlement_defeat_panel` | 战败面板。 |
| `ui_settlement_outcome_victory` | 胜利结果徽记。 |
| `ui_settlement_outcome_hp_defeat` | HP 战败结果徽记。 |
| `ui_settlement_outcome_san_collapse` | SAN 崩溃结果徽记。 |
| `ui_settlement_outcome_hp_san_defeat` | HP+SAN 复合战败结果徽记。 |
| `ui_settlement_outcome_party_wipe` | 队伍溃败 / 未知失败结果徽记。 |
| `ui_title_divider` | 标题分隔。 |
| `ui_list_row_normal` | 物品/收益列表行。 |
| `ui_icon_money` | 金币/价值。 |
| `ui_icon_warning` | HP/SAN 失败和损失警告。 |
| `ui_button_primary` | 返回工坊。 |

---

## 4. 程序迁移要求

建议节点：

```text
SettlementPanel_Runtime
  SettlementBackground_Image
  ResultCard_Image
    ResultHeader
      OutcomeEmblem_Image
    TitleDivider_Image
    SummaryArea
    LootBreakdown
    Continue_Button
```

第一轮可以继续使用文本列表，后续再升级为物品图标行。

active 规格中的关键路径：

| 路径 | VisualID / 组件 |
|---|---|
| `InventoryCanvas/SettlementPanel_Runtime/SettlementBackground_Image` | `bg_settlement_victory` / `bg_settlement_defeat` |
| `InventoryCanvas/SettlementPanel_Runtime/SettlementCard_Image` | `ui_settlement_victory_panel` / `ui_settlement_defeat_panel` |
| `InventoryCanvas/SettlementPanel_Runtime/SettlementCard_Image/ResultHeader/OutcomeEmblem_Image` | `Settlement.OutcomeEmblem` / `ui_settlement_outcome_*` |
| `InventoryCanvas/SettlementPanel_Runtime/SettlementCard_Image/ResultHeader` | `Title.Divider` / `ui_title_divider` |
| `InventoryCanvas/SettlementPanel_Runtime/SettlementCard_Image/SummaryArea` | `Icon.Money` / `ui_icon_money`、`Icon.Warning` / `ui_icon_warning` |
| `InventoryCanvas/SettlementPanel_Runtime/SettlementCard_Image/LootBreakdown/ListRow_Template` | `List.Row.Normal` / `ui_list_row_normal` |
| `InventoryCanvas/SettlementPanel_Runtime/SettlementCard_Image/Continue_Button` | `Button.Primary` / `ui_button_primary` |

结果徽记映射：

| 数据 | VisualID |
|---|---|
| `CombatOutcomeReport.OutcomeType = Victory` | `ui_settlement_outcome_victory` |
| `DefeatReason = PlayerHpDepleted` | `ui_settlement_outcome_hp_defeat` |
| `DefeatReason = PlayerSanCollapsed` | `ui_settlement_outcome_san_collapse` |
| `DefeatReason = PlayerHpAndSanDepleted` | `ui_settlement_outcome_hp_san_defeat` |
| `DefeatReason = PlayerFactionWiped` 或 `Unknown` | `ui_settlement_outcome_party_wipe` |

---

## 5. 验收标准

1. 胜利和失败截图背景、面板、标题至少有明确差异。
2. 结果徽记至少能区分胜利、HP 战败、SAN 崩溃、HP+SAN 复合战败和队伍溃败。
3. HP 战败和 SAN 崩溃在标题或摘要第一屏可区分，不只显示通用“失败”。
4. 结果标题、收益/损失摘要、返回工坊按钮可读。
5. 主面板不被背景抢占。
6. 按钮可点击，背景和面板不拦截按钮射线。
7. 文本不溢出主面板。
