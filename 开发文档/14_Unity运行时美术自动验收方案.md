---
id: dev_14_runtime_art_validation
title: Unity 运行时美术自动验收方案
type: dev
role: 程序
domain: runtime_art_validation
status: active
source_of_truth: true
related:
  - 开发文档/rules/00_程序开发总规则.md
  - 开发文档/15_P0配置Validator与自动验收底座需求.md
  - 开发文档/rules/04_自动化测试与验收流程规范.md
  - 开发文档/19_UnityMCP验收编排层设计.md
  - 开发文档/20_UnityMCP验收编排层实现计划.md
  - 开发文档/rules/03_视觉资源系统程序开发规范.md
  - 美术文档/README.md
  - 美术文档/08_Unity运行时美术验收工具需求.md
  - 美术文档/09_运行时美术验收记录.md
  - 美术文档/10_美术验收截图优化与真实数据驱动演进方案.md
  - agent_status/art.md
  - 知识库/views/art.md
last_verified: 2026-07-12
update_rule: 修改对应程序架构、接口契约、验证流程或 Unity 实现边界时同步本文件。
---

# Unity 运行时美术自动验收方案

> **定位：** 本文档定义程序侧如何实现“无人值守”的 Unity 运行时美术验收流水线。它只针对验收功能本身进行设计，不照搬美术侧提出的半自动快捷键方案。
> **核心目标：** agent 一条命令触发后，Unity 自动进入指定界面、等待 UI 稳定、截图、扫描 UI 与资源状态，并覆盖输出最新验收报告。
> **更新时间：** 2026-05-17

---

## 1. 需求重述

美术侧真正需要的不是 F9 快捷键，而是：

* 看到 Unity 运行时真实画面。
* 知道截图对应哪个界面。
* 知道关键 UI 皮肤是否真的加载。
* 知道当前 UI 是否出现 missing sprite。
* 知道背景、面板、按钮、背包格是否存在点击遮挡或尺寸异常。
* 能把截图和结构化报告交给人或 agent 持续复查。

因此程序侧应实现一条自动流水线，而不是依赖人工进入界面和手动截图。

---

## 2. 设计原则

### 2.1 全自动优先

验收工具默认面向 agent 使用：

```text
写入触发文件
    -> Unity 自动进入 Play Mode
    -> 自动跑验收流程
    -> 自动输出最新截图和报告
```

不要求用户按快捷键，不要求用户手动切界面，不要求用户肉眼判断何时截图。

### 2.2 只验收，不改玩法

工具只读取运行时 UI、资源和截图状态。

不负责：

* 修改正式 UI 布局。
* 自动替换美术资源。
* 改写 VisualAssetRegistry。
* 改变正式战斗、掉落、背包规则。
* 把截图提交到 Git。

### 2.3 稳定文件覆盖

默认输出到固定 `latest` 目录，并用稳定文件名覆盖。

重复运行时：

```text
workshop_main.png 覆盖旧 workshop_main.png
combat_hud.png 覆盖旧 combat_hud.png
report.json 覆盖旧 report.json
```

这样美术、程序和 agent 永远看最新状态，不会被历史截图干扰。

### 2.4 可选历史留档

如果需要追溯，可配置保留 `history/<RunID>/`。但默认验收入口只看 `latest/`。

---

## 3. 输出目录

默认输出：

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
  ui_snapshot.json
  registry_snapshot.json
  notes.txt
```

可选历史输出：

```text
UnityClient/Logs/ArtAcceptance/history/20260515_213000/
```

### 3.1 截图命名

截图文件名必须与验收点一一对应：

| 验收点 | 文件名 |
|---|---|
| 工坊主界面 | `workshop_main.png` |
| 深渊地图 | `dungeon_map.png` |
| 战斗 HUD | `combat_hud.png` |
| 战利品拾取 | `inventory_loot.png` |
| 小镇卖出界面 | `sell_panel.png` |
| 义体制造界面 | `prosthetic_panel.png` |
| 层级选择界面 | `layer_select.png` |
| 安全屋节点 | `safe_room.png` |
| 阶梯节点 | `stairs_room.png` |
| 结算界面 | `settlement.png` |

第一版覆盖 P0，当前版本已经扩展到 P1 和节点特殊房间：

```text
workshop_main
sell_panel
prosthetic_panel
layer_select
dungeon_map
safe_room
stairs_room
combat_hud
inventory_loot
settlement
```

---

## 4. 触发方式

新增独立触发文件：

```text
UnityClient/Logs/.art_acceptance_trigger
```

写入：

```text
RUN_ART_ACCEPTANCE
```

Editor 监听到命令后：

1. 清空触发文件为 `DONE`。
2. 确保脚本刷新完成，包括从 Play Mode 退出到 Edit Mode 后再次刷新。
3. 进入 Play Mode。
4. 在运行时启动验收流程。
5. 流程完成后写入报告。
6. 可配置是否自动退出 Play Mode。

不复用 `.test_trigger`，避免和现有 `RUN_ALL_TESTS` 混在一起。

---

## 5. 模块设计

### 5.1 Runtime 模块

建议目录：

```text
UnityClient/Assets/Scripts/ArtAcceptance/
    ArtAcceptanceRunner.cs
    ArtAcceptanceReport.cs
    ArtAcceptanceSnapshot.cs
    ArtAcceptanceRegistrySnapshot.cs
```

职责：

* 执行自动流程。
* 切换到目标界面。
* 等待 UI 稳定。
* 截图。
* 扫描 UI 层级。
* 扫描 registry。
* 输出 JSON。

### 5.2 Editor 模块

建议目录：

```text
UnityClient/Assets/Scripts/Editor/
    ArtAcceptanceEditorDaemon.cs
```

职责：

* 监听 `.art_acceptance_trigger`。
* 管理 Play Mode 进入。
* 把待执行命令写入 `EditorPrefs`。
* 在 Play Mode 启动后创建或唤醒 `ArtAcceptanceRunner`。

### 5.3 与 AutoTestDaemon 的关系

`AutoTestDaemon` 继续只负责自动化测试。

`ArtAcceptanceEditorDaemon` 独立负责美术验收。

两者共享 `UnityClient/Logs/`，但不共享触发文件和报告文件。

---

## 6. 自动流程

当前自动流程：

```text
StartRun()
    -> PrepareGameRuntime()
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
    -> ExportRegistrySnapshot()
    -> ExportUiSnapshot()
    -> ExportReport()
```

### 6.1 PrepareGameRuntime

准备运行态：

* 确保 `GameRoot.Core` 已初始化。
* 确保 `GameFlowController.Instance` 存在。
* 确保 `VisualAssetRegistry` 可加载。
* 确保 Canvas 与 CanvasScaler 存在。
* 必要时调用一次 `ConfigManager.LoadAllConfigs()`，但不绕过 `CoreBackend` 正常初始化。

如果基础对象缺失，流程不崩溃，报告记为 error。

### 6.2 CaptureWorkshopMain

流程：

* 调用 `GameFlowController.EnterWorkshop()`。
* 等待 UI 稳定。
* 截图 `workshop_main.png`。
* 记录活跃 Controller 和 UI 状态。

### 6.2.1 CaptureSellPanel

流程：

* 调用 `GameFlowController.EnterWorkshop()`。
* 通过 `WorkshopUIController.OpenSellPanel()` 打开小镇卖出面板。
* 等待 UI 稳定。
* 截图 `sell_panel.png`。
* 截图后关闭工坊验收弹层，避免污染后续界面。

### 6.2.2 CaptureProstheticPanel

流程：

* 调用 `GameFlowController.EnterWorkshop()`。
* 通过 `WorkshopUIController.OpenProstheticPanel()` 打开义体制造面板。
* 等待 UI 稳定。
* 截图 `prosthetic_panel.png`。
* 截图后关闭工坊验收弹层。

### 6.2.3 CaptureLayerSelect

流程：

* 临时解锁当前配置中存在的全部深渊层级。
* 调用 `WorkshopUIController.OpenDungeonStartLayerPanel()` 打开层级选择界面。
* 等待 UI 稳定。
* 截图 `layer_select.png`。
* 截图后恢复玩家原本的 `HighestUnlockedDungeonLayer`。

### 6.3 CaptureDungeonMap

流程：

* 调用 `GameRoot.Core.Dungeon.StartRunAtLayer(...)`，优先使用已解锁最低层。
* 等待 `DungeonEventBus.OnLayerLoaded` 后进入地图。
* 调用或等待 `DungeonMapUIController.RefreshMap()`。
* 等待 UI 稳定。
* 截图 `dungeon_map.png`。

如果当前配置没有可进入层，标记 skipped。

### 6.3.1 CaptureSafeRoom

流程：

* 构造验收专用 `SafeRoomNode` 预览对象。
* 调用 `GameFlowController.EnterSafeRoom(node)`。
* 等待 UI 稳定。
* 截图 `safe_room.png`。

该流程不订阅或触发真实深渊节点事件，只展示安全屋 UI 状态。

### 6.3.2 CaptureStairsRoom

流程：

* 确保当前存在可用深渊层。
* 构造验收专用 `StairsNode` 预览对象。
* 调用 `GameFlowController.EnterStairs(node)`。
* 等待 UI 稳定。
* 截图 `stairs_room.png`。

该流程不执行进入下一层或返回小镇，只展示阶梯房间选择 UI。

### 6.4 CaptureCombatHud

流程：

* 从当前层找到第一个 CombatNode，或使用配置构造最小战斗。
* 调用 `GameRoot.Core.Combat.StartCombat(monsterIDs)`。
* 切到战斗界面。
* 等待 HUD 刷新和敌人卡片生成。
* 截图 `combat_hud.png`。

如果没有可用怪物配置，标记 skipped。

### 6.5 CaptureInventoryLoot

流程：

* 构造一个仅用于验收的 `CombatLootPickupResult`。
* 从配置中选择 2 到 4 个现有物品实例。
* 调用 `GameFlowController.EnterCombatLoot(result)`。
* 等待战利品 UI、背包 UI 和按钮稳定。
* 截图 `inventory_loot.png`。

注意：

* 构造验收战利品不应写入正式奖励表。
* 不调用真实节点结算，不影响玩家背包最终状态。
* 如果当前 UI 只能通过真实 CombatNode 进入，应在报告中标记该截图为 acceptance-only state。

### 6.6 CaptureSettlement

流程：

* 构造验收专用 `DungeonSettlementResult`。
* 调用 `GameFlowController.EnterSettlementPreview(result)`。
* 等待 UI 稳定。
* 截图 `settlement.png`。

结算截图只使用展示用结果对象，不调用真实深渊结算，也不修改玩家仓库、背包或收益账本。

---

## 7. 等待 UI 稳定机制

### 7.1 目标

等待稳定不是给人看的等待，而是保证机器截图和扫描时 UI 状态可靠。

对用户的表现：

* 自动流程在每个界面略停顿。
* 不需要人工点确认。
* 不需要人判断动画是否结束。

### 7.2 推荐实现

```csharp
private IEnumerator WaitForVisualStable(float extraDelaySeconds = 0.2f) {
    yield return null;
    Canvas.ForceUpdateCanvases();
    yield return null;
    yield return new WaitForEndOfFrame();

    if (extraDelaySeconds > 0f) {
        yield return new WaitForSeconds(extraDelaySeconds);
        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame();
    }
}
```

### 7.3 稳定条件

第一版不做复杂动画检测，只保证：

* GameObject 激活状态已生效。
* LayoutGroup / ContentSizeFitter 已重建。
* Canvas 已刷新。
* 截图发生在帧末。

后续如果 UI 有正式入场动画，可增加：

* 等待 `VisualQueue` 清空。
* 等待指定 Controller 报告 `IsReadyForCapture`。
* 等待 Animator 不在 Transition 中。

---

## 8. report.json

`report.json` 输出在：

```text
UnityClient/Logs/ArtAcceptance/latest/report.json
```

建议结构：

```json
{
  "SchemaVersion": "1.0",
  "RunID": "20260515_213000",
  "Project": "P3",
  "Mode": "auto",
  "StartedAt": "2026-05-15T21:30:00+08:00",
  "FinishedAt": "2026-05-15T21:30:12+08:00",
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
    "MatchWidthOrHeight": 0.5
  },
  "Registry": {
    "RegistryFound": true,
    "MissingSpriteFound": true,
    "EntryCount": 49,
    "MissingRequiredVisualIDs": []
  },
  "Captures": [
    {
      "ScreenTag": "workshop_main",
      "File": "screenshots/workshop_main.png",
      "Status": "captured",
      "Resolution": "1920x1080",
      "ActiveControllers": ["GameFlowController", "WorkshopUIController"],
      "Warnings": [],
      "Errors": []
    }
  ],
  "Warnings": [],
  "Errors": []
}
```

### 8.1 Capture Status

每个截图点有状态：

| Status | 含义 |
|---|---|
| `captured` | 已成功截图 |
| `skipped` | 因状态或配置缺失跳过 |
| `failed` | 执行失败 |

---

## 9. registry_snapshot.json

记录当前 `VisualAssetRegistry` 的客观状态。

必须包含：

* registry 是否存在。
* MissingSprite 是否存在。
* EntryCount。
* P0 VisualID 是否全部存在。
* 每个 entry 对应 Sprite 名称、纹理尺寸、border、类型。

第一版 P0 必查 VisualID：

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

如果缺失，写入：

```text
report.Registry.MissingRequiredVisualIDs
report.Errors
```

---

## 10. ui_snapshot.json

`ui_snapshot.json` 记录当前运行时 UI 层级与关键组件。

第一版可以按 capture 分组：

```json
{
  "Captures": [
    {
      "ScreenTag": "combat_hud",
      "Canvases": [
        {
          "Name": "Canvas",
          "RenderMode": "ScreenSpaceOverlay",
          "SortingOrder": 0,
          "Elements": []
        }
      ]
    }
  ]
}
```

### 10.1 Element 字段

每个 UI 元素记录：

* Path。
* Active。
* ComponentTypes。
* RectTransform anchoring、position、size。
* Image sprite、type、raycastTarget、preserveAspect。
* Button interactable。
* Text 内容长度、raycastTarget。
* CanvasGroup alpha、interactable、blocksRaycasts。

### 10.2 自动风险标记

工具应自动标记：

* Image 使用 `ui_missing_sprite`。
* Image enabled 但 sprite 为空。
* 大尺寸非交互 Image 的 `raycastTarget=true`。
* Button 缺少 Image 或 targetGraphic。
* 背包格尺寸不是 `100x100`。
* CanvasScaler 不是 `ScaleWithScreenSize`。
* CanvasScaler 参考分辨率不是 `1920x1080`。
* 需要 Sliced 的 P0 UI 使用了非 Sliced。

这些属于客观风险提示，不替代美术最终审美判断。

---

## 11. 验收判定

工具只输出 `PASSED / WARNING / FAILED` 三类总体状态。

### 11.1 FAILED

出现以下情况即失败：

* 无法生成任何截图。
* P0 必需 VisualID 缺失。
* registry 不存在。
* MissingSprite 不存在。
* 主 CanvasScaler 缺失或参考分辨率不是 `1920x1080`。

### 11.2 WARNING

出现以下情况为 warning：

* 某个截图点 skipped。
* 当前 UI 使用了 missing sprite。
* 大背景或面板可能阻挡点击。
* 背包格尺寸异常。
* 按钮缺 Sprite。
* 截图分辨率不是 `1920x1080`。

### 11.3 PASSED

所有 P0 截图成功，且没有 errors。

---

## 12. 实现顺序

### 第一步：文档与基础结构

* 新增本方案文档。
* 新增 `ArtAcceptance` runtime 目录。
* 定义 report / snapshot 数据结构。

### 第二步：Editor 自动触发

* 新增 `.art_acceptance_trigger` 监听。
* 支持 `RUN_ART_ACCEPTANCE`。
* 自动进入 Play Mode。

### 第三步：Runtime 自动截图

* 自动创建或定位 `ArtAcceptanceRunner`。
* 输出 `latest/screenshots/*.png`。
* 使用固定文件名覆盖。
* 实现 `WaitForVisualStable()`。

### 第四步：Registry 与 UI Snapshot

* 输出 `registry_snapshot.json`。
* 输出 `ui_snapshot.json`。
* 写入 report warnings/errors。

### 第五步：流程覆盖

* 覆盖 `workshop_main`。
* 覆盖 `sell_panel`。
* 覆盖 `prosthetic_panel`。
* 覆盖 `layer_select`。
* 覆盖 `dungeon_map`。
* 覆盖 `safe_room`。
* 覆盖 `stairs_room`。
* 覆盖 `combat_hud`。
* 覆盖 `inventory_loot`。
* 覆盖 `settlement`。

### 第六步：验证与收口

* 用触发文件跑一次完整验收。
* 检查 `latest` 是否可持续覆盖。
* 根据报告调整误报规则。

---

## 13. 与现有规范的关系

本工具遵守：

* [`03_视觉资源系统程序开发规范.md`](./rules/03_视觉资源系统程序开发规范.md)
* [`00_程序开发总规则.md`](./rules/00_程序开发总规则.md)

关键约束：

* 不让 JSON 直接引用 Unity 对象。
* 不让验收工具成为玩法状态源。
* 不通过人工步骤作为默认流程。
* 不把运行时 fallback UI 当作正式验收通过依据。
* 不让报告产物进入 `Assets`。

---

## 14. 第一版交付标准

第一版完成后，agent 应能执行：

```powershell
Set-Content -Path "UnityClient/Logs/.art_acceptance_trigger" -Value "RUN_ART_ACCEPTANCE"
```

然后自动得到：

```text
UnityClient/Logs/ArtAcceptance/latest/screenshots/workshop_main.png
UnityClient/Logs/ArtAcceptance/latest/screenshots/sell_panel.png
UnityClient/Logs/ArtAcceptance/latest/screenshots/prosthetic_panel.png
UnityClient/Logs/ArtAcceptance/latest/screenshots/layer_select.png
UnityClient/Logs/ArtAcceptance/latest/screenshots/dungeon_map.png
UnityClient/Logs/ArtAcceptance/latest/screenshots/safe_room.png
UnityClient/Logs/ArtAcceptance/latest/screenshots/stairs_room.png
UnityClient/Logs/ArtAcceptance/latest/screenshots/combat_hud.png
UnityClient/Logs/ArtAcceptance/latest/screenshots/inventory_loot.png
UnityClient/Logs/ArtAcceptance/latest/screenshots/settlement.png
UnityClient/Logs/ArtAcceptance/latest/report.json
UnityClient/Logs/ArtAcceptance/latest/ui_snapshot.json
UnityClient/Logs/ArtAcceptance/latest/registry_snapshot.json
```

重复执行会覆盖 `latest` 下同名文件，以便持续 update。

---

## 15. 当前落地状态

更新时间：2026-05-17 (v3)

已完成自动验收工具 P0/P1 扩展版，**10 个截图点与报告功能已全部验证通过**。

* 运行时模块已按设计拆分为独立文件：

```text
UnityClient/Assets/Scripts/ArtAcceptance/
    ArtAcceptanceRunner.cs          — 运行时入口与流程逻辑
    ArtAcceptanceReport.cs          — report.json 数据结构
    ArtAcceptanceSnapshot.cs        — ui_snapshot.json 数据结构
    ArtAcceptanceRegistrySnapshot.cs — registry_snapshot.json 数据结构

UnityClient/Assets/Scripts/Tests/
    ArtAcceptanceSmokeTest.cs       — 数据结构与状态判定冒烟测试
```

* 新增 Editor 触发器 `ArtAcceptanceEditorDaemon`，位于 `UnityClient/Assets/Scripts/Editor/ArtAcceptanceEditorDaemon.cs`。
* 支持监听独立触发文件：

```text
UnityClient/Logs/.art_acceptance_trigger
```

* 支持命令：

```text
RUN_ART_ACCEPTANCE
```

* 自动输出并覆盖：

```text
UnityClient/Logs/ArtAcceptance/latest/
```

* 已实现截图点：

```text
workshop_main
sell_panel
prosthetic_panel
layer_select
dungeon_map
safe_room
stairs_room
combat_hud
inventory_loot
settlement
```

* 截图方案采用 `Camera.Render() + RenderTexture` 同步渲染，通过临时将 Screen Space Overlay Canvas 切换为 Camera 模式来捕获 UI。该方案不依赖 `WaitForEndOfFrame`，在 `EditorApplication.update` 驱动的手动协程栈中可靠工作。
* 已实现 `report.json`、`ui_snapshot.json`、`registry_snapshot.json`。
* 已实现 P0 UI VisualID 缺失检查、CanvasScaler 检查、missing sprite 风险、按钮/背景 raycast 风险、背包格尺寸风险。
* 已补齐运行时生成的小镇卖出、义体制造、层级选择、安全屋、阶梯按钮/面板皮肤，避免纯色 Image 和按钮缺 sprite 造成验收误报。
* Editor 触发器会在待执行验收任务进入 Play Mode 前刷新 `AssetDatabase`，包括从 Play Mode 退出回 Edit Mode 后的刷新，避免 agent 外部改代码后跑到旧脚本域。
* 工具不订阅战斗、掉落、深渊结算等业务事件，不作为被动业务事件触发器；只在 agent 或菜单显式触发验收时运行。

验证状态：

* `dotnet build Assembly-CSharp.csproj` 通过（0 警告 0 错误）。
* `dotnet build Assembly-CSharp-Editor.csproj` 通过（0 警告 0 错误）。
* `ArtAcceptanceSmokeTest.Run()` — PASSED。
* `RUN_ALL_TESTS` 全回归 — 13/13 PASSED。
* 运行时验收截图 — RunID=`20260517_170409`，10/10 captured，0 Errors，0 Warnings，Status=PASSED。

当前限制：

* 第一版截图流程通过公开入口主动切换界面，属于专门的自动验收运行，不适合在玩家正常游玩中途插入。
* `inventory_loot` 使用验收专用 `CombatLootPickupResult` 构造展示数据，不调用真实奖励结算，不写入奖励表。
* 目前未自动保留 `history/<RunID>`，默认只维护 `latest`。

### 15.1 自动验收与可玩接入边界

更新时间：2026-05-26

ArtAcceptance 是运行时视觉验收工具，只回答“Unity 运行时能否打开目标画面、加载关键 VisualID、输出稳定截图和结构化报告”。它不回答“玩家能否从正常流程到达该界面”，也不回答“界面按钮是否已经接入真实后端操作”。

因此后续使用 ArtAcceptance 结果时按以下边界解释：

* `captured` 只表示验收 Runner 成功切入并截图，不代表该界面已经挂到玩家主流程入口。
* `MissingRequiredVisualIDs=[]` 只表示该截图点所需 VisualID 不缺，不代表交互行为已经完成。
* Runner 可以调用公开流程入口或验收专用 preview 对象构造画面；这类入口必须继续标记为验收覆盖，不得当作玩家可玩路径证据。
* “等待 UI 稳定”是机器截图前的布局 / Canvas 刷新等待，不需要玩家感知，也不是业务事件触发。
* 玩家主流程可达需要另有游戏路径证据：例如从小镇按钮、深渊节点、战斗结算、订单入口进入目标界面。
* 真实操作闭环需要另有服务绑定证据：按钮必须调用后端 / 领域服务，失败原因、状态变化、回滚和 UI 刷新需要由 smoke test 或人工流程验证。

截至 2026-05-26，latest Formal V1 队列已覆盖 21 个 active 界面并完成截图覆盖；该结论只代表“运行时截图覆盖完成”。后续程序侧若要声明某界面“可玩接入完成”，还必须补齐玩家主流程入口和真实服务操作闭环。

## 0. 2026-07-12 V2 入口与旧 Runner 定位

日常验收入口切换为 `p3-art-validation` V2 的 MCP 实时优先流程。本文原有 `ArtAcceptanceRunner` 自动截图链继续保留，但定位调整为全量视觉回归后端；MCP 编排、按需截图、ArtRunID 和外部复核契约以 `开发文档/19_UnityMCP验收编排层设计.md` 第 14 节为准。

日常单界面或小范围验收：

```text
注册目标
-> MCP 进入并查看实时 Game View
-> MCP 读取 UI 层级和组件
-> 诊断
-> 按结论截图
-> ArtRunID 归档
-> 主美复核
```

全量回归：

```text
art_regression
-> ArtAcceptanceRunner
-> 完整截图集 / UI snapshot / Registry snapshot / report
-> 导入 ArtRunID
-> 主美复核
```

旧 runner 的截图步骤和报告结构继续有效，但不得再把“运行完整 runner”作为每次美术验收的默认前置条件。Runner 的 `PASSED` 只表示机器技术检查通过，不代表视觉质量或主美验收通过。
