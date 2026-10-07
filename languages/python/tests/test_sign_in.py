"""Headless password and external-provider sign-in against a fake gateway that answers the routes the product's sign-in uses."""

import asyncio
import base64
import hashlib
import json
import unittest
import uuid
from urllib.parse import parse_qs

import httpx
from kiota_abstractions.method import Method
from kiota_abstractions.request_information import RequestInformation
from kiota_http.httpx_request_adapter import HttpxRequestAdapter

from stackyoga_yogabase import (
    AsyncJobsClient, YogabaseApiClient, YogabaseAuthError, YogabaseJobsError, complete_external_sign_in, sign_in_with_password, start_external_sign_in)
from stackyoga_yogabase.models.invocation_request import InvocationRequest

GATEWAY = "https://apps.example.test"
REDIRECT = "https://app.example.test/callback"
PROJECT = "11111111-1111-4111-8111-111111111111"
CONFIG = {"projectId": PROJECT, "oidcPublicClientId": "client-1", "oidcRedirectUris": [REDIRECT], "oidcScopes": ["openid", "offline_access"],
          "oidcDiscoveryEndpoint": f"{GATEWAY}/.well-known/openid-configuration"}
DISCOVERY = {"authorization_endpoint": f"{GATEWAY}/connect/authorize", "token_endpoint": f"{GATEWAY}/connect/token"}
COOKIE = {"Set-Cookie": "yogabase.project.identity=cookie-1; Path=/; Secure; HttpOnly"}
MFA = {"Set-Cookie": "yogabase.mfa=mfa-1; Path=/; Secure; HttpOnly"}  # set by a 202 callback, required by the MFA step
INVOKE = f"/v1/projects/{PROJECT}/functions/hello/versions/1/invoke"


def problem(status: int, code: str) -> httpx.Response:
    return httpx.Response(status, json={"status": status, "code": code}, headers={"Content-Type": "application/problem+json", "X-Correlation-ID": "corr-1"})


def s256(verifier: str) -> str:
    return base64.urlsafe_b64encode(hashlib.sha256(verifier.encode("ascii")).digest()).rstrip(b"=").decode()


class Gateway:
    """The sign-in routes. ``login`` and ``callback`` are the (status, code) /auth/login and the provider callback answer,
    ``redirect`` the authorize Location, ``start`` a (status, code) refusal or members of the start answer to replace."""

    def __init__(self, login=(204, None), redirect=REDIRECT + "?code=code-1&state={state}", discovery=DISCOVERY, expires_in=3600, cookie=True, callback=(204, None), start=None):
        self.requests: list[httpx.Request] = []
        self.login, self.redirect, self.discovery, self.expires_in, self.cookie, self.issued, self.challenge = login, redirect, discovery, expires_in, cookie, 0, ""
        self.callback, self.start, self.starts = callback, start, {}  # starts: the state of each start and the challenge it sent

    @property
    def paths(self) -> list[str]:
        return [request.url.path for request in self.requests]

    def __call__(self, request: httpx.Request) -> httpx.Response:
        self.requests.append(request)
        path = request.url.path
        if path.startswith("/v1/client-config/"):
            return httpx.Response(200, json=CONFIG)
        if path == "/.well-known/openid-configuration":
            return httpx.Response(200, json=self.discovery)
        if path == "/auth/login":
            return problem(*self.login) if self.login[1] else httpx.Response(204, headers=COOKIE if self.cookie else {})
        if path == "/auth/mfa/verify":
            needs_cookie = self.callback[0] == 202 and "mfa-1" not in request.headers.get("cookie", "")
            return httpx.Response(204, headers=COOKIE) if b"123456" in request.content and not needs_cookie else problem(400, "mfa_invalid")
        if path == "/auth/external/google/start":
            if isinstance(self.start, tuple):
                return problem(*self.start)
            state = f"state-{len(self.starts) + 1}"
            self.starts[state] = json.loads(request.content)["codeChallenge"]
            return httpx.Response(200, json={"authorizationUrl": f"https://accounts.example.test/auth?state={state}", "state": state, **(self.start or {})})
        if path == "/auth/external/google/callback":
            body = json.loads(request.content)
            if body["code"] != "ext-code" or s256(body["codeVerifier"]) != self.starts.get(body["state"]):
                return problem(401, "external_identity_invalid")
            status, code = self.callback
            return httpx.Response(202, headers=MFA) if status == 202 else problem(status, code) if code else httpx.Response(204, headers=COOKIE)
        if path == "/connect/authorize":
            if "cookie-1" not in request.headers.get("cookie", ""):
                return problem(401, "interactive_login_required")
            query = parse_qs(request.url.query.decode())
            self.challenge = query["code_challenge"][0]
            return httpx.Response(302, headers={"Location": self.redirect.format(state=query["state"][0])})
        if path == "/connect/token":
            form = {key: value[0] for key, value in parse_qs(request.content.decode()).items()}
            if form["grant_type"] == "authorization_code":
                valid = form["code"] == "code-1" and form["redirect_uri"] == REDIRECT and s256(form["code_verifier"]) == self.challenge
            else:  # the refresh token rotates: only the latest one is accepted
                valid = form["refresh_token"] == f"refresh-{self.issued}"
            if not valid:
                return httpx.Response(400, json={"error": "invalid_grant"})
            self.issued += 1
            return httpx.Response(200, json={"access_token": f"access-{self.issued}", "token_type": "Bearer", "expires_in": self.expires_in, "refresh_token": f"refresh-{self.issued}"})
        if path.startswith(f"/v1/projects/{PROJECT}/jobs/"):
            return problem(404, "job_not_found")
        if path == INVOKE:
            return httpx.Response(200, json={"outcome": 4, "invocationId": str(uuid.uuid4()), "correlationId": "c"})
        return httpx.Response(404)


def http_for(gateway: Gateway) -> httpx.AsyncClient:
    async def handle(request: httpx.Request) -> httpx.Response:
        await asyncio.sleep(0)  # yield as real I/O does, so concurrent callers interleave
        return gateway(request)

    return httpx.AsyncClient(transport=httpx.MockTransport(handle))


async def sign_in(http: httpx.AsyncClient, **options):
    return await sign_in_with_password(GATEWAY, "sy_pub_key", "user@example.test", "secret-pw", http_client=http, **options)


async def refusal(gateway: Gateway, **options) -> YogabaseAuthError:
    async with http_for(gateway) as http:
        try:
            await sign_in(http, **options)
        except YogabaseAuthError as error:
            return error
    raise AssertionError("sign-in was not refused")


class SignInTests(unittest.IsolatedAsyncioTestCase):
    async def test_signs_in_with_the_cookie_and_pkce_and_keeps_the_tokens(self) -> None:
        gateway = Gateway()  # the token route itself rejects a wrong code, redirect URI or verifier
        async with http_for(gateway) as http:
            session = await sign_in(http)
        self.assertEqual(gateway.paths, ["/v1/client-config/sy_pub_key", "/.well-known/openid-configuration", "/auth/login", "/connect/authorize", "/connect/token"])
        login, authorize, token = gateway.requests[2:]
        query = parse_qs(authorize.url.query.decode())
        self.assertEqual([query[name][0] for name in ("response_type", "client_id", "redirect_uri", "scope", "code_challenge_method")],
                         ["code", "client-1", REDIRECT, "openid offline_access", "S256"])
        verifier = parse_qs(token.content.decode())["code_verifier"][0]
        self.assertGreaterEqual(len(verifier), 43)
        self.assertNotIn(verifier, str(authorize.url))
        self.assertIn(b"secret-pw", login.content)
        self.assertEqual((session.access_token, session.refresh_token, session.project_id), ("access-1", "refresh-1", PROJECT))

    async def test_a_login_refusal_is_named_and_stops_before_authorize(self) -> None:
        for status, code in ((401, "invalid_credentials"), (403, "mfa_enrollment_required"), (429, "authentication_rate_limited")):
            gateway = Gateway(login=(status, code))
            error = await refusal(gateway)
            self.assertEqual((error.status, error.code, error.correlation_id, gateway.paths[-1]), (status, code, "corr-1", "/auth/login"))

    async def test_without_the_cookie_there_is_no_token_request(self) -> None:
        gateway = Gateway(cookie=False)
        error = await refusal(gateway)
        self.assertEqual((error.status, error.code), (401, "interactive_login_required"))
        self.assertNotIn("/connect/token", gateway.paths)

    async def test_an_authorization_response_that_does_not_check_out_makes_no_token_request(self) -> None:
        invalid = [REDIRECT + "?code=code-1&state=other", REDIRECT + "?code=code-1", REDIRECT + "?state={state}", REDIRECT + "?code=&state={state}"]
        mismatched = ["https://evil.example.test/callback?code=code-1&state={state}", REDIRECT + "/x?code=code-1&state={state}"]
        rejected = [REDIRECT + "?error=access_denied&state={state}"]
        for code, redirects in (("authorization_response_invalid", invalid), ("authorization_redirect_mismatch", mismatched), ("access_denied", rejected)):
            for redirect in redirects:
                gateway = Gateway(redirect=redirect)
                error = await refusal(gateway)
                self.assertEqual((error.status, error.code), (400, code), redirect)
                self.assertNotIn("/connect/token", gateway.paths)

    async def test_mfa_required_is_reported_by_name_and_completes_with_the_code(self) -> None:
        gateway = Gateway(login=(202, "mfa_required"))
        error = await refusal(gateway)
        self.assertEqual((error.status, error.code, gateway.paths[-1]), (202, "mfa_required", "/auth/login"))
        self.assertEqual((await refusal(gateway, mfa_code="000000")).code, "mfa_invalid")
        self.assertNotIn("/connect/authorize", gateway.paths)
        async with http_for(gateway) as http:
            self.assertEqual((await sign_in(http, mfa_code="123456")).access_token, "access-1")

    async def test_only_an_https_gateway_and_discovery_inside_it_are_accepted(self) -> None:
        with self.assertRaises(ValueError):
            await sign_in_with_password("http://localhost:5080", "sy_pub_key", "user@example.test", "pw")
        gateway = Gateway(discovery={**DISCOVERY, "token_endpoint": "https://evil.example.test/connect/token"})
        self.assertEqual((await refusal(gateway)).code, "oidc_discovery_invalid")
        self.assertNotIn("/auth/login", gateway.paths)

    async def test_the_session_authenticates_the_generated_client_for_the_gateway_only(self) -> None:
        gateway = Gateway()
        async with http_for(gateway) as http:
            session = await sign_in(http)
            adapter = HttpxRequestAdapter(session, http_client=http)
            adapter.base_url = GATEWAY  # the constructor's base_url argument is ignored by this Kiota version
            request = InvocationRequest(idempotency_key="k", correlation_id="c", mode=0, payload_digest="sha256:" + "0" * 64, inline_payload_base64="e30=")
            result = await YogabaseApiClient(adapter).v1.projects.by_project_id(PROJECT).functions.by_function_id("hello").versions.by_function_version("1").invoke.post(request)
        self.assertEqual((result.outcome, gateway.requests[-1].headers["Authorization"]), (4, "Bearer access-1"))
        elsewhere = RequestInformation(Method.GET, "https://other.example.test/v1/servers")
        await session.authenticate_request(elsewhere)
        self.assertFalse(elsewhere.headers.contains("Authorization"))

    async def test_the_authored_clients_take_the_session_as_their_token_source(self) -> None:
        gateway = Gateway()
        async with http_for(gateway) as http:
            session = await sign_in(http)
            async with AsyncJobsClient(gateway_host=GATEWAY, project_id=PROJECT, access_token=session.get_access_token, http_client=http) as jobs:
                with self.assertRaises(YogabaseJobsError):
                    await jobs.get(str(uuid.uuid4()))
        self.assertEqual(gateway.requests[-1].headers["Authorization"], "Bearer access-1")

    async def test_an_expiring_token_is_refreshed_once_with_the_rotating_refresh_token(self) -> None:
        gateway = Gateway(expires_in=30)  # inside the one-minute skew, so already due
        async with http_for(gateway) as http:
            session = await sign_in(http)
            gateway.expires_in = 3600
            self.assertEqual(await asyncio.gather(session.get_access_token(), session.get_access_token()), ["access-2", "access-2"])
            self.assertEqual(await session.get_access_token(), "access-2")
            self.assertEqual(gateway.paths.count("/connect/token"), 2)
            session.refresh_token, session._expires_at = "refresh-1", 0.0  # an already spent token
            with self.assertRaises(YogabaseAuthError) as raised:
                await session.get_access_token()
        self.assertEqual((raised.exception.status, raised.exception.code), (400, "invalid_grant"))


def returned(pending, **query) -> str:
    return f"{REDIRECT}?" + "&".join(f"{key}={value}" for key, value in {"code": "ext-code", "state": pending.authorization_url.rsplit("=", 1)[1], **query}.items() if value)


async def start(http: httpx.AsyncClient, redirect_uri: str = REDIRECT):
    return await start_external_sign_in(GATEWAY, "sy_pub_key", "google", redirect_uri, http_client=http)


async def external_refusal(gateway: Gateway, redirect=None, **options) -> YogabaseAuthError:
    async with http_for(gateway) as http:
        pending = await start(http)
        try:
            await complete_external_sign_in(pending, redirect or returned(pending), http_client=http, **options)
        except YogabaseAuthError as error:
            return error
    raise AssertionError("sign-in was not refused")


class ExternalSignInTests(unittest.IsolatedAsyncioTestCase):
    async def test_signs_in_with_the_challenge_of_a_verifier_the_start_never_carries_and_keeps_the_tokens(self) -> None:
        gateway = Gateway()  # the callback itself rejects a wrong code, or a verifier that is not the start's
        async with http_for(gateway) as http:
            pending = await start(http)
            session = await complete_external_sign_in(pending, returned(pending), http_client=http)
        self.assertEqual(gateway.paths, ["/auth/external/google/start", "/v1/client-config/sy_pub_key", "/.well-known/openid-configuration", "/auth/external/google/callback", "/connect/authorize", "/connect/token"])
        begun, callback = gateway.requests[0], gateway.requests[3]
        verifier = json.loads(callback.content)["codeVerifier"]
        self.assertTrue(43 <= len(verifier) <= 128)
        self.assertEqual(json.loads(begun.content), {"redirectUri": REDIRECT, "codeChallenge": s256(verifier)})
        self.assertEqual(json.loads(callback.content), {"code": "ext-code", "state": "state-1", "codeVerifier": verifier})
        self.assertFalse(any("authorization" in request.headers for request in gateway.requests[:4]))
        self.assertEqual((pending.authorization_url, session.access_token, session.refresh_token, session.project_id), ("https://accounts.example.test/auth?state=state-1", "access-1", "refresh-1", PROJECT))

    async def test_a_redirect_that_does_not_check_out_makes_no_callback_request_and_leaves_the_pending_usable(self) -> None:
        async with http_for(gateway := Gateway()) as http:
            other, pending = await start(http), await start(http)  # the redirect of the first start is not the second's
            for code, redirect in (
                ("authorization_response_invalid", [returned(other), returned(pending, state="state-1x"), returned(pending, code=""), returned(pending) + "&code=ext-code"]),
                ("access_denied", [returned(pending, error="access_denied", code="")]),
                ("authorization_redirect_mismatch", [returned(pending).replace("app.example", "evil.example"), returned(pending).replace("/callback", "/other"), returned(pending) + "#x"]),
            ):
                for bad in redirect:
                    with self.assertRaises(YogabaseAuthError) as raised:
                        await complete_external_sign_in(pending, bad, http_client=http)
                    self.assertEqual((raised.exception.status, raised.exception.code), (400, code), bad)
            self.assertEqual(gateway.paths, ["/auth/external/google/start"] * 2)
            self.assertEqual((await complete_external_sign_in(pending, returned(pending), http_client=http)).access_token, "access-1")

    async def test_a_gateway_refusal_keeps_its_code_and_ends_before_the_code_flow(self) -> None:
        for status, code in ((401, "external_identity_invalid"), (409, "external_identity_email_in_use"), (403, "external_email_unverified"), (403, "mfa_enrollment_required")):
            gateway = Gateway(callback=(status, code))
            error = await external_refusal(gateway)
            self.assertEqual((error.status, error.code, error.correlation_id, gateway.paths[-1]), (status, code, "corr-1", "/auth/external/google/callback"))
        gateway = Gateway()  # a wrong code is the gateway's 401, and the verifier is its own
        self.assertEqual((await external_refusal(gateway, REDIRECT + "?code=forged&state=state-1")).code, "external_identity_invalid")

    async def test_a_start_refusal_or_an_unusable_start_answer_is_named(self) -> None:
        for refused in ((400, "external_redirect_uri_invalid"), (404, "external_provider_unknown"), (503, "external_provider_unavailable")):
            async with http_for(Gateway(start=refused)) as http:
                with self.assertRaises(YogabaseAuthError) as raised:
                    await start(http)
            self.assertEqual((raised.exception.status, raised.exception.code), refused)
        for unusable in ({"authorizationUrl": "http://accounts.example.test/auth"}, {"authorizationUrl": ""}, {"state": ""}):
            async with http_for(Gateway(start=unusable)) as http:
                with self.assertRaises(YogabaseAuthError) as raised:
                    await start(http)
            self.assertEqual((raised.exception.status, raised.exception.code), (502, "external_start_invalid"))
        with self.assertRaises(ValueError):
            await start_external_sign_in("http://localhost:5080", "sy_pub_key", "google", REDIRECT)

    async def test_mfa_required_completes_with_the_code_without_a_second_callback(self) -> None:
        gateway = Gateway(callback=(202, None))
        async with http_for(gateway) as first, http_for(gateway) as second:
            pending = await start(first)
            for options in ({}, {"mfa_code": "000000"}):  # the callback's 202 is told apart from a refusal, and the wrong code is the gateway's
                with self.assertRaises(YogabaseAuthError) as raised:
                    await complete_external_sign_in(pending, returned(pending), http_client=first, **options)
                self.assertEqual(raised.exception.code, "mfa_required" if not options else "mfa_invalid")
            self.assertNotIn("/connect/authorize", gateway.paths)
            session = await complete_external_sign_in(pending, returned(pending), mfa_code="123456", http_client=second)  # the cookies travel in the pending
        self.assertEqual((gateway.paths.count("/auth/external/google/callback"), session.access_token), (1, "access-1"))

    async def test_a_bad_discovery_document_ends_the_call_before_the_code_is_sent(self) -> None:
        gateway = Gateway(discovery={**DISCOVERY, "token_endpoint": "https://evil.example.test/connect/token"})
        async with http_for(gateway) as http:
            pending = await start(http)
            with self.assertRaises(YogabaseAuthError) as raised:
                await complete_external_sign_in(pending, returned(pending), http_client=http)
            self.assertEqual(raised.exception.code, "oidc_discovery_invalid")
            self.assertNotIn("/auth/external/google/callback", gateway.paths)
            gateway.discovery = DISCOVERY
            self.assertEqual((await complete_external_sign_in(pending, returned(pending), http_client=http)).access_token, "access-1")
        self.assertEqual(gateway.paths.count("/auth/external/google/callback"), 1)

    async def test_a_failure_after_the_callback_is_retried_without_sending_the_spent_code_again(self) -> None:
        gateway = Gateway(redirect=REDIRECT + "?code=code-1&state=forged")
        async with http_for(gateway) as http:
            pending = await start(http)
            with self.assertRaises(YogabaseAuthError) as raised:
                await complete_external_sign_in(pending, returned(pending), http_client=http)
            self.assertEqual(raised.exception.code, "authorization_response_invalid")
            gateway.redirect = REDIRECT + "?code=code-1&state={state}"
            self.assertEqual((await complete_external_sign_in(pending, returned(pending), http_client=http)).access_token, "access-1")
        self.assertEqual(gateway.paths.count("/auth/external/google/callback"), 1)


if __name__ == "__main__":
    unittest.main()
