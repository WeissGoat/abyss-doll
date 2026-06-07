---
id: art_runtime_art_validation_requirements
title: Unity 运行时美术验收工具需求与交付状态
type: art
role: 美术
domain: runtime_art_validation
status: historical
source_of_truth: false
related:
  - 开发文档/14_Unity运行时美术自动验收方案.md
  - 开发文档/rules/03_视觉资源系统程序开发规范.md
  - 美术文档/09_运行时美术验收记录.md
last_verified: 2026-05-23
update_rule: 历史记录仅在追溯或修正归档事实时更新。
---

# Unity 运行时美术验收工具需求与交付状态

> **定位：** 给美术侧、程序侧和 agent 同步 Unity 运行时美术验收工具的需求、最终实现和未交付项。  
> **更新时间：** 2026-05-17  
> **当前口径：** 以程序侧最新全自动方案 [`开发文档/14_Unity运行时美术自动验收方案.md`](../开发文档/14_Unity运行时美术自动验收方案.md) 为准。早期 `F9` 半自动截图方案不再作为默认交付目标。

---

## 1. 背景

美术侧需要看到素材接入 Unity 后的真实运行画面，而不只是确认 PNG 是否登记到 `VisualAssetRegistry`。

验收工具需要稳定输出：

* 运行时真实截图。
* 每张截图对应的界面标签。
* 当前参考分辨率与实际截图分辨率。
* 关键 UI VisualID 是否成功登记和加载。
* 当前活跃 UI 层级、`Image.sprite`、`Image.type`、`raycastTarget`、`RectTransform` 尺寸等客观状态。
* 可供人和 agent 持续复查的 `report.json`、`registry_snapshot.json`、`ui_snapshot.json`。

核心目标是：**美术侧可以通过落盘截图和结构化报告完成验收，不依赖远程观看 Unity Editor，也不依赖人工手动切界面和按快捷键。**

---

## 2. 当前最终实现

当前已经交付的是 **全自动验收模式**：

```text
写入触发文件
  -> Unity Editor 自动进入 Play Mode
  -> 运行时自动切换 P0/P1 验收界面
  -> 等待 UI 稳定
  -> 截图
  -> 扫描 UI 和 VisualAssetRegistry
  -> 覆盖输出 latest 验收产物
```

### 2.1 触发方式

写入独立触发文件：

```powershell
Set-Content -Path "UnityClient/Logs/.art_acceptance_trigger" -Value "RUN_ART_ACCEPTANCE"
```

触发器行为：

* `ArtAcceptanceEditorDaemon` 监听 `UnityClient/Logs/.art_acceptance_trigger`。
* 识别 `RUN_ART_ACCEPTANCE` 后将触发文件写回 `DONE`。
* 如果当前不在 Play Mode，则自动进入 Play Mode。
* Play Mode 运行态准备好后创建 `ArtAcceptanceRunner`。
* 验收结束后输出报告，文件触发模式默认自动退出 Play Mode。

### 2.2 输出目录

当前默认只维护稳定目录：

```text
UnityClient/Logs/ArtAcceptance/latest/
  screenshots/
    workshop_main.png
    sell_panel.png
    prosthetic_panel.png
    layer_select.png
    dungeon_map.png
    safe_room.png
    stairs_room.png
    combat_hud.png
    inventory_loot.png
    settlement.png
  report.json
  registry_snapshot.json
  ui_snapshot.json
  notes.txt
```

重复运行会覆盖 `latest` 下同名文件，保证美术、程序和 agent 永远读取最新结果，避免历史截图干扰。

### 2.3 已覆盖截图点

| 顺序 | ScreenTag | 文件 | 状态 |
|---|---|---|---|
| 1 | `workshop_main` | `screenshots/workshop_main.png` | 已交付 |
| 2 | `sell_panel` | `screenshots/sell_panel.png` | 已交付 |
| 3 | `prosthetic_panel` | `screenshots/prosthetic_panel.png` | 已交付 |
| 4 | `layer_select` | `screenshots/layer_select.png` | 已交付 |
| 5 | `dungeon_map` | `screenshots/dungeon_map.png` | 已交付 |
| 6 | `safe_room` | `screenshots/safe_room.png` | 已交付 |
| 7 | `stairs_room` | `screenshots/stairs_room.png` | 已交付 |
| 8 | `combat_hud` | `screenshots/combat_hud.png` | 已交付 |
| 9 | `inventory_loot` | `screenshots/inventory_loot.png` | 已交付 |
| 10 | `settlement` | `screenshots/settlement.png` | 已交付 |

---

## 3. 实现文件

Runtime 模块：

```text
UnityClient/Assets/Scripts/ArtAcceptance/
  ArtAcceptanceRunner.cs
  ArtAcceptanceReport.cs
  ArtAcceptanceSnapshot.cs
  ArtAcceptanceRegistrySnapshot.cs
```

Editor 触发模块：

```text
UnityClient/Assets/Scripts/Editor/
  ArtAcceptanceEditorDaemon.cs
```

测试模块：

```text
UnityClient/Assets/Scripts/Tests/
  ArtAcceptanceSmokeTest.cs
```

---

## 4. 自动流程

当前自动流程：

```text
RunAcceptanceFlow()
  -> WaitForRuntimeReady()
  -> CaptureGlobalEnvironment()
  -> CaptureWorkshopMain()
  -> CaptureSellPanel()
  -> CaptureProstheticPanel()
  -> CaptureLayerSelect()
  -> CaptureDungeonMap()
  -> CaptureSafeRoom()
  -> CaptureStairsRoom()
  -> CaptureCombatHud()
  -> CaptureInventoryLoot()
  -> CaptureSettlement()
  -> BuildRegistrySnapshot()
  -> FinalizeReport()
  -> WriteOutputs()
```

### 4.1 Runtime 准备

工具会等待以下对象可用：

* `GameRoot.Core`
* `GameRoot.Core.CurrentPlayer`
* `GameRoot.Core.CurrentPlayer.ActiveDoll`
* `GameFlowController.Instance`

如果运行态超时未准备好，会写入 error，但仍尽量输出诊断报告。

### 4.2 截图方式

当前实现使用：

```text
Camera.Render() + RenderTexture
```

截图时会临时将 Screen Space Overlay Canvas 切换到 Camera 模式进行同步渲染。这个方案比早期 `ScreenCapture.CaptureScreenshot + WaitForEndOfFrame` 更适合当前 Editor 自动驱动流程。

### 4.3 UI 稳定等待

每个截图点会：

* 等待若干帧。
* 调用 `Canvas.ForceUpdateCanvases()`。
* 等待短暂 realtime delay。
* 再截图和扫描。

这不是给人看的等待，而是为了保证 Layout、激活状态和截图帧稳定。

### 4.4 战利品界面说明

`inventory_loot` 截图使用验收专用 `CombatLootPickupResult` 构造展示数据。

它不会：

* 调用真实战斗节点结算。
* 写入奖励表。
* 改变玩家正式掉落收益。
* 修改背包最终状态。

---

## 5. report.json

输出路径：

```text
UnityClient/Logs/ArtAcceptance/latest/report.json
```

当前实际结构：

```json
{
  "SchemaVersion": "1.0",
  "RunID": "20260517_161548",
  "Project": "P3",
  "Mode": "auto",
  "Status": "PASSED",
  "IsRunning": false,
  "StartedAt": "2026-05-17T16:15:48.2331664+08:00",
  "FinishedAt": "2026-05-17T16:15:50.5928095+08:00",
  "OutputRoot": "UnityClient/Logs/ArtAcceptance/latest",
  "ReferenceResolution": {
    "Width": 1920,
    "Height": 1080
  },
  "ActualResolution": {
    "Width": 1920,
    "Height": 1080
  },
  "CanvasScaler": {
    "Found": true,
    "Mode": "ScaleWithScreenSize",
    "ReferenceResolution": "1920x1080",
    "MatchWidthOrHeight": 0.0
  },
  "Registry": {
    "RegistryFound": true,
    "MissingSpriteFound": true,
    "MissingSpriteVisualID": "ui_missing_sprite",
    "EntryCount": 50,
    "MissingRequiredVisualIDs": []
  },
  "Captures": [
    {
      "Index": 1,
      "ScreenTag": "workshop_main",
      "File": "screenshots/workshop_main.png",
      "CapturedAt": "2026-05-17T16:15:48.3518270+08:00",
      "Status": "captured",
      "Resolution": "1920x1080",
      "ActiveControllers": [
        "ArtAcceptanceRunner",
        "GameFlowController",
        "GameRoot",
        "WorkshopUIController"
      ],
      "Warnings": [],
      "Errors": []
    }
  ],
  "Warnings": [],
  "Errors": []
}
```

### 5.1 字段说明

| 字段 | 说明 |
|---|---|
| `SchemaVersion` | 报告结构版本，当前为 `1.0`。 |
| `RunID` | 本次运行 ID，使用本地时间。 |
| `Mode` | 当前固定为 `auto`。 |
| `Status` | 总体验收状态：`PASSED`、`WARNING`、`FAILED`。 |
| `ReferenceResolution` | 美术参考分辨率，固定 `1920x1080`。 |
| `ActualResolution` | 当前验收截图输出分辨率，当前固定渲染为 `1920x1080`。 |
| `CanvasScaler` | 主 CanvasScaler 配置。 |
| `Registry` | VisualAssetRegistry 检查结果。 |
| `Captures` | 每个截图点的结构化记录。 |
| `Warnings` | 可继续验收但需要关注的问题。 |
| `Errors` | 阻塞验收的问题。 |

### 5.2 Capture 状态

| Status | 含义 |
|---|---|
| `captured` | 已成功截图。 |
| `skipped` | 因配置或运行态缺失跳过。 |
| `failed` | 截图或流程执行失败。 |

---

## 6. registry_snapshot.json

输出路径：

```text
UnityClient/Logs/ArtAcceptance/latest/registry_snapshot.json
```

当前记录内容：

* `RegistryFound`
* `MissingSpriteFound`
* `MissingSpriteVisualID`
* `MissingSpriteName`
* `EntryCount`
* `MissingRequiredVisualIDs`
* 每个 registry entry 的：
  * `VisualID`
  * `HasSprite`
  * `HasPrefab`
  * `HasAudioClip`
  * `HasMaterial`
  * `SpriteName`
  * `TextureSize`
  * `SpriteRect`
  * `SpriteBorder`

### 6.1 P0 必查 VisualID

```text
ui_panel_info
ui_button_primary
ui_button_secondary
ui_button_danger
ui_inventory_chassis_panel
ui_inventory_slot_available
ui_inventory_slot_locked
ui_inventory_slot_hover
ui_inventory_slot_valid
ui_inventory_slot_invalid
ui_loot_pickup_panel
ui_loot_drop_zone
ui_combat_enemy_card
ui_combat_enemy_card_selected
ui_combat_status_bar_hp
ui_combat_status_bar_shield
ui_combat_ap_pip
ui_combat_turn_banner
ui_icon_money
```

如果缺失任一 VisualID 或对应 Sprite 为空，会写入：

```text
report.Registry.MissingRequiredVisualIDs
report.Errors
```

---

## 7. ui_snapshot.json

输出路径：

```text
UnityClient/Logs/ArtAcceptance/latest/ui_snapshot.json
```

当前按 capture 分组：

```json
{
  "SchemaVersion": "1.0",
  "RunID": "20260517_161548",
  "Captures": [
    {
      "ScreenTag": "combat_hud",
      "Canvases": [
        {
          "Name": "Canvas",
          "Path": "Canvas",
          "RenderMode": "ScreenSpaceOverlay",
          "SortingOrder": 0,
          "Elements": []
        }
      ]
    }
  ]
}
```

### 7.1 Element 字段

每个 UI 元素会记录：

* `Name`
* `Path`
* `Active`
* `AnchoredPosition`
* `SizeDelta`
* `AnchorMin`
* `AnchorMax`
* `Pivot`
* `ComponentTypes`
* `Image.Found`
* `Image.Enabled`
* `Image.SpriteName`
* `Image.Type`
* `Image.RaycastTarget`
* `Image.PreserveAspect`
* `Button.Found`
* `Button.Interactable`
* `Button.HasTargetGraphic`
* `Text.Found`
* `Text.Enabled`
* `Text.TextLength`
* `Text.RaycastTarget`
* `Text.FontSize`
* `CanvasGroup.Found`
* `CanvasGroup.Alpha`
* `CanvasGroup.Interactable`
* `CanvasGroup.BlocksRaycasts`
* `Risks`

### 7.2 已实现风险标记

| Risk | 含义 |
|---|---|
| `ImageEnabledButSpriteEmpty` | `Image` 可见但没有 sprite。 |
| `MissingSpriteVisible` | 可见 UI 使用了 `ui_missing_sprite` 或疑似 missing sprite。 |
| `LargeNonButtonImageBlocksRaycasts` | 大尺寸非按钮 Image 可能阻挡点击。 |
| `ExpectedSlicedImageButTypeIsNotSliced` | 需要 Sliced 的 P0 UI 使用了非 Sliced。 |
| `ButtonMissingTargetGraphic` | Button 缺少 targetGraphic。 |
| `ButtonMissingImageSprite` | Button 缺少 Image sprite。 |
| `InventorySlotSizeMismatch:*` | 背包格尺寸不符合程序侧 `InventoryDisplaySpec.CellSize`。 |

### 7.3 Sliced 判定口径

当前判定为需要 `Image.Type=Sliced` 的资源包括：

* `ui_button_*`
* `ui_panel_*`
* `ui_inventory_chassis_panel`
* `ui_loot_pickup_panel`
* `ui_loot_drop_zone`
* `ui_combat_enemy_card`
* `ui_combat_enemy_card_selected`
* `ui_combat_status_bar_hp`
* `ui_combat_status_bar_shield`
* `ui_combat_turn_banner`

背包格状态图标 `ui_inventory_slot_*` **不要求 Sliced**。按最新 UI DisplaySpec，背包格固定 `100x100`，`Image.Type=Simple`。

---

## 8. 验收判定

### 8.1 FAILED

出现以下情况会失败：

* 没有生成任何截图。
* `VisualAssetRegistry` 不存在。
* `VisualAssetRegistry.MissingSprite` 未配置。
* P0 必需 VisualID 缺失或没有 Sprite。
* 主 CanvasScaler 缺失。
* 主 CanvasScaler 参考分辨率不是 `1920x1080`。
* 某个关键步骤抛出未处理异常。

### 8.2 WARNING

出现以下情况会警告：

* 某个截图点 skipped。
* 当前活跃 UI 使用了 missing sprite。
* 大背景或大面板可能阻挡点击。
* 背包格尺寸异常。
* 按钮缺 Sprite 或 targetGraphic。
* 截图分辨率不是 `1920x1080`。

### 8.3 PASSED

当前判断口径：

* 当前启用的 10 个截图点全部 `captured`。
* `report.Errors` 为空。
* `report.Warnings` 为空。
* P0 VisualID 无缺失。
* `ui_snapshot.json` 和 `registry_snapshot.json` 均可解析。

---

## 9. 与自动化测试系统的关系

项目已有：

```text
UnityClient/Assets/Scripts/Editor/AutoTestDaemon.cs
UnityClient/Logs/.test_trigger
UnityClient/Logs/TestReport.json
```

当前关系：

* `AutoTestDaemon` 只负责程序自动化测试和 `RUN_ALL_TESTS`。
* `ArtAcceptanceEditorDaemon` 独立负责美术验收和 `RUN_ART_ACCEPTANCE`。
* 两者共享 `UnityClient/Logs/` 根目录，但不共享触发文件和报告文件。
* 美术验收报告不写入 `TestReport.json`。

---

## 10. 已交付清单

| 项目 | 状态 |
|---|---|
| 独立触发文件 `.art_acceptance_trigger` | 已交付 |
| `RUN_ART_ACCEPTANCE` 自动触发 | 已交付 |
| 自动进入 Play Mode | 已交付 |
| 自动创建运行时 `ArtAcceptanceRunner` | 已交付 |
| `latest` 稳定覆盖输出 | 已交付 |
| P0 四界面截图 | 已交付 |
| P1 小镇卖出、义体制造、层级选择、结算截图 | 已交付 |
| 安全屋、阶梯节点界面截图 | 已交付 |
| `report.json` | 已交付 |
| `registry_snapshot.json` | 已交付 |
| `ui_snapshot.json` | 已交付 |
| CanvasScaler 检查 | 已交付 |
| P0 VisualID 缺失检查 | 已交付 |
| MissingSprite 检查 | 已交付 |
| 背包格 `100x100` 检查 | 已交付 |
| 大图阻挡点击风险检查 | 已交付 |
| Button 基础风险检查 | 已交付 |
| Sliced/Simple 规则检查 | 已交付 |
| `ArtAcceptanceSmokeTest` | 已交付 |
| `RUN_ALL_TESTS` 回归覆盖 | 已交付 |
| Play Mode 退出后刷新脚本域再验收 | 已交付 |

---

## 11. 未交付与后续可选项

### 11.1 尚未交付

| 项目 | 影响 | 建议优先级 |
|---|---|---|
| `history/<RunID>/` 历史留档 | 目前只能看最新结果，不能自动追溯旧截图。 | P2 |
| UI 动画完成检测 | 当前只等待布局和短延迟，未等待 Animator/转场队列。 | P2 |
| 点击命中实测 | 当前只做 raycast 风险静态扫描，未模拟点击按钮/格子。 | P2 |
| 可配置截图点列表 | 当前截图点写在代码流程中，尚未抽成配置。 | P2 |
| 手动 F9 截图模式 | 最新方向不依赖人工，暂未交付。 | P3 |



---

## 12. 最近验证结果

最近一次程序侧验证：

```text
RunID=20260517_170409
Status=PASSED
Captures=10
Errors=0
Warnings=0
ActualResolution=1920x1080
MissingSpriteVisualID=ui_missing_sprite
```

同时通过：

```text
dotnet build Assembly-CSharp.csproj
dotnet build Assembly-CSharp-Editor.csproj
RUN_ALL_TESTS: 13/13 PASSED
```

---

## 13. 给美术侧的使用说明

美术侧需要复查时，只需要读取：

```text
UnityClient/Logs/ArtAcceptance/latest/
```

优先查看：

```text
screenshots/*.png
report.json
```

如果 `report.json` 为：

```text
Status=PASSED
Errors=[]
Warnings=[]
```

说明程序侧客观检查已经通过，美术侧可以继续做人眼审美验收，例如：

* 画面风格是否统一。
* 按钮与文字是否拥挤。
* 背景是否抢信息层级。
* 敌人卡片、状态条、战利品区是否清晰。
* 背包格、图标、拖拽视觉是否舒适。

如果 `Status=WARNING` 或 `FAILED`，优先看：

```text
report.json -> Warnings / Errors
ui_snapshot.json -> Risks
registry_snapshot.json -> MissingRequiredVisualIDs
```
