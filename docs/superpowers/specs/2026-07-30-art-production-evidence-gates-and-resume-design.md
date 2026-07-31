---
id: spec_art_production_evidence_gates_and_resume
title: 美术生产证据、质量门禁与人物立绘恢复设计
type: design
role: 美术
domain: art_asset_production
status: active
source_of_truth: false
related: []
last_verified: 2026-07-31
update_rule: 正式生成证据、技术 override、selected 替换门禁或人物立绘断点恢复设计变化时更新本文档；实现完成后把稳定规则回写对应事实文档与 Skill。
---

# 美术生产证据、质量门禁与人物立绘恢复设计

## 1. 背景

Project P3 已形成两条正式美术生产 route：

- `standard_asset` 由现有通用素材批量工作流处理；
- `character_portrait_set` 由现有人物立绘和差分工作流处理。

两条 route 已共用 Requirement Catalog、不可变 PromptRevision、数字 `processed/<n>`、Agent 视觉评审和 guarded selected。本设计不引入第三个编排体系，而是在现有入口和脚本上增量解决四个缺口：

1. 正式 v2 `generation.json` 仍顶层写入旧 `PromptEN / NegativePromptEN`，可能与真正执行的 PromptRevision Variant 冲突；
2. Agent staging 的技术结论能通过手写 `technical_review.json` 或 `decision.json` 伪造成通过；
3. selected 覆盖只校验新候选最低质量和覆盖授权，没有工具级强制“优于当前 selected”；
4. `Run-CharacterPortraitSet.ps1` 当前完成参考解析、计划和生成后结束，后续处理、评审、选择和恢复依赖 Agent 临时拼接。

本设计延续以下既有事实，不重新定义：

- Requirement 编译只产生 `PromptAuthoringContext + TechnicalRequest + PreservationContract`；
- Agent 发布不可变的 `natural_language_v2` / `danbooru_tags_v2` PromptRevision；
- provider adapter 只能确定性序列化已发布 Variant；
- `processed/<正整数>` 是不可变加工轮次；
- `selected -> approved -> registered -> runtime_validated` 是不同声明；
- `character_portrait_set` 不由文生图、图生图或任何固定工具定义。

承接资料：

- `2026-07-23-art-style-and-multi-format-prompt-compilation-design.md` 定义 Requirement、PromptRevision 和多格式执行模型；
- `2026-07-26-character-portrait-reference-and-background-processing-design.md` 定义人物立绘参考解析和显式背景处理；
- `2026-07-18-unified-art-processing-rounds-design.md` 定义数字轮次和 selected 边界；
- 稳定事实仍以 `p3-art-asset-production`、`generate-image`、美术流水线总览和 Manifest 规范为准。

## 2. 目标与非目标

### 2.1 目标

- 正式 v2 generation evidence 只保存 PromptRevision 来源和真实 ProviderRequest，不再混入旧 Prompt 字段。
- 新数字轮次发布前由 Registrar 根据真实图片和 Manifest 合同重算技术门禁。
- 支持独立、结构化、可审计且受白名单和授权约束的技术 override。
- selected 替换必须基于同轮新旧比较，防止同分、降质或陈旧基线覆盖。
- 在原 `Run-CharacterPortraitSet.ps1` 上补齐后半段和 `-Resume`，不新增平行工作流入口。
- Interactive 与 Automatic 共享同一证据和门禁；全自动不等于取消 Agent 判断或硬门禁。

### 2.2 非目标

- 不重写现有批量生产脚本为通用工作流引擎。
- 不把 Prompt 创作放回编译器或 provider adapter。
- 不固定角色立绘的生成方法、provider 或参考图数量。
- 不修改 Approved、Unity、Registry 或运行时验收协议。
- 不追溯改写历史 `generation.json`、`processed/<n>` 或 selected 证据。
- 不在本轮处理 UI nine-slice 专项能力本身，只保持其硬门禁不可绕过。

## 3. 采用方案：增强现有两条 route

用户可见入口保持：

```text
p3-art-asset-production
├─ standard_asset
│  └─ Run-ArtProductionBatch.ps1 / Run-ArtGeneration.ps1
└─ character_portrait_set
   └─ Run-CharacterPortraitSet.ps1 / Run-ArtGeneration.ps1
```

`generate-image` 继续只负责文生图、图生图、编辑、inpaint、provider 选择、序列化和 raw 生成证据，不拥有 processed、selected、Approved 或 Unity 状态。

确定性动作由现有脚本完成；Agent 继续负责 Prompt 创作、生成能力选择、背景处理决策、视觉评审和允许范围内的例外申请。只有在多个入口确实需要完全相同的校验时才复用同一函数，不建立独立编排服务。

## 4. Prompt v2 执行证据

### 4.1 唯一数据链

```text
Requirement
  -> Agent-authored immutable PromptRevision
  -> selected Variant by PromptFormat
  -> deterministic provider adapter
  -> exact ProviderRequest
```

`PromptRevisionSnapshot` 是创作来源证据；`ProviderRequest` 是实际执行证据。二者不是两套独立创作 Prompt，差异只能来自可复核的 provider 确定性序列化。

正式 v2 不再新增一个泛化的新版 `PromptEN` 或 `ExecutedPromptEN` 字段。选中哪一种类型由 `PromptFormat` 明确表达。

### 4.2 formal_v2 generation evidence

```json
{
  "EvidenceMode": "formal_v2",
  "RequirementRequestID": "doll_zero_cold@abc123",
  "RequirementFingerprint": "requirement-sha256",
  "RequirementSnapshot": {},
  "PromptRevisionID": "doll_zero_cold@abc123/prompt-002",
  "PromptRevisionFingerprint": "revision-sha256",
  "PromptFormat": "natural_language_v2",
  "PromptRevisionSnapshot": {},
  "ProviderRequest": {
    "Provider": "gemini_chat_image",
    "Model": "...",
    "AdapterVersion": "p3_prompt_adapter_v2",
    "Prompt": "本次真正发送的自然语言正面 Prompt",
    "NegativePrompt": "本次真正发送的自然语言负面 Prompt",
    "Width": 1024,
    "Height": 1536
  }
}
```

约束：

- formal v2 顶层禁止 `PromptEN / NegativePromptEN`；
- `PromptFormat=natural_language_v2` 时，ProviderRequest 必须由选中 Variant 的 `Positive / Negative` 序列化；
- `PromptFormat=danbooru_tags_v2` 时，ProviderRequest 必须按原顺序和 weight 序列化 `PositiveTags / NegativeTags`；
- adapter 不得追加质量词、复制 Prompt、改写自然语言或重构 PreservationContract；
- ProviderRequest 不记录认证密钥等秘密字段。

### 4.3 legacy 兼容

只有显式 legacy 参数才能消费旧 Manifest Prompt，并记录：

```json
{
  "EvidenceMode": "legacy_unverified",
  "LegacyPromptInput": {
    "PromptEN": "...",
    "NegativePromptEN": "..."
  }
}
```

formal v2 与 `legacy_unverified` 互斥，不能在同一记录中同时充当执行来源。旧 Prompt 字段可以继续作为迁移证据保留在 Manifest 或 `LegacyPromptVariants`，但不是正式批次输入。

### 4.4 可重复批量执行

脚本复用的是已发布 PromptRevision，而不是某次 `generation.json`。指定相同 `RequestID + PromptRevisionID + PromptFormat` 即可重复批量执行，无需 Agent 再次编译。RequirementFingerprint 变化后旧 Revision 变为 stale，必须发布新 revision。

## 5. 技术门禁和受控 override

### 5.1 原始技术结果不可覆盖

`technical_review.json` 只记录自动规则原始结论：

```json
{
  "SchemaVersion": "technical_review_v2",
  "RuleSetVersion": "p3_art_technical_rules_002",
  "VisualID": "doll_zero_cold",
  "ProductionProfile": "character_portrait_set",
  "Candidates": [
    {
      "File": "candidate_001.png",
      "SHA256": "...",
      "Status": "failed",
      "Reasons": ["unexpected_transparent_holes"],
      "Warnings": ["high_occupied_bbox_transparency"],
      "Metrics": {},
      "ReviewFingerprint": "..."
    }
  ]
}
```

后续即使合法放行，原 `Status` 也不能被改写为 `passed`。

### 5.2 Registrar 重算

`register_art_processing_round.py` 发布下一个数字轮次前必须：

1. 从 Manifest 读取当前 SourceSpec、CompositionSpec、ProcessSpec、AssetType 和 ProductionProfile；
2. 对 staging 中每张候选重新解码并计算 SHA、尺寸、格式、Alpha、边距、连通区域、nine-slice 等指标；
3. 复用自动处理路径使用的同一 `review_candidate` 规则；
4. 与提交的 technical review 和 ReviewFingerprint 对比；
5. 应用合法 override 后计算最终有效状态；
6. 确认 `decision.json` 与最终有效状态一致，才原子发布 `processed/<n>`。

图片、Manifest 合同、技术结果、fingerprint 或 decision 任一不一致都必须拒绝注册。编辑 JSON 本身不能产生有效 passed 轮次。

### 5.3 角色透明度规则修正

角色包围盒内背景较多不等于材质内部存在透明洞：

```text
OccupiedBBoxTransparency > threshold
  -> warning: high_occupied_bbox_transparency

TransparentHoleRatio > threshold
  -> failed: unexpected_transparent_holes
```

这避免白发、裙摆、发丝、张开的手臂和正常负空间被误报为硬失败，同时保留真正 Alpha 异常的保护。

### 5.4 override 分类

不可 override：

- 文件无法解码；
- SHA、尺寸或格式不匹配；
- 输出尺寸不符合合同；
- Alpha 完全为空；
- 明确要求 Alpha 但输出没有 Alpha；
- nine-slice 必需结构失败；
- 报告与真实图片不一致。

可由 Agent按策略判断的启发式风险：

- 高 OccupiedBBoxTransparency；
- 普通素材连通区域较多；
- 有意构图裁切引起的安全边距风险；
- 规则明确列入 Agent 白名单的边缘指标。

需要用户本次授权或预授权策略：

- 接受超出原构图合同的裁切；
- 接受明显透明洞或其他可能影响运行时的结构风险；
- 其他未列为 Agent 自动判断的硬风险。

### 5.5 独立 override 证据

合法 override 写入独立 `technical_override.json`：

```json
{
  "SchemaVersion": "technical_override_v1",
  "VisualID": "doll_zero_cold",
  "Candidate": "candidate_001.png",
  "CandidateSHA256": "...",
  "BaseReviewFingerprint": "...",
  "Overrides": [
    {
      "RuleID": "subject_outside_safe_canvas",
      "Action": "accept_as_warning",
      "Reason": "对话立绘按演出要求从膝部裁切。",
      "Evidence": ["contact_sheet.png", "process_report.json"],
      "AuthorizationMode": "user_preapproved_policy",
      "AuthorizationRef": "production-run:auto-policy-001"
    }
  ]
}
```

Registrar 必须同时校验 Candidate SHA、BaseReviewFingerprint、RuleID 白名单、允许 Action 和本次命令或 Run Policy 中的真实授权。仅在 JSON 中手写 `AuthorizationMode` 不能完成授权。

### 5.6 decision 兼容

`decision.json` 继续用 `Status` 表达现有消费者需要的最终有效状态，并补充：

```json
{
  "Status": "passed",
  "AutomaticStatus": "failed",
  "AppliedOverrides": ["subject_outside_safe_canvas"]
}
```

没有 override 时 `Status == AutomaticStatus`。Resolver、selected 和 Approved 路径仍消费 `Status`，同时可审计原始结果和例外。

## 6. selected 替换质量门禁

### 6.1 首次选择

当前不存在 selected 时继续使用既有门禁：

- 候选来自最新数字轮次；
- 技术状态有效；
- visual review `HardGate=passed`；
- `RecommendedAction=select` 或 `auto_select`；
- 总分至少 88；
- 候选 SHA、尺寸和格式一致。

### 6.2 替换选择

当前存在 selected 时，本轮 visual review 必须同时评审：

- `ReplacementBaseline`：当前 selected；
- `Candidate`：最新 processed 候选。

二者必须使用同一 `ReviewRubricVersion`、Requirement、StyleCatalog、身份合同和评分维度。历史旧分数不能直接作为可比基线；没有历史评分时，在本轮重新评分旧 selected。

### 6.3 replacement review 结构

```json
{
  "VisualID": "doll_zero_cold",
  "SelectionMode": "replacement",
  "ReviewRubricVersion": "portrait_review_002",
  "Candidate": "processed/3/candidate_001.png",
  "CandidateSHA256": "new-sha256",
  "Scores": {
    "Total": 92,
    "IdentityConsistency": 94,
    "DifferenceCompliance": 93
  },
  "ReplacementBaseline": {
    "SelectedPath": "selected/doll_zero_cold.png",
    "SelectedSHA256": "old-sha256",
    "Scores": {
      "Total": 90,
      "IdentityConsistency": 92,
      "DifferenceCompliance": 89
    }
  },
  "ReplacementPolicy": {
    "MinimumScore": 88,
    "MustExceedExisting": true,
    "MinimumScoreDelta": 1,
    "ProtectedDimensions": [
      "IdentityConsistency",
      "DifferenceCompliance"
    ]
  },
  "HardGate": "passed",
  "RecommendedAction": "select"
}
```

### 6.4 Selector 强制规则

普通替换必须同时满足：

- 新 Total 至少 88；
- 新 Total 至少为旧 Total + MinimumScoreDelta，默认 delta 为 1；
- 所有 ProtectedDimensions 不低于旧素材；
- 新候选通过技术和视觉 HardGate；
- 执行时当前 selected SHA 仍等于评审基线；
- `-AllowSelectedOverwrite` 或 Run Policy 提供了覆盖授权。

`-AllowSelectedOverwrite` 只授权文件写入，不绕过质量比较。

人物立绘默认保护身份、服装核心特征、差分符合度和输出合同。通用素材默认保护语义、项目风格、运行时可用性和资产专项结构。具体维度来自 profile review schema，不由 selector 猜测。

### 6.5 特殊结果

- 新旧 SHA 相同：返回 `already_selected`，不重复复制；
- 新旧同分：保留旧 selected；
- 新总分更低或保护维度下降：返回 `replacement_rejected_quality_regression`；
- selected SHA 在评审后变化：返回 `replacement_baseline_stale`，要求重新评审；
- 主动接受质量倒退：只能使用结构化 `SelectionMode=art_direction_override` 和明确用户授权，技术 HardGate 仍不可绕过。

selected 替换不会自动修改 Approved，selection evidence 必须继续写 `ApprovedChanged=false`。

## 7. 人物立绘后半段和断点恢复

### 7.1 保留现有入口

继续扩展：

```powershell
Run-CharacterPortraitSet.ps1 `
  -AssetSetID "zero_dialogue_portraits" `
  -ProductionRunID "portrait-zero-001" `
  -ExecutionMode Interactive
```

恢复使用同一入口：

```powershell
Run-CharacterPortraitSet.ps1 `
  -ProductionRunID "portrait-zero-001" `
  -Resume
```

全自动使用 `-ExecutionMode Automatic` 和显式 Run Policy。脚本不创建 Prompt、不替代 Agent 审美判断，也不固定生成 capability。

### 7.2 完整阶段

```text
source_audit
-> prompt_resolution
-> generation
-> output_contract_audit
-> processing_decision
-> technical_gate
-> processed_registration
-> visual_review
-> guarded_selection
-> summary
```

各阶段边界：

1. `source_audit` 固化 AssetSet、VisualID、SourceAssets、参考图片 SHA、尺寸、格式和依赖；
2. `prompt_resolution` 固化 RequestID、RequirementFingerprint、PromptRevisionID、RevisionFingerprint 和 PromptFormat；
3. `generation` 记录 Agent 选择的 capability、provider、参考图、mask 和选择理由；
4. `output_contract_audit` 在处理前检查解码、实际尺寸、格式、Alpha、数量和引用版本；
5. `processing_decision` 由 Agent选择 `already_usable`、`background_processing_required`、`manual_edit_required` 或 `regenerate_required`；
6. `technical_gate` 在 staging 中生成自动评审，准备必要的 override 申请；
7. `processed_registration` 调用现有 Registrar 重算技术结论、验证 override，并仅在有效状态通过时发布数字轮次；
8. `visual_review` 由 Agent执行，失败时建立下一生成尝试；
9. `guarded_selection` 调用增强后的现有 selector；
10. `summary` 按成员聚合真实结果。

### 7.3 方法中立

执行计划只记录本次实际方法，不把方法写入稳定 Requirement 或目录合同：

```json
{
  "VisualID": "doll_zero_cold",
  "Capability": "image_to_image",
  "Provider": "gemini_chat_image",
  "PromptRevisionID": ".../prompt-002",
  "PromptFormat": "natural_language_v2",
  "References": [
    {
      "Role": "identity_reference",
      "Path": "...",
      "SHA256": "..."
    }
  ],
  "Reason": "保持身份，只改变冷淡表情和姿态。"
}
```

未来工具只要能满足相同 Asset Contract，即可被 Agent选择，无需修改 profile 定义。

### 7.4 轻量 Run 状态

在现有 ProductionRun 证据目录保存 `portrait-set-run.json`：

```json
{
  "ProductionRunID": "portrait-zero-001",
  "AssetSetID": "zero_dialogue_portraits",
  "ExecutionMode": "Interactive",
  "Items": [
    {
      "VisualID": "doll_zero_cold",
      "Stage": "visual_review",
      "PromptRevisionID": ".../prompt-002",
      "GenerationBatchID": "batch-003",
      "ProcessedRound": 3,
      "Evidence": {
        "Generation": ".../generation.json",
        "TechnicalReview": ".../processed/3/technical_review.json"
      },
      "PendingDecision": "visual_review_required"
    }
  ]
}
```

该文件只记录执行进度和证据引用，不保存另一套 approved、registered 或 runtime_validated 生命周期状态。

### 7.5 Resume 规则

`-Resume` 不能只相信 Stage 字符串。每个已完成阶段都要重新验证：

- PromptRevision 和 Requirement fingerprint；
- 参考图、mask 和 raw 输出 SHA；
- generation 输出是否存在且可解码；
- processed 轮次是否存在且未改变；
- technical review 是否通过 Registrar 规则；
- visual review 候选 SHA；
- selected replacement baseline 是否仍然当前。

证据有效则跳过阶段；证据缺失或变化则从最早 stale 阶段继续。旧 raw 和数字轮次始终保留，不覆盖或删除。

PromptRevision 变化会建立新 generation attempt。参考图 SHA 变化会使依赖差分进入 `reference_stale`。Resume 不得复用过期 provider 输出冒充当前执行。

### 7.6 套组依赖

AssetSet 依赖继续来自 `SourceAssets` 和 SetRole：

```text
身份母版
  -> 默认演出母版
     -> 表情差分
     -> 嘴型差分
     -> 状态差分
```

- 身份参考变化：所有未完成依赖项 stale；
- 默认演出母版失败：暂停直接依赖项；
- 单个普通差分失败：只阻塞自身及其显式下游；
- 已完成证据保留，但不能作为新基线继续晋级。

### 7.7 Interactive 与 Automatic

Interactive 在以下情况返回具体 `decision_required`：

- Prompt authoring 尚未完成；
- 需要非预授权技术 override；
- 身份参考冲突；
- 需要人工编辑或美术方向决策；
- 达到重试上限仍无合格候选。

Automatic 允许当前 Agent在锁定需求和 Run Policy 内完成 Prompt authoring、能力选择、背景处理决策、warning 判断、视觉评分和严格优于旧素材的 selected 替换。它不得伪造技术通过、接受未授权质量倒退或越过 Approved 权限。

## 8. 错误处理与汇总

成员最终结果限定为：

```text
selected
kept_existing
review_required
generation_failed
technical_failed
blocked_by_dependency
```

AssetSet 总状态按成员聚合；部分差分成功不能写成全部完成。Provider 超时、限流和错误必须绑定本次 request 和 attempt，不能用旧 raw 或旧 process report 抵消失败。

自动可修复问题发布新的 processed 数字轮次。不可修复、超出重试预算或需要授权的问题停止在真实状态，并保存下一步恢复入口。

## 9. 兼容与迁移

### 9.1 历史 generation evidence

不重写。新记录用 `EvidenceMode` 区分 formal v2 和 legacy；历史记录按原 schema 保留。

### 9.2 历史 PromptRevision

RequirementFingerprint 未变化的已发布 Revision 继续有效。旧 Prompt 字段仍可作迁移证据，但不能被 formal v2 adapter 读取。

### 9.3 历史 processed 轮次

不修改既有不可变目录：

- 已 selected、Approved 或 registered 的资产不追溯降级；
- 尚未晋级的旧轮次若需要继续使用，按新规则重新处理并注册为下一个数字轮次；
- 不在旧轮次中补写 technical fingerprint 或 override。

### 9.4 历史 selected

没有历史评分时，替换评审重新评分当前 selected，不把缺失分数当作零分。

### 9.5 decision 兼容

新增 `AutomaticStatus` 和 `AppliedOverrides`，保留原 `Status` 含义，避免破坏 Resolver 和 selected 消费者。

## 10. 分阶段实施

### 阶段 1：Prompt evidence

- 调整 `run_art_generation.py`；
- formal v2 删除顶层旧 Prompt 字段并增加 EvidenceMode；
- legacy 输入收进 `LegacyPromptInput`；
- 增加自然语言、Danbooru 和 adapter 不改写测试。

### 阶段 2：技术门禁

- 修正 `art_background.py` 的人物透明度规则；
- 让 `register_art_processing_round.py` 复用现有技术评审函数重算；
- 增加 override schema、白名单和 Run Policy 校验；
- 自动 optimizer 发布前也执行相同校验。

### 阶段 3：selected replacement

- 扩展 visual review replacement schema；
- 强化 `select_art_candidate.py` 的基线 SHA、同轮评分、delta 和保护维度检查；
- selection decision 记录完整新旧比较。

### 阶段 4：人物立绘 route

- 扩展 `Run-CharacterPortraitSet.ps1` 和 `run_character_portrait_set.py`；
- 补齐后半段、轻量 Run 状态、依赖和 Resume；
- 保持生成能力由 Agent选择。

### 阶段 5：回归和事实回写

- 更新 `p3-art-asset-production`、`generate-image`、美术流水线总览、Manifest 规范和工具 README；
- 更新美术状态页，只有项目阶段或跨职能交接变化时才更新 PROJECT_STATUS；
- 运行通用素材与人物立绘各一个 selected 级 Pilot，不自动修改 Approved。

## 11. 测试策略

### 11.1 Prompt

- natural language Variant 与 ProviderRequest 一致；
- Danbooru tag 顺序和 weight 序列化一致；
- formal v2 不存在顶层 PromptEN；
- legacy 只能显式执行并隔离保存；
- stale Revision 在 provider 调用前失败；
- adapter 追加、删除或改写 Prompt 时失败。

### 11.2 技术门禁

- 手写 passed 被 Registrar 拒绝；
- 图片替换或 Manifest Spec 改变后旧 review 失效；
- 正常人物负空间只产生 warning；
- 真正透明洞仍失败；
- 非白名单、错误 SHA、错误 fingerprint 或缺少真实授权的 override 被拒绝；
- 不可 override 规则始终失败；
- 自动处理和 Agent staging 得到同一技术结论。

### 11.3 selected

- 首次选择通过；
- 新分数低于 88、等于旧分或低于旧分时拒绝替换；
- 总分提高但保护维度下降时拒绝；
- baseline SHA 变化时拒绝；
- 相同 SHA 幂等成功；
- 无覆盖授权时拒绝写入；
- Approved 文件和状态保持不变。

### 11.4 人物立绘恢复

- generation 后 Resume 不重复调用 provider；
- processed 后 Resume 不创建重复轮次；
- PromptRevision 或参考图变化后建立新 attempt；
- 母版失败阻塞依赖成员；
- 独立差分失败不阻塞无依赖成员；
- summary 不把部分成功声明成整套完成。

### 11.5 项目验证

- 美术工具全量测试；
- Request Catalog strict；
- 文档索引和文档校验；
- Agent health check strict；
- 与风险相称的通用素材和人物立绘 dry-run / fake-provider 集成测试；
- 真实 provider Pilot 另行使用明确 ProductionRunID，claim ceiling 为 selected。

## 12. 完成判定

只有同时满足以下条件才算本设计实现完成：

1. 两条既有 workflow 和用户入口保持不变；
2. formal v2 evidence 不再混入旧 PromptEN；
3. 编辑技术 JSON 不能绕过 Registrar；
4. override 有白名单、真实授权、候选 SHA 和 base fingerprint；
5. selected replacement 工具级保证严格优于当前基线且关键维度不倒退；
6. `Run-CharacterPortraitSet.ps1 -Resume` 不重复生成或 processed 发布；
7. Automatic 模式保留 Agent 创作和视觉判断，但不能越过硬门禁与权限；
8. 通用素材和人物立绘各有一条 selected 级回归证据；
9. 稳定规则已回写原 Skill、事实文档和美术状态页；
10. 测试、Catalog strict、文档校验和健康检查达到可提交标准，任何受限项均显式记录。
