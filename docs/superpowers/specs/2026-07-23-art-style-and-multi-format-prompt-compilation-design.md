---
id: spec_art_style_and_multi_format_prompt_compilation
title: 美术风格目录与多格式生成请求编译设计
type: design
role: 美术
domain: art_asset_production
status: active
source_of_truth: false
related: []
last_verified: 2026-07-23
update_rule: 修改美术风格继承、VisualIntent、生成请求编译、Prompt 格式、生产 Profile 路由、迁移或编译门禁时更新本文档。
---

# 美术风格目录与多格式生成请求编译设计

## 1. 背景

当前 `art_manifest.json` 顶层 `ArtStyle` 只有一段宽泛的中英文风格描述，Entry 直接保存人工或历史脚本形成的 `PromptCN`、`PromptEN` 和 `NegativePromptEN`。这套结构存在以下问题：

- Entry 没有 `StyleRef`，无法表达 UI、角色、场景、组件 Family、Role 和有限场景强调色的继承关系。
- 资产名称可能被脚本当作隐式风格事实；`ui_button_primary` 曾因 `primary` 字样被硬编码为青蓝色，偏离项目 UI 方向。
- Prompt 同时承担资产需求、风格、后端格式和执行输入，无法判断哪部分是事实、哪部分是编译结果。
- 当前生成计划只记录 `PromptReady`，批量脚本仍需回到 Manifest 查 Prompt，无法校验 Prompt 是否与最新需求和风格一致。
- 自然语言后端与 Danbooru tag 后端需要不同表达方式，单一 `PromptEN` 不能可靠复用。
- 角色立绘套组具有身份母版、成员依赖、差分和套组一致性，不应进入普通 VisualID 批量流。

本设计将美术需求、风格解析、Prompt 编译、批次选择和实际 provider 请求拆成独立层，并把编译结果持久化，使 Agent 只在需求或风格变化时编译一次，后续脚本可以直接批量重跑。

## 2. 目标与非目标

### 2.1 目标

- 建立单一 Manifest 下的分层 `ArtStyleCatalog` 和显式 `StyleRef`。
- 将 `VisualIntent` 定义为单项资产最终视觉需求，Prompt 降为可再生编译产物。
- 增加角色 `AssetSets` 合同，统一风格与状态，但保持角色立绘独立编排。
- 从同一 `CanonicalVisualBrief` 同时编译自然语言和 Danbooru tags 等多种 Prompt Variant。
- 将完整 provider-neutral `GenerationRequest` 保存到生成请求目录，允许脚本无需 Agent 重新编译即可批量生成。
- 通过 fingerprint、语义覆盖和路由门禁阻止过期、缺失或格式不兼容的请求被执行。
- 保持现有 `raw -> processed/<n> -> selected -> approved -> registered -> runtime_validated` 资产声明边界。
- 分阶段迁移现有 313 个 Entry，不触发出图，不修改 Approved、GUID、Registry 或公开状态。

### 2.2 非目标

- 不在稳定 Manifest 中固定 OpenAI、Gemini、NovelAI 或未来 provider。
- 不把文生图、图生图、inpaint、mask 或特定模型写入 `ProductionProfile`、VisualID 或目录合同。
- 不在本设计中定义具体色值；颜色数值由 active `design_tokens.json` 维护，本设计只锁定“墨灰底板、月白边线、绯红主行动，浅蓝限于激活/选中/能量反馈”的语义规则。
- 不修改 Approved、Unity 导入、Registry 或运行时验收的既有职责。
- 不用 Prompt 文本替代 NineSlice、Alpha、参考图、mask、尺寸或身份保持等结构化参数。

## 3. 总体架构

```text
美术事实来源
  ├─ 04_美术风格基准.md
  ├─ ui_design/design_tokens.json
  ├─ art_requirements_seed.json / 配置扫描
  ├─ UI、场景、角色、人设专项事实
  └─ 资产技术规格
        |
        v
Manifest 生成与风格快照
  ├─ ArtStyleCatalog
  ├─ AssetSets
  └─ Entries: StyleRef + VisualIntent + Spec
        |
        v
生成请求编译
  ├─ CanonicalVisualBrief
  ├─ natural_language_v1
  ├─ danbooru_tags_v1
  └─ TechnicalRequest / PreservationContract
        |
        v
美术文档/_generated/art_generation_requests.json
        |
        v
批次计划只引用 RequestID + RequestFingerprint
        |
        +--> standard_asset 批量执行器
        |
        +--> character_portrait_set 套组编排器
        |
        v
provider adapter -> 实际请求 -> raw / processed / selected
```

事实、编译产物和运行证据的职责固定为：

| 层 | 内容 | 是否事实来源 |
|---|---|---|
| 美术文档、Token、Seed、专项设计 | 人工维护的风格、身份和需求事实 | 是 |
| `art_manifest.json` | 规范化需求、风格快照、状态与请求引用 | 生成快照，不是新增事实来源 |
| `art_generation_requests.json` | 可重复执行的编译请求 | 编译产物 |
| 批次计划 | 本轮选择哪些 Request、变体数和权限 | 运行计划 |
| Run Evidence | 实际 provider、最终请求、输入、输出和失败 | 审计证据 |

## 4. 风格事实与 ArtStyleCatalog

### 4.1 事实优先级

风格事实继续维护在现有来源：

1. `美术文档/04_美术风格基准.md`：全项目画风、允许与禁止方向。
2. `美术文档/ui_design/design_tokens.json`：UI 颜色、尺寸、圆角和状态语义。
3. 角色、人设、场景和 UI 专项事实：目标领域的具体视觉合同。
4. Manifest `ArtStyleCatalog`：上述来源的标准化快照，只供脚本解析。

Catalog 必须记录来源路径、内容 hash、Catalog fingerprint 和编译版本。来源变化后，旧 Catalog 和由其产生的 Request 都进入 stale 状态。

### 4.2 Catalog 结构

```json
{
  "ArtStyleCatalog": {
    "Version": 1,
    "CatalogFingerprint": "sha256:...",
    "Sources": [
      {
        "Path": "美术文档/04_美术风格基准.md",
        "SHA256": "..."
      },
      {
        "Path": "美术文档/ui_design/design_tokens.json",
        "SHA256": "..."
      }
    ],
    "GlobalStyle": {},
    "Profiles": {},
    "Families": {},
    "ContextAccents": {}
  }
}
```

职责：

- `GlobalStyle`：所有资产共享的项目画风、情绪、禁止方向和语义片段。
- `Profiles`：UI、场景、角色立绘等领域级绘制规则。
- `Families`：按钮、面板、图标等需要稳定组件语法的资产族。
- `Roles`：Family 内的视觉角色，例如 `primary`、`secondary`、`danger`。
- `ContextAccents`：工坊、战斗、地图等允许的单一有限强调色；不能改写底板、边线或状态语义。

UI 基准为：

```text
墨灰底板 + 月白边线 + 绯红主行动
浅蓝只用于符文激活、选中和能量反馈
```

Entry、生成器和 provider adapter 禁止通过 VisualID、文件名或 DisplayName 推断颜色、Family 或 Role。

### 4.3 StyleRef 与继承顺序

```json
{
  "StyleRef": {
    "Profile": "ui_v1",
    "Family": "ui_button_core_v1",
    "Role": "primary",
    "ContextAccent": "none"
  }
}
```

标准资产继承顺序固定为：

```text
GlobalStyle
-> Profile
-> Family
-> Role
-> ContextAccent
-> VisualIntent
-> Spec 中需要转为视觉语义的部分
```

角色立绘继承顺序固定为：

```text
GlobalStyle
-> Character Profile
-> AssetSet IdentityLocks
-> 成员 SetRole / SourceAssets
-> 成员 VisualIntent
-> Spec
```

Family 和 Role 只描述视觉组件语法，不承载角色身份或表情差分。

## 5. Manifest Schema

### 5.1 顶层

```json
{
  "Version": 3,
  "StatusFlow": [],
  "ArtStyleCatalog": {},
  "AssetSets": {},
  "Entries": []
}
```

旧 `ArtStyle` 在兼容期继续输出，但内容由 `ArtStyleCatalog.GlobalStyle` 派生；任何新工具不得再把旧 `ArtStyle` 作为完整风格合同。

### 5.2 Entry

```json
{
  "VisualID": "ui_button_primary",
  "ProductionProfile": "standard_asset",
  "StyleRef": {
    "Profile": "ui_v1",
    "Family": "ui_button_core_v1",
    "Role": "primary",
    "ContextAccent": "none"
  },
  "SourceFactsCN": "正式 UI 的主要行动按钮皮肤。",
  "VisualIntent": {
    "SubjectCN": "游戏主要行动按钮",
    "AppearanceCN": "墨灰底板、月白边线、绯红主行动面",
    "MoodCN": "克制、清晰、低噪声的幻想界面",
    "CompositionCN": "横向一体式按钮，中心干净，可以安全拉伸",
    "RequiredElements": [
      "完整连续边框",
      "明确的主要行动层级"
    ],
    "ForbiddenElements": [
      "文字",
      "图标",
      "蓝白主色",
      "碎片化装饰"
    ]
  },
  "Spec": {},
  "CompiledRequest": {
    "RequestID": "ui_button_primary@4f82c1a9b311",
    "RequestFingerprint": "sha256:...",
    "CompileStatus": "ready"
  }
}
```

字段职责：

- `SourceFactsCN`：为什么需要该资产、事实来自哪里。
- `VisualIntent`：资产最终应该画成什么样。
- `StyleRef`：使用哪套公共风格和组件语法。
- `Spec`：尺寸、透明、构图、加工和运行时使用合同。
- `CompiledRequest`：指向可执行编译产物，不复制完整 Request。

### 5.3 AssetSets

```json
{
  "AssetSets": {
    "zero_dialogue_portrait_v1": {
      "ProductionProfile": "character_portrait_set",
      "StyleRef": {
        "Profile": "character_portrait_v1"
      },
      "IdentitySources": [
        "美术文档/人设/03_零号初版人设方案.md",
        "美术文档/人设/05_零号立绘素材设计与交付清单.md"
      ],
      "IdentityLocks": [
        "银白长发",
        "白布完整遮眼",
        "灰披肩",
        "浅色内衬裙",
        "裸腿裸足",
        "常态无红光"
      ],
      "ConsistencyRules": [
        "保持身体比例和画布基线",
        "成员差分不得无需求地改变身份或服装"
      ]
    }
  }
}
```

角色成员继续使用现有 `AssetID`、`VisualID`、`AssetSetID`、`SetRole` 和 `SourceAssets`。不新增 `AnchorID`；非运行时母版仍使用带 anchor role 的 `AssetID`。

## 6. 编译模型

### 6.1 两阶段编译

编译分成两步：

```text
StyleRef + VisualIntent + Spec + AssetSet
-> CanonicalVisualBrief
-> Prompt Variants + TechnicalRequest
```

`CanonicalVisualBrief` 是 provider-neutral 的完整视觉简报。它必须先解决继承、冲突、身份保持、禁止项和结构化技术语义，再交给不同 serializer。

### 6.2 CanonicalVisualBrief

```json
{
  "CanonicalVisualBrief": {
    "Style": [],
    "Subject": [],
    "Appearance": [],
    "Mood": [],
    "Composition": [],
    "Required": [],
    "Forbidden": [],
    "Preserve": [],
    "RequiredChanges": []
  }
}
```

编译器为每个输入语义生成稳定语义 ID，来源采用 JSON Pointer 或 Catalog 节点 ID，例如：

```text
/Entries/ui_button_primary/VisualIntent/RequiredElements/0
family:ui_button_core_v1/role:primary/accent
asset_set:zero_dialogue_portrait_v1/identity_lock/0
```

这些 ID 用于多格式语义覆盖检查，不要求作者手工维护。

### 6.3 Prompt Variant

默认支持：

| Format | 用途 | 默认适配能力 |
|---|---|---|
| `natural_language_v1` | 完整自然语言生成、参考图编辑和图生图 | OpenAI Images、Gemini image/chat image |
| `danbooru_tags_v1` | Danbooru tag 型生成和图生图 | NovelAI |

普通图片和角色立绘默认尝试编译两种格式。某格式无法完整表达必需语义时，必须明确标记 `unsupported`，不能生成似是而非的 Prompt。

自然语言 Variant：

```json
{
  "Format": "natural_language_v1",
  "CompileStatus": "ready",
  "Positive": "Single horizontal primary action button skin...",
  "Negative": "text, icon, dominant blue or cyan palette...",
  "VariantFingerprint": "sha256:...",
  "SemanticCoverage": {
    "Required": 12,
    "Mapped": 12,
    "Unmapped": [],
    "Coverage": 1.0
  }
}
```

Danbooru tags Variant：

```json
{
  "Format": "danbooru_tags_v1",
  "CompileStatus": "ready",
  "PositiveTags": [
    {"Tag": "1girl", "Weight": 1.0},
    {"Tag": "silver hair", "Weight": 1.2},
    {"Tag": "blindfold", "Weight": 1.3}
  ],
  "NegativeTags": [
    {"Tag": "visible eyes", "Weight": 1.2},
    {"Tag": "different clothes", "Weight": 1.1}
  ],
  "VariantFingerprint": "sha256:...",
  "SemanticCoverage": {
    "Required": 12,
    "Mapped": 12,
    "Unmapped": [],
    "Coverage": 1.0
  }
}
```

Tag 权重保持 provider-neutral 的结构化形式。NovelAI adapter 负责转换为该后端当前支持的权重语法，稳定 Request 不写死 provider 字符串格式。

### 6.4 TechnicalRequest 与 PreservationContract

以下内容不应依赖 Prompt 文本表达：

- Width、Height、Alpha、输出格式。
- ReferenceAssets、mask 和编辑区域。
- 身份、服装、比例、画布基线等必须保持项。
- RequiredChanges 和允许改变范围。
- NineSlice、透明 UI Skin 等能力要求。
- 运行时选择的 variants、seed、provider 和模型。

```json
{
  "TechnicalRequest": {
    "Width": 1024,
    "Height": 1536,
    "AlphaRequired": true,
    "CapabilityRequirements": [
      "transparent_character_portrait"
    ],
    "ReferenceAssets": [
      {
        "AssetID": "zero_dialogue_neutral",
        "Role": "identity_and_pose_reference"
      }
    ]
  },
  "PreservationContract": {
    "Preserve": [
      "identity",
      "costume",
      "body_proportions",
      "canvas_baseline"
    ],
    "RequiredChanges": [
      "restrained_confused_expression",
      "slight_uncertain_head_tilt"
    ]
  }
}
```

## 7. 生成请求目录

完整编译结果保存到：

```text
美术文档/_generated/art_generation_requests.json
```

结构：

```json
{
  "Version": 1,
  "GeneratedAt": "2026-07-23T00:00:00+08:00",
  "CompilerVersion": 1,
  "ManifestFingerprint": "sha256:...",
  "ArtStyleCatalogFingerprint": "sha256:...",
  "Requests": [
    {
      "RequestID": "ui_button_primary@4f82c1a9b311",
      "VisualID": "ui_button_primary",
      "ProductionProfile": "standard_asset",
      "InputFingerprint": "sha256:...",
      "RequestFingerprint": "sha256:...",
      "CompileStatus": "ready",
      "ResolvedLayers": [],
      "CanonicalVisualBrief": {},
      "PromptVariants": {},
      "TechnicalRequest": {},
      "PreservationContract": {}
    }
  ]
}
```

规则：

- `RequestID` 使用 `<VisualID>@<RequestFingerprint 前 12 位>`，同一输入得到同一 ID。
- Catalog 保存最新可执行版本；每次 Run Evidence 复制实际使用的完整 Request，承担历史追溯。
- `PromptCN`、`PromptEN`、`NegativePromptEN` 在兼容期继续写入 Manifest，其中英文正负 Prompt 来自 `natural_language_v1`。它们不是新脚本的执行来源。
- 批量脚本必须读取 Request Catalog，而不是自行重新理解 `VisualIntent` 或拼 Prompt。

## 8. Fingerprint 与编译状态

所有 fingerprint 使用 UTF-8、稳定 key 排序、无无意义空白的 canonical JSON 计算 SHA-256。

`InputFingerprint` 至少覆盖：

```text
CompilerVersion
ArtStyleCatalogFingerprint
StyleRef
VisualIntent
Spec 中与生成有关的字段
AssetSet IdentityLocks / ConsistencyRules
SetRole / SourceAssets
```

`RequestFingerprint` 覆盖完整编译结果，但不包含运行时 provider、seed、variants 和输出路径。

请求状态：

- `ready`：所有强制门禁通过，至少存在一个可执行 Prompt Variant 或确定性专项能力。
- `style_resolution_required`：Profile、Family、Role 或 ContextAccent 无法可靠确定。
- `unsupported`：当前 serializer 或能力无法表达必需语义。
- `invalid`：Schema、引用、冲突或 fingerprint 校验失败。
- `stale`：上游输入已变化但请求尚未重新编译。

Prompt Variant 只有 `ready`、`unsupported`、`invalid` 三种状态。语义覆盖率必须为 1.0 才能标记 `ready`；被结构化 `TechnicalRequest` 或 `PreservationContract` 承接的语义计为已映射。

## 9. 批次计划与执行

批次计划不复制 Prompt，只引用 Request：

```json
{
  "BatchID": "ui_refresh_20260723_01",
  "RunConfig": {
    "Action": "visual_v2_replace",
    "Variants": 4
  },
  "Items": [
    {
      "VisualID": "ui_button_primary",
      "RequestID": "ui_button_primary@4f82c1a9b311",
      "RequestFingerprint": "sha256:..."
    }
  ]
}
```

脚本执行顺序：

```text
读取计划
-> 读取 Manifest 与 Request Catalog
-> 校验 VisualID / RequestID / fingerprint / CompileStatus
-> 根据后端要求选择 PromptFormat
-> 校验 Variant CompileStatus 与语义覆盖
-> adapter 形成实际 provider 请求
-> 执行生成
-> 将完整 provider 请求和 Request 快照写入 Run Evidence
```

后端格式映射由 `generate-image` provider adapter 维护：

```text
OpenAI Images / GPT image edit -> natural_language_v1
Gemini image/chat image        -> natural_language_v1
NovelAI                       -> danbooru_tags_v1
```

切换到同一 PromptFormat 的其他后端、改变 variants 或 seed 不需要重新编译。目标后端需要的 Variant 不存在或不为 `ready` 时，脚本阻断，不调用 Agent 临时编译。

## 10. ProductionProfile 路由

### 10.1 standard_asset

适用背景、图标、普通装饰、纹理和 UI Skin 等可按 Entry 独立执行的资产。

```text
批次 Items
-> Request Catalog
-> 按 AssetClass / CapabilityRequirements / PromptFormat 分组
-> provider 或专项能力
-> raw
-> processed/<n>
-> guarded selection
```

`ui_skin` 且 `NineSlice.Enabled=true` 时继续要求显式专项能力。Prompt Variant 可以作为语义输入或 fallback，但不能替代确定性边框和拉伸区门禁。

工作区：

```text
UnityClient/Assets/Art/_IncomingAI/standard_assets/<VisualID>/
```

### 10.2 character_portrait_set

角色立绘共享 Catalog、Request Catalog 和正式状态，但不进入 `Run-ArtProductionBatch`。

```text
AssetSet 审计
-> 身份母版与成员依赖排序
-> 逐成员读取 Compiled Request
-> Agent/编排器根据当前能力选择方法和后端
-> 单成员技术检查
-> 套组身份与差分一致性检查
-> processed/<n> / selected
```

Prompt 可以来自自然语言或 Danbooru tags，生成可以是文生图、参考图生成、局部编辑或未来能力。稳定合同不固定生成方法。

工作区：

```text
UnityClient/Assets/Art/_IncomingAI/character_portraits/<VisualID>/
```

标准批量执行器收到 `character_portrait_set` 必须返回：

```text
route_mismatch:character_portrait_set_requires_portrait_set_orchestration
```

## 11. 编译与执行门禁

生成前依次执行：

```text
Catalog Gate
-> Style Resolution Gate
-> Asset Contract Gate
-> Request Freshness Gate
-> Prompt Variant Gate
-> Production Route Gate
```

错误码至少包括：

```text
style_catalog_missing
style_catalog_stale
style_profile_missing
style_family_missing
style_role_missing
style_role_family_mismatch
context_accent_not_allowed
style_resolution_required
visual_intent_incomplete
asset_set_missing
identity_contract_missing
compiled_request_missing
compiled_request_stale
compiled_request_fingerprint_mismatch
prompt_variant_missing:<format>
prompt_variant_not_ready:<format>
semantic_coverage_incomplete:<format>
route_mismatch:<profile>
```

失败时不得回退到旧 Prompt、旧 Request 或旧 processing round，也不得偷偷调用 Agent 修复。工具输出应明确指出需要重新编译、补事实、换能力或切换路由。

## 12. Run Evidence

每次执行记录：

```json
{
  "ProductionRunID": "...",
  "RequestID": "...",
  "RequestFingerprint": "...",
  "RequestSnapshot": {},
  "PromptFormat": "natural_language_v1",
  "Provider": "...",
  "ProviderAdapterVersion": "...",
  "ProviderRequest": {},
  "Inputs": [],
  "Outputs": [],
  "Result": "success"
}
```

Provider adapter 可以根据后端限制重排自然语言、序列化 tag 权重或添加后端参数，但最终请求必须完整写入证据。Catalog 的最新版本不承担历史 Run 复现；Run Evidence 中的 RequestSnapshot 才是该次执行事实。

## 13. 分阶段迁移

### 阶段 1：风格源收束

- 将 `04_美术风格基准.md` 和 `design_tokens.json` 收束到已确认的 UI 基准。
- 建立 Global、Profile、Family、Role 和 ContextAccent 的标准化来源映射。
- 不触发任何资产生成或状态变化。

### 阶段 2：Schema 与兼容输出

- Manifest 增加 `ArtStyleCatalog`、`AssetSets`、`StyleRef`、`VisualIntent` 和 `CompiledRequest`。
- 保留旧 `ArtStyle` 和 Prompt 字段作为兼容输出。
- 所有 313 个 Entry 获得默认 Profile；无法可靠判断的标记 `style_resolution_required`。

### 阶段 3：核心显式风格

- UI 核心 Family/Role 显式建模，禁止名称推断。
- 场景、图标等稳定资产族按实际需要补 Family，不为每个 Entry 强制制造 Family。
- 建立 `ui_button_primary` 绯红主行动回归样例。

### 阶段 4：角色套组

- 将现有 14 个零号立绘成员接入 `AssetSets`。
- 复用当前 `AssetSetID`、`AssetID`、`SetRole` 和 `SourceAssets`。
- 批量执行器硬阻断 `character_portrait_set`。

### 阶段 5：多格式编译

- 生成 `art_generation_requests.json`。
- 默认编译自然语言与 Danbooru tags；无法完整表达的 Variant 标记 `unsupported`。
- 生成迁移报告，列出 ready、required、unsupported 和 invalid 数量。

### 阶段 6：执行器切换

- 批次计划写入 RequestID 和 RequestFingerprint。
- 标准批量脚本改为消费 Request Catalog。
- provider adapter 根据能力选择 PromptFormat，并记录最终 ProviderRequest。
- 所有消费者迁移完成后，旧 Prompt 字段才可进入后续独立废弃评估。

迁移全过程必须验证：

```text
VisualID 集合不变
OutputPath 不变
Status / RegistryStatus 不变
Approved 文件 hash 不变
Unity .meta / GUID 不变
Registry 内容不因本迁移改变
```

## 14. 实现边界

建议实现组件：

```text
tools/美术工具/art_style_catalog.py
  风格源规范化、继承解析和 Catalog fingerprint

tools/美术工具/art_prompt_compiler.py
  CanonicalVisualBrief、Prompt Variant、Request fingerprint

tools/美术工具/Compile-ArtGenerationRequests.ps1
  公开编译入口和迁移报告

tools/美术工具/update_art_manifest.py
  Manifest Schema、Catalog/AssetSets/Entry 兼容输出

tools/美术工具/run_art_production_batch.py
  Request Catalog 校验、格式选择、Profile 硬路由

tools/美术工具/run_art_generation.py
  provider adapter 请求组装与实际请求证据

tools/美术工具/validate_art_generated_json.py
  Catalog、Request、fingerprint、Variant 和迁移不变量校验
```

具体文件拆分可在实施计划中根据现有模块职责微调，但不得把 Catalog、编译器和批次执行重新合并成一个难以独立测试的脚本。

## 15. 测试策略

### 15.1 单元测试

- Catalog canonical JSON 和 fingerprint 稳定。
- 继承顺序正确，ContextAccent 只能覆盖允许字段。
- Family/Role 关联非法时阻断。
- `ui_button_primary` 解析为绯红主行动，禁止蓝白主配色。
- VisualID 名称不会改变 StyleRef 解析结果。
- 自然语言 positive/negative 顺序稳定且去重。
- Danbooru tags 去重、权重保留、正负 tag 不冲突。
- 必需语义覆盖不足时 Variant 为 `unsupported`。
- Input 或 Catalog 改变后旧 Request 为 stale。
- seed、variants 或同格式 provider 变化不改变 RequestFingerprint。

### 15.2 集成测试

- 从测试 Manifest 编译出两个 Prompt Variant，并由脚本重复消费，不调用 Agent。
- OpenAI/Gemini route 选择 `natural_language_v1`。
- NovelAI route 选择 `danbooru_tags_v1` 并由 adapter 序列化权重。
- 缺少目标 Variant 时批次在生成前失败。
- `character_portrait_set` 不能进入标准批次。
- 角色套组 Request 同时包含 IdentityLocks、SourceAssets 和成员差分。
- UI NineSlice 项继续进入专项能力，不因 Prompt ready 绕过技术门禁。

### 15.3 迁移验证

- 313 个 Entry 数量和 VisualID 集合保持不变。
- 所有 Entry 有 Profile 或明确 `style_resolution_required`。
- 14 个角色立绘成员仍属于同一 AssetSet，工作区不变。
- 迁移前后 Approved hash、`.meta` GUID、Registry 和状态一致。
- 旧 Prompt 兼容字段与 `natural_language_v1` 一致。

## 16. 完成判定

本设计的实现只有在以下条件全部满足时才可声明完成：

1. 风格源、Catalog、Manifest Schema 和 Request Catalog 已落地并通过严格校验。
2. `ui_button_primary` 不再存在基于名称猜色路径，回归测试证明使用绯红主行动语义。
3. 标准批量脚本可以只凭 Batch Plan 和已编译 Request 重复生成，不调用 Agent 编译 Prompt。
4. 同一 Request 同时提供自然语言与 Danbooru tags，或明确记录某格式 `unsupported` 的原因。
5. OpenAI/Gemini 与 NovelAI 能选择正确 PromptFormat，并保存最终 ProviderRequest。
6. `character_portrait_set` 使用独立套组编排，标准批量脚本硬阻断误路由。
7. 迁移未修改任何 Approved、GUID、Registry 或公开状态。
8. 美术事实文档、工具 README、Skill 和状态页已回写，相关测试与文档校验通过。
