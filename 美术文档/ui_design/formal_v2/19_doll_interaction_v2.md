---
id: art_ui_formal_v2_doll_interaction
title: Doll Interaction Formal V2 方案
type: art
role: 美术
domain: ui_design
status: draft
source_of_truth: false
related:
  - 美术文档/ui_design/formal_v2/README.md
  - 美术文档/ui_design/formal_v2/00_formal_v2_ux_ui_overview.md
  - 美术文档/ui_design/formal_v1/doll_interaction_v1.md
last_verified: 2026-06-02
update_rule: 编写或确认 doll_interaction Formal V2 详细方案时同步本文件。
---

# Doll Interaction Formal V2 方案

> **状态：** Formal V2 设计草案。本文用于确认 `doll_interaction` 的正式化方向；确认并迁移到 active `screen_layouts.json` 前，不作为程序或素材生成规格。

---

## 1. Formal V1 问题

`doll_interaction` Formal V1 已覆盖触摸、对话、赠礼、保养和反馈，但仍像交互功能菜单：

| 问题 | 表现 | V2 处理 |
|---|---|---|
| 人偶情绪不够中心 | 菜单和列表占画面，角色像展示槽 | 魔偶作为舞台中心，交互入口围绕角色展开。 |
| 交互入口平铺 | 触摸、对话、赠礼、保养同权 | 默认突出当前交互模式，其他图标化。 |
| 反馈像系统日志 | 结果提示在底部文字条 | 反馈像魔偶表情、动作、短句和柔和状态变化。 |
| 风险状态不明显 | Broken / 低 SAN 可能被温馨 UI 覆盖 | 低 SAN、拒绝和冷却必须有明确视觉。 |

---

## 2. 玩家目标

玩家进入人偶交互界面时，目标是：

1. 观察魔偶当前情绪和状态。
2. 选择合适的互动方式。
3. 赠礼、对话或保养时看到对象和结果。
4. 不误解冷却、拒绝、低 SAN 或互动上限。

---

## 3. Formal V2 体验定位

`doll_interaction` 是“照料与回应”，不是好感菜单。

```text
魔偶舞台
  -> 当前状态
  -> 交互模式环
  -> 礼物 / 话题 / 保养对象
  -> 反馈
```

视觉目标：

* 魔偶是绝对视觉中心。
* 交互入口像围绕角色的小工具和话题卡。
* 反馈短、柔和、情绪化。
* 高风险状态不被可爱感掩盖。

---

## 4. 主结构草案

参考分辨率 `1920x1080`。

```text
┌────────────────────────────────────────────────────────────┐
│ Condition Ribbon                                           │
│                                                            │
│ Interaction Tools       Doll Stage        Gift/Topic Tray  │
│ touch/talk/gift/care    expression        selected objects │
│                                                            │
│ Feedback Dialogue / Result Hint                           │
└────────────────────────────────────────────────────────────┘
```

| ZoneID | 建议位置 | 作用 |
|---|---|---|
| `doll_interaction_background` | `0,0 1920x1080` | 工坊 / 房间照料背景。 |
| `condition_ribbon` | `170,60 1580x90` | HP/SAN、Bond、情绪、每日次数。 |
| `interaction_tools` | `180,230 330x560` | 触摸、对话、赠礼、保养、特殊入口。 |
| `doll_stage` | `610,170 650x650` | 人偶立绘、表情、触摸热区。 |
| `gift_topic_tray` | `1320,230 420x560` | 礼物、话题、保养材料。 |
| `feedback_dialogue` | `420,840 1080x150` | 短台词、结果、限制原因。 |

---

## 5. 信息层级

默认阅读顺序：

1. 魔偶当前情绪。
2. 当前可用交互模式。
3. 当前选中礼物 / 话题 / 保养对象。
4. 交互反馈和限制。
5. 次要状态数值。

不默认展开：

* 完整数值公式。
* 全部历史反馈。
* 长台词。

---

## 6. 行动层级

| 层级 | 行动 | 表达 |
|---|---|---|
| Primary | `ConfirmCurrentInteraction` | 当前模式下唯一主行动或直接热区互动。 |
| Secondary | 切换互动模式、选择礼物 / 话题、返回 | 图标 / 列表。 |
| Tertiary | 查看状态详情、查看冷却原因 | 小图标。 |
| Danger | 低 SAN 强行刺激、拒绝状态继续 | 明确警示或禁用。 |

---

## 7. 按钮合并 / 降级 / 隐藏策略

| 当前入口 | V2 策略 |
|---|---|
| 触摸 / 对话 / 赠礼 / 保养 | 做成左侧模式环，当前模式高亮。 |
| 礼物 / 话题列表 | 右侧托盘，仅当前模式显示。 |
| 反馈提示 | 底部短句，不做日志墙。 |
| 关闭 / 跳过 | 低权重。 |

---

## 8. 场景隐喻

人偶交互像安静的照料时刻：

* 魔偶坐在房间中心或工作室软椅上。
* 周围有小礼物、茶杯、保养工具和日记纸。
* 互动图标像放在桌边的小卡片。
* 低 SAN 状态可以让灯光变冷、背景变暗或出现裂痕边框。

---

## 9. 程序迁移影响

确认后建议层级：

```text
DollInteractionPanel
  DollInteractionBackground
  ConditionRibbon
  InteractionTools
  DollStage
  GiftTopicTray
  FeedbackDialogue
```

关键绑定：

| 绑定 | 来源 |
|---|---|
| HP/SAN/Bond/情绪 | Doll state snapshot。 |
| 交互次数和冷却 | Doll interaction service。 |
| 礼物 / 话题 / 保养对象 | Inventory / topic config / maintenance materials。 |
| 交互执行 | Doll interaction domain service。 |

---

## 10. 素材需求变化

第一版复用：

| VisualID | 用法 |
|---|---|
| `bg_workshop_day` | 照料背景临时复用。 |
| `doll_proto_0_stand` | 人偶展示。 |
| `ui_icon_touch` / `ui_icon_talk` / `ui_icon_gift` / `ui_icon_maintenance` | 交互模式。 |
| `ui_panel_info` | 状态和反馈。 |
| `ui_icon_warning` / `ui_icon_memento` | 风险和特殊交互。 |

可能新增但不立即跑图：

| 候选 VisualID | 说明 |
|---|---|
| `ui_doll_interaction_tool_ring` | 交互模式环。 |
| `ui_doll_feedback_bubble` | 反馈短句气泡。 |
| `ui_doll_gift_tray` | 礼物 / 话题托盘。 |

---

## 11. UX 验收标准

1. 3 秒内能看出这是人偶互动，不是普通菜单。
2. 魔偶是视觉中心。
3. 当前交互模式和可用对象清楚。
4. 反馈短句可读，不压住角色。
5. 低 SAN、拒绝、冷却等限制有明确视觉。

---

## 12. 概念图提示词

```text
Use case: ui-mockup
Asset type: Formal V2 game UI concept art for doll_interaction
Primary request: A warm Japanese anime subterranean fantasy doll interaction interface.
Scene/backdrop: cozy workshop room or doll room with soft interior glow, small gifts, tea cup, care tools, cloth and wood.
Subject: central doll sitting calmly as visual focus, left ring of interaction tools for touch talk gift care, right gift and topic tray, top condition ribbon, bottom soft feedback dialogue bubble.
Style: soft hand-painted fantasy UI, gentle emotional atmosphere, cloth, wood and small keepsake details, low information density, no brass palette, no steampunk machinery.
Composition: 16:9 landscape, doll stage centered, UI surrounding but not covering the character.
Avoid: dating sim text wall, debug stats panel, dense buttons, cold lab scene, real readable words, logos, watermark.
```
