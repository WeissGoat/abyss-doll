---
id: art_ui_migration_log
title: UI 设计版本迁移记录
type: art
role: 美术
domain: ui_design
status: active
source_of_truth: true
related:
  - 美术文档/ui_design/README.md
  - 美术文档/ui_design/versions/README.md
  - 美术文档/ui_design/versions/mvp_baseline_2026-05-22/README.md
  - 美术文档/ui_design/versions/formal_v1_candidate/README.md
last_verified: 2026-05-23
update_rule: 每次 UI 设计版本冻结、候选版本合并、程序接入或运行时验收后同步本文件。
---

# UI 设计版本迁移记录

> **定位：** 记录 UI 设计从一个版本迁移到另一个版本的过程，避免未冻结 baseline、未记录确认结论就修改 active，导致历史丢失或程序对接混乱。

---

## 1. 当前版本

| 项 | 值 |
|---|---|
| Active 规格 | `美术文档/ui_design/screen_layouts.json` |
| 当前 baseline | `versions/mvp_baseline_2026-05-22` |
| 当前 candidate | `versions/formal_v1_candidate`，可选暂存 |
| 迁移策略 | 默认设计确认后直接修改 active；复杂界面可先用 candidate 试写 |

---

## 2. 迁移总览

| ScreenID | Baseline Version | Candidate Version | Active 状态 | 迁移状态 | 备注 |
|---|---|---|---|---|---|
| `combat_hud` | `mvp_baseline_2026-05-22` | 可选 `formal_v1_candidate` | `validated` | `design_draft` | 第一优先级，需从敌人卡片改为敌方实体舞台。 |
| `workshop_main` | `mvp_baseline_2026-05-22` | 无 | `validated` | `design_draft` | 第二优先级，改为正式工坊工作台。 |
| `inventory_loot` | `mvp_baseline_2026-05-22` | 无 | `validated` | `design_draft` | 第三优先级，强化战后清点结构。 |
| `dungeon_map` | `mvp_baseline_2026-05-22` | 无 | `draft` | `design_draft` | 暂停直接 handoff，先按 Formal V1 审查。 |
| `settlement` | `mvp_baseline_2026-05-22` | 无 | `draft` | `design_draft` | 暂停直接 handoff，先按 Formal V1 审查。 |
| `sell_panel` | `mvp_baseline_2026-05-22` | 未建立 | `draft` | `pending` | 功能弹窗，等主界面结构稳定后处理。 |
| `prosthetic_panel` | `mvp_baseline_2026-05-22` | 未建立 | `draft` | `pending` | 功能弹窗，等主界面结构稳定后处理。 |
| `layer_select` | `mvp_baseline_2026-05-22` | 未建立 | `draft` | `pending` | 功能弹窗/出发简报，等主界面结构稳定后处理。 |

---

## 3. 状态枚举

| 状态 | 含义 |
|---|---|
| `pending` | 未进入当前候选版本。 |
| `design_draft` | 已有设计文档，等待确认。 |
| `active_spec` | 已写入 active `screen_layouts.json`，等待校验或交付。 |
| `candidate_design` | 已有候选结构文档，尚未写入候选 JSON；仅复杂界面使用。 |
| `candidate_spec` | 已写入候选 JSON，等待审查。 |
| `ready_to_merge` | 候选规格确认，可合并到 active。 |
| `merged_to_active` | 已合并到 active，等待程序接入。 |
| `integrated` | 程序已接入，等待运行时验收。 |
| `validated` | 当前 active 版本已通过运行时验收。 |
| `rejected` | 候选方案被否决或需重做。 |

---

## 4. 迁移记录

### 2026-05-23：建立 UI 版本化流程

* 冻结 `mvp_baseline_2026-05-22`。
* 建立 `formal_v1_candidate`，但明确为复杂界面的可选暂存区。
* 规定 `screen_layouts.json` 只代表 active 对接规格。
* Formal V1 默认在用户确认后逐界面修改 active；candidate 不作为必经流程。
