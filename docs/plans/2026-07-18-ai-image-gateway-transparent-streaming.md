---
id: ai_image_gateway_transparent_streaming_plan
title: AI 图片网关透明流式模式实施计划
type: plan
role: 程序
domain: ai_image_gateway
status: historical
source_of_truth: false
last_verified: 2026-10-01
update_rule: 历史计划记录，不再更新；现行事实以 美术文档/05_AI图片网关接入方案.md 与子模块 tools/ai-image-gateway 为准。
---

# AI 图片网关透明流式模式 Implementation Plan

> 历史计划：已于 2026-07-18 实施（`895bc46`、`6169644`）。现行事实以 `美术文档/05_AI图片网关接入方案.md` 与子模块 `tools/ai-image-gateway` 为准。

**Goal:** 为 OpenAI-compatible chat image provider 增加对调用方透明的真实 SSE 增量读取，在保持 `ImageService` 公开接口兼容的前提下支持长耗时 Gemini 文生图和图生图请求。

**Architecture:** 使用 `httpx.AsyncClient.stream()` 建立响应流，并由 `httpx-sse` 解析标准 SSE framing；新增独立 `sse_transport.py` 返回兼容 payload 和传输证据。Provider 根据最终 payload 的 `stream` 值选择 buffered 或 streaming 路径，普通 JSON 使用同一连接 fallback，不自动重发非流式请求。

**Tech Stack:** Python 3.10+、`httpx>=0.27`、`httpx-sse>=0.4,<1`、Pydantic、pytest、pytest-asyncio、PowerShell。

## Global Constraints

- 不改变 `ImageService.generate()`、`ImageService.image_to_image()`、`GenerateRequest`、`ImageToImageRequest`、`ImageResult` 或 `BatchResult` 的公开签名。
- 默认不启用流式；只有最终 chat payload 的 `stream` 为 `true` 时才走增量路径。
- 不为 Images API multipart、NovelAI 或公开 async iterator 增加流式能力。
- 流式失败时不得自动改发非流式请求，避免重复生成和重复计费。
- 不记录 API key、完整 Base64、完整 data URL 或未截断的错误响应。
- `httpx-sse` 注释心跳用于维持连接，但其公开 API 不暴露注释计数；证据不包含 `heartbeat_count`。
- 子模块当前已有 README、文档和示例改动；不得覆盖或顺带提交它们。
- 按用户要求直接在当前工作区内联执行，不创建分支或 worktree。
- 每个子模块提交只暂存本任务文件；父仓库只暂存计划、规格修正、状态回写、索引和最终子模块指针。

---

### Task 1: 引入依赖并实现独立 SSE 传输模块

**Files:**
- Modify: `tools/ai-image-gateway/pyproject.toml`
- Create: `tools/ai-image-gateway/ai_image_gateway/sse_transport.py`
- Create: `tools/ai-image-gateway/tests/test_sse_transport.py`

**Interfaces:**
- Consumes: 已打开的 `httpx.Response`，响应仍处于 `AsyncClient.stream()` 上下文中。
- Produces: `StreamReadResult`、`read_streaming_response()`、`read_response_excerpt()`。

- [ ] **Step 1: 在依赖清单加入 `httpx-sse`**

在 `pyproject.toml` 的核心依赖中加入：

```toml
"httpx-sse>=0.4,<1",
```

- [ ] **Step 2: 编写 SSE 测试基础设施和失败测试**

在 `tests/test_sse_transport.py` 定义可控异步字节流：

```python
from __future__ import annotations

import json

import httpx
import pytest

from ai_image_gateway.sse_transport import (
    SSEPayloadError,
    read_response_excerpt,
    read_streaming_response,
)


class ChunkStream(httpx.AsyncByteStream):
    def __init__(self, chunks: list[bytes], error: Exception | None = None) -> None:
        self._chunks = chunks
        self._error = error

    async def __aiter__(self):
        for chunk in self._chunks:
            yield chunk
        if self._error is not None:
            raise self._error
```

加入测试：

```python
@pytest.mark.asyncio
async def test_streaming_sse_collects_multiline_json_and_done():
    payload = {
        "choices": [{"delta": {"content": "data:image/png;base64,AAAA"}}]
    }
    encoded = json.dumps(payload, indent=2)
    body = "".join(f"data: {line}\n" for line in encoded.splitlines())
    body += "\ndata: [DONE]\n\n"
    response = httpx.Response(
        200,
        headers={"Content-Type": "text/event-stream"},
        stream=ChunkStream([body.encode("utf-8")]),
    )

    async with response:
        result = await read_streaming_response(response)

    assert result.response_mode == "sse"
    assert result.payload == {"_sse_events": [payload]}
    assert result.event_count == 1
    assert result.first_event_elapsed_s is not None
    assert result.completed_by_done is True


@pytest.mark.asyncio
async def test_streaming_sse_accepts_clean_eof_without_done():
    body = b'data: {"choices":[{"message":{"content":"https://example.com/a.png"}}]}\n\n'
    response = httpx.Response(
        200,
        headers={"Content-Type": "text/event-stream; charset=utf-8"},
        stream=ChunkStream([body]),
    )
    async with response:
        result = await read_streaming_response(response)
    assert result.event_count == 1
    assert result.completed_by_done is False


@pytest.mark.asyncio
async def test_streaming_response_falls_back_to_json_on_same_connection():
    payload = {"data": [{"url": "https://example.com/a.png"}]}
    response = httpx.Response(
        200,
        headers={"Content-Type": "application/json"},
        stream=ChunkStream([json.dumps(payload).encode("utf-8")]),
    )
    async with response:
        result = await read_streaming_response(response)
    assert result.response_mode == "json"
    assert result.payload == payload
    assert result.event_count == 0


@pytest.mark.asyncio
async def test_streaming_sse_rejects_malformed_json():
    response = httpx.Response(
        200,
        headers={"Content-Type": "text/event-stream"},
        stream=ChunkStream([b"data: {not-json}\n\n"]),
    )
    async with response:
        with pytest.raises(SSEPayloadError, match="Malformed SSE JSON"):
            await read_streaming_response(response)


@pytest.mark.asyncio
async def test_streaming_sse_propagates_transport_disconnect():
    error = httpx.ReadError("connection reset")
    response = httpx.Response(
        200,
        headers={"Content-Type": "text/event-stream"},
        stream=ChunkStream([b'data: {"choices":[]}\n\n'], error=error),
    )
    async with response:
        with pytest.raises(httpx.ReadError, match="connection reset"):
            await read_streaming_response(response)


@pytest.mark.asyncio
async def test_response_excerpt_is_bounded():
    response = httpx.Response(524, stream=ChunkStream([b"x" * 6000]))
    async with response:
        excerpt = await read_response_excerpt(response, limit=4096)
    assert len(excerpt) == 4096
```

- [ ] **Step 3: 运行测试确认失败**

Run:

```powershell
python -m pytest tests/test_sse_transport.py -q
```

Expected: collection fails because `ai_image_gateway.sse_transport` does not exist.

- [ ] **Step 4: 实现 `sse_transport.py`**

实现以下接口：

```python
from __future__ import annotations

import json
from dataclasses import dataclass
from time import monotonic
from typing import Any, Literal

import httpx
from httpx_sse import EventSource


class SSEPayloadError(ValueError):
    """The stream framing was valid but an SSE data payload was unusable."""


@dataclass(frozen=True)
class StreamReadResult:
    payload: dict[str, Any]
    response_mode: Literal["sse", "json"]
    event_count: int
    first_event_elapsed_s: float | None
    completed_by_done: bool

    def generation_params(self) -> dict[str, Any]:
        return {
            "stream_requested": True,
            "stream_response_mode": self.response_mode,
            "stream_event_count": self.event_count,
            "stream_first_event_elapsed_s": self.first_event_elapsed_s,
            "stream_completed_by_done": self.completed_by_done,
        }


def is_sse_response(response: httpx.Response) -> bool:
    content_type = response.headers.get("Content-Type", "").split(";", 1)[0].strip().lower()
    return content_type == "text/event-stream"


async def read_response_excerpt(response: httpx.Response, *, limit: int = 4096) -> str:
    collected = bytearray()
    async for chunk in response.aiter_bytes():
        remaining = limit - len(collected)
        if remaining <= 0:
            break
        collected.extend(chunk[:remaining])
        if len(collected) >= limit:
            break
    return bytes(collected).decode("utf-8", errors="replace")


async def read_streaming_response(response: httpx.Response) -> StreamReadResult:
    started_at = monotonic()
    if not is_sse_response(response):
        body = await response.aread()
        try:
            payload = json.loads(body)
        except (UnicodeDecodeError, json.JSONDecodeError) as exc:
            excerpt = body[:500].decode("utf-8", errors="replace")
            raise SSEPayloadError(f"Non-JSON streaming response: {excerpt}") from exc
        if not isinstance(payload, dict):
            raise SSEPayloadError("Expected JSON object response")
        return StreamReadResult(payload, "json", 0, None, False)

    events: list[dict[str, Any]] = []
    event_count = 0
    first_event_elapsed_s: float | None = None
    completed_by_done = False
    source = EventSource(response)
    async for event in source.aiter_sse():
        data = event.data.strip()
        if not data:
            continue
        if data == "[DONE]":
            completed_by_done = True
            break
        if first_event_elapsed_s is None:
            first_event_elapsed_s = round(monotonic() - started_at, 3)
        try:
            payload = json.loads(data)
        except json.JSONDecodeError as exc:
            raise SSEPayloadError(f"Malformed SSE JSON: {data[:500]}") from exc
        if not isinstance(payload, dict):
            raise SSEPayloadError(f"Expected SSE JSON object: {data[:500]}")
        events.append(payload)
        event_count += 1

    return StreamReadResult(
        payload={"_sse_events": events},
        response_mode="sse",
        event_count=event_count,
        first_event_elapsed_s=first_event_elapsed_s,
        completed_by_done=completed_by_done,
    )
```

- [ ] **Step 5: 安装 editable dev 依赖并运行测试**

Run:

```powershell
python -m pip install -e ".[dev]"
python -m pytest tests/test_sse_transport.py -q
```

Expected: all new transport tests pass.

- [ ] **Step 6: 提交 Task 1**

```powershell
git add pyproject.toml ai_image_gateway/sse_transport.py tests/test_sse_transport.py
git commit -m "feat: add incremental sse transport"
```

---

### Task 2: 将透明流式传输接入 chat image provider

**Files:**
- Modify: `tools/ai-image-gateway/ai_image_gateway/providers/openai_compatible.py`
- Modify: `tools/ai-image-gateway/tests/test_openai_compatible_provider.py`

**Interfaces:**
- Consumes: `read_streaming_response()`、`read_response_excerpt()` 和 `StreamReadResult.generation_params()`。
- Produces: `_post_json()` 在 `payload["stream"] is True` 时增量读取，并通过保留键 `_ai_image_gateway_transport` 把证据传给 generation params。

- [ ] **Step 1: 编写 provider 流式失败测试**

在 `tests/test_openai_compatible_provider.py` 加入基于 `httpx.MockTransport` 的助手：

```python
class StreamingBytes(httpx.AsyncByteStream):
    def __init__(self, chunks: list[bytes]) -> None:
        self._chunks = chunks

    async def __aiter__(self):
        for chunk in self._chunks:
            yield chunk


def _streaming_transport(handler):
    return httpx.MockTransport(handler)
```

加入测试：

```python
@pytest.mark.asyncio
async def test_chat_generate_streams_sse_and_records_transport_evidence():
    requests: list[dict] = []

    async def handler(request: httpx.Request) -> httpx.Response:
        requests.append(json.loads(request.content))
        event = {
            "choices": [{"delta": {"content": f"![image](data:image/png;base64,{_png_b64()})"}}]
        }
        body = f"data: {json.dumps(event)}\n\ndata: [DONE]\n\n".encode()
        return httpx.Response(
            200,
            headers={"Content-Type": "text/event-stream"},
            stream=StreamingBytes([body]),
        )

    provider = OpenAIChatImageProvider(ProviderConfig(
        auth={"api_key": "test-key"},
        settings={
            "base_url": "https://proxy.example.com/v1",
            "model": "gemini-3.1-flash-image",
            "stream": True,
        },
    ))
    await provider.initialize()
    await provider._client.aclose()
    provider._client = httpx.AsyncClient(transport=_streaming_transport(handler))

    results = await provider.generate(GenerateRequest(prompt="stream icon"))

    assert requests[0]["stream"] is True
    assert len(results) == 1
    assert results[0].generation_params["stream_requested"] is True
    assert results[0].generation_params["stream_response_mode"] == "sse"
    assert results[0].generation_params["stream_event_count"] == 1
    assert results[0].generation_params["stream_completed_by_done"] is True
    await provider.close()


@pytest.mark.asyncio
async def test_chat_i2i_request_extra_disables_provider_stream_default():
    provider = OpenAIChatImageProvider(ProviderConfig(
        auth={"api_key": "test-key"},
        settings={"base_url": "https://proxy.example.com/v1", "model": "model", "stream": True},
    ))
    await provider.initialize()
    response = _mock_response({"data": [{"b64_json": _png_b64()}]})
    with patch.object(provider._client, "post", new_callable=AsyncMock, return_value=response) as post:
        results = await provider.image_to_image(ImageToImageRequest(
            images=[_png_bytes()],
            prompt="edit",
            extra={"stream": False},
        ))
    assert len(results) == 1
    assert post.await_count == 1


@pytest.mark.asyncio
async def test_chat_stream_true_accepts_plain_json_without_second_request():
    calls = 0

    async def handler(request: httpx.Request) -> httpx.Response:
        nonlocal calls
        calls += 1
        return httpx.Response(
            200,
            headers={"Content-Type": "application/json"},
            json={"data": [{"b64_json": _png_b64()}]},
        )

    provider = OpenAIChatImageProvider(ProviderConfig(
        auth={"api_key": "test-key"},
        settings={
            "base_url": "https://proxy.example.com/v1",
            "model": "gemini-3.1-flash-image",
            "stream": True,
        },
    ))
    await provider.initialize()
    await provider._client.aclose()
    provider._client = httpx.AsyncClient(transport=_streaming_transport(handler))

    results = await provider.generate(GenerateRequest(prompt="stream icon"))

    assert calls == 1
    assert len(results) == 1
    assert results[0].generation_params["stream_requested"] is True
    assert results[0].generation_params["stream_response_mode"] == "json"
    await provider.close()
```

- [ ] **Step 2: 运行目标测试确认失败**

Run:

```powershell
python -m pytest tests/test_openai_compatible_provider.py -k "stream" -q
```

Expected: tests fail because provider still calls buffered `post()` and does not record stream evidence.

- [ ] **Step 3: 接入流式传输**

在 `openai_compatible.py` 添加：

```python
from ..sse_transport import read_response_excerpt, read_streaming_response

_TRANSPORT_META_KEY = "_ai_image_gateway_transport"


def _pop_transport_meta(response: dict[str, Any]) -> dict[str, Any]:
    value = response.pop(_TRANSPORT_META_KEY, None)
    return dict(value) if isinstance(value, dict) else {}
```

把 `_post_json()` 的请求部分改为：

```python
if payload.get("stream") is True:
    async with self._client.stream("POST", url, json=payload, headers=headers) as response:
        if response.status_code == 429:
            retry_after = float(response.headers.get("Retry-After", "5"))
            await response.aclose()
            raise RateLimitError(self.name, retry_after=retry_after)
        if response.status_code >= 400:
            excerpt = await read_response_excerpt(response)
            raise ProviderError(self.name, f"HTTP {response.status_code}: {excerpt}")
        result = await read_streaming_response(response)
        response_payload = dict(result.payload)
        response_payload[_TRANSPORT_META_KEY] = result.generation_params()
        return response_payload

response = await self._client.post(url, json=payload, headers=headers)
```

在 chat `generate()` 和 `image_to_image()` 中：

```python
response = await self._post_json(payload)
transport_meta = _pop_transport_meta(response)
generation_params = {
    "api_surface": "chat/completions",
    "model": model,
    "prompt": request.prompt,
    "negative_prompt": request.negative_prompt,
    "width": request.width,
    "height": request.height,
    "count": request.count,
    **transport_meta,
}
```

`image_to_image()` 使用同一合并方式，并保留它已有的模式与参考图字段：

```python
response = await self._post_json(payload)
transport_meta = _pop_transport_meta(response)
generation_params = {
    "api_surface": "chat/completions",
    "mode": "image_to_image",
    "model": model,
    "prompt": request.prompt,
    "negative_prompt": request.negative_prompt,
    "width": request.width,
    "height": request.height,
    "count": request.count,
    "reference_image_count": len(request.images),
    **transport_meta,
}
```

非流式 generation params 不新增流式键。

- [ ] **Step 4: 运行 provider 流式测试**

Run:

```powershell
python -m pytest tests/test_openai_compatible_provider.py -k "stream" -q
```

Expected: stream tests pass; request `extra` override and JSON fallback each只发送一次请求。

- [ ] **Step 5: 运行 provider 全文件回归**

Run:

```powershell
python -m pytest tests/test_openai_compatible_provider.py -q
```

Expected: existing Images API and chat provider tests all pass.

- [ ] **Step 6: 提交 Task 2**

```powershell
git add ai_image_gateway/providers/openai_compatible.py tests/test_openai_compatible_provider.py
git commit -m "feat: stream chat image responses transparently"
```

---

### Task 3: 补齐错误、断流和无重复请求回归

**Files:**
- Modify: `tools/ai-image-gateway/tests/test_sse_transport.py`
- Modify: `tools/ai-image-gateway/tests/test_openai_compatible_provider.py`
- Modify: `tools/ai-image-gateway/ai_image_gateway/sse_transport.py`
- Modify: `tools/ai-image-gateway/ai_image_gateway/providers/openai_compatible.py`

**Interfaces:**
- Consumes: Task 1/2 的透明流式路径。
- Produces: malformed event、provider error、HTTP error excerpt、transport failure和 clean EOF 的稳定语义。

- [ ] **Step 1: 增加 provider error 与敏感内容截断测试**

新增：

```python
@pytest.mark.asyncio
async def test_streaming_provider_error_is_preserved():
    event = {"error": {"message": "token pool exhausted"}}
    # return text/event-stream containing event and [DONE]
    with pytest.raises(ProviderError, match="token pool exhausted"):
        await provider.generate(GenerateRequest(prompt="icon", extra={"stream": True}))


@pytest.mark.asyncio
async def test_streaming_http_error_body_is_bounded():
    # return 524 with 6000-byte body
    with pytest.raises(ProviderError) as exc_info:
        await provider.generate(GenerateRequest(prompt="icon", extra={"stream": True}))
    assert "HTTP 524" in str(exc_info.value)
    assert len(exc_info.value.detail) < 4300


@pytest.mark.asyncio
async def test_streaming_disconnect_does_not_retry_as_buffered():
    stream_calls = 0
    buffered_calls = 0
    # streaming transport raises httpx.ReadError after one partial event
    # patch provider._client.post to count accidental fallback
    with pytest.raises(ProviderError, match="HTTP transport error"):
        await provider.generate(GenerateRequest(prompt="icon", extra={"stream": True}))
    assert stream_calls >= 1
    assert buffered_calls == 0
```

- [ ] **Step 2: 运行新增测试确认当前缺口**

Run:

```powershell
python -m pytest tests/test_sse_transport.py tests/test_openai_compatible_provider.py -k "streaming_provider_error or bounded or disconnect" -q
```

Expected: any missing error mapping or unbounded response behavior fails explicitly.

- [ ] **Step 3: 最小修复错误映射**

要求：

- `SSEPayloadError` 在 provider 层转换为 `ProviderError(self.name, str(exc), exc)`。
- `httpx.TimeoutException` 和 `httpx.HTTPError` 保留现有 retry 分支。
- 流式 429 使用现有 `RateLimitError`。
- 任何 streaming exception 不调用 buffered `post()`。
- `_extract_provider_error_message()` 继续从 `_sse_events` 中返回服务器 message。

实现形态：

```python
except SSEPayloadError as exc:
    last_error = ProviderError(self.name, str(exc), exc)
    break
except httpx.TimeoutException as exc:
    last_error = ProviderError(self.name, f"Timeout: {exc}", exc)
    if attempt >= self._retry:
        break
    import asyncio
    await asyncio.sleep(2)
except httpx.HTTPError as exc:
    last_error = ProviderError(self.name, f"HTTP transport error: {exc}", exc)
    if attempt >= self._retry:
        break
    import asyncio
    await asyncio.sleep(2)
```

- [ ] **Step 4: 运行目标和全量测试**

Run:

```powershell
python -m pytest tests/test_sse_transport.py tests/test_openai_compatible_provider.py -q
python -m pytest tests -q
```

Expected: target tests and gateway full suite pass.

- [ ] **Step 5: 提交 Task 3**

```powershell
git add ai_image_gateway/sse_transport.py ai_image_gateway/providers/openai_compatible.py tests/test_sse_transport.py tests/test_openai_compatible_provider.py
git commit -m "test: harden streaming image response failures"
```

---

### Task 4: 增加可重复的真实流式 smoke

**Files:**
- Create: `tools/ai-image-gateway/examples/smoke_streaming_chat_image.py`
- Create: `tools/ai-image-gateway/tests/test_streaming_smoke_cli.py`

**Interfaces:**
- Consumes: `ImageService`、`GenerateRequest`、`ImageToImageRequest`、`resolve_image_inputs()`。
- Produces: 命令行 smoke、最终图片、`summary.json` 和非敏感流式证据。

- [ ] **Step 1: 编写 CLI 参数与 summary 失败测试**

测试核心纯函数：

```python
from examples.smoke_streaming_chat_image import build_summary


def test_build_summary_exposes_stream_evidence_without_prompt_or_base64():
    summary = build_summary(
        mode="image_to_image",
        elapsed_s=130.0,
        result=_fake_image_result({
            "stream_requested": True,
            "stream_response_mode": "sse",
            "stream_event_count": 4,
            "stream_first_event_elapsed_s": 2.5,
            "stream_completed_by_done": True,
        }),
        output_path=r"C:\Users\WhiteSheep\AppData\Local\Temp\P3StreamingImageSmoke\20260718_150000\result.png",
    )
    assert summary["stream_first_event_elapsed_s"] == 2.5
    assert "prompt" not in summary
    assert "b64_json" not in json.dumps(summary)
```

- [ ] **Step 2: 运行测试确认失败**

```powershell
python -m pytest tests/test_streaming_smoke_cli.py -q
```

Expected: module does not exist.

- [ ] **Step 3: 实现 smoke CLI**

参数：

```text
--config PATH
--provider NAME                 default gemini_chat_image
--mode generate|image_to_image
--prompt TEXT
--image PATH                   repeatable; required for image_to_image
--width INT                    default 320
--height INT                   default 180
--output-dir PATH              default system temp/P3StreamingImageSmoke/<stamp>
```

请求必须显式包含：

```python
extra={"stream": True}
```

输出 `summary.json`：

```json
{
  "status": "ok",
  "mode": "image_to_image",
  "provider": "gemini_chat_image",
  "model": "gemini-3.1-flash-image",
  "elapsed_s": 123.4,
  "bytes": 123456,
  "output_path": "C:\\Users\\WhiteSheep\\AppData\\Local\\Temp\\P3StreamingImageSmoke\\20260718_150000\\result.png",
  "stream_requested": true,
  "stream_response_mode": "sse",
  "stream_event_count": 3,
  "stream_first_event_elapsed_s": 1.2,
  "stream_completed_by_done": true
}
```

失败时写入 `status=fail`、错误类型、截断错误和 `validation_limited`，不写密钥或图片 data URL。

- [ ] **Step 4: 运行 CLI 单元测试和 dry import**

```powershell
python -m pytest tests/test_streaming_smoke_cli.py -q
python examples/smoke_streaming_chat_image.py --help
```

Expected: tests pass and help lists both modes.

- [ ] **Step 5: 提交 Task 4**

```powershell
git add examples/smoke_streaming_chat_image.py tests/test_streaming_smoke_cli.py
git commit -m "feat: add streaming chat image smoke"
```

---

### Task 5: 文档合并、真实双图验收和父仓库回写

**Files:**
- Modify: `tools/ai-image-gateway/README.md`
- Modify: `tools/ai-image-gateway/docs/openai_compatible_relay_integration.md`
- Modify: `agent_status/art.md` only if real stream evidence changes current GIF/provider limitation
- Modify: `docs/specs/2026-07-18-ai-image-gateway-transparent-streaming-design.md`
- Modify: `DOCS_INDEX.md`
- Modify: `docs_index.json`
- Modify: parent submodule pointer `tools/ai-image-gateway`

**Interfaces:**
- Consumes: committed submodule implementation and smoke CLI。
- Produces: current fact documentation、真实 Gemini stream evidence、scoped parent commit。

- [ ] **Step 1: 保护子模块已有重叠文档改动**

在子模块记录当前状态并只暂存重叠文件到临时 stash：

```powershell
git status --short
git stash push -m "codex-temp-stream-doc-overlap" -- README.md docs/openai_compatible_relay_integration.md
git status --short
```

确认其余用户修改的示例和模板仍留在工作区。

- [ ] **Step 2: 在干净 HEAD 文档上写入流式事实**

README 最小示例：

```yaml
gemini_chat_image:
  enabled: true
  auth:
    api_key: ${AI_IMAGE_PROXY_KEY}
  settings:
    base_url: https://proxy.example.com/v1
    endpoint: /chat/completions
    model: gemini-3.1-flash-image
    stream: true
```

中转事实文档必须明确：

- `stream=true` 使用真正的增量 SSE。
- 普通 JSON 在同连接 fallback。
- 注释心跳由 `httpx-sse` 消费但不统计。
- 不自动重发非流式请求。
- 证据字段和 smoke 命令。

- [ ] **Step 3: 运行子模块静态与自动测试**

```powershell
python -m pytest tests/test_sse_transport.py tests/test_openai_compatible_provider.py tests/test_streaming_smoke_cli.py -q
python -m pytest tests -q
python -m compileall -q ai_image_gateway examples/smoke_streaming_chat_image.py
git diff --check
```

Expected: all tests pass, compile passes, no whitespace errors.

- [ ] **Step 4: 执行真实 Gemini 文生图流式 smoke**

```powershell
python examples/smoke_streaming_chat_image.py `
  --config config.local.yaml `
  --provider gemini_chat_image `
  --mode generate `
  --prompt "Generate one tiny blue crystal dot on a white background. No text." `
  --width 64 `
  --height 64
```

验收：生成图片可解码，summary 记录 stream response mode、首事件耗时和总耗时。

- [ ] **Step 5: 执行当前双图图生图流式 smoke**

```powershell
python examples/smoke_streaming_chat_image.py `
  --config config.local.yaml `
  --provider gemini_chat_image `
  --mode image_to_image `
  --prompt "Replace the character in the second image with the character from the first image. Preserve the second image background, pose, camera and composition. Output one 320x180 image." `
  --image "C:\Users\WhiteSheep\Pictures\QQ截图20241117000123.png" `
  --image "C:\Users\WhiteSheep\AppData\Local\Temp\p3-gif-illya-preview-retry3-20260718_134806\gif_character_replace\gif_replace_20260718_134808_25695b4b\timeline\frames_original\frame_0000.png" `
  --width 320 `
  --height 180
```

第二个 `--image` 是当前 GIF Run 已拆出的动作帧 PNG；不得把 GIF 文件本身作为 image input。

验收：

- 如果 120 秒前收到事件并最终成功，记录真实通过。
- 如果服务端直到最终图片才发送首事件，记录 `validation_limited:upstream_no_early_sse_event`。
- 如果仍出现 524，检查 summary 和服务器日志，不能声称客户端流式解决了代理超时。

- [ ] **Step 6: 提交子模块文档并恢复用户改动**

```powershell
git add README.md docs/openai_compatible_relay_integration.md
git commit -m "docs: document transparent chat image streaming"
git stash pop stash@{0}
```

如果 stash pop 冲突，只合并本任务流式段与用户原改动；恢复后解除暂存，让用户改动保持未提交。确认 `git diff --name-only --diff-filter=U` 为空。

- [ ] **Step 7: 回写 P3 状态和规格事实**

只有真实 smoke 改变现有边界时，增量更新 `agent_status/art.md` 的 GIF/provider 段：

- 客户端透明 SSE 已实现。
- 自动测试证据。
- 真实首事件时间和最终结果，或准确的 validation limitation。

把设计规格 `status` 改为 `implemented`，同步最终实际文件和验收结论。

- [ ] **Step 8: 刷新父仓库索引并验证**

```powershell
.\tools\docs\Generate-DocsIndex.ps1
.\tools\docs\Validate-Docs.ps1
.\tools\agent\Invoke-AgentHealthCheck.ps1 -Strict
git diff --check
git status --short
```

严格健康检查若因既有用户脏文件告警，记录具体范围；不得清理或提交无关改动。

- [ ] **Step 9: 提交父仓库范围**

只暂存：

```text
tools/ai-image-gateway
docs/specs/2026-07-18-ai-image-gateway-transparent-streaming-design.md
agent_status/art.md（仅有本任务回写时）
DOCS_INDEX.md
docs_index.json
```

提交：

```powershell
git commit -m "feat: integrate transparent image gateway streaming"
```

不得暂存当前工作区其他美术、配置、生成物或 skill 改动。

---

## Final Review Checklist

- [ ] `stream=true` 通过 `AsyncClient.stream()` 增量读取。
- [ ] `stream=false` 保持现有 buffered 行为。
- [ ] `request.extra` 可以覆盖 provider 默认 stream。
- [ ] SSE 多行 data、`[DONE]`、clean EOF、malformed JSON、provider error 和 transport disconnect 有测试。
- [ ] 普通 JSON fallback 不发送第二次请求。
- [ ] generate 和 image-to-image 都返回现有结果模型。
- [ ] generation params 包含流式证据且没有密钥/Base64。
- [ ] 子模块目标测试和全量测试通过。
- [ ] 真实 Gemini 文生图和双图 smoke 有证据或准确 limitation。
- [ ] 子模块原有用户改动已恢复且未被提交。
- [ ] 父仓库只提交本任务文件和子模块指针。
