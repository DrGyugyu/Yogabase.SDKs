"""Live smoke test: signs in to a real installation and uses what the token opens. Skipped unless its inputs are set."""

import base64
import hashlib
import os
import unittest
import uuid
from datetime import datetime, timedelta, timezone

import httpx
from kiota_abstractions.base_request_configuration import RequestConfiguration
from kiota_abstractions.headers_collection import HeadersCollection
from kiota_http.httpx_request_adapter import HttpxRequestAdapter
from kiota_http.kiota_client_factory import KiotaClientFactory

from stackyoga_yogabase import YogabaseApiClient, platform_ssl_context, sign_in_with_password, storage_object_for_logical_key
from stackyoga_yogabase.models.invocation_request import InvocationRequest

ENV = {name: os.environ.get("YOGABASE_SMOKE_" + name, "") for name in ("GATEWAY", "PUBLISHABLE_KEY", "EMAIL", "PASSWORD", "FUNCTION_ID", "FUNCTION_VERSION")}


@unittest.skipUnless(all(ENV.values()), "Set the YOGABASE_SMOKE_* variables (GATEWAY, PUBLISHABLE_KEY, EMAIL, PASSWORD, FUNCTION_ID, FUNCTION_VERSION) to run against an installation.")
class LiveSmokeTests(unittest.IsolatedAsyncioTestCase):
    """Signs in as an existing, confirmed user without MFA, invokes a deployed stateless function that accepts ``{}``, then puts and gets an object."""

    async def test_sign_in_then_invoke_and_store(self) -> None:
        session = await sign_in_with_password(ENV["GATEWAY"], ENV["PUBLISHABLE_KEY"], ENV["EMAIL"], ENV["PASSWORD"])
        self.assertTrue(session.access_token)
        async with httpx.AsyncClient(verify=platform_ssl_context()) as http:
            adapter = HttpxRequestAdapter(session, http_client=KiotaClientFactory.create_with_default_middleware(http))
            adapter.base_url = ENV["GATEWAY"]  # the constructor's base_url argument is ignored by this Kiota version
            client = YogabaseApiClient(adapter)
            payload = b"{}"
            request = InvocationRequest(
                idempotency_key=uuid.uuid4().hex, correlation_id="live-smoke", deadline=datetime.now(timezone.utc) + timedelta(seconds=20), mode=0,
                payload_digest="sha256:" + hashlib.sha256(payload).hexdigest(), inline_payload_base64=base64.b64encode(payload).decode())
            invoked = await client.v1.projects.by_project_id(session.project_id).functions.by_function_id(ENV["FUNCTION_ID"]).versions.by_function_version(ENV["FUNCTION_VERSION"]).invoke.post(request)
            self.assertEqual(invoked.outcome, 4)  # Completed

            data = b"live smoke " + uuid.uuid4().bytes
            headers = HeadersCollection()
            headers.add("X-Yogabase-Content-Sha256", "sha256:" + hashlib.sha256(data).hexdigest())
            headers.add("Idempotency-Key", uuid.uuid4().hex)
            item = storage_object_for_logical_key(client.v1.storage.objects, ENV["GATEWAY"], f"smoke/{uuid.uuid4().hex}.txt")
            await item.put(data, RequestConfiguration(headers=headers))
            self.assertEqual(await item.get(), data)


if __name__ == "__main__":
    unittest.main()
