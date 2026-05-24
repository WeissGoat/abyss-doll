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
last_verified: 2026-05-24
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

当前 Formal V1 设计状态：

1. 15 个 active Formal V1 界面已直接写入 `screen_layouts.json`。
2. `faction_shop`、`doll_interaction`、`scenario_event`、`doll_room` 仍是 design draft，未进入 active。
3. 本目录当前不承担程序交付职责；后续只有复杂 Formal V2 或局部重构需要结构化试写时才使用。

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
| `combat_hud` | 未使用 | Formal V1 active，已完成程序接入和 ArtAcceptance | 继续按 active 截图验收和资源质量迭代。 |
| `workshop_main` | 未使用 | Formal V1 active | 等程序接入当前 active 后截图验收。 |
| `inventory_loot` | 未使用 | Formal V1 active | 等程序接入当前 active 后截图验收。 |
| `dungeon_map` | 未使用 | Formal V1 active | 等程序接入当前 active 后截图验收。 |
| `settlement` | 未使用 | Formal V1 active | 等程序接入当前 active 后截图验收。 |

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
