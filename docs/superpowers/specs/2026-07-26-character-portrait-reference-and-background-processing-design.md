---
id: spec_character_portrait_reference_and_background_processing
title: 角色立绘参考资产解析与背景处理能力设计
type: design
role: 美术
domain: character_portrait_production
status: active
source_of_truth: false
related: []
last_verified: 2026-07-26
update_rule: 角色立绘 SourceAssets 解析、参考图传递、背景处理候选、PromptRevision 策略或 cold Pilot 门禁变化时更新本文档。
---

# 角色立绘参考资产解析与背景处理能力设计

## 1. 目标

补齐 `character_portrait_set` 在 PromptRevision 之后仍缺失的执行能力，使逻辑 `SourceAssets` 能解析为真实、可审计的参考图片并传给图片后端；使 `BackgroundPolicy=agent_required` 的候选能够通过显式、受控的背景处理进入下一不可变数字轮次。以 `doll_zero_cold` 为首个真实 Pilot，最多推进到 guarded `selected`，不修改 Approved、Unity 或 Registry。

## 2. 已确认根因

`zero_cold_portrait_chain_20260726_01` 产生 6 张可解码输出但 0 张通过硬门禁：

- Gemini 候选较好地保留白布眼罩和 cold 姿态，但返回 `1376x768 RGB JPEG` 与烘焙棋盘格；
- OpenAI Images 返回 RGBA，但移除白布眼罩并露出眼睛；
- 当前 `run_character_portrait_set.py` 只把 `SourceAssets` 原样复制为 `ReferenceAssets`，没有解析文件、hash、尺寸和状态；
- 执行器调用 `run_art_generation.py` 时没有传递参考图片，因此正式 PromptRevision 与真实图生图输入断开；
- `art_background.py` 对 `agent_required` 正确返回 `decision_required`，但缺少一个接收显式处理输入、写 Run staging 并交给数字轮次注册器的稳定入口。

现有 `processed/2` 和 90 分 `selected/001.png` 保持权威。没有新候选通过全部硬门禁时，不得发布 `processed/3`。

## 3. 方案比较

### 方案 A：把 Resolver、图生图和背景处理全塞进人物执行器

实现快，但人物执行器会同时负责事实解析、文件选择、Provider 协议和像素处理，难以测试，也会让其他 Profile 无法复用背景能力。

### 方案 B：独立 Resolver + 通用参考图生成输入 + 独立背景候选入口（采用）

Resolver 只把逻辑关系变成真实文件证据；底层生成器根据是否存在参考图选择 `image_to_image`；背景处理入口只处理一个明确候选并输出 staging evidence；人物执行器只做编排。边界清晰，可分别测试和恢复。

### 方案 C：继续使用一次性脚本手工跑 cold

可以再次得到图片，但不能打通正式链路，后续每个差分都会重复同样的问题，不采用。

## 4. 参考资产 Resolver

新增 `portrait_reference_resolver.py`，输入 Manifest、目标 Entry 和项目根目录，输出有序 `ResolvedReferenceAssets`。

每个逻辑来源必须解析：

```json
{
  "AssetID": "zero_dialogue_neutral",
  "VisualID": "doll_zero_dialogue_neutral",
  "Role": "identity_and_pose_reference",
  "Path": "UnityClient/Assets/Art/Approved/Dolls/doll_zero_dialogue_neutral.png",
  "State": "approved",
  "SHA256": "...",
  "Width": 1024,
  "Height": 1536,
  "Mode": "RGBA"
}
```

解析优先级固定为：

1. 来源 Entry 已是 `approved` 且 `ApprovedPath` / `OutputPath` 文件存在时，使用 Approved；
2. Manifest `SelectedPath` 存在时使用该文件；
3. Profile 工作区 `selected/` 中恰好存在一个可解码图片时使用该文件；
4. 其他情况返回明确错误，不回退旧 processing round、raw 或 `_legacy_runs`。

同一 AssetSet 内 AssetID 缺失、重复、路径不存在、图片不可解码或 selected 多候选都必须在 Provider 调用前失败。Resolver 不改变 Manifest 或工作区。

## 5. 生成器参考图传递

`run_art_generation.py` 增加重复参数 `--reference-image`。存在参考图时：

- 调用网关 `ImageService.image_to_image()`；
- 读取 exact active PromptRevision，不追加 Preserve / Change 前缀；
- 按 Resolver 顺序发送参考图；
- 在 `ProviderRequest` 和 `generation.json` 中记录路径、hash、尺寸、role 和实际 capability；
- Provider 不支持图生图、参考图缺失或读取后 hash 改变时，调用前失败。

没有参考图时保持现有文生图行为。`character_portrait_set` 不固定 provider；本 Run 由 Agent 根据能力和证据选择。

## 6. 显式背景处理候选

新增 `prepare_art_background_candidate.py` 与 PowerShell wrapper。输入一个 raw / Run staging 图片，输出到显式 `--staging-dir`，并写 `background-processing.json`。支持三种方法：

- `alpha_passthrough`：输入已含有效 Alpha，只做证据和技术检查；
- `connected_border`：只移除与画布边界连通的近似纯色背景，适用于与角色颜色明显分离的纯色 matte；
- `explicit_mask`：使用与输入同尺寸的显式灰度 / Alpha mask 设置输出 Alpha，支持 Agent 或外部能力先调整 mask。

该入口禁止写入 `selected/`、Approved 或现有 `processed/<n>`。输出通过技术门禁后，才允许使用 `Register-ArtProcessingRound.ps1` 把 staging 发布为下一数字轮次。任何 mask 尺寸不匹配、全黑 / 全白无效 mask、输入 hash 变化、白发 / 眼罩等身份硬失败或 Alpha 门禁失败都会停止发布。

## 7. PromptRevision 策略

`doll_zero_cold/prompt-001` 保留历史。背景策略和真实参考图输入发生变化后，由 Agent 发布 `prompt-002`：

- `natural_language_v2` 与 `danbooru_tags_v2` 独立创作；
- 保持白色不透明眼罩、银白散发、灰披肩、浅色内裙、裸足和无红光；
- cold 差分在小尺寸可辨认，但不是愤怒、恐惧或背面构图；
- 最终合同仍是 `1024x1536 RGBA`；当后端不能可靠输出 Alpha 时，请求干净、无棋盘格、适合显式分割的背景，不把假透明当成功；
- Provider adapter 仍只序列化，不按后端临时重写 Prompt。

## 8. Cold Pilot 执行顺序

1. 审计并记录旧 `processed/2`、selected hash 与 90 分基线。
2. Resolver 将 `zero_dialogue_neutral` 解析为 Approved 图片并记录证据。
3. 发布 `prompt-002`，strict 验证 Catalog。
4. 对目标后端执行配置检查和最小 smoke。
5. 先用 Gemini 图生图生成 2 张；只有明确需要时才使用 OpenAI edit 或有 mask 的 NovelAI inpaint 作为有记录的能力切换。
6. 对可用 raw 选择显式背景处理方法，输出 Run staging 候选。
7. 通过尺寸、格式、Alpha、透明洞、构图和身份硬门禁后注册 `processed/3`。
8. Agent 查看原尺寸、对话显示尺寸和与 neutral / 旧 cold 的对比，写 visual review。
9. 新候选必须无硬失败且优于旧 90 分 selected，才允许 guarded replacement；否则保留旧 selected。

## 9. 状态与安全边界

- 本任务 claim ceiling 为 `selected`；不请求 Approved 授权。
- 新 Run 失败不得覆盖 `processed/2`、selected、Approved、`.meta`、GUID、Unity 或 Registry。
- `_IncomingAI` 和 Run 图片保持忽略，不提交生成候选。
- 稳定事实仍在 Manifest、角色交付清单和 PromptRevision Catalog；Run evidence 不是第二套台账。

## 10. 验收标准

- Resolver 对 neutral 解析到 Approved 文件，并对缺失、重复、路径失效和多 selected 明确失败。
- 人物 dry-run 输出真实 `ResolvedReferenceAssets`；真实执行 evidence 记录 reference path/hash 和 `image_to_image`。
- 背景处理入口不直接写数字轮次，三种方法和错误分支都有测试。
- `prompt-002` 双格式通过 strict validator，旧 `prompt-001` 保持不可变。
- cold 新 Run 至少产生可追溯 raw；只有通过全部硬门禁才发布 `processed/3`。
- selected 只有在新候选优于现有 90 分且无身份冲突时才替换。
- Approved、Unity、Registry 和 GUID 不变。
