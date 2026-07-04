---
id: art_ui_formal_v2_scenario_event
title: Scenario Event Formal V2 方案
type: art
role: 美术
domain: ui_design
status: draft
source_of_truth: false
related:
  - 美术文档/ui_design/formal_v2/README.md
  - 美术文档/ui_design/formal_v2/00_formal_v2_ux_ui_overview.md
  - 美术文档/ui_design/formal_v1/scenario_event_v1.md
last_verified: 2026-06-02
update_rule: 编写或确认 scenario_event Formal V2 详细方案时同步本文件。
---

# Scenario Event Formal V2 方案

> **状态：** Formal V2 设计草案。本文用于确认 `scenario_event` 的正式化方向；确认并迁移到 active `screen_layouts.json` 前，不作为程序或素材生成规格。

---

## 1. Formal V1 问题

`scenario_event` Formal V1 已能承接 AVG、系统弹窗、Lore、选项和跳过摘要，但结构偏通用弹窗：

| 问题 | 表现 | V2 处理 |
|---|---|---|
| 事件类型差异弱 | AVG / Lore / SystemModal 共用大卡，气质接近 | 建立同一骨架下的三种视觉皮肤：对白、记录、警告。 |
| 角色存在感不足 | 说话者只是左侧槽位 | 让说话者 / 物品 / 徽记成为事件焦点。 |
| 选项像列表 | 选项行缺少故事感 | 选项像纸签或命令卡，带后果暗示。 |
| 跳过摘要容易被忽略 | Summary 像底部信息 | 跳过摘要做成盖章结果条，必须可读。 |

---

## 2. 玩家目标

玩家进入事件界面时，目标是：

1. 理解当前事件是谁、在哪里、发生了什么。
2. 阅读对白 / 说明 / Lore。
3. 做选择或确认继续。
4. 若跳过，仍能看到关键结果摘要。

---

## 3. Formal V2 体验定位

`scenario_event` 是“故事聚焦层”，不是通用提示框。

```text
当前场景暗化
  -> 事件焦点图
  -> 对白 / 正文
  -> 选择纸签
  -> 继续 / 跳过 / 摘要
```

视觉目标：

* 背后仍能感到事件发生在当前场景。
* 不同事件类型用边框、色调和图标区分。
* 文本区域大而干净，不被装饰压住。
* 选择和跳过摘要有明确层级。

---

## 4. 主结构草案

参考分辨率 `1920x1080`。

```text
┌────────────────────────────────────────────────────────────┐
│ Dimmed Current Scene                                       │
│                                                            │
│ Speaker / Event Focus      Dialogue / Lore Panel           │
│ portrait, emblem, item      text, choice cards             │
│                                                            │
│ Result Summary Strip                         Continue      │
└────────────────────────────────────────────────────────────┘
```

| ZoneID | 建议位置 | 作用 |
|---|---|---|
| `scenario_event_overlay` | `0,0 1920x1080` | 当前场景暗化层。 |
| `event_focus_slot` | `260,190 430x520` | 角色、魔偶、物品、势力徽记或 Lore 图。 |
| `event_text_panel` | `740,190 860x310` | 对白 / Lore / 系统说明。 |
| `event_choice_cards` | `740,540 860x240` | 1-4 个选择纸签。 |
| `event_summary_strip` | `300,820 760x120` | 跳过摘要、奖励 / 惩罚 / 旗标提示。 |
| `event_action_bar` | `1120,820 480x120` | Continue、Skip、Close。 |

---

## 5. 信息层级

默认阅读顺序：

1. 事件类型和焦点对象。
2. 当前正文。
3. 是否有选择。
4. 继续或确认。
5. 跳过摘要 / 结果。

不默认展开：

* 全部事件日志。
* 后台命令列表。
* 过长 Lore 全文，必要时分页。

---

## 6. 行动层级

| 层级 | 行动 | 表达 |
|---|---|---|
| Primary | `Continue` / `ConfirmChoice` | 唯一主按钮。 |
| Secondary | 选择选项、Skip、Close | 选项纸签 / 小按钮。 |
| Tertiary | 查看日志、回看上一句 | 后续可图标化。 |
| Danger | 跳过不可逆事件 | 警示和摘要。 |

---

## 7. 按钮合并 / 降级 / 隐藏策略

| 当前入口 | V2 策略 |
|---|---|
| Continue / Confirm | 根据状态只保留一个主按钮。 |
| Skip | 次行动；不可跳过时禁用并显示原因。 |
| 选项 | 纸签卡，不和主按钮混排。 |
| Close | 只用于非阻塞事件或完成状态。 |

---

## 8. 场景隐喻

事件界面像把当前场景临时“聚焦”：

* 背景暗化但保留当前场景轮廓。
* 左侧像舞台灯照到说话者、物品或徽记。
* 右侧是干净纸面或对话卷轴。
* 选项像放在桌上的纸签。
* 跳过摘要像盖章后的结果条。

---

## 9. 程序迁移影响

确认后建议层级：

```text
ScenarioEventPanel
  ScenarioEventOverlay
  EventFocusSlot
  EventTextPanel
  EventChoiceCards
  EventSummaryStrip
  EventActionBar
```

关键绑定：

| 绑定 | 来源 |
|---|---|
| 事件类型和优先级 | ScenarioEventService。 |
| 说话者 / 图标 / 焦点 | Event payload / visual refs。 |
| 正文和选项 | Localization / Event payload。 |
| 跳过摘要 | Event command summary。 |
| Continue / Choice / Skip | Event service command。 |

---

## 10. 素材需求变化

第一版复用：

| VisualID | 用法 |
|---|---|
| `ui_panel_main` / `ui_panel_info` | 事件文本和摘要。 |
| `ui_list_row_normal` / `ui_list_row_selected` | 选项卡临时底板。 |
| `ui_icon_event` / `ui_icon_lore` / `ui_icon_skip` | 事件类型和跳过。 |
| `ui_icon_warning` | 不可跳过 / 风险。 |
| `doll_proto_0_stand` | 人偶说话者 fallback。 |

可能新增但不立即跑图：

| 候选 VisualID | 说明 |
|---|---|
| `ui_event_dialogue_scroll` | 对白卷轴。 |
| `ui_event_choice_card` | 选择纸签。 |
| `ui_event_result_stamp` | 跳过摘要结果章。 |
| `ui_event_focus_frame` | 说话者 / 物品焦点框。 |

---

## 11. UX 验收标准

1. 3 秒内能看出这是剧情 / 事件界面。
2. 正文区域大而干净，中文不压边。
3. 选项和主行动层级清楚。
4. Skip 不隐藏关键结果摘要。
5. AVG、Lore、SystemModal 可用同一骨架但有视觉区分。

---

## 12. 概念图提示词

```text
Use case: ui-mockup
Asset type: Formal V2 game UI concept art for scenario_event
Primary request: A warm Japanese anime subterranean fantasy story event overlay interface.
Scene/backdrop: dimmed current game scene behind a parchment story overlay, soft magical focus light, subtle particles.
Subject: left event focus slot with doll portrait or faction emblem, right large clean dialogue panel, choice cards below, result summary stamp strip, one clear continue button and small skip option.
Style: soft hand-painted fantasy UI, parchment scrolls, illustrated trim, low information density, clean reading area, no brass palette, no steampunk machinery.
Composition: 16:9 landscape, current scene dimmed behind, story card centered.
Avoid: mobile pop-up ad, visual novel text wall covering everything, dense system log, real readable words, logos, watermark.
```
