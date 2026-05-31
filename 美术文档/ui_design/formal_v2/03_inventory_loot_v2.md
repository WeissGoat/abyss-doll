---
id: art_ui_formal_v2_inventory_loot
title: Inventory Loot Formal V2 方案
type: art
role: 美术
domain: ui_design
status: draft
source_of_truth: false
related:
  - 美术文档/ui_design/formal_v2/README.md
  - 美术文档/ui_design/formal_v2/00_formal_v2_ux_ui_overview.md
  - 美术文档/ui_design/formal_v1/inventory_loot_v1.md
last_verified: 2026-05-31
update_rule: 编写或确认 inventory_loot Formal V2 详细方案时同步本文件。
---

# Inventory Loot Formal V2 方案

> **状态：** Formal V2 设计草案。本文用于给用户确认 `inventory_loot` 的正式化方向；确认并迁移到 active `screen_layouts.json` 前，不作为程序或素材生成规格。

---

## 1. Formal V1 问题

`inventory_loot` Formal V1 已有左背包、右战利品缓存和确认区，但正式感仍可能不足：

| 问题 | 表现 | V2 处理 |
|---|---|---|
| 拾取像弹窗 | 只是物品列表 + 背包格 | 改为撤离前清点台，强调“带出决策”。 |
| 容量压力不够强 | 容量只是文字说明 | 容量、未带出损失和高价值物品成为主信息。 |
| 操作路径不清 | 拾取、丢弃、确认、返回权重接近 | 确认带出是唯一主行动，丢弃是危险行动。 |
| 详情弱 | 物品价值、用途、风险不够集中 | 选中物品进入详情区，列表只显示摘要。 |

---

## 2. 玩家目标

玩家在战利品清点界面的目标是：

1. 判断这轮战斗 / 节点拿到了什么。
2. 在有限背包容量内决定哪些物品带走。
3. 理解未带走物品的损失。
4. 确认清点并回到地图、房间或结算流程。

---

## 3. Formal V2 体验定位

`inventory_loot` 是“撤离清点台”，不是普通物品弹窗。

```text
战利品缓存 -> 物品详情 / 价值判断 -> 当前背包 -> 确认带出
```

视觉目标：

* 左右形成明确比较：当前背包容量 vs 待带走战利品。
* 高价值 / 任务相关 / 风险物品能被快速看见。
* 默认只展开选中物品详情，避免列表塞满字段。
* 背包仍是玩法对象，`100x100` 格不缩放。

---

## 4. 主结构草案

参考分辨率 `1920x1080`。

```text
┌────────────────────────────────────────────────────────────┐
│ Loot Header: source, danger cleared, capacity summary       │
│                                                            │
│ Loot Cache List        Item Detail        Backpack Grid     │
│ gained items           value / use / tag   current layout   │
│                                                            │
│ Loss Preview / Capacity Pressure / Quest Reminder           │
│                                                            │
│ Secondary: auto place / sort       Primary: confirm carry   │
└────────────────────────────────────────────────────────────┘
```

| ZoneID | 建议位置 | 作用 |
|---|---|---|
| `loot_header` | `96,52 1728x88` | 来源、战斗结果、容量摘要。 |
| `loot_cache` | `96,170 470x650` | 待拾取战利品列表 / 图标格。 |
| `item_detail` | `590,170 470x650` | 选中物品价值、用途、标签、带出建议。 |
| `backpack_grid` | `1100,170 640x650` | 当前背包，格子 `100x100`。 |
| `loss_preview` | `96,840 1050x92` | 未带出损失、任务物风险、价值总计。 |
| `confirm_area` | `1180,830 560x120` | 确认带出主行动和次级整理。 |

---

## 5. 信息层级

默认阅读顺序：

1. 可带出容量和已占用情况。
2. 新获得物品列表。
3. 当前选中物品详情。
4. 未带出损失预览。
5. 确认带出主行动。

列表行只显示：

* 图标。
* 名称。
* 价值 / 稀有度 / 任务标记中的 1-2 项。
* 是否已放入背包。

完整描述、标签、出售价值、制造用途进入 `item_detail`。

---

## 6. 行动层级

| 层级 | 行动 | 表达 |
|---|---|---|
| Primary | `ConfirmCarry` | 右下唯一最高权重按钮。 |
| Core Interaction | 拖拽物品、自动放置、旋转、移出 | 背包和缓存区。 |
| Secondary | 排序、全部尝试放入、查看详情 | 小按钮 / 图标。 |
| Danger | 丢弃已带物、放弃全部未拾取 | 低权重并二次确认。 |

---

## 7. 按钮合并 / 降级 / 隐藏策略

| 当前入口 | V2 策略 |
|---|---|
| 拾取单个 | 优先拖拽 / 点击放入，不做每行大按钮。 |
| 丢弃 | 降级为危险操作，不和确认带出并列。 |
| 返回 / 关闭 | 合并到确认带出或流程返回，不做同权按钮。 |
| 自动整理 | 次级按钮，放在背包区附近。 |
| 查看详情 | 选中物品自动显示，不单独占主按钮。 |

---

## 8. 场景隐喻

界面像一张撤离清点桌：

* 左边是刚取回来的战利品托盘。
* 右边是打开的背包 / 底盘。
* 中间是鉴定牌和价值说明。
* 底部是带出风险与确认签收。

背景可以复用战斗或深渊氛围，但主 UI 应像“战后清点”，不是普通系统背包页。

---

## 9. 程序迁移影响

确认后建议层级：

```text
InventoryLootPanel
  LootHeader
  LootCache
  ItemDetail
  BackpackGrid
  LossPreview
  ConfirmArea
```

关键绑定：

| 绑定 | 来源 |
|---|---|
| 待拾取列表 | 当前节点 / 战斗掉落缓存。 |
| 背包网格 | 全局背包对象和 `InventoryDisplaySpec`。 |
| 容量压力 | 背包占用和可放置计算。 |
| 未带出损失 | 掉落缓存中未放入物品。 |
| 任务 / 订单提示 | 物品标签或订单需求，只读提示。 |

约束：

* 不创建第二套背包数据。
* 不压缩 `100x100` 格。
* 丢弃和放弃未拾取必须有明确确认或流程后果。

---

## 10. 素材需求变化

第一版复用：

| VisualID | 用法 |
|---|---|
| `ui_loot_pickup_panel` | 战利品缓存 / 主容器。 |
| `ui_loot_drop_zone` | 放入 / 移出区域提示。 |
| `ui_inventory_chassis_panel` / `ui_inventory_slot_*` | 背包。 |
| `ui_panel_info` | 详情和损失预览。 |
| `ui_button_primary` / `ui_button_secondary` / `ui_button_danger` | 确认、整理、危险操作。 |

可能新增但不立即跑图：

| 候选 VisualID | 说明 |
|---|---|
| `ui_loot_value_badge` | 高价值 / 任务相关标记。 |
| `ui_loot_loss_warning_plate` | 未带出损失提示底板。 |

---

## 11. UX 验收标准

运行时截图需要满足：

1. 3 秒内能看出当前任务是“决定带走哪些战利品”。
2. 背包容量压力明显可读。
3. 确认带出是唯一最高权重主行动。
4. 战利品列表、详情区、背包区三者边界清晰。
5. 背包格 `100x100`，拖拽和旋转不被装饰遮挡。
6. 未带出物品或损失有明确提示。
7. 危险操作不和确认按钮等权。

---

## 12. 用户确认问题

建议确认以下 3 点：

1. `inventory_loot` 是否改为“撤离清点台”结构。
2. 是否同意把逐行拾取按钮降级，优先用拖拽 / 点击放入表达。
3. 是否把“确认带出”作为唯一主行动，并把丢弃 / 放弃未拾取作为危险操作。
