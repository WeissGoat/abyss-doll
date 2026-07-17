---
id: art_gif_character_replacement_workflow
title: GIF 小循环人物替换工作流
type: art
role: 美术
domain: gif_character_replacement
status: active
source_of_truth: true
related:
  - 美术文档/README.md
  - 美术文档/05_AI图片网关接入方案.md
  - docs/superpowers/plans/2026-07-18-gif-character-replacement.md
  - tools/美术工具/README.md
  - agent_status/art.md
  - .codex/skills/p3-generate-image/SKILL.md
last_verified: 2026-07-18
update_rule: 修改 GIF 适用范围、输入契约、拆帧、人物替换、预审、质量检查、重试、重编码、运行状态或实现验收口径时同步本文件。
---

# GIF 小循环人物替换工作流

> **状态：** `design_approved`。本文定义首版工具契约和实现边界；截至 2026-07-18 尚未实现脚本、执行真实 GIF 转换或产生正式资产验收证据。

## 1 目标与范围

首版面向 `8-30` 帧的小型循环 GIF，例如待机、眨眼、表情和挥手。用户提供：

- 一个输入 GIF；
- 必填人物 / 画风文字描述；
- `0-N` 张可选人物参考图。

工具应保持原 GIF 的动作、人物位置、背景、镜头、构图、分辨率、帧持续时间、播放顺序和循环次数，只替换人物身份、外观和用户明确要求的画风属性。

冲突优先级固定为：

```text
用户文字描述 > 用户参考图 > 原 GIF 人物外观
```

动作、构图、背景和时间轴属于必须保留项，不参与上述画风和人物身份优先级竞争。用户文字中的画风要求默认只作用于替换后的人物；首版不允许用画风描述隐式重绘背景或整个画面。

首版选择“各帧完全独立生成”：每帧只依赖固定身份包与当前原始帧，不引用上一张生成结果。这样单帧失败或重跑不会传播到其他帧。

## 2 默认后端与编辑方式

- 默认且首版唯一正式支持的编辑后端为 `gemini_chat_image.image_to_image`。
- 默认执行整帧图生图，不要求人物蒙版。
- Prompt 必须明确“保持 / 改变 / 禁止改变”，并要求只替换人物。
- 背景漂移帧可以在修复阶段使用更严格 Prompt、本地差分合成或可选 mask；mask 不是正常批次前置条件。
- 每帧每次请求固定 `count=1`，串行执行并保留合理间隔。
- 输入整个 GIF 给 provider 不等于动画替换；必须先在本地解析时间轴并拆成完整合成帧。

## 3 总体数据流

```text
GIF 输入
  -> 输入检查与时间轴解析
  -> 拆分完整 RGBA PNG 帧
  -> 建立固定角色身份包
  -> 身份帧 + 动作帧预审
  -> 用户批准
  -> 所有帧独立 Gemini 图生图
  -> 技术检查与视觉风险标记
  -> 仅重跑异常帧
  -> 使用原时间轴重新编码 GIF
  -> 输出 GIF、帧序列、请求证据和复核报告
```

## 4 角色身份包

身份包由 `identity_contract.json`、固定 Prompt 和参考图输入组成。

`identity_contract.json` 至少包含：

- `must_have`：身份、发型、脸部、服装、主配色、物种或机械特征；
- `may_change`：姿势、表情、透视、遮挡和动作形变；
- `must_preserve`：原动作、人物位置、背景、镜头、构图和画布；
- `must_not_have`：额外人物、文字、水印、服装漂移、背景替换和画幅变化；
- 原始用户文字和结构化后的 provider Prompt；
- 参考图路径、顺序、哈希和用途。

不同参考图数量的处理规则：

- `0` 张：先按文字通过 `openai_images` 生成三张静态身份锚点候选，进入 `awaiting_identity_selection`；用户选择一张后才能生成 GIF 预审帧。
- `1-3` 张：原图按固定顺序进入身份包；文字与图片冲突时以文字为准。
- `4+` 张：本地生成统一参考板，避免每帧上传大量零散图片；原始参考图仍保留在运行证据中。

## 5 时间轴解析与拆帧

解析器必须记录：

- 画布宽高；
- 帧数和每帧持续时间；
- loop 次数；
- transparency、disposal 和 blend 信息；
- 原始文件哈希。

GIF 可能只在部分帧保存局部差分块。拆帧器必须先按 disposal 规则还原完整画布，再输出 `frames_original/frame_0000.png` 形式的完整 RGBA 帧，不能把局部差分块直接发给图片后端。

少于 `8` 帧、超过 `30` 帧、无法解析时间轴或存在损坏帧时，只输出 preflight 报告，不产生 AI 请求。

## 6 两帧预审门禁

完整批量开始前自动选择：

1. `identity_frame`：人物正面、脸部或主要身份特征最清晰的帧；
2. `action_frame`：姿势变化最大、侧身程度最高或遮挡最复杂的帧。

两帧各生成一张预览。预审阶段允许用户：

- 批准并运行完整批次；
- 修改文字后建立新批次；
- 更换身份锚点或参考图后建立新批次；
- 单独重跑其中一帧。

两帧通过前状态为 `awaiting_preview_approval`，不得自动运行剩余帧。

## 7 独立逐帧生成

每帧请求均使用相同的：

- provider、model 和输出规格；
- 用户文字与结构化身份契约；
- 参考图或统一参考板；
- Prompt 模板和参考图顺序。

唯一变化输入是当前原始帧、帧编号和该帧尺寸。请求不得包含上一生成帧，避免单帧错误沿时间轴传播。

每完成一帧立即写入：

- raw 图片；
- provider、model、Prompt、参考图、seed、尺寸和 generation params；
- 请求开始 / 结束时间、错误、重试次数；
- 当前帧状态。

中断恢复时只处理缺失、失败或用户显式指定重跑的帧。

## 8 质量检查与异常重跑

技术检查属于硬门禁：

- 图片可解码且不是空图、纯色图或损坏图；
- 输出宽高与原 GIF 一致；
- 帧数、顺序和时间轴完整；
- 最终 GIF 可解码并保持原循环参数。

视觉检查只负责标记风险，不冒充美术终审：

- `background_drift`：差异大面积扩散到固定背景、画面边缘或多个无关区域；
- `temporal_flicker`：新 GIF 相邻帧变化显著高于原 GIF 的动作变化；
- `identity_drift`：可识别的人脸、发型轮廓、主色或人物面积发生突变；
- `identity_check_limited`：无法可靠识别人脸或人物区域；
- `loop_seam`：最后一帧到第一帧的跳变显著高于原 GIF；
- `transparent_silhouette_limited`：新人物轮廓超出原透明 alpha 可表达范围。

异常处理：

- `429`、`502`、`503`、`504`、`524` 等临时错误最多重试三次并递增等待；
- 尺寸偏差可以本地规范化一次，严重比例错误必须重跑；
- 背景或人物漂移使用更严格保留 Prompt 重跑，默认最多两次；
- 单帧持续失败时标记 `manual_review` 并继续其他帧；
- 不得用未替换的原人物帧静默填补最终 GIF。

## 9 GIF 重编码

- 使用全部通过技术检查的完整输出帧重新编码；
- 使用全局调色板，避免每帧独立量化造成额外颜色闪烁；
- 保留原帧持续时间、顺序和循环次数；
- 默认不降低分辨率或帧率；
- 透明 GIF 优先复用原帧 alpha；若新人物轮廓超出原 alpha，则报告限制，不静默裁切；
- 编码器默认 `Auto`：优先 FFmpeg palette 流程，环境不可用时使用 Pillow 并记录实际编码器。

## 10 运行工作区与证据

探索任务默认输出到用户指定目录，不直接进入 Approved：

```text
gif_character_replace/<RunID>/
├─ input/
│  ├─ source.gif
│  ├─ character_prompt.txt
│  └─ references/
├─ timeline/
│  ├─ source_metadata.json
│  └─ frames_original/
├─ identity/
│  ├─ identity_contract.json
│  ├─ reference_board.png
│  └─ anchor_candidates/
├─ preview/
│  ├─ identity_frame/
│  └─ action_frame/
├─ generated/
│  ├─ raw/
│  ├─ accepted/
│  └─ rejected/
├─ output/
│  ├─ result.gif
│  └─ preview_contact_sheet.png
└─ reports/
   ├─ requests.jsonl
   ├─ frame_review.json
   └─ run_summary.json
```

`run_config.json` 创建后不可变。用户修改文字、参考图、provider、model 或尺寸时必须建立新 RunID，避免新旧帧混用；仅改变重试强度、复核标记或编码参数可以沿用原 RunID。

## 11 工具入口设计

编排器应位于 `tools/美术工具/`，不要把 P3 工作区、预审和 GIF 时间轴逻辑写入 `tools/ai-image-gateway` 子模块。

建议入口：

```powershell
.\tools\美术工具\Invoke-GifCharacterReplace.ps1 `
  -InputGif "F:\input\source.gif" `
  -Prompt "替换为……" `
  -Reference "F:\refs\front.png","F:\refs\face.png" `
  -OutputRoot "F:\output"
```

恢复批准后的完整批次：

```powershell
.\tools\美术工具\Invoke-GifCharacterReplace.ps1 `
  -RunID "gif_replace_20260718_001" `
  -ApprovePreview `
  -Resume
```

重跑异常帧：

```powershell
.\tools\美术工具\Invoke-GifCharacterReplace.ps1 `
  -RunID "gif_replace_20260718_001" `
  -RerunFrame 4,9,10 `
  -RepairMode Strict
```

仅重新编码：

```powershell
.\tools\美术工具\Invoke-GifCharacterReplace.ps1 `
  -RunID "gif_replace_20260718_001" `
  -EncodeOnly
```

首版参数至少包括：`InputGif`、`Prompt` / `PromptFile`、`Reference`、`Provider`、`MaxFrames`、`DelaySeconds`、`RetryCount`、`VisualRetryCount`、`PreserveTransparency`、`Encoder`、`DryRun`、`RunID` 和 `Resume`。

## 12 运行状态与完成边界

一次运行使用以下状态：

- `awaiting_identity_selection`：无参考图，等待选择身份锚点；
- `awaiting_preview_approval`：两帧预审已生成，等待批准；
- `running`：完整批次进行中；
- `ready`：时间轴完整、技术检查通过且没有高风险视觉异常；
- `review_required`：已输出预览 GIF，但存在闪烁、背景漂移、透明轮廓或身份检查受限；
- `failed`：存在无法生成的帧、时间轴不完整或 GIF 无法编码。

`ready` 只表示探索工具输出已具备复核条件，不表示 P3 正式资产 Approved、Unity 接入或运行时验收通过。正式资产仍由 `p3-art-asset-production` 承接。

## 13 首版范围外

- 超过 30 帧的长动画；
- MP4、WebM 或通用视频转绘；
- 多人物分别识别和替换；
- 自动改变动作、镜头或背景；
- 自动插帧、补间或提升帧率；
- 强制依赖人物蒙版；
- 复杂遮挡下的像素级轮廓保证；
- 自动同步 Manifest、Approved、`.meta`、Registry 或 Unity。

## 14 实现验收样例

首版实现至少准备三个固定样例：

1. 不透明背景、`8-12` 帧眨眼或待机循环；
2. 不透明背景、`20-30` 帧挥手或转身循环；
3. 带透明通道的小循环，用于验证 alpha 限制报告。

验收至少证明：

- dry-run 不调用 provider；
- 未批准预审时不会运行完整帧；
- 每帧独立请求且 `count=1`；
- 中断后可以恢复，已通过帧不会重复调用；
- 单帧失败不会丢失其他成功帧；
- 帧数、每帧时长、循环次数和画布尺寸保持；
- FFmpeg 不可用时可以降级 Pillow；
- 风险帧进入报告和 contact sheet；
- 未进入正式资产链时不会修改 Manifest、Approved、Registry 或 Unity。
