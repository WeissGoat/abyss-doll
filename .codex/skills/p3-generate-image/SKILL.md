---
name: generate-image
description: Use for image generation or image editing capabilities, including text-to-image, image-to-image, character or state differences, inpaint, backend selection, provider-specific prompt formatting, raw output evidence, and troubleshooting ChatGPT/Gemini/NovelAI image backends. It does not own Project P3 asset admission, selection, Approved synchronization, Unity integration, or runtime validation.
---

# Image Generation

## 定位

提供图片生成和调整能力，并留下可追溯的原始生成证据。正式 P3 美术资产生产由 `p3-art-asset-production` 编排；本 Skill 不决定需求准入、候选 selected、Approved、Registry 或运行时验收。

## 开工读取

1. 读取 `AGENTS.md`、`PROJECT_STATUS.md`、`agent_status/art.md`。
2. 读取 `美术文档/04_美术风格基准.md` 和任务对应的角色、CG、UI 或资产规格。
3. P3 正式资产请求额外读取 `美术文档/05_AI图片网关接入方案.md` 和上游 `p3-art-asset-production` 提供的 Asset Contract。
4. 后端、能力或提示词不确定时读取 [provider-selection.md](references/provider-selection.md)。
5. 执行路径不确定时读取 [generation-workflows.md](references/generation-workflows.md)。
6. 需要保存 `_IncomingAI` raw 或生成证据时读取 [evidence-and-acceptance.md](references/evidence-and-acceptance.md)。Approved、Manifest 和验收由上游生产 Skill 负责。

## 路由规则

按顺序判断：

1. 新建设计图 / 概念图：优先使用 Codex 内置 `image_gen`；当前环境没有暴露时，使用 `openai_images`，不再停下等待用户确认。
2. 新建通用正式资产：默认 `openai_images`；二次元特化、Danbooru tag 控制或原生 negative prompt 需求优先 `novelai`。
3. 已有图片整体满足、只有局部问题，或需要角色 / 状态 / 姿态差分：优先 `gemini_chat_image` 的 `image_to_image`。
4. 需要遮罩局部重绘且改动指向性不强：可用 `novelai` inpaint。它的精确控制较弱，不适合要求严格位置、形状或身份锁定的修改。
5. `grok_chat_image` 只作显式实验后端，先 smoke，不作为默认生产路由。

## 提示词格式

不得把同一套 prompt 原样发送给所有后端：

- `openai_images`：使用结构清楚的自然语言，说明主体、构图、材质、风格、镜头、必须保留和必须避免。
- `gemini_chat_image`：使用自然语言；图生图时明确哪些内容必须保持、哪些内容允许改变、输出仍需满足的构图和禁项。
- `novelai`：使用 Danbooru tag 式正向 prompt 和独立 negative prompt；把关键身份、服装、姿态、画面类型拆成短 tag，避免长篇自然语言。

## 执行协议

1. 先确认任务是探索候选、正式 Manifest 资产，还是对已有图做调整。
2. 确认输出规格、参考图、允许变化、禁止变化和是否需要透明背景。
3. 使用 `tools/美术工具/Test-AIImageBackends.ps1 -CheckConfigOnly` 检查配置；真实批量前做目标后端最小 smoke。
4. 批量数量 `N` 一律拆成 N 次请求，每次 `count=1`，串行执行并保留间隔。
5. 输出只进入任务指定目录或 `_IncomingAI/raw`，不得直接声明 selected、Approved、registered 或 validated。
6. 返回 raw 图片、provider/model/prompt/seed/尺寸、参考图或 mask、错误与时间证据，然后把控制权交还上游工作流。

## 常用入口

- 三后端连通性：`.\tools\美术工具\Test-AIImageBackends.ps1`
- Manifest 批量文生图：`.\tools\美术工具\Run-ArtGeneration.ps1`
- 文件夹图生图：`tools/ai-image-gateway/examples/run_batch_i2i_folder.py`
- 批量文生图模板：`tools/ai-image-gateway/examples/run_batch_generate.py`

## 声明边界

- 出图成功只表示 `raw` 候选存在。
- 本 Skill 不选择 selected，不同步 Approved，不修改 Registry，不运行 ArtAcceptance。
- 不得提交 API key、`config.local.yaml`、临时 smoke 图或 `_IncomingAI` 原始候选，除非用户明确要求。
