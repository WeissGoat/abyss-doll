---
id: art_ui_design_readme
title: UI 设计流水线
type: art
role: 美术
domain: art_pipeline
status: active
source_of_truth: true
related:
  - 开发文档/05_表现层架构与事件总线(ViewAndEventBus).md
  - 开发文档/00_Unity表现层与编辑器构建规范.md
  - 开发文档/09_视觉资源系统程序开发规范.md
  - 美术文档/10_正式版核心纵切美术路线.md
  - 美术文档/13_正式纵切UI与素材覆盖矩阵.md
  - 美术文档/02_资源规格与接入规范.md
  - 美术文档/README.md
  - 美术文档/archive/README.md
  - 美术文档/ui_design/ui_iteration_process.md
  - 美术文档/ui_design/handoff_checklist.md
  - 美术文档/ui_design/formal_v1/screen_structure_review.md
  - 美术文档/ui_design/formal_v1/doll_interaction_v1.md
  - 美术文档/ui_design/formal_v1/doll_room_v1.md
  - 美术文档/ui_design/formal_v1/faction_shop_v1.md
  - 美术文档/ui_design/formal_v1/scenario_event_v1.md
  - 美术文档/ui_design/versions/README.md
  - 美术文档/ui_design/versions/migration_log.md
  - 美术文档/archive/11_P0_UI骨架接入交付.md
  - 美术文档/archive/12_P1_UI骨架接入准备.md
  - 知识库/views/art.md
last_verified: 2026-05-24
update_rule: 修改美术流水线、资源规格、UI 交付或运行时验收要求时同步本文件。
---

# UI 设计流水线

> **定位：** UI 结构版本管理和程序对接规格入口。当前 active 规格只看 `screen_layouts.json`，设计草案不直接交给程序。
> **更新时间：** 2026-05-24

---

## 1. 为什么单独建 UI 设计流

UI 资产不能只按单张图片生产。面板、按钮、背包格、状态条、卡框、列表行必须先形成统一组件系统，再进入 AI 跑图、切图和程序接入。

正式版核心纵切阶段的原则是：用批次控制当前处理哪些界面，不用半成品标准降低进入批次的完成质量。凡是进入当前批次的 UI，结构、组件、VisualID、程序绑定和验收标准都按正式版处理；未进入批次的界面不抢跑。

UI 设计流解决六件事：

* UI 设计版本如何冻结、确认、合并和验收：由 `ui_iteration_process.md` 记录。
* 正式版结构怎么从 MVP 骨架迁移：由 `formal_v1/` 记录结构文档。
* 当前程序应该按什么接入：只看 `screen_layouts.json`。
* 组件怎么复用：由 `component_catalog.json` 记录。
* 视觉基础标准是什么：由 `design_tokens.json` 记录。
* 交付是否完整：由 `Validate-UIDesign.ps1` 校验并生成 handoff。

---

## 2. 文件职责

| 文件 | 职责 |
|---|---|
| `design_tokens.json` | UI 参考分辨率、安全区、颜色、字号、间距、圆角和层级标准。 |
| `component_catalog.json` | UI 组件目录，记录组件、VisualID、状态、拉伸方式、使用界面和程序接入要求。 |
| `screen_layouts.json` | 当前 active 界面布局规格，程序只按它对接。 |
| `ui_iteration_process.md` | UI 设计迭代与版本迁移流程，定义 baseline、active、candidate 的关系。 |
| `formal_v1/` | 正式版 UI 结构 V1 设计层，先审查舞台、区域、信息层级和程序对象边界。 |
| `versions/` | UI 设计版本管理目录，保存 baseline、可选 candidate 和迁移记录。 |
| `handoff_checklist.md` | UI 从设计到程序接入的检查清单。 |
| `_generated/ui_design_handoff.md` | 校验脚本生成的当前 UI 交付摘要。 |

---

## 3. 当前状态

| 类型 | 范围 | 接入口径 |
|---|---|---|
| active Formal V1 | 15 个界面，详见 `screen_layouts.json` 和 `13_正式纵切UI与素材覆盖矩阵.md` | 程序、美术素材生成、验收都可以使用。 |
| draft Formal V1 | `faction_shop`、`doll_interaction`、`scenario_event`、`doll_room` | 只作为设计审阅，不是程序接入口，不触发素材生成。 |
| historical baseline | `versions/mvp_baseline_2026-05-22/` | 只读归档，用于对比和回退参考。 |
| candidate | `versions/formal_v1_candidate/` | 可选暂存区，不是必经流程。 |

程序侧永远不直接读取 `formal_v1/*.md`、baseline 或 candidate。程序接入只读 active `screen_layouts.json`、`component_catalog.json` 和生成的 `ui_design_handoff.md`。

---

## 4. 标准流程

```text
MVP Baseline / Runtime Findings
  -> Freeze Baseline in versions/
  -> Formal V1 Structure Review
  -> User / Art Review
  -> Update Active screen_layouts.json
  -> Validate UI Design
  -> Preset Seed / Manifest
  -> Prompt / Spec
  -> AI Generation
  -> Preprocess / 9-slice Check
  -> Approved
  -> Registry / Unity Layout
  -> In-game Validation
```

关键门槛：

* 已通过验收的 UI 设计必须先冻结到 `versions/`，作为 Baseline 保留。
* `screen_layouts.json` 只表示当前 active 对接规格；程序不直接接 candidate。
* 正式版结构调整先写入 `formal_v1/`；你确认后，逐界面修改 active `screen_layouts.json`。
* `versions/formal_v1_candidate/` 是复杂界面的可选暂存区，不是必经流程。
* 新界面或重做界面必须先更新 `screen_layouts.json`，不要先跑图。
* 核心界面的 `ScreenID`、主区域、程序绑定和 `VisualID` 应保持长期稳定。
* 文字、数字、价格、物品名、按钮文案继续由 Unity Text 渲染，不烘焙进 UI Sprite。
* 背包玩法格固定为 `100x100`，美术只定义格子皮肤、底盘装饰、状态图和容器摆放。
* 图片可以后续替换为更高质量同名 `VisualID`，但组件边界和布局契约不应随图片替换而改变。

### Step A：界面布局

新增界面时，先判断是否直接进入 active：

* 若是全新、不影响已验收流程的小界面，可直接新增到 `screen_layouts.json`。
* 若会重构已验收核心界面，必须先写设计文档并由你确认；复杂界面可选进入 candidate 试写。

重做现有核心界面时，流程是：

```text
formal_v1/*.md
  -> 用户 / 美术确认
  -> update active screen_layouts.json
  -> Validate-UIDesign.ps1
  -> update versions/migration_log.md
  -> program handoff
  -> ArtAcceptance
```

先在 `formal_v1/` 写结构重审文档，确认以下内容后，再写入 active `screen_layouts.json`：

* 该界面的 MVP Baseline 问题。
* Formal V1 的主区域和信息层级。
* 哪些对象属于游戏舞台，哪些对象属于 UI 控件。
* 需要复用的 VisualID 和建议新增的 VisualID。
* 程序侧需要迁移的 Unity 节点或对象边界。
* 运行时截图验收标准。

对于 `combat_hud` 这类复杂界面，可以先写入 `versions/formal_v1_candidate/` 作为暂存和对比，再合并到 active。

写入 active 时至少包含：

* `ScreenID`
* `Priority`
* `StructureVersion`
* `PreviousValidatedVersion`
* `BackgroundVisualID`
* `Zones`
* `RequiredComponents`
* `ProgramHandoff`
* `ControllerBindings`
* `UnityHierarchy`
* `SpriteAssignments`
* `AcceptanceCriteria`

不要先跑图。先让界面区域、层级和程序交互点稳定。进入正式版纵切的 active 界面，布局状态应按以下顺序推进：

```text
planned -> draft -> handoff -> integrated -> validated
```

含义：

| 状态 | 含义 |
|---|---|
| `planned` | 已列入正式 UI 骨架，但区域和组件还未完全定稿。 |
| `draft` | 区域、组件、VisualID 和程序交付基本明确，可进入资源生产。 |
| `handoff` | 已通过 `Validate-UIDesign.ps1`，可交给程序接入。 |
| `integrated` | Unity 已接入，等待运行时截图验收。 |
| `validated` | ArtAcceptance 截图和快照已通过美术侧验收。 |

Candidate 仅作复杂界面的可选暂存，迁移状态不复用 `LayoutStatus` 单独表达，而记录在：

```text
美术文档/ui_design/versions/migration_log.md
```

涉及背包的界面还必须填写 `InventoryLayerPolicy`：

* `UsesGlobalInventory`：是否复用现有全局背包对象。
* `GridCellSize`：当前玩法格固定为 `100`。
* `GridSpacing`：当前格间距固定为 `5`。
* `TargetZone`：背包对象应锚定到哪个 `ZoneID`。
* `Rules`：显示顺序、射线、缩放和拖拽约束。

背包格尺寸以程序玩法逻辑为主。当前 `DraggableItemUI`、`GridSlotUI`、`MVPEditorSetup` 均按 `100x100` 格处理，美术侧只定义格子皮肤、底盘装饰、状态图和容器摆放。

### Step B：组件映射

界面需要的可复用 UI 组件写进 `component_catalog.json`。如果组件需要切图，则必须有稳定 `VisualID`，并同步存在于 `art_requirements_seed.json` 或 Manifest。

### Step C：资产生产

组件进入 `art_requirements_seed.json` 后，继续走原美术流水线：

```powershell
.\tools\美术工具\Update-ArtManifest.ps1
.\tools\美术工具\Generate-ArtPrompts.ps1
.\tools\美术工具\Run-ArtGeneration.ps1 -Domain ui -VisualID ui_panel_main -Variants 4
```

### Step D：程序交付

程序侧以 `screen_layouts.json` 的区域和 `component_catalog.json` 的组件清单为依据：

* 需要换皮的 Image/Sprite。
* 需要九宫格的 Sprite border。
* 需要新建或调整的布局容器。
* 不应该烘焙到图片里的文字、数值、状态。

---

## 4. 校验命令

```powershell
.\tools\美术工具\Validate-UIDesign.ps1
```

校验内容：

* 组件引用的 `VisualID` 是否存在于 seed 或 Manifest。
* 界面引用的 `ComponentID` 是否存在于组件目录。
* 界面要求的 `VisualID` 是否存在于 seed 或 Manifest。
* 组件的 `RequiredForScreens` 与屏幕的 `RequiredComponents` 是否双向一致。
* `UnityHierarchy` 和 `SpriteAssignments` 是否引用了已声明组件和素材。
* `InventoryLayerPolicy.TargetZone` 是否存在于当前界面的 `Zones`。
* P0 界面是否已有 `ControllerBindings`、`UnityHierarchy`、`SpriteAssignments`、`AcceptanceCriteria`。
* 使用背包组件的 P0 界面是否已有 `InventoryLayerPolicy`。
* 是否生成当前 handoff 摘要。

