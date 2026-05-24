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
last_verified: 2026-05-24
update_rule: 修改 UI 版本迭代、active/candidate/baseline 关系或美术素材生成入口时同步本文件。
---

# UI 设计迭代与版本迁移流程

> **定位：** UI 版本迭代工作流入口。它规定一个 UI 版本如何从旧版冻结、设计草案、用户确认、active 规格、素材生产、程序接入到运行时验收。

---

## 1. 核心结论

默认流程：

```text
冻结旧版本 baseline
  -> 编写新版本 design draft
  -> 用户 / 美术确认
  -> 修改 active screen_layouts.json
  -> Validate-UIDesign.ps1 生成 handoff
  -> 补 seed / Manifest / Prompt / Spec
  -> 生成或替换 Approved 素材
  -> 刷新可接入素材清单
  -> 程序接入
  -> ArtAcceptance 运行时验收
  -> active 标记 integrated / validated
  -> 记录 migration_log
```

核心规则：

* 程序只接 active `screen_layouts.json`，不接 draft、baseline 或 candidate。
* 素材生成只从 active 触发，不从草案触发。
* 每次 UI 结构变更都要记录版本迁移，不把新旧结构混在同一份口头说明里。
* `candidate` 不是必经步骤，只在复杂界面需要先试写结构化 JSON、做差异对比或拆分合并时使用。

---

## 2. 版本文件分工

| 类型 | 路径 | 作用 |
|---|---|---|
| Active | `美术文档/ui_design/screen_layouts.json` | 当前正式对接规格。程序、美术素材生成、验收都以它为准。 |
| Draft | `美术文档/ui_design/formal_v1/*.md` 或后续 `formal_v2/*.md` | 设计草案，说明目标、区域、VisualID 和程序边界。确认前不触发接入。 |
| Baseline | `美术文档/ui_design/versions/mvp_baseline_2026-05-22/` | 已验收旧版本备份，只读保存，用于回溯和对比。 |
| Candidate | `美术文档/ui_design/versions/formal_v1_candidate/` | 可选暂存区。复杂界面可先在这里试写，不直接影响 active。 |
| Migration Log | `美术文档/ui_design/versions/migration_log.md` | 记录从哪个版本迁移到哪个版本、何时确认、何时接入和验收。 |

程序永远只对接 active，不对接 baseline 或 candidate。

---

## 3. Formal V1 当前流程

Formal V1 用于从 MVP UI 骨架迁移到正式结构。后续 Formal V2 / Visual V2 / Animation V1 也按同样规则处理。

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

当前状态：

* P0 / P1 / P2 共 15 个界面已经写入 active Formal V1。
* `faction_shop`、`doll_interaction`、`scenario_event`、`doll_room` 只有 design draft，用户确认前不写入 active。

如果界面很复杂，例如涉及敌我实体、战斗舞台、背包锚点和程序对象边界，可以先走：

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

未来需要冻结新 baseline 的常见节点：

* Formal V1 active 规格整体通过截图验收。
* 准备进入 Formal V2 结构重做。
* 准备批量替换组件体系或视觉皮肤。
* 程序侧需要稳定回退点。

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

禁止事项：

* 不在 draft 文档确认前直接修改 active。
* 不让程序按 draft 或 archive 文档接入。
* 不为了某张图片临时改 `ScreenID` 或核心 `VisualID`。
* 不把文字、价格、数值、按钮文案烘焙进 Sprite。
* 不把旧交付快照当成当前接入规格。

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
  -> Validate-UIDesign.ps1
  -> RequiredVisualIDs 确认
  -> art_requirements_seed.json / Manifest 更新
  -> prompt/spec 生成
  -> AI 图片生成
  -> 预处理 / Approved
  -> Registry / 程序接入
```

---

## 7. 版本命名

| 类型 | 命名示例 | 用途 |
|---|---|---|
| 结构版本 | `FormalV1`、`FormalV2` | 主区域、信息层级、交互边界变化。 |
| 视觉版本 | `VisualV1`、`VisualV2` | 同结构下替换皮肤、图标、背景、字体样式。 |
| 动效版本 | `MotionV1` | hover、打开、关闭、受击、拾取等动效。 |
| 验收 baseline | `mvp_baseline_2026-05-22`、`formal_v1_validated_YYYY-MM-DD` | 已通过验收的回溯点。 |

结构版本变更必须走 UI 版本流程。视觉版本如果只替换同名 VisualID，可走 Manifest / Approved 流程，但要记录 Manifest 和可接入素材清单。

---

## 8. 未来 V2/V3 迭代

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

## 9. 当前执行策略

当前阶段采用：

```text
MVP Baseline 已冻结
Formal V1 active 已覆盖 15 个界面
P3 四个界面处于 draft
用户确认后逐界面修改 active
复杂界面可选用 candidate 暂存
```

当前优先级：

1. 程序侧按 active `screen_layouts.json` 和 latest 可接入素材清单接入 15 个 active Formal V1 界面。
2. 美术侧用 ArtAcceptance 逐屏验收，回填缺图、黑块、尺寸和射线问题。
3. 用户确认后，把 `faction_shop`、`doll_interaction`、`scenario_event`、`doll_room` 从 draft 逐个推进到 active。
4. active 更新后再补 seed、Manifest、Prompt、Spec、Approved 和可接入素材清单。
5. Formal V1 结构稳定后，才进入 Visual V2 / Motion V1 等表现增强批次。
