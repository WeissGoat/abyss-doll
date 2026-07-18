---
id: spec_zero_dialogue_neutral_character_portrait_pilot
title: 零号对话中性立绘首轮流程试跑设计
type: design
role: 美术
domain: character_portrait_production
status: active
source_of_truth: false
related: []
last_verified: 2026-07-18
update_rule: 首轮角色立绘试跑的 Asset Contract、生产边界、候选来源、门禁结果或后续接入授权变化时更新本文档。
---

# 零号对话中性立绘首轮流程试跑设计

## 1. 目标与边界

用一个低风险、已有候选的角色立绘成员验证 `character_portrait_set` 的正式前半段链路：需求准入、Manifest、Profile 工作区、候选导入、预处理、技术门禁、视觉决策和证据写回。

本轮明确不做：

- 不替换现有 `doll_proto_0_stand`；
- 不修改现有 Approved、Registry、Prefab 或运行时绑定；
- 不运行真实图片生成，先把已有 selected 候选作为 imported source 进入新工作区；
- 不把白底 RGB 候选直接宣称为透明正式素材；
- 不执行 Approved 同步、Unity 导入或 ArtAcceptance。

## 2. 方案比较

### 方案 A：导入既有候选并走到技术/视觉决策（采用）

把 `zero_dialogue_neutral.png` 导入新 Profile 的 `raw/`，用现有预处理脚本生成透明候选、尺寸检查和 contact sheet，再在 `SELECTION_DECISION` 记录结果。

优点：不依赖外部图片后端，能完整验证工作区和候选证据链；失败时不会污染 Approved。缺点：不能验证 provider 选择和真实生成证据。

### 方案 B：在新 Profile 中重新生成 4 张候选

让 Agent 根据 Contract 选择当前图片能力并记录 generation evidence。

优点：覆盖完整生成链路。缺点：成本和不确定性更高，第一次 Profile 试跑同时暴露生成质量问题，不利于区分流程问题和图片问题。

### 方案 C：直接替换现有 `doll_proto_0_stand`

优点：能快速验证运行时接入。缺点：这是共享 fallback 的同 VisualID 替换，会混入 `.meta`/GUID、Registry 和玩家路径风险，不适合作为首轮 Profile 测试。

采用方案 A；方案 B 在本轮流程通过后作为第二轮，方案 C 保持单独的同 VisualID replacement 任务。

## 3. 正式生产契约

```yaml
mode: interactive
source_type: preset
operation: new_asset
production_profile: character_portrait_set
visual_id: doll_zero_dialogue_neutral
asset_set_id: zero_dialogue_portrait_v1
asset_id: zero_dialogue_neutral
set_role: neutral_dialogue_master
source_assets: []
target_quality_tier: formal_ai_v2
claim_ceiling: processed_candidate
allow_approved_sync: false
allow_same_visualid_replace: false
allow_provider_fallback: true
```

来源事实：

- `美术文档/人设/03_零号初版人设方案.md`：身份、白布遮眼、银白散发、灰披肩、裸足和核心仓隐藏规则；
- `美术文档/人设/05_零号立绘素材设计与交付清单.md`：`stand_dialogue_3q`、P0 对话用途、画布和安全区；
- `美术文档/人设/AI出图/zero_dialogue_differences_20260712_01/selection_review.md`：候选来源和当前筛选限制。

来源候选：

```text
美术文档/人设/AI出图/zero_dialogue_differences_20260712_01/selected/zero_dialogue_neutral.png
```

## 4. Asset Contract

```yaml
asset_type: portrait
output_path: UnityClient/Assets/Art/Approved/Dolls/doll_zero_dialogue_neutral.png
source_spec:
  format: png
  width: 1024
  height: 1536
  background: transparent
  alpha_required: true
display_spec:
  reference_resolution: 1920x1080
  display_width: 420
  display_height: 720
  fit_mode: contain
  pivot: bottom_center
composition_spec:
  pose: stand_dialogue_3q
  safe_padding_percent: 6
  subject_occupancy_min: 0.86
  subject_occupancy_max: 0.94
  anchor: bottom_center
  baseline_percent: 94
style_anchors:
  - clean Japanese anime game linework
  - restrained soft cel shading
  - low visual noise
identity_anchors:
  - petite but non-childlike proportions
  - pale silver loose long hair
  - closed white cloth blindfold
  - complete gray shoulder cloak covering both shoulders and chest
  - simple separate inner dress
  - bare legs and bare feet
must_preserve:
  - face and hair silhouette
  - three-quarter dialogue composition
  - full-body framing
  - cloak/dress separation
  - hidden eyes and hidden core chamber
allowed_changes:
  - transparent background extraction
  - crop and scale within safe padding
  - deterministic edge cleanup without redesign
forbidden:
  - visible eyes or red glow in neutral state
  - exposed shoulder or merged cloak-dress silhouette
  - shoes, socks, text, watermark, scenery
  - cropped head, hair, hands, feet or unsafe baseline
  - identity, pose direction or costume redesign
```

## 5. Workflow and stop gates

1. Add one `SourceType=preset` entry to `美术文档/art_requirements_seed.json` with the fields above and `ProductionProfile=character_portrait_set`.
2. Run `Update-ArtManifest.ps1`; verify the entry resolves to `character_portraits/doll_zero_dialogue_neutral/` and does not alter existing entries.
3. Copy the existing selected PNG into that member's `raw/` with a `reference_inputs.json` record containing source path, hash, dimensions, and provenance.
4. Run `Optimize-ArtAssets.ps1` for this VisualID. Expected deterministic processing: white-background removal, 1024x1536 RGBA PNG, safe padding, processed output and contact sheet.
5. Inspect processed pixels and contact sheet at full size and dialogue-scale preview. Record hard gate, score, risks and recommendation in `production_decision.json`.
6. Stop at `SELECTION_DECISION` if alpha extraction damages hair/feet, the candidate is not visibly a 3/4 dialogue pose, or identity/scale cannot be compared reliably. Ask for a concrete repair or source decision.
7. Do not run `Sync-ApprovedArt.ps1` in this pilot. The maximum claim is `processed_candidate`; `Approved`, `registered` and `runtime_validated` remain false.

Expected workspace:

```text
UnityClient/Assets/Art/_IncomingAI/character_portraits/doll_zero_dialogue_neutral/
  raw/r01_001.png
  processed/r01_001.png
  contact_sheet/doll_zero_dialogue_neutral_contact_sheet.png
  asset_contract.json
  production_plan.json
  reference_inputs.json
  process_report.json
  production_decision.json
```

## 6. Success criteria

- Manifest contains one legal `character_portrait_set` entry with stable `AssetID`, `AssetSetID`, `SetRole`, VisualID and OutputPath.
- Resolver maps only to `character_portraits/doll_zero_dialogue_neutral/`; no root-level or `_legacy_runs` input is read.
- Raw provenance and hash are recorded.
- Processed output is decodable RGBA PNG at 1024x1536, with no meaningful white background, cropped body parts or unsafe baseline.
- Contact sheet and decision evidence are present and reproducible.
- Existing Approved, Registry, `.meta`, runtime code and unrelated Manifest entries are unchanged.
- The result is reported as `processed_candidate` or `decision_required`, never as an Approved or runtime-complete asset.

## 7. Follow-up after this pilot

- If this pilot reaches `processed_candidate`, run a second member such as `doll_zero_dialogue_command_ready` to test set-level identity consistency.
- Only after two members pass individually and as a set should the user decide whether to permit Approved synchronization.
- A separate later task may test actual image generation in the same Profile; it must keep the same method-neutral Contract and record the selected capability in run evidence.
