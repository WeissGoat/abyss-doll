---
id: spec_art_style_and_multi_format_prompt_compilation
title: 美术风格目录与多格式生成请求编译设计
type: design
role: 美术
domain: art_asset_production
status: active
source_of_truth: false
related: []
last_verified: 2026-07-26
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
- 已落地的 `natural_language_v1` 只是把 Style、VisualIntent、IdentityLocks 和 RequiredChanges 按固定顺序用逗号拼接；字段覆盖完整不等于提示词质量合格。Zero 冷淡差分复测中，这种扁平 Prompt 弱化了身份优先级、编辑边界和后端输出策略，效果明显劣于旧版由 Agent 根据参考图编写的分段自然语言指令。
- provider adapter 曾在执行时额外追加手工编辑说明，再拼接完整编译 Prompt，造成语义重复、优先级不清和 Run 间不可控差异。

本设计将美术需求、风格解析、Agent Prompt 创作、Prompt 发布、批次选择和实际 provider 请求拆成独立层。结构化字段只作为事实、参考和硬要求；具体提示词由 Agent 根据参考图、历史证据、资产类型和当前能力独立判断。Agent 的结果版本化保存，后续脚本可以直接批量重跑，不在运行时重新猜测或拼接 Prompt。

## 2. 目标与非目标

### 2.1 目标

- 建立单一 Manifest 下的分层 `ArtStyleCatalog` 和显式 `StyleRef`。
- 将 `VisualIntent` 定义为单项资产最终视觉需求，Prompt 降为可再生、可版本化的 Agent 创作产物。
- 增加角色 `AssetSets` 合同，统一风格与状态，但保持角色立绘独立编排。
- 将确定性编译结果收束为 `PromptAuthoringContext`、`TechnicalRequest` 和 `PreservationContract`，禁止编译器机械生成正式 Prompt。
- 由 Agent 针对同一上下文分别创作 `natural_language_v2` 与 `danbooru_tags_v2`；两种格式不得互相机械转换。
- 将 Agent 创作结果保存为不可变、可审计的 `PromptRevision`，允许脚本无需 Agent 重新创作即可批量生成。
- 通过 fingerprint、语义覆盖和路由门禁阻止过期、缺失或格式不兼容的请求被执行。
- 让通用批量素材和角色立绘共享 PromptRevision 体系，同时保持各自独立编排与验收规则。
- 保持现有 `raw -> processed/<n> -> selected -> approved -> registered -> runtime_validated` 资产声明边界。
- 分阶段迁移现有 313 个 Entry，不触发出图，不修改 Approved、GUID、Registry 或公开状态。

### 2.2 非目标

- 不在稳定 Manifest 中固定 OpenAI、Gemini、NovelAI 或未来 provider。
- 不把文生图、图生图、inpaint、mask 或特定模型写入 `ProductionProfile`、VisualID 或目录合同。
- 不在本设计中定义具体色值；颜色数值由 active `design_tokens.json` 维护，本设计只锁定“墨灰底板、月白边线、绯红主行动，浅蓝限于激活/选中/能量反馈”的语义规则。
- 不修改 Approved、Unity 导入、Registry 或运行时验收的既有职责。
- 不用 Prompt 文本替代 NineSlice、Alpha、参考图、mask、尺寸或身份保持等结构化参数。
- 不要求每个 Prompt 使用固定句式、固定段落、固定标签表或统一风格开头。
- 不允许 provider adapter 在已发布 Prompt 后追加会改变语义的手工指令。

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
确定性需求编译
  ├─ PromptAuthoringContext
  ├─ TechnicalRequest
  └─ PreservationContract
        |
        v
Agent Prompt Authoring
  ├─ 检查参考图与身份事实
  ├─ 选择生成 / 编辑 / 修复策略
  ├─ 独立创作 natural_language_v2
  └─ 独立创作 danbooru_tags_v2
        |
        v
PromptRevision 校验与发布
        |
        v
美术文档/_generated/art_generation_requests.json
        |
        v
批次计划只引用 RequestID + RequirementFingerprint + PromptRevisionID
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
| `art_generation_requests.json` | 确定性需求上下文与 Agent-authored PromptRevision | 编译与创作产物 |
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
    "RequirementFingerprint": "sha256:...",
    "RequirementStatus": "ready",
    "PromptAuthoringStatus": "prompt_ready",
    "ActivePromptRevisionID": "ui_button_primary@4f82c1a9b311/prompt-001"
  }
}
```

字段职责：

- `SourceFactsCN`：为什么需要该资产、事实来自哪里。
- `VisualIntent`：资产最终应该画成什么样。
- `StyleRef`：使用哪套公共风格和组件语法。
- `Spec`：尺寸、透明、构图、加工和运行时使用合同。
- `CompiledRequest`：指向当前需求版本和 active PromptRevision，不复制完整 Request。

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

## 6. 编译与 Prompt 创作模型

### 6.1 两阶段职责

系统分成确定性需求编译和 Agent Prompt 创作，不允许编译器直接代写正式 Prompt：

```text
StyleRef + VisualIntent + Spec + AssetSet + 专项事实
-> PromptAuthoringContext + TechnicalRequest + PreservationContract
-> Agent 独立创作 PromptRevision
-> Validator 发布可重复执行版本
```

确定性编译器负责继承、冲突检测、事实来源、身份保持、禁止项、参考图引用和技术合同。Agent 负责阅读上下文、检查实际参考图、选择当前可用能力，并判断怎样组织提示词才能产生可靠结果。

### 6.2 PromptAuthoringContext

```json
{
  "PromptAuthoringContext": {
    "HardConstraints": {
      "Identity": [],
      "RequiredChanges": [],
      "ForbiddenChanges": [],
      "Technical": []
    },
    "Guidance": {
      "Style": [],
      "Subject": [],
      "Appearance": [],
      "Mood": [],
      "Composition": []
    },
    "Evidence": {
      "IdentityDocuments": [],
      "ReferenceAssets": [],
      "PreviousSuccessfulPrompts": [],
      "PreviousFailures": []
    },
    "ResolvedLayers": [],
    "SetRole": ""
  }
}
```

编译器为每个硬约束生成稳定语义 ID，来源使用 JSON Pointer、Catalog 节点 ID 或 AssetSet 节点 ID，例如：

```text
/Entries/ui_button_primary/VisualIntent/RequiredElements/0
family:ui_button_core_v1/role:primary/accent
asset_set:zero_dialogue_portrait_v1/identity_lock/white_blindfold
```

这些 ID 用于 PromptRevision 的约束映射和 stale 判断，不要求 Agent 使用固定措辞。

### 6.3 Agent 自由与硬边界

Agent 可以：

- 自行决定提示词的语言、顺序、段落、重点和详略。
- 根据实际参考图补充可见身份特征、构图事实和失败风险。
- 判断使用文生图、单参考图编辑、多参考图编辑、inpaint、确定性处理或组合能力。
- 针对后端决定纯白底、透明底或后续独立去底策略。
- 参考旧版成功 Prompt、历史失败和 provider 行为，但不受固定模板约束。
- 对同一批次的多个 VisualID 一次性完成分析和创作，但每个 VisualID 必须发布独立 PromptRevision。

Agent 不可以：

- 改写或忽略 HardConstraints。
- 只因风格偏好改变身份、服装、构图合同、Alpha 合同或运行时用途。
- 把自然语言 Prompt 机械切词为 Danbooru tags，或反向拼装。
- 在没有检查参考图时声明身份保持 Prompt 已完成。
- 把旧 Prompt 原样复制为新版本而不记录适用性判断。

旧版 `doll_zero_cold` Gemini Prompt 作为回归样例：它证明“精确身份锚点 -> 只改变演出 -> 明确禁止项 -> 后端合适的纯白底输出”比扁平关键词列表可靠。它不是所有角色或资产的固定模板。

### 6.4 PromptRevision

一个 PromptRevision 是 Agent 一次完整判断的不可变结果：

```json
{
  "PromptRevisionID": "doll_zero_cold@521c47f73d6e/prompt-001",
  "RequirementFingerprint": "sha256:...",
  "AuthoringMode": "agent_authored",
  "Status": "ready",
  "CommonStrategy": {
    "Operation": "character_difference",
    "Capability": "single_reference_image_edit",
    "ReferenceAssets": []
  },
  "Variants": {
    "natural_language_v2": {},
    "danbooru_tags_v2": {}
  },
  "AuthorNotes": [],
  "CreatedAt": "...",
  "RevisionFingerprint": "sha256:..."
}
```

已发布 Revision 不得覆盖。修改措辞、标签、参考图、生成策略、背景策略或禁止项表达时创建新的 `prompt-002`；改变 seed、生成数量或重跑同一 Revision 不创建新版本。

### 6.5 双 Prompt Variant

每个需要 AI 图片能力的 PromptRevision 都固定拥有两个独立槽位：

| Format | 用途 | 默认适配能力 |
|---|---|---|
| `natural_language_v2` | 自然语言生成、参考图编辑和多参考图修复 | OpenAI Images、Gemini image/chat image |
| `danbooru_tags_v2` | Danbooru tag 型生成、img2img 和 inpaint | NovelAI |

两种 Variant 都由 Agent 针对各自表达体系独立创作。一个 Variant 无法可靠承接硬约束时标记 `unsupported` 并写明原因，不能用泛化内容伪装为 ready。

自然语言 Variant：

```json
{
  "Format": "natural_language_v2",
  "Status": "ready",
  "CompatibleCapabilities": ["single_reference_image_edit"],
  "Positive": "Use the supplied dialogue-neutral portrait as the exact identity...",
  "Negative": "visible eyes, transparent blindfold, back view...",
  "OutputContract": {
    "Background": "pure_white",
    "PostProcessBackgroundRemoval": true
  },
  "ConstraintMapping": {
    "identity:white_blindfold": ["wide opaque white cloth completely covering both eyes"]
  },
  "VariantFingerprint": "sha256:..."
}
```

Danbooru tags Variant：

```json
{
  "Format": "danbooru_tags_v2",
  "Status": "ready",
  "CompatibleCapabilities": ["image_to_image", "inpaint"],
  "PositiveTags": [
    {"Tag": "silver hair", "Weight": 1.2},
    {"Tag": "white blindfold", "Weight": 1.3}
  ],
  "NegativeTags": [
    {"Tag": "visible eyes", "Weight": 1.4},
    {"Tag": "back view", "Weight": 1.2}
  ],
  "ReferenceControls": {
    "PreserveReferenceIdentity": true,
    "PreserveReferenceCostume": true
  },
  "ConstraintMapping": {
    "identity:white_blindfold": ["white blindfold", "visible eyes"]
  },
  "VariantFingerprint": "sha256:..."
}
```

Tag 权重保持 provider-neutral 的结构化形式。NovelAI adapter 只负责转换成当前后端权重语法，不负责选择、补充或删除标签。现有 `TAG_MAP` 只可作为 Agent 候选词典或 Validator 辅助，不再生成正式 Prompt。

### 6.6 TechnicalRequest 与 PreservationContract

以下内容不应依赖 Prompt 文本表达：

- Width、Height、Alpha、输出格式和 BackgroundPolicy。
- ReferenceAssets、mask、参考图角色和编辑区域。
- 身份、服装、比例、画布基线等必须保持项。
- RequiredChanges、AllowedChanges 和 ForbiddenChanges。
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

PromptRevision 必须解释自己如何承接这些合同，但不能替代这些结构化字段。

## 7. 生成请求目录

完整结果继续保存到单一目录：

```text
美术文档/_generated/art_generation_requests.json
```

不新增第二套 Prompt 事实源。Catalog 同时保存确定性上下文和已发布 Agent Revision：

```json
{
  "Version": 2,
  "GeneratedAt": "2026-07-26T00:00:00+08:00",
  "CompilerVersion": 2,
  "ManifestFingerprint": "sha256:...",
  "ArtStyleCatalogFingerprint": "sha256:...",
  "Requests": [
    {
      "RequestID": "ui_icon_warning@4f82c1a9b311",
      "VisualID": "ui_icon_warning",
      "ProductionProfile": "standard_asset",
      "RequirementFingerprint": "sha256:...",
      "RequirementStatus": "ready",
      "PromptAuthoringStatus": "prompt_ready",
      "PromptAuthoringContext": {},
      "TechnicalRequest": {},
      "PreservationContract": {},
      "ActivePromptRevisionID": "ui_icon_warning@4f82c1a9b311/prompt-001",
      "PromptRevisions": []
    }
  ]
}
```

规则：

- `RequestID` 使用 `<VisualID>@<RequirementFingerprint 前 12 位>`；它标识需求版本，不因 Prompt 措辞变化而改变。
- 确定性重新编译时，如果 RequirementFingerprint 未变，保留已发布 PromptRevisions；如果发生变化，旧 Revision 保留追溯但标记 `stale`。
- Agent 通过受控发布入口追加 PromptRevision，不能直接手写覆盖 Catalog 中的 active Revision。
- 每次 Run Evidence 复制实际使用的 RequirementContext 和完整 PromptRevision，承担历史复现。
- `PromptCN`、`PromptEN`、`NegativePromptEN` 只作为旧兼容字段；`natural_language_v1` 和 `danbooru_tags_v1` 标记 `legacy_compiled`，不得进入新的正式生成批次。
- 批量脚本只能消费已发布 PromptRevision，不得重新理解 VisualIntent、调用 TAG_MAP 生成 Prompt 或运行时追加语义。

## 8. Fingerprint 与状态

所有 fingerprint 使用 UTF-8、稳定 key 排序、无无意义空白的 canonical JSON 计算 SHA-256。

`RequirementFingerprint` 至少覆盖：

```text
CompilerVersion
ArtStyleCatalogFingerprint
StyleRef
VisualIntent
Spec 中与生成有关的字段
AssetSet IdentityLocks / ConsistencyRules
SetRole / SourceAssets
PromptAuthoringContext 的证据引用
```

`RevisionFingerprint` 覆盖 CommonStrategy、两个 Prompt Variant、ConstraintMapping、输出策略和参考图选择，不包含 seed、生成数量和输出路径。

需求状态：

- `ready`：事实、风格、引用和技术合同完整。
- `style_resolution_required`：Profile、Family、Role 或 ContextAccent 无法可靠确定。
- `invalid`：Schema、引用或冲突校验失败。
- `stale`：上游事实已变化但尚未重新编译。

Prompt Authoring 状态：

- `prompt_authoring_required`：需求可用，但没有匹配当前 RequirementFingerprint 的 Agent Revision。
- `prompt_ready`：至少存在一个当前执行计划可用的 ready Variant。
- `prompt_stale`：active Revision 对应旧 RequirementFingerprint。
- `prompt_invalid`：约束映射、参考图、格式或 Revision fingerprint 不合法。
- `not_required`：确定性专项能力不需要 AI Prompt，例如纯 NineSlice 模板生成。

单个 Variant 状态为 `ready`、`unsupported` 或 `invalid`。`ready` 要求所有 HardConstraints 都被 Prompt、TechnicalRequest 或 PreservationContract 明确承接；Validator 检查映射存在性和引用有效性，但不以固定关键词判断文案质量。

## 9. 批次 Prompt 创作与执行

### 9.1 Authoring Batch

通用批量素材可以让 Agent 一次分析多个上下文，但发布结果仍按 VisualID 隔离：

```text
Replacement / Generate Plan
-> 收集 prompt_authoring_required Items
-> 按 AssetClass、风格和参考关系组织 Authoring Batch
-> Agent 检查上下文与参考证据
-> 为每个 VisualID 独立发布 PromptRevision
-> 批次执行器消费 active Revision
```

背景、图标、普通道具、特效和角色立绘不能共享一条通用 Prompt。Agent 可以复用共同风格理解，但必须按资产用途分别组织主体、构图、输出和禁项。

### 9.2 Batch Plan

批次计划不复制 Prompt，只引用确定版本：

```json
{
  "BatchID": "formalv2_standard_batch_002",
  "RunConfig": {
    "Action": "visual_v2_replace",
    "Variants": 4
  },
  "Items": [
    {
      "VisualID": "bg_combat_abyss",
      "RequestID": "bg_combat_abyss@4f82c1a9b311",
      "RequirementFingerprint": "sha256:...",
      "PromptRevisionID": "bg_combat_abyss@4f82c1a9b311/prompt-002",
      "PromptFormat": "natural_language_v2"
    }
  ]
}
```

### 9.3 执行顺序

```text
读取计划
-> 读取 Manifest 与 Request Catalog
-> 校验 RequestID / RequirementFingerprint / PromptRevisionID
-> 校验 PromptRevision 为当前需求版本
-> 校验目标 PromptFormat ready 且能力兼容
-> provider adapter 只做协议序列化
-> 执行生成
-> 保存 RequirementSnapshot、PromptRevisionSnapshot 和 ProviderRequest
```

后端格式映射由 `generate-image` provider adapter 维护：

```text
OpenAI Images / GPT image edit -> natural_language_v2
Gemini image/chat image        -> natural_language_v2
NovelAI generate / img2img     -> danbooru_tags_v2
NovelAI inpaint                -> danbooru_tags_v2 + mask / ReferenceControls
```

同一 Revision 下改变 seed、生成数量或重跑不需要 Agent。切换到另一个已声明兼容的同格式 provider 可以直接执行；如果需要改变 Prompt 内容、背景策略、参考图或编辑方式，必须发布新 Revision。Provider adapter 不得追加“Do not redesign”等语义前缀，也不得重复拼接 Catalog Prompt。

## 10. ProductionProfile 路由

### 10.1 standard_asset

适用背景、图标、普通装饰、纹理和 UI Skin 等可按 Entry 独立执行的资产。

```text
批次 Items
-> PromptAuthoringContext
-> Agent 为各 VisualID 发布独立 PromptRevision
-> 按 AssetClass / CapabilityRequirements / PromptFormat / provider 分组
-> provider 或专项能力
-> raw
-> processed/<n>
-> guarded selection
```

`ui_skin` 且 `NineSlice.Enabled=true` 时继续要求显式专项能力。纯确定性模板可以使用 `PromptAuthoringStatus=not_required`；PromptRevision 只能作为纹理或风格输入，不能替代确定性边框和拉伸区门禁。

工作区：

```text
UnityClient/Assets/Art/_IncomingAI/standard_assets/<VisualID>/
```

### 10.2 character_portrait_set

角色立绘共享 Catalog、Request Catalog 和正式状态，但不进入 `Run-ArtProductionBatch`。

```text
AssetSet 审计
-> 身份母版与成员依赖排序
-> 逐成员读取 PromptAuthoringContext 与参考图
-> Agent 根据当前能力分别发布 PromptRevision
-> 逐成员读取 active PromptRevision 并按依赖执行
-> 单成员技术检查
-> 套组身份与差分一致性检查
-> processed/<n> / selected
```

Prompt 可以来自 `natural_language_v2` 或 `danbooru_tags_v2`，生成可以是文生图、参考图生成、局部编辑或未来能力。稳定合同不固定生成方法；同一套组成员也不要求使用同一 provider 或同一方法，但必须满足身份与套组一致性。

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
-> Prompt Authoring Gate
-> Prompt Revision Freshness Gate
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
prompt_authoring_required:<VisualID>
prompt_revision_missing:<VisualID>
prompt_revision_stale:<VisualID>
prompt_revision_fingerprint_mismatch:<VisualID>
prompt_variant_missing:<format>
prompt_variant_not_ready:<format>
constraint_mapping_incomplete:<format>
reference_asset_missing:<asset_id>
provider_capability_mismatch:<format>
route_mismatch:<profile>
```

失败时不得回退到 `legacy_compiled` Prompt、旧 PromptRevision、旧 Request 或旧 processing round，也不得由 adapter 偷偷补语义。工具输出应明确指出需要重新编译需求、调用 Agent 创作/修订 Prompt、补参考证据、换能力或切换路由。

## 12. Run Evidence

每次执行记录：

```json
{
  "ProductionRunID": "...",
  "RequestID": "...",
  "RequirementFingerprint": "...",
  "RequirementSnapshot": {},
  "PromptRevisionID": ".../prompt-001",
  "PromptRevisionFingerprint": "...",
  "PromptRevisionSnapshot": {},
  "PromptFormat": "natural_language_v2",
  "Provider": "...",
  "ProviderAdapterVersion": "...",
  "ProviderRequest": {},
  "Inputs": [],
  "Outputs": [],
  "Result": "success"
}
```

Provider adapter 可以序列化 tag 权重、填充 API 参数、上传参考图或 mask，但不能重写自然语言、补充 tag 或追加语义指令。最终请求必须完整写入证据。Catalog 的最新 active Revision 不承担历史 Run 复现；Run Evidence 中的 RequirementSnapshot、PromptRevisionSnapshot 和 ProviderRequest 才是该次执行事实。

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

### 阶段 5：需求上下文迁移

- 将现有 `CanonicalVisualBrief` 迁移为 `PromptAuthoringContext`，拆分 HardConstraints、Guidance 和 Evidence。
- 现有 313 个机械 `natural_language_v1` / `danbooru_tags_v1` 保留为 `legacy_compiled` 审计证据，不触发出图，也不再满足正式执行门禁。
- 现有 selected、Approved、registered 和 runtime_validated 状态不因 Prompt 迁移改变。

### 阶段 6：Agent PromptRevision

- 增加 Prompt authoring package、受控发布、Revision fingerprint、active revision 和 stale 处理。
- 只为进入 active 生成/替换批次的资产创作 PromptRevision，不一次性重写全部 313 项。
- 首批回归同时覆盖 `standard_asset` 批量和 `character_portrait_set` 差分；Zero 旧版成功 Prompt 作为自然语言质量基线。

### 阶段 7：执行器切换

- 批次计划写入 RequestID、RequirementFingerprint、PromptRevisionID 和 PromptFormat。
- 标准批量脚本与角色套组脚本只消费 ready PromptRevision。
- provider adapter 根据能力选择 `natural_language_v2` 或 `danbooru_tags_v2`，只做协议序列化并记录最终 ProviderRequest。
- 所有消费者迁移完成后，旧 Prompt 字段和 v1 Variant 才可进入后续独立废弃评估。

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
  PromptAuthoringContext、TechnicalRequest、PreservationContract、Requirement fingerprint

tools/美术工具/Compile-ArtGenerationRequests.ps1
  公开需求编译入口、旧 Revision 保留和迁移报告

tools/美术工具/art_prompt_revision.py
  PromptRevision schema、双 Variant、constraint mapping、fingerprint 和 stale 判定

tools/美术工具/Export-ArtPromptAuthoringPackage.ps1
  输出单项或批量 Agent authoring package，不生成 Prompt

tools/美术工具/Publish-ArtPromptRevision.ps1
  校验并追加不可变 PromptRevision，受控切换 ActivePromptRevisionID

tools/美术工具/update_art_manifest.py
  Manifest Schema、Catalog/AssetSets/Entry 兼容输出

tools/美术工具/run_art_production_batch.py
  Requirement/Revision 校验、格式选择、Profile 硬路由

tools/美术工具/run_character_portrait_set.py
  AssetSet 依赖、参考图解析、成员 Revision 与套组一致性编排

tools/美术工具/run_art_generation.py
  provider adapter 纯协议序列化与实际请求证据，禁止补写语义

tools/美术工具/validate_art_generated_json.py
  Catalog、Requirement、PromptRevision、双 Variant 和迁移不变量校验
```

具体文件拆分可在实施计划中根据现有模块职责微调，但不得把 Catalog、需求编译器、Agent authoring、Revision 发布和批次执行重新合并成一个难以独立测试的脚本。

## 15. 测试策略

### 15.1 单元测试

- Catalog canonical JSON 和 fingerprint 稳定。
- 继承顺序正确，ContextAccent 只能覆盖允许字段。
- Family/Role 关联非法时阻断。
- `ui_button_primary` 解析为绯红主行动，禁止蓝白主配色。
- VisualID 名称不会改变 StyleRef 解析结果。
- 确定性编译只生成 PromptAuthoringContext，不出现可执行 Positive/Negative 或自动 tags。
- PromptRevision 发布后不可覆盖，active 切换保留历史版本。
- `natural_language_v2` 与 `danbooru_tags_v2` 分别校验，不允许从另一 Variant 自动派生。
- Danbooru tags 去重、权重保留、正负 tag 不冲突；TAG_MAP 不参与正式创作。
- ConstraintMapping 缺少 HardConstraint 时 Variant 不能 ready。
- Requirement 或 Catalog 改变后旧 PromptRevision 为 stale。
- seed、variants 或同格式兼容 provider 变化不改变 RevisionFingerprint。

### 15.2 集成测试

- 从测试 Manifest 编译 PromptAuthoringContext，缺少 Agent Revision 时返回 `prompt_authoring_required`。
- 发布双 Variant PromptRevision 后，脚本可以重复消费，不再次调用 Agent。
- OpenAI/Gemini route 选择 `natural_language_v2`，且 ProviderRequest 不包含 adapter 追加或重复 Prompt。
- NovelAI route 选择 `danbooru_tags_v2` 并只由 adapter 序列化权重。
- 缺少、stale 或不兼容目标 Variant 时批次在生成前失败。
- 通用批量允许一次 authoring 多个 VisualID，但 Catalog 中每项拥有独立 RevisionID。
- `character_portrait_set` 不能进入标准批次。
- 角色套组 Context 同时包含 IdentityLocks、SourceAssets、实际参考图和成员差分。
- Zero 冷淡差分回归检查身份、白布眼罩、三分之四构图和纯白底策略，不要求复制旧 Prompt 文案。
- UI NineSlice 项继续进入专项能力，不因 Prompt ready 绕过技术门禁。

### 15.3 迁移验证

- 313 个 Entry 数量和 VisualID 集合保持不变。
- 所有 Entry 有 Profile 或明确 `style_resolution_required`。
- 14 个角色立绘成员仍属于同一 AssetSet，工作区不变。
- 迁移前后 Approved hash、`.meta` GUID、Registry 和状态一致。
- 旧 Prompt 兼容字段和 v1 Variant 保留为 `legacy_compiled`，新的正式执行器拒绝消费。

## 16. 完成判定

本设计的实现只有在以下条件全部满足时才可声明完成：

1. 风格源、Catalog、Manifest Schema 和 Request Catalog 已落地并通过严格校验。
2. `ui_button_primary` 不再存在基于名称猜色路径，回归测试证明使用绯红主行动语义。
3. 确定性编译器不再机械生成正式 Prompt，且 active 资产可以导出完整 PromptAuthoringContext。
4. Agent 可以为通用批量和角色立绘分别发布不可变 PromptRevision；后续脚本只凭 Batch Plan 和 Revision 重复生成，不再次调用 Agent。
5. 同一 PromptRevision 固定拥有独立 `natural_language_v2` 与 `danbooru_tags_v2` 槽位，或明确记录某格式 `unsupported` 的原因。
6. OpenAI/Gemini 与 NovelAI 能选择正确 PromptFormat，并保存未被 adapter 改写的最终 ProviderRequest。
7. `character_portrait_set` 使用独立套组编排，标准批量脚本硬阻断误路由。
8. 旧 313 项按需迁移；未进入 active 批次的 v1 Prompt 不要求立即重写。
9. 迁移未修改任何 Approved、GUID、Registry 或公开状态。
10. 美术事实文档、工具 README、Skill 和状态页已回写，相关测试与文档校验通过。
