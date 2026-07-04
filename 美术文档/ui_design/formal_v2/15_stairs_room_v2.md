---
id: art_ui_formal_v2_stairs_room
title: Stairs Room Formal V2 方案
type: art
role: 美术
domain: ui_design
status: draft
source_of_truth: false
related:
  - 美术文档/ui_design/formal_v2/README.md
  - 美术文档/ui_design/formal_v2/00_formal_v2_ux_ui_overview.md
  - 美术文档/ui_design/formal_v1/stairs_room_v1.md
last_verified: 2026-06-02
update_rule: 编写或确认 stairs_room Formal V2 详细方案时同步本文件。
---

# Stairs Room Formal V2 方案

> **状态：** Formal V2 设计草案。本文用于确认 `stairs_room` 的正式化方向；确认并迁移到 active `screen_layouts.json` 前，不作为程序或素材生成规格。

---

## 1. Formal V1 问题

`stairs_room` Formal V1 已覆盖下一层简报、携带风险、背包整理和进入/撤离选择，但结构仍偏信息面板：

| 问题 | 表现 | V2 处理 |
|---|---|---|
| 层间压力不够直观 | 下一层风险写在面板中 | 让阶梯 / 升降井成为视觉中心，深处光雾表现未知风险。 |
| 去留按钮并列 | Descend、Extract、Organize 权重接近 | Descend 是默认主行动，Extract 是明确的结束本轮动作。 |
| 携带风险像数据块 | 背包重量、格外物品、损失风险并列 | 用“携带托盘 / 掉落边缘”表现超载和丢失风险。 |
| 和 safe_room 区分不足 | 都有背包和按钮 | `stairs_room` 更紧张，是最后决策门槛；`safe_room` 是休整。 |

---

## 2. 玩家目标

玩家进入阶梯房间时，目标是：

1. 判断是否进入下一层。
2. 看懂下一层风险和预期收益。
3. 处理当前携带风险和背包压力。
4. 选择继续深入或撤离。

---

## 3. Formal V2 体验定位

`stairs_room` 是“层间门槛”，不是普通房间菜单。

```text
阶梯 / 升降井
  -> 下一层简报
  -> 携带风险
  -> 最后整理
  -> 继续深入 / 撤离
```

视觉目标：

* 阶梯或升降井是画面中心。
* 深处应有明确“更危险”的视觉暗示。
* 背包整理是进入下一层前的最后动作。
* 行动层级比安全区更紧张。

---

## 4. 主结构草案

参考分辨率 `1920x1080`。

```text
┌────────────────────────────────────────────────────────────┐
│ Next Layer Briefing                                        │
│                                                            │
│ Stairs / Shaft Gate        Carry Risk Panel                │
│ visual center              overload, loot risk             │
│                                                            │
│ Inventory Workbench        Descend / Extract               │
└────────────────────────────────────────────────────────────┘
```

| ZoneID | 建议位置 | 作用 |
|---|---|---|
| `stairs_room_background` | `0,0 1920x1080` | 完全不透明阶梯 / 升降井背景。 |
| `stairs_gate_focus` | `560,120 760x540` | 阶梯、升降井、下一层光雾，视觉中心。 |
| `next_layer_briefing` | `90,80 620x210` | 下一层风险、预期奖励、层级名。 |
| `carry_risk_panel` | `1300,100 500x300` | 超载、格外物品、重要战利品风险。 |
| `stairs_inventory_workbench` | `220,690 760x330` | 最后整理背包。 |
| `stairs_decision_panel` | `1080,700 620x300` | Descend、Extract、Organize 次行动。 |

---

## 5. 信息层级

默认阅读顺序：

1. 下一层入口在哪里。
2. 进入下一层风险是什么。
3. 当前携带是否危险。
4. Descend / Extract 选择。
5. 必要时整理背包。

不默认展开：

* 所有下一层节点。
* 完整战利品明细。
* 复杂掉落规则。

---

## 6. 行动层级

| 层级 | 行动 | 表达 |
|---|---|---|
| Primary | `DescendNextLayer` | 唯一主按钮，默认推进。 |
| Secondary | `Extract`、整理背包 | Extract 明确是结束本轮，视觉低于 Descend 但醒目。 |
| Tertiary | 查看风险详情、查看战利品 | 折叠详情。 |
| Danger | 超载仍深入 | 二次确认或风险警示。 |

---

## 7. 按钮合并 / 降级 / 隐藏策略

| 当前入口 | V2 策略 |
|---|---|
| Descend | 主行动。 |
| Extract | 次高权重结束动作，不能和 Descend 同色。 |
| Organize | 背包工作台上的小入口。 |
| 携带风险详情 | 右侧风险卡，默认只显示关键风险。 |

---

## 8. 场景隐喻

阶梯房间像深渊里的门槛：

* 中央有向下的阶梯、缆车或升降井。
* 深处有冷雾、微光和模糊危险轮廓。
* 近处有放在台阶边缘的背包和战利品。
* 超载物品可以靠近边缘，暗示可能丢失。

---

## 9. 程序迁移影响

确认后建议层级：

```text
StairsRoomPanel
  StairsRoomBackground
  StairsGateFocus
  NextLayerBriefing
  CarryRiskPanel
  StairsInventoryWorkbench
  StairsDecisionPanel
```

关键绑定：

| 绑定 | 来源 |
|---|---|
| 下一层风险 | DungeonPreviewService。 |
| 携带风险 | Inventory / RunLoot state。 |
| 背包整理 | 复用 InventoryPresentationController。 |
| Descend / Extract | Dungeon flow service。 |

---

## 10. 素材需求变化

第一版复用：

| VisualID | 用法 |
|---|---|
| `bg_stairs_room` | 阶梯房间背景，需完全不透明。 |
| `ui_panel_info` | 下一层简报和携带风险。 |
| `ui_panel_main` | 决策面板。 |
| `ui_inventory_chassis_panel` / `ui_inventory_slot_available` | 背包工作台。 |
| `ui_button_primary` / `ui_button_secondary` / `ui_button_danger` | Descend / Organize / Extract。 |

可能新增但不立即跑图：

| 候选 VisualID | 说明 |
|---|---|
| `ui_stairs_gate_marker` | 下一层入口焦点。 |
| `ui_carry_risk_tag` | 携带风险标签。 |
| `ui_loot_edge_warning` | 格外物品丢失风险提示。 |

---

## 11. UX 验收标准

1. 3 秒内能看出这是进入下一层前的阶梯房。
2. Descend 是唯一主行动，Extract 明确但不抢主行动。
3. 下一层风险和携带风险可读。
4. 背包格保持 100x100，不被背景或面板遮挡。
5. 背景完全不透明，无透明黑洞。

---

## 12. 概念图提示词

```text
Use case: ui-mockup
Asset type: Formal V2 game UI concept art for stairs_room
Primary request: A Japanese anime subterranean fantasy abyss stair room interface for deciding whether to descend deeper.
Scene/backdrop: underground stone stairwell and carved lift gate, cold mist below, soft rune lights near the player, loot bags near the edge.
Subject: central stairs or shaft gate as visual focus, left next-layer briefing card, right carry risk panel, bottom inventory workbench, one clear descend button and a smaller extract option.
Style: soft hand-painted fantasy UI with subtle danger, parchment panels, stone and cloth details, luminous cave accents, low information density, no brass palette, no steampunk machinery.
Composition: 16:9 landscape, central vertical descent path, decision panel at bottom-right.
Avoid: safe cozy camp feeling, modern sci-fi elevator, dense spreadsheet UI, real readable text, logos, watermark, transparent holes.
```
