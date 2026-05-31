---
id: art_ui_formal_v2_settlement
title: Settlement Formal V2 方案
type: art
role: 美术
domain: ui_design
status: draft
source_of_truth: false
related:
  - 美术文档/ui_design/formal_v2/README.md
  - 美术文档/ui_design/formal_v2/00_formal_v2_ux_ui_overview.md
  - 美术文档/ui_design/formal_v1/settlement_v1.md
last_verified: 2026-05-31
update_rule: 编写或确认 settlement Formal V2 详细方案时同步本文件。
---

# Settlement Formal V2 方案

> **状态：** Formal V2 设计草案。本文用于给用户确认 `settlement` 的正式化方向；确认并迁移到 active `screen_layouts.json` 前，不作为程序或素材生成规格。

---

## 1. Formal V1 问题

`settlement` Formal V1 已支持胜利、HP 战败、SAN 崩溃、复合战败和队伍溃败等结果，但仍可能像普通结果面板：

| 问题 | 表现 | V2 处理 |
|---|---|---|
| 情绪峰值不足 | 胜败只是标题和列表 | 结果徽记 + 背景气氛 + 报告结构形成仪式感。 |
| 收益损失混在一起 | 战利品、损失、状态、时间线堆叠 | 分成结果摘要、收益 / 损失、复盘、下一步压力。 |
| 下一步不明确 | 返回、继续、账单等按钮并列 | 根据结果给唯一主行动，如返回工坊 / 进入账本。 |
| 战斗复盘占比失衡 | 长日志可能压过结果 | 默认显示最近关键事件，完整时间线折叠。 |

---

## 2. 玩家目标

玩家在结算界面的目标是：

1. 立刻理解这次结果是成功、撤离、HP 战败还是 SAN 崩溃。
2. 看清带回了什么、损失了什么、状态变成什么。
3. 理解这次失败或胜利的主要原因。
4. 知道下一步应该回工坊、处理账本、维护魔偶还是继续流程。

---

## 3. Formal V2 体验定位

`settlement` 是战斗 / 下潜报告，不是普通弹窗。

```text
结果徽记 -> 摘要 -> 收益 / 损失 -> 关键复盘 -> 下一步行动
```

视觉目标：

* 胜利和战败的情绪差异明确。
* 结果报告像一张正式的撤离记录或损伤报告。
* 主行动只有一个。
* 文本密度受控，长时间线可展开。

---

## 4. 主结构草案

参考分辨率 `1920x1080`。

```text
┌────────────────────────────────────────────────────────────┐
│ Result Header: emblem, outcome, short reason               │
│                                                            │
│ Summary Panel       Gains / Losses          Doll Condition │
│ key numbers         loot, money, lost item   HP/SAN/risk   │
│                                                            │
│ Timeline Highlights / Cause of Outcome                    │
│                                                            │
│ Next Pressure                          Primary Next Action │
└────────────────────────────────────────────────────────────┘
```

| ZoneID | 建议位置 | 作用 |
|---|---|---|
| `settlement_backdrop` | `0,0 1920x1080` | 胜利 / 战败氛围背景，不能含透明黑洞。 |
| `result_header` | `260,80 1400x150` | 结果徽记、标题、短原因。 |
| `summary_panel` | `260,260 420x360` | 金币、战利品数量、击败 / 逃离 / 失败摘要。 |
| `gain_loss_panel` | `720,260 500x360` | 带出、遗失、消耗、奖励。 |
| `condition_panel` | `1260,260 400x360` | 魔偶 HP / SAN / 维护风险 / 状态后果。 |
| `timeline_panel` | `260,650 960x220` | 关键复盘 2-4 条，完整展开可选。 |
| `next_action_panel` | `1260,650 400x220` | 下一步压力和唯一主行动。 |

---

## 5. 信息层级

默认阅读顺序：

1. 结果类型和短原因。
2. 最大收益 / 最大损失。
3. 魔偶状态变化。
4. 最近关键事件。
5. 下一步主行动。

不默认展开：

* 完整战斗日志。
* 全部掉落明细。
* 所有数值公式。
* 经济账单长列表。

这些进入展开详情或后续账本界面。

---

## 6. 行动层级

| 层级 | 行动 | 表达 |
|---|---|---|
| Primary | `ReturnWorkshop` / `OpenBill` / `ContinueFlow` | 右下唯一最高权重按钮。 |
| Secondary | 查看全部时间线、查看战利品详情、继续查看账本 | 低权重按钮。 |
| Tertiary | 复制摘要、调试查看 | 正式截图默认隐藏。 |
| Danger | 放弃战利品、典当补款、黑市处理 | 不在默认结算页直接执行。 |

---

## 7. 按钮合并 / 降级 / 隐藏策略

| 当前入口 | V2 策略 |
|---|---|
| 返回工坊 / 继续 | 根据结果只保留一个主行动。 |
| 查看详情 | 降级为展开按钮，不抢结果摘要。 |
| 查看账单 | 若结算后必经账单，主行动可变为进入账本；否则为次级。 |
| 重试 / 调试 | 正式截图隐藏。 |
| 危险经济处理 | 进入账本或市场界面处理，不在结算页直接执行。 |

---

## 8. 场景隐喻

结算界面像一张“撤离报告 / 损伤报告”：

* 成功结果像盖章的回收记录。
* HP 战败像破损修理单。
* SAN 崩溃像污染记录或警告档案。
* 复合失败像红色事故报告。

背景和徽记可以逐步升级，但文字、数值和物品名必须由 Unity Text 渲染，不烘焙进图片。

---

## 9. 程序迁移影响

确认后建议层级：

```text
SettlementPanel
  SettlementBackdrop
  ResultHeader
  SummaryPanel
  GainLossPanel
  ConditionPanel
  TimelinePanel
  NextActionPanel
```

关键绑定：

| 绑定 | 来源 |
|---|---|
| 结果类型 / 原因 | `CombatOutcomeReport` 或 `DungeonSettlementResult`。 |
| 收益 / 损失 | 掉落、背包、金币、物品消耗记录。 |
| 魔偶状态 | HP / SAN / 维护风险快照。 |
| 时间线 | `CombatTimelineRecorder` 或节点事件记录。 |
| 下一步行动 | GameFlow 根据结果决定。 |

约束：

* UI 只展示已结算结果，不二次计算收益损失。
* `OutcomeEmblem` 按状态切换，文本和数字由运行时叠加。
* 背景必须符合 `AlphaRequired=false`，不再出现中间透明黑块。

---

## 10. 素材需求变化

第一版复用：

| VisualID | 用法 |
|---|---|
| `bg_settlement_victory` / `bg_settlement_defeat` | 成功 / 失败背景。 |
| `ui_settlement_victory_panel` / `ui_settlement_defeat_panel` | 主面板皮肤。 |
| `ui_settlement_outcome_*` | 结果徽记。 |
| `ui_panel_info` / `ui_panel_main` | 摘要、收益损失、复盘、下一步。 |
| `ui_button_primary` / `ui_button_secondary` | 主行动和详情展开。 |
| `ui_icon_money` / `ui_icon_warning` | 收益和风险提示。 |

可能新增但不立即跑图：

| 候选 VisualID | 说明 |
|---|---|
| `ui_settlement_report_stamp` | 报告盖章 / 结果状态装饰。 |
| `ui_settlement_timeline_plate` | 时间线事件条底板。 |
| `ui_settlement_loss_badge` | 损失 / 遗失标记。 |

---

## 11. UX 验收标准

运行时截图需要满足：

1. 3 秒内能看出胜利、HP 战败、SAN 崩溃或复合失败。
2. 结果标题、徽记、短原因构成明确视觉焦点。
3. 收益、损失、状态变化和复盘分区清楚。
4. 默认最高权重行动只有一个。
5. 长时间线不压过结果摘要。
6. 背景不出现透明 / 半透明黑块。
7. 中文文本不压边、不和按钮重叠。

---

## 12. 用户确认问题

建议确认以下 3 点：

1. `settlement` 是否采用“报告式结算”结构，而不是普通结果弹窗。
2. 不同失败原因是否用结果徽记 + 色调 + 短原因区分，详细时间线默认折叠。
3. 下一步主行动是否根据结果唯一化，例如返回工坊或进入账本。
