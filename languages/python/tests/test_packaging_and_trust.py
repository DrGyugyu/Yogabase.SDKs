"""Build-backend pin and TLS trust checks for the Python package.

The owner decided on 2026-09-17 that the package pins its build backend exactly and trusts the
platform's certificate store rather than certifi's bundle. certifi itself cannot leave the runtime
closure while the Kiota HTTP stack depends on httpx: httpx 0.28.1 and httpcore 1.0.9 both declare it
unconditionally. What this package controls is which trust store its own HTTP clients verify
against, and that is what these tests pin.
"""

import pathlib
import re
import ssl
import sys
import unittest
from unittest import mock

import httpx

import stackyoga_yogabase
from stackyoga_yogabase import AsyncDeliveryClient, platform_ssl_context

PACKAGE_ROOT = pathlib.Path(__file__).resolve().parents[1]


def _read(name: str) -> str:
    return (PACKAGE_ROOT / name).read_text(encoding="utf-8")


class BuildBackendPinTests(unittest.TestCase):
    @unittest.skipIf(sys.version_info < (3, 11), "tomllib needs Python 3.11")
    def test_the_build_backend_is_pinned_exactly_to_the_locked_setuptools(self) -> None:
        import tomllib

        project = tomllib.loads(_read("pyproject.toml"))
        self.assertEqual(project["build-system"]["requires"], ["setuptools==84.0.0"])
        self.assertEqual(project["build-system"]["build-backend"], "setuptools.build_meta")
        locked = re.search(r"^setuptools==(\S+) \\$", _read("build-requirements.lock"), re.MULTILINE)
        self.assertIsNotNone(locked)
        self.assertEqual(locked.group(1), "84.0.0")

    @unittest.skipIf(sys.version_info < (3, 11), "tomllib needs Python 3.11")
    def test_certifi_is_not_a_direct_dependency(self) -> None:
        import tomllib

        dependencies = tomllib.loads(_read("pyproject.toml"))["project"]["dependencies"]
        self.assertFalse(any(dependency.lower().startswith("certifi") for dependency in dependencies))


class PlatformTrustTests(unittest.IsolatedAsyncioTestCase):
    def test_the_platform_context_verifies_peers_and_host_names(self) -> None:
        context = platform_ssl_context()
        self.assertIsInstance(context, ssl.SSLContext)
        self.assertEqual(context.verify_mode, ssl.CERT_REQUIRED)
        self.assertTrue(context.check_hostname)

    def test_the_platform_context_loads_the_default_store_and_never_certifi(self) -> None:
        calls: list[dict[str, object]] = []
        real = ssl.create_default_context

        def spy(*args: object, **kwargs: object) -> ssl.SSLContext:
            calls.append({"args": args, **kwargs})
            return real()

        with mock.patch("stackyoga_yogabase.delivery.ssl.create_default_context", side_effect=spy):
            platform_ssl_context()
        self.assertEqual(calls, [{"args": ()}])

    async def test_the_default_delivery_transport_uses_the_platform_context(self) -> None:
        context = ssl.create_default_context()

        def certifi_bundle_consulted() -> str:
            raise AssertionError("the Delivery client consulted certifi's bundle")

        import certifi

        with (
            mock.patch("stackyoga_yogabase.delivery.platform_ssl_context", return_value=context),
            mock.patch.object(certifi, "where", side_effect=certifi_bundle_consulted),
        ):
            client = AsyncDeliveryClient(delivery_host="https://delivery.example.test", access_token="token")
        try:
            pool = client._http._transport._pool  # httpx 0.28.1 / httpcore 1.0.9, both pinned
            self.assertIs(pool._ssl_context, context)
        finally:
            await client.aclose()

    async def test_a_supplied_http_client_is_used_unchanged(self) -> None:
        supplied = httpx.AsyncClient(transport=httpx.MockTransport(lambda request: httpx.Response(204)))
        with mock.patch("stackyoga_yogabase.delivery.platform_ssl_context", side_effect=AssertionError("not needed")):
            client = AsyncDeliveryClient(delivery_host="https://delivery.example.test", access_token="token", http_client=supplied)
        self.assertIs(client._http, supplied)
        await supplied.aclose()

    async def test_the_readme_recipe_keeps_the_platform_context_under_kiota_middleware(self) -> None:
        from kiota_http.kiota_client_factory import KiotaClientFactory

        context = platform_ssl_context()
        http = KiotaClientFactory.create_with_default_middleware(httpx.AsyncClient(verify=context, http2=True))
        try:
            wrapped = http._transport.transport  # AsyncKiotaTransport over the httpx transport
            self.assertIs(wrapped._pool._ssl_context, context)
        finally:
            await http.aclose()

    def test_the_helper_is_exported(self) -> None:
        self.assertIn("platform_ssl_context", stackyoga_yogabase.__all__)
