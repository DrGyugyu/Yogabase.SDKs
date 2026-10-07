"""Headless password sign-in: the identity cookie from ``/auth/login`` stands in for the browser in the PKCE code flow."""

from __future__ import annotations

import asyncio
import base64
import hashlib
import hmac
import secrets
import time
from contextlib import nullcontext
from typing import Any
from urllib.parse import parse_qsl, quote, urlsplit

import httpx
from kiota_abstractions.authentication import AuthenticationProvider
from kiota_abstractions.request_information import RequestInformation

from ._gateway import normalize_host, read_problem
from .delivery import platform_ssl_context


class YogabaseAuthError(Exception):
    """A refused sign-in or token request: the gateway's ``status`` and ``code`` and the ``correlation_id`` to quote."""

    def __init__(self, message: str, status: int, code: str, correlation_id: str | None = None) -> None:
        super().__init__(message)
        self.status, self.code, self.correlation_id = status, code, correlation_id


def _refusal(response: httpx.Response) -> YogabaseAuthError:
    problem = read_problem(response)
    error = (problem.problem or {}).get("error")  # a token endpoint names its code "error", the gateway "code"
    return YogabaseAuthError(problem.message, problem.status, error if isinstance(error, str) else problem.code, problem.correlation_id)


class YogabaseSession(AuthenticationProvider):
    """A signed-in user's tokens, refreshed when about to expire. It is the Kiota authentication provider (the
    bearer goes to the gateway only); the authored clients take ``get_access_token`` as their ``access_token``."""

    def __init__(self, gateway_host: str, project_id: str, client_id: str, token_endpoint: str, http_client: httpx.AsyncClient | None) -> None:
        self.gateway_host, self.project_id = gateway_host, project_id
        self.access_token, self.refresh_token = "", None
        self._client_id, self._token_endpoint, self._http_client = client_id, token_endpoint, http_client
        self._expires_at, self._lock = 0.0, asyncio.Lock()

    async def get_access_token(self) -> str:
        async with self._lock:  # the refresh token rotates, so two callers must not both spend it
            if self.refresh_token and time.monotonic() > self._expires_at - 60:
                async with _open(self._http_client) as http:
                    await self._token_request(http, {"grant_type": "refresh_token", "refresh_token": self.refresh_token})
            return self.access_token

    async def authenticate_request(self, request: RequestInformation, additional_authentication_context: dict[str, Any] | None = None) -> None:
        if request.url.lower().startswith(self.gateway_host.lower() + "/"):  # never send the token to another host
            request.headers.try_add("Authorization", f"Bearer {await self.get_access_token()}")

    async def _token_request(self, http: httpx.AsyncClient, fields: dict[str, str]) -> None:
        response = await http.post(self._token_endpoint, data={"client_id": self._client_id, **fields})
        if response.status_code != 200:
            raise _refusal(response)
        try:
            body = response.json()
            self.access_token, self._expires_at = body["access_token"], time.monotonic() + float(body["expires_in"])
        except (ValueError, KeyError, TypeError):
            raise YogabaseAuthError("The token endpoint returned an invalid token response.", 502, "token_response_invalid") from None
        self.refresh_token = body.get("refresh_token")


def _open(client: httpx.AsyncClient | None):
    return nullcontext(client) if client is not None else httpx.AsyncClient(verify=platform_ssl_context())


async def _read(http: httpx.AsyncClient, url: str, *names: str) -> list[Any]:
    response = await http.get(url)
    if response.status_code != 200:
        raise _refusal(response)
    try:
        return [response.json()[name] for name in names]
    except (ValueError, KeyError, TypeError):
        raise YogabaseAuthError(f"{url} did not return {', '.join(names)}.", 502, "response_invalid") from None


def _s256(verifier: str) -> str:
    return base64.urlsafe_b64encode(hashlib.sha256(verifier.encode("ascii")).digest()).rstrip(b"=").decode()


async def _discover(http: httpx.AsyncClient, gateway: str, publishable_key: str) -> tuple[Any, ...]:
    """The bootstrap's project, client, redirect URIs and scopes, and the two endpoints the code flow calls."""
    project_id, client_id, redirect_uris, scopes, discovery = await _read(
        http, f"{gateway}/v1/client-config/{quote(publishable_key, safe='')}",
        "projectId", "oidcPublicClientId", "oidcRedirectUris", "oidcScopes", "oidcDiscoveryEndpoint")
    authorize_url, token_url = await _read(http, discovery, "authorization_endpoint", "token_endpoint")
    if not (authorize_url.startswith(gateway + "/") and token_url.startswith(gateway + "/")):
        raise YogabaseAuthError("The discovery document points outside the gateway.", 502, "oidc_discovery_invalid")
    return project_id, client_id, redirect_uris, scopes, authorize_url, token_url


def _read_redirect(redirect: str, registered: str, state: str) -> str:
    """The code in what came back to ``registered``, held to that address and to the ``state`` sent."""
    location, expected = urlsplit(redirect), urlsplit(registered)
    if (location.scheme, location.netloc.lower(), location.path) != (expected.scheme, expected.netloc.lower(), expected.path) or location.fragment:
        raise YogabaseAuthError("The authorization response used another redirect URI.", 400, "authorization_redirect_mismatch")
    pairs = parse_qsl(location.query, keep_blank_values=True)
    query = dict(pairs)
    if len(query) != len(pairs) or not hmac.compare_digest(query.get("state", "").encode(), state.encode()):
        raise YogabaseAuthError("The authorization response repeats a parameter or came back with another state.", 400, "authorization_response_invalid")
    if "error" in query:
        raise YogabaseAuthError("The authorization server rejected sign-in.", 400, query["error"])
    if not query.get("code"):
        raise YogabaseAuthError("The authorization response lacks a code.", 400, "authorization_response_invalid")
    return query["code"]


async def _authorize(http: httpx.AsyncClient, gateway: str, found: tuple[Any, ...], http_client: httpx.AsyncClient | None) -> YogabaseSession:
    """The PKCE code flow, the identity cookie just set answering ``/connect/authorize`` whose redirect is read, never followed."""
    project_id, client_id, redirect_uris, scopes, authorize_url, token_url = found
    redirect_uri = redirect_uris[0]
    verifier, state = secrets.token_urlsafe(48), secrets.token_urlsafe(32)
    response = await http.get(authorize_url, follow_redirects=False, params={
        "response_type": "code", "client_id": client_id, "redirect_uri": redirect_uri, "scope": " ".join(scopes),
        "code_challenge": _s256(verifier), "code_challenge_method": "S256", "state": state})
    if response.status_code != 302 or "location" not in response.headers:
        raise _refusal(response)  # without the cookie this is 401 interactive_login_required
    code = _read_redirect(response.headers["location"], redirect_uri, state)
    session = YogabaseSession(gateway, project_id, client_id, token_url, http_client)
    await session._token_request(http, {"grant_type": "authorization_code", "code": code, "redirect_uri": redirect_uri, "code_verifier": verifier})
    return session


async def sign_in_with_password(
    gateway_host: str,
    publishable_key: str,
    email: str,
    password: str,
    *,
    mfa_code: str | None = None,
    http_client: httpx.AsyncClient | None = None,
) -> YogabaseSession:
    """Signs in with no browser: logs in for the identity cookie, then runs the PKCE code flow with that cookie
    answering ``/connect/authorize``, whose redirect is read and never followed. A refusal is a
    :class:`YogabaseAuthError` with the gateway's code: ``invalid_credentials`` (401: a wrong password, an
    unknown, unconfirmed or locked account, never told apart), ``mfa_enrollment_required`` (403),
    ``authentication_rate_limited`` (429), and ``mfa_required`` (202) for an account with MFA and no
    ``mfa_code``: call again with the authenticator code. A ``http_client`` you pass must keep cookies and
    not follow redirects (httpx's default)."""
    gateway = normalize_host(gateway_host, False)
    async with _open(http_client) as http:
        found = await _discover(http, gateway, publishable_key)

        response = await http.post(f"{gateway}/auth/login", json={"email": email, "password": password})
        if response.status_code == 202:
            if mfa_code is None:
                raise YogabaseAuthError("Complete the second authentication factor: pass mfa_code.", 202, "mfa_required")
            response = await http.post(f"{gateway}/auth/mfa/verify", json={"code": mfa_code})
        if response.status_code != 204:
            raise _refusal(response)

        return await _authorize(http, gateway, found, http_client)
