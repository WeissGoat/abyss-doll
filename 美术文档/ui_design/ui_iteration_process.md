---
id: art_ui_iteration_process
title: UI 设计迭代与版本迁移流程
type: art
role: 美术
domain: ui_design
status: active
source_of_truth: true
related:
  - 美术文档/ui_design/README.md
  - agent_status/art.md
  - 知识库/views/art.md
  - 美术文档/ui_design/formal_v1/screen_structure_review.md
  - 美术文档/ui_design/versions/README.md
  - 美术文档/ui_design/versions/migration_log.md
last_verified: 2026-05-23
update_rule: 修改 UI 版本迭代、active/candidate/baseline 关系或美术素材生成入口时同步本文件。
---

# UI 设计迭代与版本迁移流程

> **定位：** 规范 UI 从 MVP 骨架到正式结构 V1，以及未来 V2/V3 的设计迭代方式。核心目标是：已验收版本可追溯，新版本先设计确认，再修改当前 active 规格，最后进入素材生产和程序接入。

---

## 1. 核心结论

默认流程：

```text
冻结旧版本 baseline
  -> 编写新版本设计文档
  -> 用户/美术确认
  -> 修改当前 active：screen_layouts.json
  -> Validate-UIDesign.ps1
  -> 生成/补充美术素材
  -> 程序接入
  -> ArtAcceptance 运行时验收
  -> active 标记 validated
```

`candidate` 不是必经步骤。它只在复杂界面需要先试写结构化 JSON、做差异对比或拆分合并时使用。

---

## 2. 三类版本文件

| 类型 | 路径 | 作用 |
|---|---|---|
| Active | `美术文档/ui_design/screen_layouts.json` | 当前正式对接规格。程序、美术素材生成、验收都以它为准。 |
| Baseline | `美术文档/ui_design/versions/mvp_baseline_2026-05-22/` | 已验收旧版本备份，只读保存，用于回溯和对比。 |
| Candidate | `美术文档/ui_design/versions/formal_v1_candidate/` | 可选暂存区。复杂界面可先在这里试写，不直接影响 active。 |

程序永远只对接 active，不对接 baseline 或 candidate。

---

## 3. Formal V1 当前流程

Formal V1 用于从 MVP UI 骨架迁移到正式结构。

```text
formal_v1/*.md
  -> 用户确认
  -> 修改 screen_layouts.json 对应界面
  -> Validate-UIDesign.ps1
  -> 更新 art_requirements_seed / Manifest
  -> 生成或补充素材
  -> 程序接入
  -> ArtAcceptance
```

如果界面很复杂，例如 `combat_hud` 涉及敌我实体、战斗舞台、背包锚点和程序对象边界，可以先走：

```text
formal_v1/combat_hud_v1.md
  -> formal_v1_candidate 中试写 combat_hud
  -> 对比 MVP baseline
  -> 用户确认
  -> 合并 combat_hud 到 active screen_layouts.json
```

---

## 4. 什么时候必须冻结 baseline

以下情况需要冻结 baseline：

1. 某批 UI 已经通过 ArtAcceptance。
2. 即将进行结构性迁移，例如 MVP -> Formal V1。
3. 即将批量替换组件结构或关键 VisualID。
4. 需要给程序一个稳定回退点。

冻结内容至少包括：

```text
screen_layouts.json
component_catalog.json
design_tokens.json
验收 RunID 和结论说明
```

当前已冻结：

```text
versions/mvp_baseline_2026-05-22/
```

---

## 5. Active 修改规则

只有在设计方案确认后，才修改 active。

修改 active 时必须满足：

1. 一次只改一个界面，或一组强相关界面。
2. 保留 `ScreenID` 稳定，除非确实是删除旧界面。
3. 增加或更新 `StructureVersion` 和 `PreviousValidatedVersion`。
4. `Zones`、`ControllerBindings`、`UnityHierarchy`、`SpriteAssignments`、`AcceptanceCriteria` 同步更新。
5. 跑 `Validate-UIDesign.ps1`。
6. 更新 `versions/migration_log.md`。

---

## 6. 素材生成入口

素材生成只从 active 触发，不从 design draft 或 candidate 触发。

原因：

* 避免为未确认结构生成大量无用素材。
* 保证 Manifest 和程序对接规格一致。
* 保证 VisualID 的生命周期清楚。

推荐顺序：

```text
active screen_layouts.json 更新
  -> RequiredVisualIDs 确认
  -> art_requirements_seed.json / Manifest 更新
  -> prompt/spec 生成
  -> AI 图片生成
  -> 预处理 / Approved
  -> Registry / 程序接入
```

---

## 7. 未来 V2/V3 迭代

未来 UI 迭代也按同样规则处理：

| 迭代类型 | 示例 | 是否需要新 baseline |
|---|---|---|
| 结构迭代 | Formal V1 -> Formal V2 | 需要 |
| 组件视觉迭代 | UI 皮肤 V1 -> UI 皮肤 V2 | 建议需要 |
| 单素材替换 | 替换某个按钮 PNG | 不一定，需要记录 Manifest |
| 动效迭代 | 增加 hover / 打开转场 | 如果影响 UI 结构，建议需要 |

原则：

```text
结构变更走 UI 设计版本流程。
单纯换图走 Manifest / Approved 资源流程。
程序对象边界变化必须先写设计文档。
```

---

## 8. 当前执行策略

当前阶段采用：

```text
MVP Baseline 已冻结
Formal V1 文档先行
你确认后直接修改 active
combat_hud 如需复杂结构对比，可选用 candidate 暂存
```

当前优先级：

1. `combat_hud` Formal V1。
2. `workshop_main` Formal V1。
3. `inventory_loot` Formal V1。
4. `dungeon_map` Formal V1。
5. `settlement` Formal V1。
