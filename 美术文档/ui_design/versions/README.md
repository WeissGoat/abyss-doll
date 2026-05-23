---
id: art_ui_versions_readme
title: UI 设计版本管理
type: art
role: 美术
domain: ui_design
status: active
source_of_truth: true
related:
  - 美术文档/ui_design/README.md
  - 美术文档/ui_design/ui_iteration_process.md
  - 美术文档/ui_design/formal_v1/screen_structure_review.md
  - 美术文档/ui_design/versions/migration_log.md
  - 美术文档/ui_design/versions/formal_v1_candidate/README.md
  - 美术文档/ui_design/versions/mvp_baseline_2026-05-22/README.md
last_verified: 2026-05-23
update_rule: 新增 UI 设计版本、冻结基线或合并候选版本时同步本文件。
---

# UI 设计版本管理

> **定位：** 像版本管理一样保存 UI 设计基线、候选版本和迁移记录。`screen_layouts.json` 始终代表当前 active 对接规格；历史版本和新版本候选放在 `versions/` 下。

---

## 1. 目录规则

```text
美术文档/ui_design/
  screen_layouts.json              # 当前 active 对接规格
  component_catalog.json           # 当前 active 组件目录
  design_tokens.json               # 当前 active 设计 token
  ui_iteration_process.md          # UI 迭代和版本迁移主流程
  formal_v1/                       # Formal V1 结构草案，人读设计层
  versions/
    mvp_baseline_2026-05-22/       # 已验收 MVP UI 基线快照
    formal_v1_candidate/           # 可选暂存区，复杂界面先结构化试写
    migration_log.md               # 版本迁移登记表
```

---

## 2. 版本类型

| 类型 | 含义 | 是否供程序直接接入 |
|---|---|---:|
| `active` | 当前 `screen_layouts.json`、`component_catalog.json`、`design_tokens.json`。 | 是 |
| `baseline` | 已验收版本的只读快照，用于回溯和对比。 | 否 |
| `candidate` | 可选暂存区。复杂界面可先结构化试写和对比，确认后再合并 active。 | 否 |
| `merged` | candidate 的某个界面已合并到 active。 | 合并后是 |

---

## 3. 基线冻结规则

当一个 UI 设计版本经过运行时验收后，应冻结为 baseline：

1. 复制当时的 `screen_layouts.json`。
2. 复制当时的 `component_catalog.json`。
3. 复制当时的 `design_tokens.json`。
4. 记录验收 RunID、状态、通过范围和已知缺陷。
5. baseline 后续不直接修改，如需补说明只能追加 notes。

MVP Baseline 已冻结在：

```text
versions/mvp_baseline_2026-05-22/
```

---

## 4. Candidate 使用规则

默认流程不要求使用 candidate。设计确认后可以直接修改 active：

```text
formal_v1/*.md
  -> 用户 / 美术确认
  -> update active screen_layouts.json
  -> Validate-UIDesign.ps1
  -> program integration
  -> ArtAcceptance
```

只有复杂界面需要先试写结构化 JSON 或对比差异时，才使用 candidate：

```text
formal_v1/*.md
  -> write candidate spec
  -> compare with baseline
  -> 用户 / 美术确认
  -> merge one screen into active screen_layouts.json
  -> program integration
  -> ArtAcceptance
  -> migration_log.md 标记完成
```

合并要求：

使用 candidate 时的要求：

* 一次只试写和合并一个或一组强相关界面。
* 合并前保留该界面在 baseline 中的引用。
* 合并后 `screen_layouts.json` 必须能通过 `Validate-UIDesign.ps1`。
* 程序未接入前，active 状态只能到 `handoff`，不能标 `validated`。
* 运行时验收通过后才能把 active 的该界面标为 `validated`。

---

## 5. 命名规则

| 版本 | 目录命名 |
|---|---|
| MVP 骨架基线 | `mvp_baseline_YYYY-MM-DD` |
| 正式结构 V1 候选 | `formal_v1_candidate` |
| 正式结构 V2 候选 | `formal_v2_candidate` |
| UI 组件视觉 V2 候选 | `visual_v2_candidate` |

---

## 6. 当前版本状态

| 版本 | 状态 | 说明 |
|---|---|---|
| `mvp_baseline_2026-05-22` | frozen | P0 已运行时验收，P1 为当时 draft。 |
| `formal_v1_candidate` | optional | 可选暂存区。`combat_hud` 如需复杂结构对比，可先在这里试写。 |
| `active` | mixed | 当前 active 仍是 MVP Baseline 运行时通过版 + P1 draft；后续逐界面合并 Formal V1。 |
