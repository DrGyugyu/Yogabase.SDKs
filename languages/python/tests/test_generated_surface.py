"""Import and round-trip checks for the generated package, whichever copy is imported.

Run from ``languages/python`` with ``PYTHONPATH=src`` these check the source tree; run in an
environment that has only the built wheel installed they check the wheel's contents.
"""

import importlib
import json
import pathlib
import unittest

import httpx
from kiota_abstractions.authentication.anonymous_authentication_provider import AnonymousAuthenticationProvider
from kiota_http.httpx_request_adapter import HttpxRequestAdapter

import stackyoga_yogabase
from stackyoga_yogabase import YogabaseApiClient, storage_object_for_logical_key
from stackyoga_yogabase.models.problem import Problem

GATEWAY = "https://apps.example.test"


class GeneratedSurfaceTests(unittest.IsolatedAsyncioTestCase):
    def test_every_module_in_the_imported_package_imports(self) -> None:
        # The generated subpackages have no __init__.py, so pkgutil.walk_packages would stop at the
        # top level; walk the files instead.
        root = pathlib.Path(stackyoga_yogabase.__file__).parent
        names = sorted(
            ".".join(("stackyoga_yogabase", *path.relative_to(root).with_suffix("").parts))
            for path in root.rglob("*.py")
            if path.name != "__init__.py"
        )
        self.assertIn("stackyoga_yogabase.v1.servers.servers_request_builder", names)
        self.assertIn("stackyoga_yogabase.delivery", names)
        failures = {}
        for name in names:
            try:
                importlib.import_module(name)
            except Exception as exception:  # noqa: BLE001 - every failure is reported together
                failures[name] = repr(exception)
        self.assertEqual(failures, {})
        self.assertGreaterEqual(len(names), 87)

    async def test_generated_game_server_list_round_trips_through_kiota(self) -> None:
        requests: list[httpx.Request] = []
        body = [{
            "serverId": "srv-1",
            "state": "Ready",
            "networkPorts": [{"name": "game", "internalPort": 7777, "externalPort": 30001, "protocol": "udp", "host": "203.0.113.10"}],
        }]

        def handler(request: httpx.Request) -> httpx.Response:
            requests.append(request)
            return httpx.Response(200, content=json.dumps(body).encode(), headers={"Content-Type": "application/json"}, request=request)

        async with httpx.AsyncClient(transport=httpx.MockTransport(handler)) as http:
            client = _client(http)
            servers = await client.v1.servers.get()

        self.assertEqual(str(requests[0].url), f"{GATEWAY}/v1/servers")
        self.assertEqual(requests[0].method, "GET")
        self.assertEqual(requests[0].headers["Accept"], "application/json")
        self.assertEqual(len(servers), 1)
        self.assertEqual(servers[0].server_id, "srv-1")
        self.assertEqual(servers[0].state, "Ready")
        port = servers[0].network_ports[0]
        self.assertEqual((port.name, port.internal_port, port.external_port, port.host), ("game", 7777, 30001, "203.0.113.10"))
        self.assertEqual(port.protocol.value, "udp")

    async def test_generated_stop_refusal_raises_the_contract_problem(self) -> None:
        requests: list[httpx.Request] = []
        problem = {"type": "about:blank", "title": "Conflict", "status": 409, "code": "server_stop_not_available", "traceId": "trace-1"}

        def handler(request: httpx.Request) -> httpx.Response:
            requests.append(request)
            return httpx.Response(409, content=json.dumps(problem).encode(), headers={"Content-Type": "application/problem+json"}, request=request)

        async with httpx.AsyncClient(transport=httpx.MockTransport(handler)) as http:
            client = _client(http)
            with self.assertRaises(Problem) as raised:
                await client.v1.servers.by_server_id("srv 1").stop.post()

        self.assertEqual(requests[0].method, "POST")
        self.assertEqual(str(requests[0].url), f"{GATEWAY}/v1/servers/srv%201/stop")
        self.assertEqual(raised.exception.code, "server_stop_not_available")
        self.assertEqual(raised.exception.status, 409)
        self.assertEqual(raised.exception.trace_id, "trace-1")

    async def test_logical_key_adapter_addresses_the_generated_item_builder(self) -> None:
        async with httpx.AsyncClient(transport=httpx.MockTransport(lambda request: httpx.Response(500))) as http:
            client = _client(http)
            item = storage_object_for_logical_key(client.v1.storage.objects, GATEWAY, "avatars/user 1/photo.png")
            request = item.to_get_request_information()
        self.assertEqual(request.url, f"{GATEWAY}/v1/storage/objects/avatars/user%201/photo.png")


def _client(http: httpx.AsyncClient) -> YogabaseApiClient:
    adapter = HttpxRequestAdapter(AnonymousAuthenticationProvider(), http_client=http)
    adapter.base_url = GATEWAY
    return YogabaseApiClient(adapter)


if __name__ == "__main__":
    unittest.main()
