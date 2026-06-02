---
id: art_ui_formal_v2_safe_room
title: Safe Room Formal V2 方案
type: art
role: 美术
domain: ui_design
status: draft
source_of_truth: false
related:
  - 美术文档/ui_design/formal_v2/README.md
  - 美术文档/ui_design/formal_v2/00_formal_v2_ux_ui_overview.md
  - 美术文档/ui_design/formal_v1/safe_room_v1.md
last_verified: 2026-06-02
update_rule: 编写或确认 safe_room Formal V2 详细方案时同步本文件。
---

# Safe Room Formal V2 方案

> **状态：** Formal V2 设计草案。本文用于确认 `safe_room` 的正式化方向；确认并迁移到 active `screen_layouts.json` 前，不作为程序或素材生成规格。

---

## 1. Formal V1 问题

`safe_room` Formal V1 已覆盖休整、整理、使用物品、撤离和继续深入，但仍像功能面板叠在背景上：

| 问题 | 表现 | V2 处理 |
|---|---|---|
| 安全感不足 | 房间背景和按钮面板关系弱 | 改成可休息的深渊临时营地，以火光和地面空间承接决策。 |
| 行动并列 | Continue / Extract / Organize / Use Item 权重接近 | Continue 或 Extract 根据上下文成为主行动，整理和使用物品降级。 |
| 背包区域抢画面 | 背包工作台像另一个系统弹窗 | 背包作为地面摊开的整理毯，只显示必要格子和溢出风险。 |
| 魔偶反馈弱 | 通讯面板像信息框 | 让魔偶状态或小投影成为安静陪伴，不做大文本面板。 |

---

## 2. 玩家目标

玩家进入安全区时，目标是：

1. 判断是否继续深入还是撤离。
2. 借安全区处理背包、恢复和消耗品。
3. 看懂当前战利品和携带风险。
4. 获得一次节奏上的喘息。

---

## 3. Formal V2 体验定位

`safe_room` 是“深渊中的安全营地”，不是恢复菜单。

```text
安全营地
  -> 当前状态恢复
  -> 去留决策
  -> 轻量背包整理
  -> 魔偶安静反馈
```

视觉目标：

* 第一眼能感到“这里安全一点”。
* 火光、布毯、补给箱和墙面标记形成场景中心。
* 背包不是弹窗，而是营地里的整理行为。
* 决策明确：继续深入或撤离。

---

## 4. 主结构草案

参考分辨率 `1920x1080`。

```text
┌────────────────────────────────────────────────────────────┐
│ Rest Status / current layer                                │
│                                                            │
│ Safe Camp Scene          Doll Signal / Recovery            │
│ fire, supplies           quiet status feedback             │
│                                                            │
│ Inventory Mat            Decision Panel                    │
└────────────────────────────────────────────────────────────┘
```

| ZoneID | 建议位置 | 作用 |
|---|---|---|
| `safe_room_background` | `0,0 1920x1080` | 完全不透明安全区背景，火光和安全标记。 |
| `rest_status_strip` | `90,70 720x120` | 恢复结果、当前层、HP/SAN 摘要。 |
| `safe_camp_focus` | `430,160 760x430` | 火光、补给箱、休息点，作为视觉中心。 |
| `doll_signal_panel` | `1260,150 420x360` | 魔偶状态、恢复反馈、轻量互动。 |
| `inventory_mat` | `240,650 760x330` | 背包整理毯，保留 100x100 格规则。 |
| `safe_decision_panel` | `1120,660 560x300` | Continue / Extract 主决策和次行动。 |

---

## 5. 信息层级

默认阅读顺序：

1. 当前是否恢复、是否安全。
2. 继续深入还是撤离。
3. 背包是否超载或需要整理。
4. 魔偶状态是否支持继续。

不默认展开：

* 所有恢复公式。
* 全部战利品明细。
* 长对话或背景说明。

---

## 6. 行动层级

| 层级 | 行动 | 表达 |
|---|---|---|
| Primary | `ContinueDive` 或 `Extract` | 当前推荐行动作为唯一主按钮。 |
| Secondary | 整理背包、使用消耗品、另一种去留选择 | 小按钮或图标入口。 |
| Tertiary | 查看恢复详情、查看战利品摘要 | 折叠详情。 |
| Danger | 状态过差继续深入 | 二次确认。 |

---

## 7. 按钮合并 / 降级 / 隐藏策略

| 当前入口 | V2 策略 |
|---|---|
| Continue / Extract | 同屏保留，但只有一个最高权重。 |
| Organize | 变成背包整理毯上的次行动。 |
| Use Item | 图标化放在整理区，不作为大按钮。 |
| Doll Comm | 融入右侧魔偶反馈，不做独立大按钮。 |

---

## 8. 场景隐喻

安全区像深渊墙缝里的临时营地：

* 地面有布毯、背包、药剂和战利品。
* 中央有小灯或火光，照出安全范围。
* 墙上有前人标记、绳索、苔藓和木板。
* 魔偶反馈像小投影、低声对话或坐在光边缘。

---

## 9. 程序迁移影响

确认后建议层级：

```text
SafeRoomPanel
  SafeRoomBackground
  RestStatusStrip
  SafeCampFocus
  DollSignalPanel
  InventoryMat
  SafeDecisionPanel
```

关键绑定：

| 绑定 | 来源 |
|---|---|
| 恢复结果 | SafeRoom / Recovery 服务快照。 |
| 去留状态 | Dungeon run state。 |
| 背包整理 | 复用 InventoryPresentationController。 |
| 使用物品 | ItemUseService。 |
| 魔偶反馈 | Doll state / event feedback。 |

---

## 10. 素材需求变化

第一版复用：

| VisualID | 用法 |
|---|---|
| `bg_safe_room` | 安全区背景，需完全不透明。 |
| `ui_panel_info` | 状态条和反馈面板。 |
| `ui_panel_main` | 决策面板。 |
| `ui_inventory_chassis_panel` / `ui_inventory_slot_available` | 背包整理毯临时承托。 |
| `ui_button_primary` / `ui_button_secondary` / `ui_button_danger` | 去留与风险动作。 |

可能新增但不立即跑图：

| 候选 VisualID | 说明 |
|---|---|
| `ui_safe_room_inventory_mat` | 地面整理毯。 |
| `ui_safe_room_camp_light` | 安全光源 / 火光 UI 焦点。 |
| `ui_safe_room_rest_badge` | 恢复结果徽记。 |

---

## 11. UX 验收标准

1. 3 秒内能看出这是安全区，不是普通背包界面。
2. 继续深入和撤离的决策清楚。
3. 背包格保持 100x100，不被装饰压缩。
4. 背景完全不透明，无黑洞或旧 UI 穿透。
5. 安全感、喘息感和当前风险同时成立。

---

## 12. 概念图提示词

```text
Use case: ui-mockup
Asset type: Formal V2 game UI concept art for safe_room
Primary request: A cozy fantasy and light steampunk safe room interface inside an abyss cave.
Scene/backdrop: small warm camp in a dangerous underground abyss, lantern firelight, cloth mat, supply crates, mossy stone walls, rope marks.
Subject: central safe camp scene, bottom-left inventory mat with grid-like bag area, right decision panel with continue or extract, small doll signal feedback panel, rest status strip.
Style: warm hand-painted fantasy UI, soft parchment panels, brass details, low information density, gentle safe atmosphere.
Composition: 16:9 landscape, camp light as visual center, UI panels integrated into the room.
Avoid: modern sci-fi bunker, dense menu buttons, spreadsheet layout, unreadable tiny text, real readable words, logos, watermark, transparent holes.
```
