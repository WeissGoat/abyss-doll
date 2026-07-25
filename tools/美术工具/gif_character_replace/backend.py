"""Adapters for single-image gateway calls used by GIF replacement."""

from __future__ import annotations

import asyncio
import sys
from dataclasses import dataclass, replace
from io import BytesIO
from pathlib import Path
from typing import Any, Awaitable, Callable, Protocol, Sequence

from PIL import Image


GATEWAY_ROOT = Path(__file__).resolve().parents[2] / "ai-image-gateway"
if str(GATEWAY_ROOT) not in sys.path:
    sys.path.insert(0, str(GATEWAY_ROOT))

from ai_image_gateway import ImageService  # noqa: E402
from ai_image_gateway.schema import (  # noqa: E402
    BatchResult,
    GenerateRequest,
    ImageToImageRequest,
    ImageResult,
)


TRANSIENT_ERROR_CODES = (
    "429",
    "502",
    "503",
    "504",
    "524",
    "incomplete chunked read",
    "peer closed connection",
)
TERMINAL_ERROR_CODES = ("401", "403")


class BackendCallError(RuntimeError):
    """Raised when an image gateway call cannot produce one image."""

    def __init__(self, errors: Sequence[str], attempts: int = 1) -> None:
        self.errors = tuple(errors)
        self.attempts = attempts
        super().__init__("image backend call failed: " + " | ".join(self.errors))


@dataclass(frozen=True)
class GeneratedImage:
    image_bytes: bytes
    provider: str
    model: str
    seed: int | None
    generation_params: dict[str, Any]
    cost: float


class ImageBackend(Protocol):
    async def generate_anchor(self, prompt: str, width: int, height: int) -> GeneratedImage:
        raise NotImplementedError

    async def replace_frame(
        self,
        identity_images: Sequence[bytes],
        frame: bytes,
        prompt: str,
        width: int,
        height: int,
    ) -> GeneratedImage:
        raise NotImplementedError


def _generated_image(result: ImageResult) -> GeneratedImage:
    return GeneratedImage(
        image_bytes=result.image_bytes,
        provider=result.provider_name,
        model=result.model_name,
        seed=result.seed,
        generation_params=dict(result.generation_params),
        cost=float(result.cost),
    )


def _contains_code(errors: Sequence[str], codes: Sequence[str]) -> bool:
    return any(code in error for error in errors for code in codes)


def _normalize_frame_image(
    generated: GeneratedImage, width: int, height: int
) -> GeneratedImage:
    with Image.open(BytesIO(generated.image_bytes)) as image:
        image.load()
        provider_size = image.size
        if provider_size == (width, height):
            normalized_bytes = generated.image_bytes
        else:
            has_alpha = "A" in image.getbands() or "transparency" in image.info
            normalized = image.convert("RGBA" if has_alpha else "RGB").resize(
                (width, height), Image.Resampling.LANCZOS
            )
            output = BytesIO()
            normalized.save(output, format="PNG")
            normalized_bytes = output.getvalue()
    params = dict(generated.generation_params)
    params["provider_output_size"] = list(provider_size)
    params["normalized_output_size"] = [width, height]
    params["output_resized"] = provider_size != (width, height)
    return replace(generated, image_bytes=normalized_bytes, generation_params=params)


async def call_with_retry(
    operation: Callable[[], Awaitable[BatchResult]],
    retry_count: int,
    delay_seconds: float,
) -> GeneratedImage:
    """Call the gateway serially, retrying only documented transient failures."""

    if retry_count < 1:
        raise ValueError("retry_count must be at least 1")
    collected_errors: list[str] = []
    for attempt in range(retry_count):
        batch = await operation()
        if batch.results:
            if len(batch.results) != 1:
                raise BackendCallError(
                    [f"expected exactly one image, received {len(batch.results)}"]
                )
            generated = _generated_image(batch.results[0])
            params = dict(generated.generation_params)
            params["gateway_attempt_count"] = attempt + 1
            params["gateway_retry_errors"] = list(collected_errors)
            return replace(generated, generation_params=params)

        errors = list(batch.errors) or ["provider returned no image and no error"]
        collected_errors.extend(errors)
        if _contains_code(errors, TERMINAL_ERROR_CODES):
            raise BackendCallError(collected_errors, attempt + 1)
        transient = _contains_code(errors, TRANSIENT_ERROR_CODES)
        if not transient or attempt + 1 >= retry_count:
            raise BackendCallError(collected_errors, attempt + 1)
        if delay_seconds > 0:
            await asyncio.sleep(delay_seconds * (attempt + 1))

    raise BackendCallError(collected_errors, retry_count)


class GatewayImageBackend:
    """Small facade that keeps P3 workflow logic outside the gateway submodule."""

    def __init__(
        self,
        service: Any | None = None,
        *,
        config_path: str | Path | None = None,
        retry_count: int = 3,
        delay_seconds: float = 2.0,
        anchor_provider: str = "openai_images",
        frame_provider: str = "gemini_chat_image",
    ) -> None:
        self.service = service
        self.config_path = config_path
        self.retry_count = retry_count
        self.delay_seconds = delay_seconds
        self.anchor_provider = anchor_provider
        self.frame_provider = frame_provider
        self._owns_service = service is None

    async def __aenter__(self) -> "GatewayImageBackend":
        if self.service is None:
            self.service = ImageService(self.config_path)
            await self.service.__aenter__()
        return self

    async def __aexit__(self, *args: object) -> None:
        if self._owns_service and self.service is not None:
            await self.service.__aexit__(*args)
            self.service = None

    def _require_service(self) -> Any:
        if self.service is None:
            raise RuntimeError("GatewayImageBackend must be used as an async context manager")
        return self.service

    async def generate_anchor(self, prompt: str, width: int, height: int) -> GeneratedImage:
        service = self._require_service()

        async def operation() -> BatchResult:
            request = GenerateRequest(
                prompt=prompt,
                width=width,
                height=height,
                count=1,
                provider=self.anchor_provider,
            )
            return await service.generate(request)

        return await call_with_retry(operation, self.retry_count, self.delay_seconds)

    async def replace_frame(
        self,
        identity_images: Sequence[bytes],
        frame: bytes,
        prompt: str,
        width: int,
        height: int,
    ) -> GeneratedImage:
        service = self._require_service()

        async def operation() -> BatchResult:
            request = ImageToImageRequest(
                images=[*identity_images, frame],
                prompt=prompt,
                width=width,
                height=height,
                count=1,
                provider=self.frame_provider,
                extra={"stream": True},
            )
            return await service.image_to_image(request)

        generated = await call_with_retry(operation, self.retry_count, self.delay_seconds)
        return _normalize_frame_image(generated, width, height)
