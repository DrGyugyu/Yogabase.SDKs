import asyncio
import time
import unittest

import httpx

from yogabase_client import (
    AsyncDeliveryClient,
    DeliveryPollingTimeoutError,
    YogabaseDeliveryError,
)


def operation(state: str) -> dict:
    return {
        "operationId": "operation-1",
        "state": state,
        "kind": "admit",
        "revision": 1,
        "updatedAt": "2026-09-01T00:00:00Z",
    }


class DeliveryClientTests(unittest.IsolatedAsyncioTestCase):
    async def test_delivery_host_must_be_an_origin(self) -> None:
        with self.assertRaisesRegex(ValueError, "origin-only"):
            AsyncDeliveryClient(
                delivery_host="https://delivery.example.test/not-an-origin",
                access_token="token",
            )
        with self.assertRaisesRegex(ValueError, "HTTPS"):
            AsyncDeliveryClient(
                delivery_host="http://delivery.example.test",
                access_token="token",
            )

    async def test_preview_and_submit_keep_delivery_surface_and_headers_separate(self) -> None:
        requests: list[httpx.Request] = []

        def handler(request: httpx.Request) -> httpx.Response:
            requests.append(request)
            if request.url.path.endswith("/previews"):
                return httpx.Response(200, json={
                    "previewId": "preview-1", "previewDigest": "sha256:preview", "action": "create",
                    "expectedRevision": 0, "approvalRequired": False, "expiresAt": "2026-09-01T00:00:00Z",
                }, headers={"ETag": 'W/"7"'}, request=request)
            return httpx.Response(202, json=operation("Queued"), headers={"Location": "/operations/operation-1", "Retry-After": "3"}, request=request)

        async with AsyncDeliveryClient(delivery_host="https://delivery.example.test", access_token="token", http_client=httpx.AsyncClient(transport=httpx.MockTransport(handler))) as client:
            preview = await client.preview_environment("project", {"name": "dev", "templateId": "template", "templateVersion": 1}, if_match='W/"7"')
            submitted = await client.submit_environment("project", {"previewId": "preview-1", "previewDigest": "sha256:preview"}, idempotency_key="request-1")
        self.assertEqual(preview.data["action"], "create")
        self.assertEqual(preview.etag, 'W/"7"')
        self.assertEqual(submitted.data["operationId"], "operation-1")
        self.assertEqual(submitted.retry_after, 3)
        self.assertEqual(str(requests[0].url), "https://delivery.example.test/api/v1/delivery/projects/project/environments/previews")
        self.assertEqual(requests[0].headers["Authorization"], "Bearer token")
        self.assertEqual(requests[0].headers["If-Match"], 'W/"7"')
        self.assertEqual(requests[1].headers["Idempotency-Key"], "request-1")

    async def test_polling_is_bounded_and_cancellable(self) -> None:
        def handler(request: httpx.Request) -> httpx.Response:
            return httpx.Response(200, json=operation("Running"), request=request)

        transport = httpx.MockTransport(handler)
        async with AsyncDeliveryClient(delivery_host="https://delivery.example.test", access_token="token", http_client=httpx.AsyncClient(transport=transport)) as client:
            with self.assertRaises(DeliveryPollingTimeoutError):
                await client.wait_for_operation("project", "operation-1", max_attempts=1, interval=0, timeout=0.1)
            for state in ("AwaitingApproval", "Queued", "Dispatched", "Running"):
                pending_transport = httpx.MockTransport(lambda request, state=state: httpx.Response(200, json=operation(state), request=request))
                async with AsyncDeliveryClient(delivery_host="https://delivery.example.test", access_token="token", http_client=httpx.AsyncClient(transport=pending_transport)) as pending_client:
                    with self.assertRaises(DeliveryPollingTimeoutError):
                        await pending_client.wait_for_operation("project", "operation-1", max_attempts=1, interval=0, timeout=0.1)
            blocked_transport = httpx.MockTransport(lambda request: httpx.Response(200, json=operation("Blocked"), request=request))
            async with AsyncDeliveryClient(delivery_host="https://delivery.example.test", access_token="token", http_client=httpx.AsyncClient(transport=blocked_transport)) as blocked_client:
                self.assertEqual((await blocked_client.wait_for_operation("project", "operation-1", max_attempts=1, interval=0, timeout=0.1))["state"], "Blocked")
            cancel = asyncio.Event()
            cancel.set()
            with self.assertRaises(asyncio.CancelledError):
                await client.wait_for_operation("project", "operation-1", cancel_event=cancel)

        request_started = asyncio.Event()
        request_cancelled = asyncio.Event()

        async def cancellable_handler(request: httpx.Request) -> httpx.Response:
            request_started.set()
            try:
                await asyncio.sleep(60)
            except asyncio.CancelledError:
                request_cancelled.set()
                raise
            return httpx.Response(200, json=operation("Running"), request=request)

        async with AsyncDeliveryClient(delivery_host="https://delivery.example.test", access_token="token", http_client=httpx.AsyncClient(transport=httpx.MockTransport(cancellable_handler))) as cancellable_client:
            cancel = asyncio.Event()
            polling = asyncio.create_task(cancellable_client.wait_for_operation("project", "operation-1", cancel_event=cancel))
            await request_started.wait()
            cancel.set()
            with self.assertRaises(asyncio.CancelledError):
                await polling
            await request_cancelled.wait()

        requests = 0

        def slow_handler(request: httpx.Request) -> httpx.Response:
            nonlocal requests
            requests += 1
            time.sleep(0.02)
            return httpx.Response(200, json=operation("Running"), request=request)

        async with AsyncDeliveryClient(delivery_host="https://delivery.example.test", access_token="token", http_client=httpx.AsyncClient(transport=httpx.MockTransport(slow_handler))) as slow_client:
            with self.assertRaises(DeliveryPollingTimeoutError):
                await slow_client.wait_for_operation("project", "operation-1", max_attempts=10, interval=0, timeout=0.005)
        self.assertEqual(requests, 1)

    async def test_problem_response_is_typed(self) -> None:
        def handler(request: httpx.Request) -> httpx.Response:
            return httpx.Response(409, json={"title": "Conflict", "status": 409, "code": "stale-preview", "correlationId": "c-1"}, request=request)

        async with AsyncDeliveryClient(delivery_host="https://delivery.example.test", access_token="token", http_client=httpx.AsyncClient(transport=httpx.MockTransport(handler))) as client:
            with self.assertRaises(YogabaseDeliveryError) as raised:
                await client.get_operation("project", "operation-1")
        self.assertEqual(raised.exception.status, 409)
        self.assertEqual(raised.exception.correlation_id, "c-1")

    async def test_get_template_without_hosted_service_policy_digest(self) -> None:
        def handler(request: httpx.Request) -> httpx.Response:
            return httpx.Response(200, json={
                "id": "template-1",
                "name": "web",
                "version": 1,
                "state": "Active",
                "targetId": "target-1",
                "templateDigest": "sha256:aaaa",
                "revision": 1,
            }, request=request)

        async with AsyncDeliveryClient(delivery_host="https://delivery.example.test", access_token="token", http_client=httpx.AsyncClient(transport=httpx.MockTransport(handler))) as client:
            template = await client.get_template("project", "template-1")
        self.assertNotIn("hostedServicePolicyDigest", template.data)
        self.assertIsNone(template.data.get("hostedServicePolicyDigest"))

    async def test_get_template_surfaces_hosted_service_policy_digest(self) -> None:
        def handler(request: httpx.Request) -> httpx.Response:
            return httpx.Response(200, json={
                "id": "template-1",
                "name": "web",
                "version": 1,
                "state": "Active",
                "targetId": "target-1",
                "templateDigest": "sha256:aaaa",
                "hostedServicePolicyDigest": "sha256:bbbb",
                "revision": 1,
            }, request=request)

        async with AsyncDeliveryClient(delivery_host="https://delivery.example.test", access_token="token", http_client=httpx.AsyncClient(transport=httpx.MockTransport(handler))) as client:
            template = await client.get_template("project", "template-1")
        self.assertEqual(template.data["hostedServicePolicyDigest"], "sha256:bbbb")

    async def test_sse_stream_is_parsed_and_resumed(self) -> None:
        def handler(request: httpx.Request) -> httpx.Response:
            self.assertEqual(request.headers["Last-Event-ID"], "0")
            return httpx.Response(200, content=b': heartbeat\n\nid: 1\nevent: delivery-operation\ndata: {"operationId":"operation-1","state":"Succeeded","detail":"done","occurredAt":"2026-09-01T00:00:00Z"}\n\n', headers={"Content-Type": "text/event-stream"}, request=request)

        async with AsyncDeliveryClient(delivery_host="https://delivery.example.test", access_token="token", http_client=httpx.AsyncClient(transport=httpx.MockTransport(handler))) as client:
            events = [event async for event in client.stream_operation_events("project", last_event_id="0")]
        self.assertEqual(events[0]["id"], "1")
        self.assertEqual(events[0]["event"], "delivery-operation")
        self.assertEqual(events[0]["data"]["state"], "Succeeded")

    async def test_sse_field_values_lose_exactly_one_leading_space(self) -> None:
        # The resume id is opaque and must round-trip unchanged: the SSE grammar, the .NET client
        # and the Dart parser all remove exactly one U+0020 after the colon, and nothing else.
        body = (
            b'id:  spaced-resume\n'
            b'event:\tdelivery-operation\n'
            b'data: {"operationId":"operation-1","state":"Running","detail":"","occurredAt":"2026-09-01T00:00:00Z"}\n\n'
            b'id:unspaced-resume\n'
            b'event:delivery-operation\n'
            b'data:{"operationId":"operation-1","state":"Succeeded","detail":"","occurredAt":"2026-09-01T00:00:01Z"}\n\n'
        )

        def handler(request: httpx.Request) -> httpx.Response:
            return httpx.Response(200, content=body, headers={"Content-Type": "text/event-stream"}, request=request)

        async with AsyncDeliveryClient(delivery_host="https://delivery.example.test", access_token="token", http_client=httpx.AsyncClient(transport=httpx.MockTransport(handler))) as client:
            events = [event async for event in client.stream_operation_events("project")]
        self.assertEqual([event["id"] for event in events], [" spaced-resume", "unspaced-resume"])
        self.assertEqual([event["event"] for event in events], ["\tdelivery-operation", "delivery-operation"])
        self.assertEqual([event["data"]["state"] for event in events], ["Running", "Succeeded"])


    async def test_release_preview_carries_the_longest_job_run_and_does_not_invent_one(self) -> None:
        bodies: list[dict] = []

        def handler(request: httpx.Request) -> httpx.Response:
            import json
            bodies.append(json.loads(request.content))
            return httpx.Response(200, json={
                "previewId": "preview-1", "previewDigest": "sha256:preview", "action": "create",
                "expectedRevision": 0, "approvalRequired": False, "expiresAt": "2026-09-01T00:00:00Z",
            }, request=request)

        base = {"artifactAdmissionIds": ["a"], "migrationEvidenceDigest": "sha256:m"}
        async with AsyncDeliveryClient(delivery_host="https://delivery.example.test", access_token="token", http_client=httpx.AsyncClient(transport=httpx.MockTransport(handler))) as client:
            await client.preview_release("project", "environment", {**base, "configuration": {"statelessFunctions": True, "scheduler": False, "jobs": True, "jobsMaxRunSeconds": 120}}, if_match='W/"1"')
            await client.preview_release("project", "environment", {**base, "configuration": {"statelessFunctions": True, "scheduler": False, "jobs": True}}, if_match='W/"1"')
        # A release carries the whole configuration: the client sends what it was given and nothing
        # else, so omitting the field is the caller returning the environment to the 30-second default.
        self.assertEqual(bodies[0]["configuration"]["jobsMaxRunSeconds"], 120)
        self.assertNotIn("jobsMaxRunSeconds", bodies[1]["configuration"])


if __name__ == "__main__":
    unittest.main()
