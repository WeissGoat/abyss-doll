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
  - docs/plans/2026-07-18-gif-character-replacement.md
  - tools/美术工具/README.md
  - agent_status/art.md
  - .codex/skills/p3-generate-image/SKILL.md
last_verified: 2026-07-19
update_rule: 修改 GIF 适用范围、输入契约、拆帧、人物替换、预审、质量检查、重试、重编码、运行状态或实现验收口径时同步本文件。
---

# GIF 小循环人物替换工作流

> **状态：** `implemented_offline`。首版工具已在 `tools/美术工具/gif_character_replace/` 实现并通过离线测试；真实 Gemini 身份帧与动作帧预审均已取得可解码输出，但当前 Illya 动作预览仍处于 `awaiting_preview_approval`，不得声明完整 GIF 一致性通过或正式资产验收。

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

当前版本选择“各帧完全独立生成”：每帧不引用上一张生成结果，避免单帧失败或重跑沿时间轴传播。为降低独立生成造成的身份与服装闪烁，先固化一个经过用户确认的外观锚点；锚点确认后，每帧只依赖该锚点与当前原始帧，仍不形成前后帧依赖链。

## 2 默认后端与编辑方式

- 默认且首版唯一正式支持的编辑后端为 `gemini_chat_image.image_to_image`。
- 默认执行整帧图生图，不要求人物蒙版。
- Prompt 使用简洁三段式契约：用户文字最高优先级、图片顺序、编辑规则；必须明确“保持 / 改变 / 禁止改变”，并要求只替换人物。避免重复同义约束导致双参考图请求异常变慢。
- 外观锚点批准后的第二阶段必须继续携带 `identity_contract.json` 中的原始用户文字；不得用通用“图一改成图二动作”文案覆盖或丢弃用户指定的角色、服装和禁止项。
- GIF 帧请求显式设置 `stream=true`；若完整图片 SSE 事件已到达但响应尾部断开，可保留已验证图片，未收到完整图片事件时仍按传输失败处理。
- Provider 返回尺寸与原时间轴不一致时，本地解码后按原画布规范化一次，并在 generation params 记录 provider 尺寸、目标尺寸和是否缩放。
- 背景漂移帧可以在修复阶段使用更严格 Prompt、本地差分合成或可选 mask；mask 不是正常批次前置条件。
- 每帧每次请求固定 `count=1`，串行执行并保留合理间隔。
- 输入整个 GIF 给 provider 不等于动画替换；必须先在本地解析时间轴并拆成完整合成帧。

## 3 总体数据流

```text
GIF 输入
  -> 输入检查与时间轴解析
  -> 拆分完整 RGBA PNG 帧
  -> 建立固定角色身份包
  -> 使用用户参考图生成身份预审帧
  -> 用户批准身份预审帧作为固定外观锚点
  -> 使用外观锚点生成动作预审帧
  -> 用户批准动作迁移效果
  -> 外观锚点 + 当前原始帧独立 Gemini 图生图
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

两帧按顺序生成，不再并列使用原始用户参考图：

1. `identity_frame` 使用“用户参考图或零参考身份候选 + 身份清晰的原始 GIF 帧”生成；
2. 用户批准后，将该输出复制为 `identity/appearance_anchor.png`，记录 SHA-256，并固化为本 Run 唯一的 `appearance_anchor`；
3. `action_frame` 使用“appearance_anchor + 动作原始 GIF 帧”生成，不再发送用户参考图；
4. 动作帧通过后，完整批次复用完全相同的双图输入规则。

外观锚点只约束人物身份、脸部、发型、服装、主色和人物画风，不提供动作、表情、遮挡、背景、镜头或构图。当前原始 GIF 帧负责所有动作与场景结构。用户文字描述始终高于外观锚点和当前原始帧中的人物外观。

预审阶段允许用户：

- 批准并运行完整批次；
- 修改文字后建立新批次；
- 更换身份锚点或参考图后建立新批次；
- 单独重跑其中一帧。

身份帧批准前状态为 `awaiting_appearance_approval`，不得生成动作预审帧；动作帧生成后状态为 `awaiting_preview_approval`，批准前不得运行剩余帧。普通重试和定向重跑必须校验并复用同一个已批准外观锚点及其哈希，不得静默重新生成或替换锚点。

## 7 独立逐帧生成

身份锚点批准后的每帧请求均使用相同的：

- provider、model 和输出规格；
- 用户文字与结构化身份契约；
- 已批准的 `appearance_anchor`，不再携带用户原始参考图或统一参考板；
- Prompt 模板和参考图顺序。

固定图片顺序为：

1. `appearance_anchor`：只提供身份、脸、发型、服装、主色和人物画风；
2. 当前原始 GIF 帧：提供动作、表情、位置、遮挡、背景、镜头、构图和画布。

动作迁移可选使用 Run 内的 `identity/frame_action_texts.json` 边车。边车必须记录原 GIF 的 `source_sha256`、完整 `frame_count`，以及覆盖每个帧索引的非空 `actions` 文本；导入时校验哈希、帧数和索引全集，预审批准后不得替换。存在边车时，预审、完整批次和定向重跑在固定双图输入之外，仅把当前帧对应的简短 `ACTION_TEXT` 注入 Prompt，并把最终文本写入请求证据；不存在边车时继续使用通用极简动作模板，兼容旧 Run。边车不携带用户参考图、不改变 `run_config.json`，也不建立前后帧依赖链。

唯一变化输入是当前原始帧、帧编号和该帧尺寸。请求不得包含上一生成帧，避免单帧错误沿时间轴传播。只有显式严格修复模式且证据表明锚点没有展示所需身份特征时，才允许将用户参考图作为额外 fallback 输入；普通生成、普通重试和背景漂移修复不得自动恢复用户参考图。

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

- `429`、`502`、`503`、`504`、`524`、`incomplete chunked read` 等临时错误最多重试三次并递增等待；
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
│  ├─ appearance_anchor.png
│  ├─ appearance_anchor.json
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
  -OutputRoot "F:\output" `
  -ApproveAppearanceAnchor `
  -Resume

.\tools\美术工具\Invoke-GifCharacterReplace.ps1 `
  -RunID "gif_replace_20260718_001" `
  -OutputRoot "F:\output" `
  -ApprovePreview `
  -Resume
```

重跑异常帧：

```powershell
.\tools\美术工具\Invoke-GifCharacterReplace.ps1 `
  -RunID "gif_replace_20260718_001" `
  -OutputRoot "F:\output" `
  -RerunFrame 4,9,10 `
  -RepairMode Strict
```

仅重新编码：

```powershell
.\tools\美术工具\Invoke-GifCharacterReplace.ps1 `
  -RunID "gif_replace_20260718_001" `
  -OutputRoot "F:\output" `
  -EncodeOnly
```

参数至少包括：`InputGif`、`Prompt` / `PromptFile`、`Reference`、`Provider`、`MaxFrames`、`DelaySeconds`、`RetryCount`、`VisualRetryCount`、`PreserveTransparency`、`Encoder`、`DryRun`、`RunID`、`Resume`、`ApproveAppearanceAnchor` 和 `ApprovePreview`。

## 12 运行状态与完成边界

一次运行使用以下状态：

- `awaiting_identity_selection`：无参考图，等待选择身份锚点；
- `awaiting_appearance_approval`：身份预审帧已生成，等待固化为外观锚点；
- `awaiting_preview_approval`：动作预审帧已使用固定外观锚点生成，等待批准；
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

## 15 当前实现与证据边界

已实现的入口：

```powershell
.\tools\美术工具\Invoke-GifCharacterReplace.ps1 `
  -InputGif "F:\input\source.gif" `
  -Prompt "替换为银发机械师，保持动作、背景和构图" `
  -Reference "F:\refs\front.png" `
  -OutputRoot "F:\output"
```

实现覆盖：运行目录与不可变配置、8-30 帧 disposal-aware RGBA 拆帧、0-N 参考图身份包、用户文字最高优先级、简洁三段式 provider prompt、身份预审到外观锚点批准、锚点驱动动作预审、独立逐帧 Gemini 流式图生图、响应尾部断流保护、输出尺寸规范化、恢复与定向重跑、技术硬门禁、视觉风险标记、contact sheet、Pillow 共享调色板编码和 FFmpeg 模拟分支。GIF 工具离线测试当前为 `53 passed, 2 subtests passed`；AI 图片网关全量测试为 `123 passed`；`Test-AIImageBackends.ps1 -CheckConfigOnly` 使用现有配置检查通过。

逐帧 `ACTION_TEXT` 边车核心已完成离线实现：完整映射校验、Run 内持久化、预审/批次/重跑逐帧注入和请求证据字段均有聚焦测试。真实 Illya Run `gif_replace_20260718_231923_234e76a6` 已导入完整 8 帧动作描述；请求证据确认每帧只发送 `appearance_anchor + current_original_frame`、`stream=true`、`count=1`，且包含当前帧 `action_text`。

该实测同时暴露并修复了第二阶段 Prompt 丢弃原始用户文字的问题：修复前第 0 帧错误保留原角色的巫师帽、粉发和幻想服装；修复后请求重新包含“魔法少女伊莉雅、白色短袖水手校服、移除帽子与尖耳”等最高优先级要求，输出恢复白发校服身份并复刻手指靠近嘴唇的动作。用户批准动作预览后，工作流复用第 0、4 帧并串行生成其余 6 帧，最终得到 8 帧 `320x180`、原逐帧时长、无限循环的 Pillow 编码 GIF。

完整批次的 6 个缺失帧共发生 13 次 provider 尝试，其中 7 次为 `incomplete chunked read` 后的自动重试；所有帧最终取得可解码图片，没有 `manual_review`、拒绝帧或缺失帧。离线 GIF 测试为 `58 passed, 7 subtests passed`，语法、文档校验和健康检查通过。自动报告状态为 `review_required`：无技术硬失败，但记录 `temporal_flicker`，并对各帧保守标记 `background_drift` 与 `identity_check_limited`；视觉上白发校服身份已明显稳定，第 4 帧的表情与头部比例仍形成可见跳变，因此不得声明视觉一致性完全通过或正式资产验收完成。

真实双参考图 smoke 使用简洁提示词在 `66.688s` 返回可解码图片，首事件 `1.719s`、8 个 SSE 事件并收到 `[DONE]`。测试 Run `gif_replace_20260718_134808_25695b4b` 已完成 8 帧、`320x180`、原始逐帧时长与无限循环重编码，输出 `output/result.gif`；技术硬门禁无失败，但因 `temporal_flicker`、背景漂移和身份检查受限处于 `review_required`。该结果证明真实 GIF 链路可运行，不等于视觉一致性通过或 P3 正式资产验收；工作区仍位于用户临时目录，不进入 Manifest、Approved、Registry 或 Unity。
