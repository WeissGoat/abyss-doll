---
id: art_ui_formal_v2_doll_room
title: Doll Room Formal V2 方案
type: art
role: 美术
domain: ui_design
status: draft
source_of_truth: false
related:
  - 美术文档/ui_design/formal_v2/README.md
  - 美术文档/ui_design/formal_v2/00_formal_v2_ux_ui_overview.md
  - 美术文档/ui_design/formal_v1/doll_room_v1.md
last_verified: 2026-06-02
update_rule: 编写或确认 doll_room Formal V2 详细方案时同步本文件。
---

# Doll Room Formal V2 方案

> **状态：** Formal V2 设计草案。本文用于确认 `doll_room` 的正式化方向；确认并迁移到 active `screen_layouts.json` 前，不作为程序或素材生成规格。

---

## 1. Formal V1 问题

`doll_room` Formal V1 已覆盖房间背景、待机人偶、纪念物、窗外状态、日记和观察反馈，但仍偏“带槽位的展示界面”：

| 问题 | 表现 | V2 处理 |
|---|---|---|
| 房间叙事不够自然 | 纪念物、日记、窗外状态像功能区 | 改为真实房间构图：窗、床、桌、展架自然承载信息。 |
| 人偶待机不够中心 | 待机人偶和展柜分区并列 | 人偶是房间的情绪中心，纪念物围绕房间散布。 |
| 纪念物像仓库槽 | 槽位感过强 | 纪念物像摆件、贴纸、挂件或桌面物。 |
| 零压力入口不够明确 | 观察、日记、返回仍像按钮区 | 低密度热点和短反馈，不打断房间感。 |

---

## 2. 玩家目标

玩家进入人偶房间时，目标是：

1. 以低压力方式观察魔偶状态。
2. 回看长期记忆、纪念物和日记。
3. 感受房间随着经历变化。
4. 从房间返回工坊或进入互动。

---

## 3. Formal V2 体验定位

`doll_room` 是“视觉日记房间”，不是仓库，也不是状态表。

```text
人偶房间
  -> 人偶待机
  -> 窗外状态
  -> 纪念物散布
  -> 日记 / 观察反馈
```

视觉目标：

* 房间本身就是主界面。
* 魔偶是情绪中心，但不是唯一可点物。
* 纪念物自然摆放，暗示经历。
* 文本少，反馈短，保留呼吸感。

---

## 4. 主结构草案

参考分辨率 `1920x1080`。

```text
┌────────────────────────────────────────────────────────────┐
│ Window State                                               │
│                                                            │
│ Memento Shelf          Doll Idle Stage        Diary Desk    │
│ room objects           mood / posture        diary notes    │
│                                                            │
│ Detail Whisper / Action Strip                             │
└────────────────────────────────────────────────────────────┘
```

| ZoneID | 建议位置 | 作用 |
|---|---|---|
| `doll_room_background` | `0,0 1920x1080` | 完全不透明人偶房间背景。 |
| `window_state_area` | `110,80 480x330` | 窗外天气、红月、债务阴影、时间氛围。 |
| `memento_shelf` | `1200,160 500x470` | 纪念物、礼物、展示品和空位。 |
| `doll_idle_stage` | `650,190 540x640` | 人偶待机、表情、低 SAN / Bond 姿态。 |
| `diary_desk` | `170,520 480x330` | 日记本、近期记忆、可点条目。 |
| `room_detail_whisper` | `760,860 700x120` | 选中物短反馈。 |
| `room_action_strip` | `1470,860 320x120` | 观察、互动、返回工坊。 |

---

## 5. 信息层级

默认阅读顺序：

1. 魔偶当前状态和房间气氛。
2. 房间里有哪些明显变化。
3. 日记或纪念物是否有新内容。
4. 当前选中物的短反馈。
5. 返回或进入互动。

不默认展开：

* 长日记全文。
* 纪念物完整来源数据。
* 数值细节。

---

## 6. 行动层级

| 层级 | 行动 | 表达 |
|---|---|---|
| Primary | 无强制主行动 | 这是零压力房间，默认不要求玩家推进。 |
| Secondary | 观察、互动、查看日记、返回工坊 | 热点 / 小按钮。 |
| Tertiary | 查看纪念物详情、切换日记条目 | 小热点。 |
| Danger | 无 | 房间不承载高风险操作。 |

---

## 7. 按钮合并 / 降级 / 隐藏策略

| 当前入口 | V2 策略 |
|---|---|
| 观察 / 日记 / 返回 | 降低视觉权重，放在角落行动条。 |
| 纪念物槽位 | 改为自然摆件热点。 |
| 窗外状态 | 融入背景，不做大面板。 |
| 详情 | 只显示短反馈，长文本进入日记展开。 |

---

## 8. 场景隐喻

人偶房间像长期旅途后逐渐被填满的私人空间：

* 窗边显示外界状态和压力。
* 魔偶坐在床边、地毯上或窗光里。
* 纪念物是柜台摆件、墙上挂件、桌上小物，不是整齐格子。
* 日记本在桌上打开，只有被选中时显示短句。
* 房间暖而安静，但低 SAN 或债务压力会改变灯光和窗外色调。

---

## 9. 程序迁移影响

确认后建议层级：

```text
DollRoomPanel
  DollRoomBackground
  WindowStateArea
  MementoShelf
  DollIdleStage
  DiaryDesk
  RoomDetailWhisper
  RoomActionStrip
```

关键绑定：

| 绑定 | 来源 |
|---|---|
| 待机姿态和情绪 | Doll state / room presentation service。 |
| 纪念物 | Room memento / event history。 |
| 窗外状态 | Economy / story / time state。 |
| 日记条目 | Diary / event log service。 |
| 观察 / 互动 | Room interaction service，不推进 Day。 |

---

## 10. 素材需求变化

第一版复用：

| VisualID | 用法 |
|---|---|
| `bg_doll_room_attic` | 人偶房间背景，需完全不透明。 |
| `doll_proto_0_stand` | 人偶待机 fallback。 |
| `ui_room_memento_slot` | 临时纪念物热点。 |
| `ui_icon_diary` / `ui_icon_memento` | 日记和纪念物。 |
| `ui_panel_info` | 短反馈和详情。 |
| `ui_icon_warning` | 低 SAN、债务阴影或缺资源。 |

可能新增但不立即跑图：

| 候选 VisualID | 说明 |
|---|---|
| `bg_doll_room_formal_v2` | 更正式的人偶房间背景。 |
| `ui_memento_shelf_hotspot` | 自然摆件热点。 |
| `ui_diary_desk_panel` | 日记桌面区。 |
| `ui_room_whisper_bubble` | 房间短反馈气泡。 |

---

## 11. UX 验收标准

1. 3 秒内能看出这是人偶房间，不是工坊或仓库。
2. 房间有安全、安静、视觉日记感。
3. 魔偶是情绪中心。
4. 纪念物自然融入房间，不像背包格。
5. 房间操作低压力，不出现主行动压迫。
6. 背景完全不透明，无透明黑洞。

---

## 12. 概念图提示词

```text
Use case: ui-mockup
Asset type: Formal V2 game UI concept art for doll_room
Primary request: A warm Japanese anime subterranean fantasy doll room interface, a quiet visual diary room.
Scene/backdrop: cozy attic-like doll room, soft window light, wooden floor, fabric, plants, small handmade objects, memory shelf, diary desk.
Subject: central doll idle stage, left window state area and diary desk, right memento shelf with small keepsakes, bottom soft detail whisper panel, tiny low-weight action strip.
Style: soft hand-painted fantasy UI, gentle emotional tone, low information density, lived-in room, cloth, wood, plants and keepsake details, no brass palette, no steampunk machinery.
Composition: 16:9 landscape, room itself as main visual, doll centered, UI hotspots integrated naturally.
Avoid: warehouse inventory slots, dense status dashboard, hard industrial lab, real readable text, logos, watermark, transparent holes.
```
