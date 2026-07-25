# 后端与提示词选择

## 任务矩阵

| 任务 | 首选 | 备选 | 提示词 |
|---|---|---|---|
| 新建设计图 / 概念图 | Codex `image_gen` | `openai_images` | 自然语言 |
| 通用文生图、完成度较高的概念资产 | `openai_images` | `gemini_chat_image` | 自然语言 |
| 保身份、保构图、状态差分、参考图编辑 | `gemini_chat_image` | `novelai` inpaint | 自然语言 + 保持 / 改变清单 |
| 二次元特化、角色结构草稿、原生负面词 | `novelai` | `openai_images` | Danbooru tag + negative tags |
| 指向性要求不强的遮罩局部重绘 | `novelai` inpaint | `gemini_chat_image` 图生图 | Danbooru tag + mask |

## 图片调整判断

不要因为局部瑕疵就整张重做。满足以下任一条件时进入图片调整流程：

- 主体身份、整体风格和构图已经成立，但手部、服装局部、背景元素或小面积细节有问题。
- 需要同一角色的情绪、姿态、状态、服装损伤或光效差分。
- 需要保留原图大部分内容，只改变明确区域或属性。

优先用 `gemini_chat_image.image_to_image`。在 prompt 中分开写：

- 必须保持：身份、脸、发型、服装主体、构图、镜头、色板。
- 允许改变：目标姿态、表情、局部物件、磨损、光效。
- 禁止改变：角色数量、背景类型、画幅、文字、水印和非目标区域。

只有已有明确 mask、且目标允许一定随机性时才考虑 NovelAI inpaint。若要求“精确移动到某位置”“严格保持脸部”“只改一个确定形状”，优先回到 Gemini 图生图或人工后处理。

## Prompt 格式

### OpenAI Images

使用完整自然语言，推荐顺序：任务类型、主体、构图、视觉风格、材质和色彩、必须保留、必须避免、输出要求。

### Gemini Chat Image

使用自然语言。文生图写清画面目标；图生图必须写清“保持 / 改变 / 禁止改变”，避免只说“优化一下”。

### NovelAI

正向和负向都使用逗号分隔的 Danbooru tag。优先写主体数量、角色属性、姿态、服装、构图、背景和质量标签。避免把项目剧情、玩法黑话或长篇自然语言直接塞入 prompt。

## 持久化 Prompt Variant 消费

正式 P3 任务的输入优先级固定为：`Request Catalog -> selected Prompt Variant -> provider adapter`。`natural_language_v1` 和 `danbooru_tags_v1` 是可审计的编译结果；Agent 不得根据同一份 `VisualIntent` 在调用 provider 前临时改写另一份 prompt。

| provider | Catalog variant | adapter 行为 |
|---|---|---|
| `openai_images` / GPT image edit | `natural_language_v1` | 发送自然语言正负约束，并附带 `TechnicalRequest` 中的尺寸、参考图和 edit intent。 |
| `gemini_chat_image` | `natural_language_v1` | 将 `PreservationContract` 拆成必须保持、允许改变和禁止改变。 |
| `novelai` | `danbooru_tags_v1` | 将结构化 `PositiveTags` / `NegativeTags` 转成后端需要的字符串；权重只在 adapter 层表达。 |

每次调用都要把 `RequestID`、`RequestFingerprint`、`PromptFormat`、选中的 Variant fingerprint 和最终 `ProviderRequest` 写入 generation evidence。请求缺失、过期、fingerprint 不匹配或 Variant 语义覆盖不足时，在 provider 调用前失败。

## 后端可达性测试

`Test-AIImageBackends.ps1` 支持 `chatgpt`、`gemini`、`novelai`。先只检查配置解析和凭证字段：

```powershell
.\tools\美术工具\Test-AIImageBackends.ps1 -CheckConfigOnly
```

真实任务前只 smoke 本次选中的后端，避免无关 provider 的故障干扰判断：

```powershell
.\tools\美术工具\Test-AIImageBackends.ps1 -Backend chatgpt -Attempts 1
.\tools\美术工具\Test-AIImageBackends.ps1 -Backend gemini,novelai -Attempts 2
.\tools\美术工具\Test-AIImageBackends.ps1 -Backend novelai -Attempts 3 -RetryDelaySeconds 30
```

可使用 `-ConfigPath` 指定配置，使用 `-OutputDir` 固定证据目录。未指定时输出到系统临时目录 `P3BackendSmoke/<timestamp>/`，不进入 `_IncomingAI`、Approved、Manifest 或 Registry。检查产物包括：

- `config_summary.json`：配置解析与模型摘要。
- `summary.json`：各次 smoke 的成功或失败结果。
- `provider.log`：provider 请求日志。
- 成功后端的 smoke 图片及对应 attempt 元数据。

## 不可用报告协议

出现配置或凭证错误、限流、超时、请求异常、上游失败，或返回 0 张可解码图片时，立即停止该后端的真实批量任务。回复用户时必须明确包含：

- 不可用的 provider 和 model。
- 实际执行的配置检查与 smoke 命令。
- 具体错误摘要和证据目录。
- 是否存在能力和风险可接受的备选后端。
- `validation_limited:provider_unavailable:<provider>`，并说明当前没有成功出图证据。

不得静默 fallback。只有既定默认路由 `image_gen -> openai_images` 可在 `image_gen` 未暴露时直接执行，但结果中仍要说明发生了切换。其他后端切换若会改变风格、一致性、提示词格式、negative prompt、inpaint 或图生图能力，先把影响和备选方案告诉用户，再按用户选择继续。
