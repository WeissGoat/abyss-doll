from __future__ import annotations

import sys
import unittest
from pathlib import Path


TOOLS_DIR = Path(__file__).resolve().parents[1]
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

from ai_image_gateway.schema import BatchResult, ImageResult  # noqa: E402
from gif_character_replace.backend import (  # noqa: E402
    BackendCallError,
    GatewayImageBackend,
)


def fake_image_result(provider: str = "openai_images") -> ImageResult:
    return ImageResult(
        image_bytes=b"png-bytes",
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
        self.assertEqual(results[0].generation_params, {"quality": "test"})

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
