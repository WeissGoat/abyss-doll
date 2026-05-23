---
id: art_ui_mvp_baseline_2026_05_22
title: MVP UI Baseline 2026-05-22
type: art
role: 美术
domain: ui_design
status: frozen
source_of_truth: false
related:
  - 美术文档/09_运行时美术验收记录.md
  - 美术文档/ui_design/versions/README.md
  - 美术文档/ui_design/versions/migration_log.md
last_verified: 2026-05-23
update_rule: baseline 为冻结快照，不修改规格文件；如需说明，只追加本文 notes。
---

# MVP UI Baseline 2026-05-22

> **定位：** 当前可运行 MVP UI 骨架的冻结快照。该版本用于回溯、对比和回退参考，不再作为正式结构继续演进。

---

## 1. 冻结来源

```text
ArtAcceptance RunID: 20260522_232558
Status: PASSED
ReferenceResolution: 1920x1080
```

运行时验收记录：

```text
美术文档/09_运行时美术验收记录.md
```

---

## 2. 快照文件

| 文件 | 来源 |
|---|---|
| `screen_layouts.json` | 冻结时的当前 active UI 布局规格。 |
| `component_catalog.json` | 冻结时的当前 active 组件目录。 |
| `design_tokens.json` | 冻结时的当前 active 设计 token。 |

---

## 3. 版本范围

| ScreenID | 冻结状态 | 说明 |
|---|---|---|
| `workshop_main` | `validated` | MVP 骨架运行时通过。 |
| `combat_hud` | `validated` | MVP 骨架运行时通过，但敌人卡片结构不代表正式结构。 |
| `inventory_loot` | `validated` | MVP 骨架运行时通过。 |
| `dungeon_map` | `draft` | P1 草案，未作为 Formal V1 验收。 |
| `settlement` | `draft` | P1 草案，未作为 Formal V1 验收。 |
| `sell_panel` | `draft` | P1 草案。 |
| `prosthetic_panel` | `draft` | P1 草案。 |
| `layer_select` | `draft` | P1 草案。 |

---

## 4. 已知结构问题

1. `combat_hud` 的敌人以右上卡片为主表现，更像 MVP 信息卡，不符合正式战斗舞台结构。
2. `workshop_main` 顶部状态条过长，主行动和工坊服务的层级不够正式。
3. `inventory_loot` 可用，但战后清点的仪式感和安全边距不足。
4. `dungeon_map`、`settlement` 尚未按 Formal V1 重审，不应直接 handoff。

---

## 5. 使用规则

* 不直接修改本目录中的 JSON 快照。
* Formal V1 迁移时可引用本版本做结构差异表。
* 如果 active 规格出现问题，可用本版本辅助定位是结构迁移问题还是程序接入问题。
