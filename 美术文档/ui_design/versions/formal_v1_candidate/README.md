---
id: art_ui_formal_v1_candidate
title: Formal V1 Candidate
type: art
role: 美术
domain: ui_design
status: draft
source_of_truth: false
related:
  - 美术文档/ui_design/formal_v1/screen_structure_review.md
  - 美术文档/ui_design/versions/README.md
  - 美术文档/ui_design/versions/migration_log.md
last_verified: 2026-05-23
update_rule: Formal V1 候选规格变更时同步本文件和 migration_log。
---

# Formal V1 Candidate

> **定位：** 正式版 UI 结构 V1 的候选版本。它不是程序直接接入的 active 规格，而是逐界面审核、合并、接入和验收的中间版本。

---

## 1. 来源

结构设计层：

```text
美术文档/ui_design/formal_v1/
```

当前候选版本优先级：

1. `combat_hud`
2. `workshop_main`
3. `inventory_loot`
4. `dungeon_map`
5. `settlement`

---

## 2. 文件职责

| 文件 | 职责 |
|---|---|
| `screen_layouts.formal_v1_candidate.json` | Formal V1 候选结构化规格。逐界面确认后再合并到 active `screen_layouts.json`。 |
| `README.md` | 本候选版本说明。 |

---

## 3. 当前状态

| ScreenID | Candidate 状态 | Active 状态 | 下一步 |
|---|---|---|---|
| `combat_hud` | structure draft | MVP Baseline `validated` | 先审查战斗舞台结构，确认后写入 candidate JSON。 |
| `workshop_main` | structure draft | MVP Baseline `validated` | 等战斗结构确认后细化。 |
| `inventory_loot` | structure draft | MVP Baseline `validated` | 等战斗结构确认后细化。 |
| `dungeon_map` | structure draft | active `draft` | 不急于 handoff，先按 Formal V1 审查。 |
| `settlement` | structure draft | active `draft` | 不急于 handoff，先按 Formal V1 审查。 |

---

## 4. 合并条件

某个界面从 candidate 合并到 active 前，必须满足：

1. 对应 `formal_v1/*.md` 已确认。
2. Candidate JSON 中该界面字段完整。
3. 需要的 VisualID 已存在，或明确使用临时 fallback。
4. 运行 `Validate-UIDesign.ps1` 不报错。
5. `migration_log.md` 记录合并计划。
