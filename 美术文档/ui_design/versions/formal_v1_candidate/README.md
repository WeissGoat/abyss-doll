---
id: art_ui_formal_v1_candidate
title: Formal V1 Candidate Optional Staging
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

# Formal V1 Candidate Optional Staging

> **定位：** 正式版 UI 结构 V1 的可选暂存区。它不是程序直接接入的 active 规格，也不是必经流程；只有复杂界面需要先结构化试写、对比差异或拆分合并时才使用。

---

## 1. 来源

结构设计层：

```text
美术文档/ui_design/formal_v1/
```

当前 Formal V1 设计优先级：

1. `combat_hud`
2. `workshop_main`
3. `inventory_loot`
4. `dungeon_map`
5. `settlement`

---

## 2. 文件职责

| 文件 | 职责 |
|---|---|
| `screen_layouts.formal_v1_candidate.json` | 可选暂存规格。复杂界面可先在这里试写结构化字段，确认后再合并到 active `screen_layouts.json`。 |
| `README.md` | 本候选版本说明。 |

---

## 3. 当前状态

| ScreenID | Candidate 使用 | Active 状态 | 下一步 |
|---|---|---|---|
| `combat_hud` | 可选使用 | MVP Baseline `validated` | 先审查战斗舞台结构；如结构字段较复杂，可先写入 candidate JSON。 |
| `workshop_main` | 默认不用 | MVP Baseline `validated` | 你确认文档后直接修改 active。 |
| `inventory_loot` | 默认不用 | MVP Baseline `validated` | 你确认文档后直接修改 active。 |
| `dungeon_map` | 默认不用 | active `draft` | 不急于 handoff，先按 Formal V1 审查，确认后修改 active。 |
| `settlement` | 默认不用 | active `draft` | 不急于 handoff，先按 Formal V1 审查，确认后修改 active。 |

---

## 4. 合并条件

若某个界面使用 candidate，从 candidate 合并到 active 前，必须满足：

1. 对应 `formal_v1/*.md` 已确认。
2. Candidate JSON 中该界面字段完整。
3. 需要的 VisualID 已存在，或明确使用临时 fallback。
4. 运行 `Validate-UIDesign.ps1` 不报错。
5. `migration_log.md` 记录合并计划。

未使用 candidate 的界面，按主流程执行：

```text
formal_v1/*.md
  -> 用户 / 美术确认
  -> 修改 active screen_layouts.json
  -> Validate-UIDesign.ps1
  -> 素材生成 / 程序接入 / ArtAcceptance
```
