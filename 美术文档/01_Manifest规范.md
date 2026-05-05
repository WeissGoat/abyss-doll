# Manifest 规范

> **定位：** 规定 `art_manifest.json` 的字段结构、字段含义，以及美术流水线每一步应该填充哪些字段。
> **更新时间：** 2026-05-05

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

对应 `SourceType=preset`。

后续预置需求增多时，迁移到独立种子文件：

```text
美术文档/art_requirements_seed.json
```

---

## 3. 顶层结构

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

`Spec` 必须是对象，不是自然语言字符串。

示例：

```json
{
  "Format": "png",
  "Width": 512,
  "Height": 512,
  "Background": "transparent",
  "AlphaRequired": true,
  "SafePaddingPercent": 10,
  "Composition": "centered single object",
  "PostProcess": ["resize", "trim_transparent_edges", "fit_safe_padding"],
  "PreviewSize": 64
}
```

字段说明：

| 字段 | 说明 |
|---|---|
| `Format` | 目标格式，例如 `png`。 |
| `Width` | 最终入库宽度。 |
| `Height` | 最终入库高度。 |
| `Background` | `transparent`、`opaque_environment`、`transparent_or_simple_dark` 等。 |
| `AlphaRequired` | 是否必须保留透明通道。 |
| `SafePaddingPercent` | 主体安全边距百分比。 |
| `Composition` | 构图要求，给预处理和人工检查参考。 |
| `PostProcess` | 后处理步骤列表。 |
| `PreviewSize` | 缩略图检查尺寸。 |
