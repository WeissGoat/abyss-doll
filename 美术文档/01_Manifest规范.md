---
id: art_manifest_spec
title: Manifest 规范
type: art
role: 美术
domain: art_manifest
status: active
source_of_truth: true
related:
  - 美术文档/03_AI生成与筛选规范.md
  - 美术文档/05_AI图片网关接入方案.md
  - 美术文档/02_资源规格与接入规范.md
  - 美术文档/00_美术流水线总览.md
  - 美术文档/04_美术风格基准.md
  - tools/美术工具/README.md
last_verified: 2026-10-02
update_rule: 修改美术流水线、资源规格、UI 交付或运行时验收要求时同步本文件。
---

# Manifest 规范

> **定位：** 规定 `art_manifest.json` 的字段结构、字段含义，以及美术流水线每一步应该填充哪些字段。
> **更新时间：** 2026-08-02

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
| `Monsters/*.json` 的 `PortraitID` | 怪物头像 | `mob_scavenger_bug -> monster_mob_scavenger_bug_portrait` |
| `Monsters/*.json` 的 `CombatVisualID` | 怪物战斗实体 | `mob_scavenger_bug -> monster_mob_scavenger_bug_combat` |
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

`same_visualid_replacement`、`new_asset`、`character_difference` 等属于生产 `Operation`，不是新的 `SourceType`。Operation 写入 ProductionRun / `production_decision.json`；在脚本正式支持前，不向 Manifest 临时增加无契约字段。
| `ui_dungeon_map` | 深渊地图路线、节点底板等 UI。 |
| `ui_settlement` | 胜利、战败、撤离结算 UI。 |
| `program_gap` | 程序侧反馈的临时缺图或临时色块资产。 |

---

## 3. 顶层结构

`ConfigRoot` 指向 Unity 运行时读取的配置副本。版本源仍是仓库根目录的 `配置表(JSON)`；运行美术扫描前先执行 `tools/config/Sync-Configs.ps1 -Clean`，把源配置同步到 `UnityClient/Assets/StreamingAssets/Configs`。

```json
{
  "Version": 3,
  "ConfigRoot": "UnityClient/Assets/StreamingAssets/Configs",
  "StatusFlow": ["todo", "prompted", "generated", "selected", "approved", "registered", "validated", "rejected", "deprecated"],
  "Entries": []
}
```

当前 `StatusFlow` 为旧脚本兼容字段。事实语义按三轴理解：

1. 生产轴：`todo -> prompted -> generated -> selected -> approved`；
2. 接入轴：Unity 导入和 `RegistryStatus`；
3. 验收轴：ArtRun / 玩家路径证据。

旧脚本可能继续把主 `Status` 推进到 `registered / validated`，但接入和验收结论不得只依赖该值；应同时核对 `RegistryStatus`、live Unity 和运行时证据。后续脚本字段迁移完成后再删除兼容值。

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
| `ProductionProfile` | `standard_asset` | 工作区与编排 Profile：`standard_asset` 或 `character_portrait_set`；不表示生成方法。缺省归一为 `standard_asset`。 |
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

### Step 2 需求与 Prompt authoring

| 字段 | 说明 |
|---|---|
| `StyleRef` | 引用标准化风格合同，不从 VisualID 或旧 Prompt 猜测。 |
| `VisualIntent` | 单项资产最终视觉需求，只描述需要的结果。 |
| `Spec` | 结构化输出、显示、构图与处理规格对象。 |
| `CompiledRequest` | 指向已编译 Requirement 及当前 active PromptRevision。 |

旧的独立 Prompt 字段和中间提示状态已从 active Manifest schema 删除。正式流程只由 Requirement 的 `PromptAuthoringStatus` 表示是否还需 Agent authoring，并在 `PromptRevision` 发布后才允许执行。

### Step 3-5 填充

| 字段 | 步骤 | 说明 |
|---|---|---|
| `BatchID` | Step 3 | AI 生成批次 ID，只作记录，不作为 `_IncomingAI` 目录层级。 |
| `RawPath` | Step 3 | 原始生成图路径，指向当前 Profile 工作区的 `raw/`。 |
| `SelectedPath` | Step 5 | 初筛通过的候选图路径，通常位于当前 Profile 工作区的 `selected/`。 |
| `ApprovedPath` | Step 5 | 规格整理后的正式素材路径。 |
| `RegistryStatus` | Step 5 | `unregistered` 或 `registered`。运行时美术验收写入 ArtRun，不写入 Manifest。 |
| `Notes` | 任意 | 备注、返工原因、筛选结论。 |

Asset Contract 字段归属：

- `VisualID`、`OutputPath`、`SourceSpec`、`DisplaySpec`、`CompositionSpec`、`ProcessSpec`、`QualityTier` 来自 Manifest；
- 角色身份、场景 / 风格锚点、must-preserve、allowed-changes、forbidden 等来自专项事实文档；
- ProductionRunID、候选评分、交互决定、文件 hash 和恢复状态写入运行证据与当前 Profile 工作区的 `production_decision.json`，在脚本正式支持前不塞入 Manifest。

工作区映射固定为：`standard_asset -> _IncomingAI/standard_assets/<VisualID>/`，`character_portrait_set -> _IncomingAI/character_portraits/<VisualID>/`。角色 Profile 可额外使用 `AssetSetID`、`AssetID`、`SetRole` 和 `SourceAssets` 描述套组与来源关系，但这些字段不要求任何特定生图方式，也不增加 `AssetSetID` 物理目录层级。

正式 `SelectedPath` 必须绑定本次采用候选。选择证据至少能追溯 CandidateBatchID / ProductionRunID、文件 hash 和选择结论；不得仅依赖 `selected/` 中文件名字典序推断当前候选。

角色套组的 `portrait-set-run.json`、处理决策、visual review、数字轮次和 Resume checkpoint 都属于 ProductionRun 证据，不是 Manifest 生命周期字段。Manifest 只保存稳定需求、`SelectedPath`、Approved 目标和 `RegistryStatus`；Resume 必须重新核对这些证据的 SHA，不能用 checkpoint 直接覆盖当前事实。

### Visual V2 质量替换字段

已进入 `approved` / `registered` 的素材，如果只是要替换更高质量图片，不应把 `Status` 改回 `generated`。运行时 `runtime_validated` 由 `p3-art-validation` 的检查证据产生，不是 Manifest 状态。这类流程使用候选字段记录新批次：

| 字段 | 步骤 | 说明 |
|---|---|---|
| `CandidateBatchID` | Step 3 | Visual V2 候选生成批次，保留原 `Status`。 |
| `CandidateRawFiles` | Step 3 | 本批候选 raw 文件列表，供预处理脚本只处理新候选。 |
| `QualityTier` | Step 5 | 当前 Approved 质量层级，常用值：`local_v0`、`placeholder`、`formal_ai_v2`、`final`、`production`。 |
| `ReplacementBatchID` | Step 5 | 最近一次同名替换所用批次。 |
| `QualityUpdatedAt` | Step 5 | 最近一次质量替换时间。 |

`QualityTier=formal_ai_v2/final/production` 表示当前素材已达到对应正式质量；质量清单不再因为旧备注里残留 `local_v0` 字样而继续报 `visual_v2_replace`。技术风险和尺寸规格问题仍会继续报出。

---

## 5. 填充边界

| 步骤 | 必填 | 不应填写 |
|---|---|---|
| Step 1：扫描 | 来源、配置事实、资产类型、VisualID、目标路径、状态 | 最终 Prompt、未经事实支持的风格或生成方式 |
| Step 2：需求与 authoring | `StyleRef`、`VisualIntent`、`Spec`、`CompiledRequest`；需要出图时发布 PromptRevision | 玩法数值、Unity 对象引用、项目名、玩法黑话、引擎词；把旧 Prompt 字段当正式执行源 |
| Step 3：生成 | `BatchID`、`RawPath`；Visual V2 用 `CandidateBatchID`、`CandidateRawFiles` | `ApprovedPath` |
| Step 4：预处理 | `Notes` 可记录处理结果 | 人工筛选结论 |
| Step 5：筛选接入 | `SelectedPath`、`ApprovedPath`、`RegistryStatus`；Visual V2 用 `QualityTier`、`ReplacementBatchID`、`QualityUpdatedAt` | 改写配置事实、改写 `VisualID`、改写 DisplaySpec |

---

## 6. Spec 结构

`Spec` 必须是对象，不是自然语言字符串。它用于连接美术生产、AI 生成、预处理脚本和 Unity 显示验证。

`Spec.ProcessSpec.BackgroundPolicy` 为必填字段，合法值仅限 `preserve`、`already_transparent`、`auto_simple`、`agent_required`。`AlphaRequired` 只描述最终 alpha 契约，不隐式选择去底方式。

---

## 7. 风格合同与编译请求

### 7.1 顶层字段

Manifest 顶层可以包含以下生成快照：

| 字段 | 说明 |
|---|---|
| `ArtStyleCatalog` | 从美术风格基准、Token 和专项事实规范化的风格快照，包含 `CatalogFingerprint`。 |
| `AssetSets` | 角色立绘等相关成员的身份来源、IdentityContract、PresentationGroup、一致性规则、成员关系和最新有效套组评审指针。 |
| `Entries` | 单项资产需求、StyleRef、VisualIntent、Spec、状态和编译请求引用。 |

### 7.2 Entry 新字段

| 字段 | 说明 |
|---|---|
| `StyleRef` | `Profile`、可选 `Family`、可选 `Role` 和允许的 `ContextAccent`。不得从 VisualID 推断。 |
| `VisualIntent` | 资产最终视觉需求，包括 Subject、Appearance、Mood、Composition、RequiredElements 和 ForbiddenElements。 |
| `RequirementSources` | 同一正式视觉需求的一个或多个配置、推导或预置来源。多个来源只允许存在于单一 VisualID Entry 内。 |
| `VisualReusePolicy` | 多来源共用视觉时的显式决策；`Mode=shared_visual` 必须有 Reason 和事实来源。不得根据 Prompt 相同自动推断。 |
| `CompiledRequest` | `RequestID`、`RequirementFingerprint`、`RequirementStatus`、`PromptAuthoringStatus` 和 `ActivePromptRevisionID`，指向持久化需求与当前 PromptRevision。 |

### 7.3 编译请求

完整编译结果保存于：

```text
美术文档/_generated/art_generation_requests.json
```

Request Catalog v2 把需求与 Prompt 分开：

- Requirement 层：`PromptAuthoringContext`、`TechnicalRequest`、`PreservationContract` 和 `RequirementFingerprint`，由编译器确定性生成。
- Prompt 层：Agent-authored、不可变的 `PromptRevisions`；active Revision 由 `ActivePromptRevisionID` 指向。
- `natural_language_v2`：OpenAI/GPT image edit 和 Gemini image/chat image 使用的自然语言正负 Prompt。
- `danbooru_tags_v2`：NovelAI 使用的结构化正负 tag 与 weight。

每个 ready Variant 必须包含全部硬约束的 `ConstraintMapping`；`unsupported` Variant 必须写明原因。active Manifest 与 Catalog 都不保存旧 Prompt 字段或 v1 Variant。新批量脚本必须消费 active PromptRevision，不得根据 VisualID 或同一份 VisualIntent 重新猜测最终 Prompt。

正式 generation evidence 使用 `EvidenceMode=formal_v2`，保存精确 `PromptRevisionSnapshot` 和 `ProviderRequest`。历史快照保持不可变，但不存在可由当前工具重新执行的旧 Prompt 恢复路径。

### 7.4 编译门禁

当 Catalog、StyleRef、VisualIntent、Spec、AssetSet 身份合同或编译器版本变化时，`RequirementFingerprint` 变化，旧 PromptRevision 因绑定旧 fingerprint 自动 stale。`Status`、候选批次 / raw 列表、替换批次、质量时间戳、路径和 Registry 等运行态证据不参与 Manifest fingerprint；这些字段在 `selected -> approved -> registered -> runtime_validated` 或新处理轮次中变化时，不要求重新编译。缺少 Requirement、fingerprint / active pointer 不匹配、`prompt_authoring_required`、Revision 无效、目标 Variant 未 ready 或硬约束映射不完整时，生成必须在 provider 调用前失败。

Catalog 编译前必须检查 `VisualID`、`RequestID` 和 `OutputPath` 唯一性。相同 VisualID 的多个来源只有在已经归一为单一 Entry、且 `VisualReusePolicy=shared_visual` 明确成立时才允许继续；否则输出 collision report 并保持已有正式 Catalog 不变。局部 `--visual-id` 重编译只能按 VisualID 合并回完整 Catalog，不能用局部集合覆盖未参与本次编译的 Request。

Catalog `Summary` 是 Requests 的派生值，不是独立事实。编译和 PromptRevision 发布都必须调用同一重算逻辑；strict validator 应从 Requests 独立重算，并在文件值不一致时报 `catalog_summary_stale`。Manifest、Catalog 和 migration report 只有在唯一性、pointer 和 Summary 校验全部通过后才能原子发布。

处理候选位于当前 Profile 工作区的 `processed/<正整数>/`。Manifest `SelectedPath` 可明确指向 `selected/` 或某个数字轮次中的候选；自动解析优先级为 `SelectedPath -> selected/ -> 最新数字轮次中唯一且通过的候选`。最新轮次失败、待决策、未验证或多候选时禁止回退旧轮次。

`technical_override.json`、`ReplacementBaseline`、`ReplacementPolicy` 和 `portrait-set-run.json` 不会改变 Manifest 的稳定 schema。override 必须由用户或上游调用方通过命令参数显式授权；替换候选必须基于执行时仍匹配的 `SelectedPath` / `SelectedSHA256`，且通过严格分数与保护维度门禁后才可写入 selected。

### 7.5 角色 AssetSet 合同

`character_portrait_set` 的稳定角色事实维护在结构化需求源的 `AssetSets` 中，Manifest 只保存生成快照；不得在编译器里硬编码具体角色的 IdentityLocks。每个角色合同保持轻量：

```json
{
  "IdentityContract": {
    "Version": 1,
    "Required": ["角色必须保持的事实"],
    "Forbidden": ["角色禁止出现的事实"],
    "Conditional": ["仅在目标状态明确要求时允许的变化"]
  }
}
```

成员使用 `PresentationGroup` 表达运行时快速切换族，例如 `dialogue_standing`、`maintenance_seated`、`combat_cutin`、`narrative_cutin`。该字段只控制套组一致性检查范围，不限制生成方法或姿势设计。

套组评审通过后，Manifest `AssetSets.<AssetSetID>.LatestConsistencyReview` 只保存 `State`、`SetSnapshotFingerprint`、`ProductionRunID` 和 `EvidencePath`。它是当前证据指针，不是第二套进度表；任一 selected 成员 SHA 改变后指纹不匹配，旧报告自动失效。

> 2026-08-02 状态：本节为已确认目标 schema，当前工具和生成物尚未完成迁移，不能据此声明门禁已生效。

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
