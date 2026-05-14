# UI 设计流水线

> **定位：** 管理 MVP 以及后续版本的 UI 设计系统、界面布局、组件清单和程序交付检查。
> **更新时间：** 2026-05-13

---

## 1. 为什么单独建 UI 设计流

UI 资产不能只按单张图片生产。面板、按钮、背包格、状态条、卡框、列表行必须先形成统一组件系统，再进入 AI 跑图、切图和程序接入。

UI 设计流解决四件事：

* 界面区域怎么摆：由 `screen_layouts.json` 记录。
* 组件怎么复用：由 `component_catalog.json` 记录。
* 视觉基础标准是什么：由 `design_tokens.json` 记录。
* 交付是否完整：由 `Validate-UIDesign.ps1` 校验并生成 handoff。

---

## 2. 文件职责

| 文件 | 职责 |
|---|---|
| `design_tokens.json` | UI 参考分辨率、安全区、颜色、字号、间距、圆角和层级标准。 |
| `component_catalog.json` | UI 组件目录，记录组件、VisualID、状态、拉伸方式、使用界面和程序接入要求。 |
| `screen_layouts.json` | 界面布局规格，记录每个界面的区域、锚点、尺寸、组件引用和程序交互点。 |
| `handoff_checklist.md` | UI 从设计到程序接入的检查清单。 |
| `_generated/ui_design_handoff.md` | 校验脚本生成的当前 UI 交付摘要。 |

---

## 3. 标准流程

```text
UI Tokens
  -> Component Catalog
  -> Screen Layout
  -> Validate UI Design
  -> Preset Seed / Manifest
  -> Prompt / Spec
  -> AI Generation
  -> Preprocess / 9-slice Check
  -> Approved
  -> Registry / Unity Layout
  -> In-game Validation
```

### Step A：界面布局

新增或重做界面时，先在 `screen_layouts.json` 新增一条 `Screen`：

* `ScreenID`
* `Priority`
* `BackgroundVisualID`
* `Zones`
* `RequiredComponents`
* `ProgramHandoff`
* `ControllerBindings`
* `UnityHierarchy`
* `SpriteAssignments`
* `AcceptanceCriteria`

不要先跑图。先让界面区域、层级和程序交互点稳定。

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
