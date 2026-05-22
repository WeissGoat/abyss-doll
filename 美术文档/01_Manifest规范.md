# Manifest 规范

> **定位：** 规定 `art_manifest.json` 的字段结构、字段含义，以及美术流水线每一步应该填充哪些字段。
> **更新时间：** 2026-05-13

---

## 1. Manifest 是什么

Manifest 是美术生产台账，不是玩法配置表，也不是 Unity 运行时资源注册表。

它记录：

* 当前需要哪些视觉资产。
* 资产来自配置表、配置推导，还是预置美术需求。
* 每个资产对应哪个 `VisualID`。
* 每个资产的中文审阅描述、英文提示词、结构化规格、生成批次、筛选结果和接入状态。

---

## 2. Step 1 来源规则

测试或调试配置不进入正式美术需求。文件名以 `_test.json` 结尾的配置表由自动化测试或临时验证使用，扫描时跳过；如果旧 Manifest 中已有对应条目，后续增量更新会将其标记为 `deprecated`。

### 2.1 配置表直接扫描

| 来源 | 生成资产 | 示例 |
|---|---|---|
| `Items/*.json` | 物品图标 | `gear_tactical_blade -> item_gear_tactical_blade_icon` |
| `Monsters/*.json` | 怪物头像 | `mob_scavenger_bug -> monster_mob_scavenger_bug_portrait` |
| `Prosthetics/*.json` | 义体图标 | `pros_power_arm -> prosthetic_pros_power_arm_icon` |
| `Chassis/*.json` | 底盘表现 | `chassis_lv1_basic -> chassis_chassis_lv1_basic_frame` |
| `Dolls/*.json` | 魔偶立绘 | `doll_proto_0 -> doll_proto_0_stand` |

对应 `SourceType=config`。

### 2.2 配置表推导

| 来源 | 推导资产 | 示例 |
|---|---|---|
| `Dungeons/*.json` 的 `NodePool.NodeType` | 地图节点图标 | `CombatNode -> node_combat_icon` |
| `Dungeons/*.json` 的 `BossNode` | Boss 节点图标 | `BossNode -> node_boss_icon` |
| `Dungeons/*.json` 的 `LayerID/Name` | 层级背景基调 | `layer_1 -> bg_dungeon_layer_1` |

对应 `SourceType=derived`。

### 2.3 预置美术需求

| 预置资产 | 用途 | 示例 VisualID |
|---|---|---|
| 缺失占位图 | Registry fallback | `ui_missing_sprite` |
| 工坊背景 | 工坊整备界面 | `bg_workshop_day` |
| 通用战斗背景 | 战斗界面底图 | `bg_combat_abyss` |
| 深渊路线图背景 | 地图界面底图 | `bg_dungeon_map` |
| UI 面板皮肤 | 通用弹窗、列表、结算 | `ui_panel_main` |
| 背包格子状态 | 可用、锁定、悬停、可放置、不可放置 | `ui_inventory_slot_available` |
| 战斗 HUD 皮肤 | 敌人卡框、状态条、行动点 | `ui_combat_enemy_card` |
| 程序缺口反馈 | 程序侧临时色块、fallback 或缺图项 | 按实际 `VisualID` 命名 |

对应 `SourceType=preset`。

预置需求统一维护在独立种子文件：

```text
美术文档/art_requirements_seed.json
```

`preset` 仍然属于 Step 1 的来源之一，不新增 `SourceType`。为了方便管理，种子文件内可用 `PresetCategory` 细分：

| `PresetCategory` | 用途 |
|---|---|
| `system_fallback` | 缺失占位、全局 fallback。 |
| `screen_background` | 配置扫不出的界面背景或房间插图。 |
| `ui_skin` | 通用 UI 皮肤，如面板、按钮、列表行。 |
| `ui_inventory` | 背包、战利品拾取专项 UI。 |
| `ui_combat` | 战斗 HUD 专项 UI。 |
| `ui_dungeon_map` | 深渊地图路线、节点底板等 UI。 |
| `ui_settlement` | 胜利、战败、撤离结算 UI。 |
| `program_gap` | 程序侧反馈的临时缺图或临时色块资产。 |

---

## 3. 顶层结构

`ConfigRoot` 指向 Unity 运行时读取的配置副本。版本源仍是仓库根目录的 `配置表(JSON)`；运行美术扫描前先执行 `tools/config/Sync-Configs.ps1 -Clean`，把源配置同步到 `UnityClient/Assets/StreamingAssets/Configs`。

```json
{
  "Version": 1,
  "ConfigRoot": "UnityClient/Assets/StreamingAssets/Configs",
  "StatusFlow": ["todo", "prompted", "generated", "selected", "approved", "registered", "validated", "rejected", "deprecated"],
  "Entries": []
}
```

---

## 4. Entry 字段

### Step 1 填充

| 字段 | 示例 | 说明 |
|---|---|---|
| `Domain` | `item` | 资产领域：`item`、`monster`、`node`、`background` 等。 |
| `SourceType` | `config` | 来源类型：`config`、`derived`、`preset`。 |
| `DeriveRule` | `Items/*.json -> item icon` | 资产如何被扫出或推导。 |
| `ConfigSource` | `UnityClient/.../gear_tactical_blade.json` | 来源文件或预置需求文档。 |
| `ConfigID` | `gear_tactical_blade` | 配置 ID 或预置 ID。 |
| `DisplayName` | `战术长刀` | 中文名。 |
| `AssetType` | `icon` | `icon`、`portrait`、`background`、`frame`、`stand` 等。 |
| `VisualID` | `item_gear_tactical_blade_icon` | 程序侧稳定引用 ID。 |
| `OutputPath` | `UnityClient/Assets/Art/Approved/...png` | Approved 后目标路径。 |
| `Priority` | `P0` | 优先级。 |
| `Status` | `todo` | 新扫出的资产默认 `todo`。 |
| `SourceFactsCN` | `配置表物品：战术长刀...` | 只记录配置事实或需求事实，不写美术提示词。 |

`SourceType=preset` 可额外包含以下字段：

| 字段 | 示例 | 说明 |
|---|---|---|
| `PresetCategory` | `ui_inventory` | 预置需求分类，只用于美术管理和筛选。 |
| `Screen` | `inventory_loot` | 主要服务的界面或流程。通用项可填 `global`。 |
| `Usage` | `背包网格可用格子` | 具体用途，便于程序和美术对齐。 |
| `ProgramReference` | `VisualAssetRegistry` | 可选，记录程序侧反馈来源或引用点。 |

### Step 2 填充

| 字段 | 说明 |
|---|---|
| `PromptCN` | 中文审阅描述，供人类查看，不直接给绘图工具。 |
| `PromptEN` | 正式 AI 绘图提示词，必须使用英文视觉语言。 |
| `NegativePromptEN` | 英文负面提示词。 |
| `Spec` | 结构化输出规格对象，供后续预处理脚本读取。 |

Step 2 完成后，将 `Status` 改为 `prompted`。

### Step 3-5 填充

| 字段 | 步骤 | 说明 |
|---|---|---|
| `BatchID` | Step 3 | AI 生成批次 ID，只作记录，不作为 `_IncomingAI` 目录层级。 |
| `RawPath` | Step 3 | 原始生成图路径，指向 `_IncomingAI/<VisualID>/raw`。 |
| `SelectedPath` | Step 5 | 初筛通过的候选图路径，通常位于 `_IncomingAI/<VisualID>/selected`。 |
| `ApprovedPath` | Step 5 | 规格整理后的正式素材路径。 |
| `RegistryStatus` | Step 5 | `unregistered`、`registered`、`validated` 等。 |
| `Notes` | 任意 | 备注、返工原因、筛选结论。 |

---

## 5. 填充边界

| 步骤 | 必填 | 不应填写 |
|---|---|---|
| Step 1：扫描 | 来源、配置事实、资产类型、VisualID、目标路径、状态 | `PromptCN`、`PromptEN`、`NegativePromptEN`、`Spec` |
| Step 2：提示词 | `PromptCN`、`PromptEN`、`NegativePromptEN`、`Spec` | 玩法数值、Unity 对象引用、项目名、玩法黑话、引擎词 |
| Step 3：生成 | `BatchID`、`RawPath` | `ApprovedPath` |
| Step 4：预处理 | `Notes` 可记录处理结果 | 人工筛选结论 |
| Step 5：筛选接入 | `SelectedPath`、`ApprovedPath`、`RegistryStatus` | 改写配置事实 |

---

## 6. Spec 结构

`Spec` 必须是对象，不是自然语言字符串。它用于连接美术生产、AI 生成、预处理脚本和 Unity 显示验证。

从 2026-05-10 起，`Spec` 分为四组：

* `SourceSpec`：最终入库素材的文件、尺寸、透明度和背景要求，供 AI 生成与预处理读取。
* `DisplaySpec`：在 Unity 参考分辨率下的显示容器，不等于图片源尺寸。
* `CompositionSpec`：主体占比、留白、锚点、安全区和基线要求，供筛选和预处理参考。
* `ProcessSpec`：后处理、缩略检查和 contact sheet 参数。

示例：

```json
{
  "SourceSpec": {
    "Format": "png",
    "Width": 512,
    "Height": 512,
    "Background": "transparent",
    "AlphaRequired": true
  },
  "DisplaySpec": {
    "ReferenceResolution": "1920x1080",
    "DisplayWidth": 64,
    "DisplayHeight": 64,
    "Unit": "ui_px",
    "FitMode": "contain",
    "Pivot": "center"
  },
  "CompositionSpec": {
    "SafePaddingPercent": 10,
    "SubjectOccupancyMin": 0.74,
    "SubjectOccupancyMax": 0.84,
    "Anchor": "center"
  },
  "ProcessSpec": {
    "PostProcess": ["resize", "trim_transparent_edges", "fit_safe_padding"],
    "PreviewSize": 64
  }
}
```

字段说明：

### 6.1 `SourceSpec`

| 字段 | 说明 |
|---|---|
| `Format` | 目标格式，例如 `png`。 |
| `Width` | 最终入库宽度。 |
| `Height` | 最终入库高度。 |
| `Background` | `transparent`、`opaque_environment`、`transparent_or_simple_dark` 等。 |
| `AlphaRequired` | 是否必须保留透明通道。 |

### 6.2 `DisplaySpec`

| 字段 | 说明 |
|---|---|
| `ReferenceResolution` | 显示规格基准分辨率，默认 `1920x1080`。 |
| `DisplayWidth` | 参考分辨率下的显示容器宽度。 |
| `DisplayHeight` | 参考分辨率下的显示容器高度。 |
| `Unit` | `ui_px` 表示 UGUI 参考像素，`world_unit` 表示世界单位。 |
| `FitMode` | `contain` 保持完整显示，`cover` 填满容器允许裁切，`stretch` 只允许特殊 UI 底图使用。 |
| `Pivot` | 对齐点：`center`、`bottom_center`、`top_left` 等。 |

### 6.3 `CompositionSpec`

| 字段 | 说明 |
|---|---|
| `SafePaddingPercent` | 主体安全边距百分比。 |
| `SubjectOccupancyMin` | 主体占画面比例下限。图标按主体包围盒占比，立绘按高度占比。 |
| `SubjectOccupancyMax` | 主体占画面比例上限。 |
| `Anchor` | 主体构图锚点，例如 `center`、`bottom_center`。 |
| `BaselinePercent` | 立绘、角色、怪物站地基线，按图片高度百分比记录；无基线需求时省略。 |
| `SafeArea` | 背景或 UI 底图的核心安全区，例如 `center_16_9`、`center_4_3`。 |
| `Composition` | 可选的简短构图备注，给人工检查参考。 |

### 6.4 `ProcessSpec`

| 字段 | 说明 |
|---|---|
| `PostProcess` | 后处理步骤列表。 |
| `PreviewSize` | 缩略图检查尺寸。 |
| `NineSlice` | 可选。UI 面板、按钮、列表行等需要九宫格拉伸时填写。 |

旧版单层 `Spec.Width/Height/Format` 仍允许脚本兼容读取，但新生成和新维护的 entry 必须使用四段结构。
