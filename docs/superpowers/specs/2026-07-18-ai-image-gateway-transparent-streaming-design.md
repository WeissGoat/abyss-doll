---
id: ai_image_gateway_transparent_streaming_design
title: AI 图片网关透明流式模式设计
type: design
role: 程序
domain: ai_image_gateway
status: implemented
source_of_truth: false
last_verified: 2026-07-18
update_rule: 透明流式接口、SSE 解析、fallback、错误语义、证据字段或验收范围变化时更新本设计。
---

# AI 图片网关透明流式模式设计

## 目标

为 `ai-image-gateway` 的 OpenAI-compatible chat image provider 增加真正的透明 SSE 流式传输，解决长耗时图片生成在中转或 Cloudflare 层等待完整响应时容易触发 `HTTP 524` 的问题。

调用方继续使用现有接口：

```python
await service.generate(GenerateRequest(...))
await service.image_to_image(ImageToImageRequest(...))
```

流式模式只改变 provider 内部的 HTTP 读取方式。调用方最终仍得到现有 `ImageResult` / `BatchResult`，不暴露新的事件流 API。

## 已确认问题

当前 chat image provider 已允许把 `stream` 作为安全字段发送到 `/v1/chat/completions`，但 `_post_json()` 仍使用 `httpx.AsyncClient.post()` 等待完整响应，再一次性解析 JSON 或完整 SSE 文本。因此当前实现具备“SSE 响应格式兼容”，不具备“增量 SSE 读取”。

实际 Gemini 双图请求中，服务器可能最终生成成功，但中转客户端在收到最终响应前已经被 Cloudflare 以约 120 秒的等待限制返回 `524`。仅在 payload 中加入 `"stream": true` 不能证明问题解决；服务端必须发送早期事件，客户端也必须持续读取这些事件。

## 范围

本轮覆盖：

- `openai_chat_image`
- `gemini_chat_image`
- `grok_chat_image`
- chat image `generate`
- chat image `image_to_image`
- SSE 增量读取、普通 JSON 兼容、错误解析、证据记录和真实 smoke

本轮不覆盖：

- `openai_images` 的 Images API multipart 流式化
- NovelAI provider
- 对调用方公开 async iterator 或进度回调
- 服务端异步 job / polling 协议
- Cloudflare 或中转服务器配置修改
- 自动把失败的流式请求重新提交为非流式请求

## 方案选择

采用“真实透明 SSE 流式”方案：

- 使用 `httpx.AsyncClient.stream()` 建立流式 HTTP 响应。
- 使用 `httpx-sse` 负责 SSE framing、事件边界、多行 `data:`、心跳和标准字段解析。
- 网关只负责 provider 事件聚合、图片提取、错误语义和证据。
- 现有服务接口和返回模型保持不变。

不采用：

- 只发送 `stream=true`、但仍使用 buffered `client.post()`。
- 为所有 provider 建立公开事件总线和新的 async iterator API。
- 流式失败后自动重新发送非流式请求；该行为可能重复生成和重复计费。

## 依赖

在 `tools/ai-image-gateway/pyproject.toml` 增加核心依赖：

```toml
"httpx-sse>=0.4,<1"
```

选择理由：

- 与当前 `httpx` 异步栈直接配合。
- 不引入额外 HTTP 客户端。
- 提供标准 SSE framing 与增量读取。
- 比在 provider 内维护自制 SSE 状态机更可靠。

## 架构

### 公开接口

不修改 `ImageService.generate()`、`ImageService.image_to_image()`、`GenerateRequest`、`ImageToImageRequest`、`ImageResult` 或 `BatchResult` 的公开签名。

### 内部文件边界

新增：

```text
ai_image_gateway/sse_transport.py
```

该模块只承担：

- 判断 SSE content type。
- 通过 `httpx-sse` 增量消费事件。
- 统计业务事件、首事件时间和结束方式；注释心跳由 `httpx-sse` 消费并维持连接，不通过其公开 API 计数。
- 把事件聚合为现有 `{"_sse_events": [...]}` 兼容结构。
- 对普通 JSON 响应进行同连接 fallback，不重新发送请求。

`providers/openai_compatible.py` 继续承担：

- provider URL、auth、model 和 payload。
- HTTP 状态到 `ProviderError` 的转换。
- provider retry。
- 调用流式或非流式传输。
- 复用现有图片与 provider error 提取逻辑。

### 内部结果

`sse_transport.py` 返回：

```python
@dataclass(frozen=True)
class StreamReadResult:
    payload: dict[str, Any]
    response_mode: Literal["sse", "json"]
    event_count: int
    first_event_elapsed_s: float | None
    completed_by_done: bool
```

## 数据流

```text
ImageService.generate / image_to_image
  -> chat provider 构造 model + messages
  -> settings 与 request.extra 合并安全透传字段
  -> 根据最终 payload["stream"] 判断路径

stream != true
  -> 原有 buffered POST
  -> 原有 JSON / 完整 SSE 解析

stream == true
  -> AsyncClient.stream("POST", ...)
  -> 检查 HTTP 状态
  -> content-type == text/event-stream
       -> httpx-sse 增量读取
       -> 收集事件与证据
       -> 聚合为 _sse_events
     否则
       -> 在同一响应上读取普通 JSON
  -> 复用现有图片与错误提取
  -> ImageResult / BatchResult
```

## 配置语义

Provider 配置：

```yaml
gemini_chat_image:
  settings:
    stream: true
```

规则：

- 未配置 `stream` 时保持现有非流式行为。
- `settings.stream=true` 为该 provider 默认启用透明流式。
- `request.extra["stream"]` 可以覆盖 provider 设置。
- 传输选择基于最终 payload 的 `stream` 值，不直接读取原始配置。
- 不新增配置 schema；provider `settings` 继续保持开放字典。
- 现有 `timeout` 继续用于 connect/write/read/pool timeout。服务端持续发送事件时，read timeout 按每次读取活动重新计算。

## SSE 解析规则

- 支持标准 `event`、`data`、`id`、`retry` 字段。
- 支持多行 `data:` 合并。
- 忽略空行和注释心跳；显式 `event: ping` 仍作为标准 SSE 事件读取。
- `[DONE]` 表示显式正常结束。
- 允许正常 EOF 且已经收到合法图片事件的中转不发送 `[DONE]`。
- 支持 `choices[].delta`、`choices[].message`、嵌套 JSON、`b64_json`、data URL、Markdown 图片 URL 和裸 HTTP(S) URL。
- SSE event payload 继续聚合到 `_sse_events`，复用已有 `_chat_payloads()` 和图片提取逻辑。

## 普通 JSON fallback

当请求包含 `stream=true`，但服务端返回普通 JSON：

- 在当前 HTTP 响应连接上读取完整 body。
- 使用现有 JSON 解析规则。
- `response_mode` 记录为 `json`。
- 不重新发送请求。

该兼容保证允许中转忽略 `stream` 参数，同时避免重复生成。

## 错误处理

- HTTP `4xx/5xx`：读取有上限的错误正文，转换为现有 `ProviderError`。
- `429`：保留现有 `Retry-After` 语义。
- SSE provider error：保留服务器原始错误信息。
- SSE JSON 损坏：错误包含截断后的事件片段，不输出完整 Base64。
- 连接在事件中途发生 transport error：当前请求失败，不使用可能截断的图片。
- 正常 EOF 但没有合法图片：沿用 `No image data found`。
- 不做流式到非流式自动 fallback。
- 保留现有 provider retry 和上层工作流 retry；每次 retry 都是一次完整请求。

## 可观测性

流式请求在 `generation_params` 和 provider 日志中记录：

```text
stream_requested
stream_response_mode
stream_event_count
stream_first_event_elapsed_s
stream_completed_by_done
```

限制：

- 不记录 API key。
- 不记录完整 Base64 图片。
- 不记录完整请求正文或 data URL。
- 错误正文和 malformed event 只保留有限长度片段。

## 测试

### 单元测试

新增 `tests/test_sse_transport.py`，覆盖：

1. 多行 `data:` 合并。
2. 注释心跳和空行忽略。
3. `[DONE]` 正常结束。
4. 无 `[DONE]` 的正常 EOF。
5. provider error event。
6. malformed SSE JSON。
7. transport 中途断开。
8. 普通 JSON 同连接 fallback。

扩展 `tests/test_openai_compatible_provider.py`，覆盖：

1. `stream=false` 保持 buffered POST。
2. `stream=true` 使用流式传输。
3. `request.extra["stream"]` 覆盖 provider 默认配置。
4. generate 和 image-to-image 都使用相同流式路径。
5. 流式结果继续解析 data URL、Markdown URL、裸 URL 和 `b64_json`。
6. 流式错误继续映射为 `ProviderError`。
7. `generation_params` 包含流式证据字段。

### 全量回归

```powershell
python -m pytest tests/test_sse_transport.py tests/test_openai_compatible_provider.py -q
python -m pytest tests -q
```

### 真实 smoke

新增：

```text
examples/smoke_streaming_chat_image.py
```

能力：

- 读取本地忽略配置。
- 支持 `generate` 和 `image_to_image`。
- 通过 `request.extra["stream"] = True` 显式启用。
- 输出首事件耗时、总耗时、事件数、provider、model 和最终图片路径。
- 输出仅进入用户指定目录或系统临时目录。

最终使用当前 Gemini 双图请求验证：

- 120 秒前是否收到 SSE 事件。
- 是否避免 Cloudflare `524`。
- 最终图片是否可解码。
- `ImageService` 返回接口是否与非流式一致。

如果服务端未发送任何早期事件，记录 `validation_limited:upstream_no_early_sse_event`；该结果说明客户端流式支持已成立，但不能由客户端单独解决服务端或 Cloudflare 超时。

## 实施与验证结果

2026-07-18 已在 `tools/ai-image-gateway` 实现并提交：

- `78962f4`：`httpx-sse` 依赖、`sse_transport.py` 和基础传输测试。
- `9c23672`：chat image provider 透明流式接入。
- `88b4d55`：错误、断流和无 buffered 二次请求回归。
- `51fe7f8`：真实服务 streaming smoke CLI。
- `c2610b6`：子模块 README 与中转事实文档。
- `2b2da58`：注释心跳与 `image_to_image(stream=true)` 补充覆盖。
- `d4db1a4`：把首事件计时起点前移到 HTTP 请求发起之前。
- `10523bb`：按修正后的计时口径刷新真实 smoke 证据。

自动验证：

```text
targeted streaming tests: 35 passed
full gateway suite: 121 passed
compileall: passed
```

真实 Gemini 证据：

- 文生图通过：总耗时 `49.859s`，从 HTTP 请求发起前计时的首个业务事件
  `0.312s`，事件数 `5`，收到 `[DONE]`，输出 `107560` 字节 JPEG 且可解码。
- 双参考图图生图未通过：连接保持到 `292.906s` 后，上游关闭不完整 chunked
  response，错误为 `incomplete chunked read`，没有返回可解码图片；记录
  `validation_limited:stream_request_failed_before_success_evidence`。

因此客户端增量 SSE 能力与文生图长耗时路径已经获得真实证据，双图图生图仍受上游
响应完整性限制，不能声明端到端稳定或完整解决所有代理超时。

## 文档与状态回写

- 在子模块现有 `docs/openai_compatible_relay_integration.md` 中更新 chat payload、流式配置、响应解析和 smoke 事实。
- 在子模块 `README.md` 中补充最小配置示例。
- 不新增第二套网关事实文档。
- 仅当 P3 图片生成能力边界发生变化时，增量更新 `agent_status/art.md`；不改变项目阶段和 Unity 状态。

## 工作区与 Git 边界

- 本任务明确允许修改 `tools/ai-image-gateway` 子模块。
- 子模块当前已有用户对 README、文档和示例的改动，实施时必须保留并增量合并。
- 核心 provider、测试和依赖文件当前无用户改动，可作为主要实现范围。
- 父仓库只记录本任务明确需要的设计、状态和子模块指针；不得暂存其他脏文件。
- 按用户要求直接在当前工作区内联执行，不创建额外分支或 worktree。

## 验收标准

1. `stream=true` 触发真正的增量 HTTP/SSE 读取，而不是 buffered `post()`。
2. 非流式默认行为和公开接口保持兼容。
3. SSE 心跳、多行事件、`[DONE]`、普通 JSON fallback 和错误事件均有自动测试。
4. 流式传输不会自动重复提交非流式请求。
5. generate 和 image-to-image 都能返回现有 `ImageResult` / `BatchResult`。
6. 流式证据不泄露密钥或 Base64 图片。
7. 子模块目标测试和全量测试通过。
8. 真实 Gemini smoke 留下首事件时间、总耗时和最终结果；若上游不发早期事件，准确记录验证限制。
9. 现有用户对子模块 README、文档和示例的改动不被覆盖。
