---
id: art_ui_handoff_checklist
title: UI 交付检查清单
type: art
role: 美术
domain: art_pipeline
status: active
source_of_truth: true
related:
  - 开发文档/05_表现层架构与事件总线(ViewAndEventBus).md
  - 开发文档/00_Unity表现层与编辑器构建规范.md
  - 美术文档/10_正式版核心纵切美术路线.md
  - 美术文档/ui_design/README.md
  - 美术文档/11_P0_UI骨架接入交付.md
  - 美术文档/12_P1_UI骨架接入准备.md
last_verified: 2026-05-23
update_rule: 修改美术流水线、资源规格、UI 交付或运行时验收要求时同步本文件。
---

# UI 交付检查清单

> **定位：** 每次 UI 设计、跑图、切图、程序接入前后的验收门槛。
> **更新时间：** 2026-05-13

---

## 1. Layout Gate

进入资产生产前必须确认：

* `screen_layouts.json` 中已有对应 `ScreenID`。
* 主要区域已拆成 `Zones`，包含 `Rect`、`Anchor`、`Purpose`。
* `RequiredComponents` 已列出界面依赖的 UI 组件。
* `ProgramHandoff.LayoutChanges` 已写明程序需要改哪些容器或层级。
* P0 界面已填写 `ControllerBindings`，明确复用哪些现有脚本字段。
* P0 界面已填写 `UnityHierarchy`，明确新增或换皮对象路径、层级、图片类型和射线设置。
* P0 界面已填写 `SpriteAssignments`，明确 Sprite 应分配到哪些目标。
* P0 界面已填写 `AcceptanceCriteria`，程序接入后可直接验收。
* 文本、数值、按钮文案没有要求烘焙进图片。

涉及背包的界面必须额外确认：

* `InventoryLayerPolicy.TargetZone` 指向当前界面已有 `ZoneID`。
* `InventoryLayerPolicy.GridCellSize` 与 `design_tokens.json` 的 `LayoutGrid.InventoryCellSize` 一致。
* 当前玩法格标准为 `100x100`，不得在 UI 方案中改成 `64x64`。
* `GridContainer` 和 `InventoryItemLayer` 应同步移动，不复制第二套可交互背包。

---

## 2. Component Gate

进入 Manifest 前必须确认：

* `component_catalog.json` 中已有对应 `ComponentID`。
* 需要切图的组件有稳定 `VisualID`。
* 组件的 `ResizeMode` 明确：`fixed`、`nine_slice`、`tile_or_stretch` 等。
* 有状态变体的组件写入 `StateVisualIDs`。
* `VisualID` 已存在于 `art_requirements_seed.json` 或 Manifest。

---

## 3. Art Production Gate

跑图前必须确认：

* Manifest 中目标 UI entry 为 `Status=prompted`。
* `PromptEN` 是英文视觉语言，无项目名、引擎词、玩法黑话。
* `Spec.SourceSpec`、`Spec.DisplaySpec`、`Spec.ProcessSpec` 完整。
* 九宫格类组件有 `Spec.ProcessSpec.NineSlice`。

---

## 4. Integration Gate

程序接入前必须确认：

* 正式 PNG 已进入 `UnityClient/Assets/Art/Approved/UI/`。
* 需要 Registry 的资源已登记 `VisualAssetRegistry`。
* 九宫格 Sprite 的 border 与 Manifest `NineSlice.Border` 一致。
* 背景、面板、底纹等非交互 Image 默认 `raycastTarget=false`。
* 交互区域仍由 Button、格子、物品拖拽、敌人卡片等程序对象负责。
* `GridSlotUI`、物品图标、按钮和敌人卡片保留必要的 `raycastTarget=true`。
* 背包格状态切换优先级为 `invalid/valid > hover > locked/available`。
* 文本、数值、血条填充、AP 数量、列表内容由程序运行时生成，不烘焙进 Sprite。

---

## 5. Validation Gate

游戏内验证时必须检查：

* `1920x1080` 下无重叠、无文字溢出、无主体裁切。
* `16:10`、`21:9`、`4:3` 下核心信息仍在安全区。
* 小图标在实际显示尺寸下可识别。
* UI 皮肤不会盖住可点击区域。
* 背包拖拽、敌人选择、按钮点击没有被装饰图阻挡。

