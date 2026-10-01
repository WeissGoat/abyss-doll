---
id: gemini_portrait_output_contract_design
title: Gemini 角色立绘输出契约设计
type: design
role: Owner
domain: ai_image_gateway_art_pipeline
status: active
source_of_truth: false
related:
  - docs/plans/2026-08-08-gemini-portrait-output-contract.md
  - 美术文档/05_AI图片网关接入方案.md
  - 美术文档/人设/05_零号立绘素材设计与交付清单.md
  - .codex/skills/p3-generate-image/SKILL.md
  - .codex/skills/p3-art-asset-production/SKILL.md
last_verified: 2026-08-08
update_rule: Gemini chat image 画幅参数、角色立绘背景策略、生成证据或 Zero hurt 修复边界变化时更新本设计。
---

# Gemini 角色立绘输出契约设计

## 目标

修正 Zero 角色立绘差分的两个责任错位：画幅不再依赖 Prompt 文本，背景不再请求后端直接生成透明 Alpha。Gemini 负责生成完整角色和可分割绿幕，P3 既有背景处理能力负责生成透明 PNG、规范化到 `1024x1536`，正式生产门禁继续负责 processed、selected、Approved 和 Registry。

## 已确认问题

`gemini_chat_image` 当前只是 `OpenAIChatImageProvider` 的空别名。`ImageToImageRequest.width/height` 没有进入请求 JSON，只被追加为：

```text
Target size: 1024x1536.
```

`doll_zero_hurt@609744e4ff67/prompt-002` 同时写了“Prefer true alpha”和“否则使用绿幕”，导致 Gemini 生成了烘焙棋盘格。`zero_hurt_repair_20260808_06_gemini` 的成功候选实际为 `1376x768 RGB`，不能满足角色立绘输出合同。

历史 `zero_pose_variation_20260801_01/segmentation_retry_20260801_02` 已证明：Gemini 横版 RGB 候选可以通过现有 segmentation/rembg 路径生成显式 mask，再规范化为透明 `1024x1536`。本轮复用该路径，不创建第二套去底系统。

## 责任边界

### Gemini provider

- 根据请求宽高计算最简整数画幅比。
- 只在 `gemini_chat_image` payload 中发送结构化图片配置。
- 不修改已发布 Prompt 文本。
- 在 `generation_params` 记录实际序列化的图片参数。
- 不承诺后端一定返回精确像素尺寸；精确尺寸由后处理合同保证。

### PromptRevision

- 描述角色身份、姿态、损伤状态、构图和禁项。
- 明确请求单一、饱和、无渐变、无纹理、无地面、无阴影的 chroma-green matte。
- 不出现 `transparent background`、`true alpha`、`Prefer alpha` 或棋盘格兜底。
- 不写 `1024x1536` 或其他像素尺寸；画幅由 provider 参数承担。

### P3 后处理

- 对 RGB 输出使用已有 `segmentation`/`rembg` 能力生成 mask。
- 保存原始 mask 和背景处理证据。
- 保护银白发、白布眼罩、浅色裙、皮肤和脚趾边缘。
- 规范化到 `1024x1536 RGBA`，必要时做有界绿边去色。
- 只通过 Registrar 发布下一不可变 `processed/<n>`。

## Provider 设计

在 `OpenAIChatImageProvider` 增加内部 payload 扩展钩子，默认返回空字典。`GeminiChatImageProvider` 覆盖该钩子，把请求宽高映射为：

```json
{
  "generationConfig": {
    "responseModalities": ["IMAGE"],
    "imageConfig": {
      "aspectRatio": "three-four",
      "imageSize": "2k"
    }
  }
}
```

Flow2API 模型别名只支持 `landscape / portrait / square / four-three / three-four` 和
`2k / 4k`。网关按请求宽高选择最接近的受支持画幅；`1024x1536` 映射为
`three-four + 2k`，再由后处理精确规范化为 `1024x1536`。宽高缺失时不发送图片配置。

字段形态来自当前 Flow2API `/openapi.json` 的 `ChatCompletionRequest -> GenerationConfigParam -> ImageConfig` schema。不把它加入通用 chat passthrough allow-list，避免 OpenAI/Grok relay 收到 Gemini 专属字段。

## 证据合同

每个 Gemini 输出的 `generation_params` 增加：

```json
{
  "provider_image_parameters": {
    "generationConfig": {
      "responseModalities": ["IMAGE"],
      "imageConfig": {
        "aspectRatio": "three-four",
        "imageSize": "2k"
      }
    }
  }
}
```

P3 `generation.json` 的 `ProviderRequest` 同步记录首个成功结果的 `provider_image_parameters`，使请求参数不只埋在单张 `Outputs[].Params` 中。不得记录密钥、完整 Base64 或本地配置内容。

## Prompt-003 策略

`prompt-003` 保留 prompt-002 的身份锁、姿态意图和双臂分离要求，只调整输出表述：

- 删除所有原生透明或 Alpha 请求。
- 删除 Prompt 内像素尺寸。
- 把背景收敛为均匀饱和绿色分割幕布。
- `technical:alpharequired` 映射到“后处理生成 Alpha”，不映射到 provider 原生透明能力。
- `technical:width/height` 映射到 provider request 参数和后处理规范化，不映射到自然语言 Prompt。
- Danbooru Variant 使用 `green background`、`simple background` 等标签，不使用 `transparent background`。

## 执行与门禁

```text
prompt-003
  -> Gemini image_to_image + structured aspect ratio
  -> raw RGB candidate
  -> output contract audit
  -> segmentation/rembg mask
  -> 1024x1536 RGBA staging
  -> visual-review evidence
  -> Registrar publishes next processed/<n>
  -> six-dimension replacement review
  -> guarded selected overwrite only if strictly better
```

生成成功最多声明 `raw`。分割后但未注册最多声明 staging candidate。只有新的数字轮次和替换门禁全部通过，才允许修改 selected。Approved、`.meta`、GUID、Registry 和现有 `registered` 状态默认保持不变。

## 验收标准

1. 通用 `OpenAIChatImageProvider` 的 Prompt 与 payload 行为不变。
2. Gemini generate 和 image-to-image 都发送约分后的结构化画幅比。
3. Gemini Prompt 文本不再自动包含 `Target size`；尺寸只存在于结构化参数和证据。
4. `prompt-003` 不请求 Alpha、不写像素尺寸，只请求统一绿幕。
5. 真实 Gemini smoke 至少证明中转接受参数并返回可解码图片，或留下准确的 provider rejection/timeout 证据。
6. 新 raw 必须经过现有 segmentation/rembg 和不可变数字轮次流程。
7. 现有 selected、Approved、`.meta`、GUID 和 Registry 在替换门禁通过前保持不变。
