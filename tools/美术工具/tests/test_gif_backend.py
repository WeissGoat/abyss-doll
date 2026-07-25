from __future__ import annotations

import sys
import unittest
from io import BytesIO
from pathlib import Path

from PIL import Image


TOOLS_DIR = Path(__file__).resolve().parents[1]
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

from ai_image_gateway.schema import BatchResult, ImageResult  # noqa: E402
from gif_character_replace.backend import (  # noqa: E402
    BackendCallError,
    GatewayImageBackend,
)


def fake_png(width: int = 64, height: int = 80) -> bytes:
    output = BytesIO()
    Image.new("RGB", (width, height), (40, 80, 120)).save(output, format="PNG")
    return output.getvalue()


def fake_image_result(
    provider: str = "openai_images", *, image_bytes: bytes | None = None
) -> ImageResult:
    return ImageResult(
        image_bytes=image_bytes or fake_png(),
        seed=123,
        provider_name=provider,
        model_name="fake-model",
        generation_params={"quality": "test"},
        cost=0.25,
    )


class FakeService:
    def __init__(self, responses: list[BatchResult]) -> None:
        self.responses = list(responses)
        self.generate_requests = []
        self.image_to_image_requests = []

    async def generate(self, request):
        self.generate_requests.append(request)
        return self.responses.pop(0)

    async def image_to_image(self, request):
        self.image_to_image_requests.append(request)
        return self.responses.pop(0)


class GatewayImageBackendTests(unittest.IsolatedAsyncioTestCase):
    async def test_three_anchor_calls_are_separate_count_one_requests(self) -> None:
        service = FakeService(
            [BatchResult(results=[fake_image_result()]) for _ in range(3)]
        )
        backend = GatewayImageBackend(service, delay_seconds=0)

        results = [await backend.generate_anchor("prompt", 512, 768) for _ in range(3)]

        self.assertEqual(len(service.generate_requests), 3)
        self.assertTrue(all(request.count == 1 for request in service.generate_requests))
        self.assertTrue(
            all(request.provider == "openai_images" for request in service.generate_requests)
        )
        self.assertEqual(results[0].provider, "openai_images")
        self.assertEqual(results[0].model, "fake-model")
        self.assertEqual(results[0].generation_params["quality"], "test")
        self.assertEqual(results[0].generation_params["gateway_attempt_count"], 1)
        self.assertEqual(results[0].generation_params["gateway_retry_errors"], [])

    async def test_frame_request_orders_identity_images_before_current_frame(self) -> None:
        service = FakeService(
            [BatchResult(results=[fake_image_result("gemini_chat_image")])]
        )
        backend = GatewayImageBackend(service, delay_seconds=0)

        result = await backend.replace_frame(
            [b"identity-a", b"identity-b"], b"current-source-frame", "replace", 64, 80
        )

        request = service.image_to_image_requests[0]
        self.assertEqual(
            request.images, [b"identity-a", b"identity-b", b"current-source-frame"]
        )
        self.assertEqual(request.count, 1)
        self.assertEqual(request.provider, "gemini_chat_image")
        self.assertEqual((request.width, request.height), (64, 80))
        self.assertEqual(result.provider, "gemini_chat_image")

    async def test_frame_request_explicitly_enables_streaming(self) -> None:
        service = FakeService(
            [BatchResult(results=[fake_image_result("gemini_chat_image")])]
        )
        backend = GatewayImageBackend(service, delay_seconds=0)

        await backend.replace_frame([b"identity"], b"frame", "replace", 64, 80)

        self.assertIs(service.image_to_image_requests[0].extra["stream"], True)

    async def test_frame_response_is_normalized_to_requested_canvas(self) -> None:
        service = FakeService(
            [
                BatchResult(
                    results=[
                        fake_image_result(
                            "gemini_chat_image", image_bytes=fake_png(1376, 768)
                        )
                    ]
                )
            ]
        )
        backend = GatewayImageBackend(service, delay_seconds=0)

        result = await backend.replace_frame(
            [b"identity"], b"frame", "replace", 320, 180
        )

        with Image.open(BytesIO(result.image_bytes)) as image:
            self.assertEqual(image.size, (320, 180))
        self.assertEqual(result.generation_params["provider_output_size"], [1376, 768])
        self.assertEqual(result.generation_params["normalized_output_size"], [320, 180])
        self.assertIs(result.generation_params["output_resized"], True)

    async def test_transient_error_is_retried(self) -> None:
        service = FakeService(
            [
                BatchResult(errors=["HTTP 524 timeout"]),
                BatchResult(results=[fake_image_result()]),
            ]
        )
        backend = GatewayImageBackend(service, retry_count=3, delay_seconds=0)

        result = await backend.generate_anchor("prompt", 512, 768)

        self.assertEqual(len(service.generate_requests), 2)
        self.assertEqual(result.provider, "openai_images")
        self.assertEqual(result.generation_params["gateway_attempt_count"], 2)
        self.assertEqual(result.generation_params["gateway_retry_errors"], ["HTTP 524 timeout"])

    async def test_incomplete_chunked_read_is_retried(self) -> None:
        service = FakeService(
            [
                BatchResult(errors=["HTTP transport error: incomplete chunked read"]),
                BatchResult(results=[fake_image_result("gemini_chat_image")]),
            ]
        )
        backend = GatewayImageBackend(service, retry_count=3, delay_seconds=0)

        result = await backend.replace_frame(
            [b"identity"], b"frame", "replace", 64, 80
        )

        self.assertEqual(len(service.image_to_image_requests), 2)
        self.assertEqual(result.generation_params["gateway_attempt_count"], 2)
        self.assertEqual(
            result.generation_params["gateway_retry_errors"],
            ["HTTP transport error: incomplete chunked read"],
        )

    async def test_auth_errors_stop_after_one_call(self) -> None:
        for error in ("HTTP 401 invalid token", "HTTP 403 forbidden"):
            with self.subTest(error=error):
                service = FakeService([BatchResult(errors=[error])])
                backend = GatewayImageBackend(service, retry_count=3, delay_seconds=0)
                with self.assertRaises(BackendCallError):
                    await backend.generate_anchor("prompt", 512, 768)
                self.assertEqual(len(service.generate_requests), 1)

    async def test_exhausted_transient_errors_include_all_messages(self) -> None:
        service = FakeService(
            [
                BatchResult(errors=["HTTP 502 first"]),
                BatchResult(errors=["HTTP 503 second"]),
                BatchResult(errors=["HTTP 504 third"]),
            ]
        )
        backend = GatewayImageBackend(service, retry_count=3, delay_seconds=0)

        with self.assertRaises(BackendCallError) as raised:
            await backend.generate_anchor("prompt", 512, 768)

        self.assertEqual(len(service.generate_requests), 3)
        message = str(raised.exception)
        self.assertIn("first", message)
        self.assertIn("second", message)
        self.assertIn("third", message)


if __name__ == "__main__":
    unittest.main()
