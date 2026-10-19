"""Plumbing the authored application-gateway clients share (durable Jobs and schedules).

The gateway origin and project checks, the user's access token, the one place a request is built and sent,
and the reading of a refusal. It is package-internal: :mod:`yogabase_client.jobs` and
:mod:`yogabase_client.schedules` are the public surface, and only the status-derived error codes are
re-exported from the package.
"""

from __future__ import annotations

import inspect
import json
import re
from collections.abc import Awaitable, Callable, Mapping
from dataclasses import dataclass
from typing import Any
from urllib.parse import urlsplit, urlunsplit

import httpx

from .delivery import platform_ssl_context

#: The code a refusal carries when the gateway named none, derived from the HTTP status so a caller can
#: match on it without a status table. A refusal that names its own code (a Jobs ``job_not_found``, a
#: schedule's ``schedule_payload_too_large``) keeps it.
#:
#: 401: no signed-in user, or a token the gateway cannot authenticate.
STATUS_AUTHENTICATION_REQUIRED = "authentication_required"
#: 403: the token may not call the route, or the caller may not do this to that resource.
STATUS_FORBIDDEN = "forbidden"
#: 404 with no body: no such resource, another user's resource, or an installation that does not serve the route.
STATUS_NOT_FOUND = "not_found"
#: 409 with no body: the request conflicts with what the gateway holds.
STATUS_CONFLICT = "conflict"
#: 412: the ETag sent is stale, or the precondition does not hold for what the gateway holds.
STATUS_PRECONDITION_FAILED = "precondition_failed"
#: 413 with no body: a body over its listener's limit.
STATUS_PAYLOAD_TOO_LARGE = "payload_too_large"
#: 415: the request body is not JSON.
STATUS_UNSUPPORTED_MEDIA_TYPE = "unsupported_media_type"
#: 428: the route needs an ``If-Match`` or ``If-None-Match`` precondition and the request sent none.
STATUS_PRECONDITION_REQUIRED = "precondition_required"
#: 429: too many requests.
STATUS_RATE_LIMITED = "rate_limited"
#: Any other refusal, which a caller reads from the status.
STATUS_REQUEST_FAILED = "request_failed"

_CODE_BY_STATUS = {
    401: STATUS_AUTHENTICATION_REQUIRED,
    403: STATUS_FORBIDDEN,
    404: STATUS_NOT_FOUND,
    409: STATUS_CONFLICT,
    412: STATUS_PRECONDITION_FAILED,
    # A gateway that predates the named refusal answers a bare 413 for a body over its listener's limit.
    413: STATUS_PAYLOAD_TOO_LARGE,
    415: STATUS_UNSUPPORTED_MEDIA_TYPE,
    428: STATUS_PRECONDITION_REQUIRED,
    429: STATUS_RATE_LIMITED,
}

CANONICAL_UUID = re.compile(r"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}")
TIMESTAMP = re.compile(r"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?(Z|[+-]\d{2}:\d{2})")
CONTROL_CHARACTER = re.compile(r"[\u0000-\u001f\u007f-\u009f]")
_LOOPBACK_HOSTS = ("localhost", "127.0.0.1", "::1")

GatewayAccessToken = str | Callable[[], str | Awaitable[str]]


def normalize_host(value: str, allow_insecure_loopback: bool) -> str:
    if not value:
        raise TypeError("gateway_host is required")
    parsed = urlsplit(value)
    loopback = parsed.hostname in _LOOPBACK_HOSTS
    secure = parsed.scheme == "https" or (allow_insecure_loopback and loopback and parsed.scheme == "http")
    if not secure or not parsed.netloc or parsed.username or parsed.password or parsed.query or parsed.fragment or parsed.path not in ("", "/"):
        raise ValueError("gateway_host must be an origin-only HTTPS URL (plain http only for a loopback host with allow_insecure_loopback)")
    return urlunsplit((parsed.scheme, parsed.netloc, "", "", ""))


def is_non_zero_uuid(value: object) -> bool:
    """True for a canonical hyphenated UUID that is not the all-zero one."""
    return isinstance(value, str) and bool(CANONICAL_UUID.fullmatch(value)) and bool(value.replace("-", "").strip("0"))


def require_uuid(value: str, name: str) -> str:
    if not is_non_zero_uuid(value):
        raise ValueError(f"{name} must be a non-empty canonical UUID")
    return value


def bounded_timeout(value: float) -> float:
    if isinstance(value, bool) or not isinstance(value, (int, float)) or not 0 < value <= 900:
        raise ValueError("timeout must be greater than 0 and no more than 900 seconds")
    return float(value)


async def resolve_token(token: GatewayAccessToken, what: str) -> str:
    value = token() if callable(token) else token
    if inspect.isawaitable(value):
        value = await value
    if not isinstance(value, str):
        raise TypeError(f"The {what} access token provider must return a string")
    return value


@dataclass(frozen=True)
class GatewayProblem:
    """What a refusal says, read once from the response; each client wraps it in its own error class."""

    message: str
    status: int
    #: The gateway's stable code, or the one derived from the status when the body names none.
    code: str
    problem: dict[str, Any] | None
    correlation_id: str | None
    limit_bytes: int | None
    max_body_bytes: int | None


def read_problem(response: httpx.Response) -> GatewayProblem:
    problem: dict[str, Any] | None = None
    try:
        parsed = response.json()
        if isinstance(parsed, dict):
            problem = parsed
    except (ValueError, json.JSONDecodeError):
        pass

    def named(name: str) -> int | None:
        value = (problem or {}).get(name)
        return value if isinstance(value, int) and not isinstance(value, bool) and value > 0 else None

    detail = (problem or {}).get("detail")
    title = (problem or {}).get("title")
    code = (problem or {}).get("code")
    trace = (problem or {}).get("traceId")
    return GatewayProblem(
        detail if isinstance(detail, str) else title if isinstance(title, str) else f"Yogabase returned HTTP {response.status_code}.",
        response.status_code,
        code if isinstance(code, str) else _CODE_BY_STATUS.get(response.status_code, STATUS_REQUEST_FAILED),
        problem,
        response.headers.get("X-Correlation-ID") or (trace if isinstance(trace, str) else None),
        named("limitBytes"),
        named("maxBodyBytes"),
    )


class GatewayChannel:
    """The channel an authored client calls the application gateway through.

    The validated origin and project, the user's access token, and the one place a request is built and
    sent, so the Jobs and schedule clients cannot drift apart in how they authenticate, refuse redirects or
    bound a call's timeout.
    """

    def __init__(
        self,
        what: str,
        *,
        gateway_host: str,
        project_id: str,
        access_token: GatewayAccessToken,
        timeout: float,
        headers: Mapping[str, str] | None,
        allow_insecure_loopback: bool,
        http_client: httpx.AsyncClient | None,
        refuse: Callable[[httpx.Response], Exception],
    ) -> None:
        self.what = what
        self.gateway_host = normalize_host(gateway_host, allow_insecure_loopback)
        self.project_id = require_uuid(project_id, "project_id")
        if not access_token:
            raise TypeError(f"access_token is required for the {what} API")
        self._token = access_token
        self._timeout = bounded_timeout(timeout)
        self._headers = dict(headers or {})
        self._http = http_client or httpx.AsyncClient(verify=platform_ssl_context())
        self._owns_http = http_client is None
        self._refuse = refuse

    async def aclose(self) -> None:
        if self._owns_http:
            await self._http.aclose()

    async def send(
        self,
        method: str,
        path: str,
        body: bytes | None = None,
        *,
        accept: str = "application/json, application/problem+json",
        headers: Mapping[str, str] | None = None,
        stream: bool = False,
    ) -> httpx.Response:
        """Sends one request to ``path`` under the gateway origin and returns its successful response.

        A refusal is read and raised as the client's own error. With ``stream`` the caller owns the response
        and closes it.
        """
        token = await resolve_token(self._token, self.what)
        if not token:
            raise TypeError(f"The {self.what} access token provider returned an empty token")
        merged = {**self._headers, "Accept": accept, "Authorization": f"Bearer {token}", **(headers or {})}
        if body is not None:
            merged["Content-Type"] = "application/json"
        request = self._http.build_request(
            method,
            f"{self.gateway_host}{path}",
            headers=merged,
            content=body,
            timeout=self._timeout,
        )
        response = await self._http.send(request, stream=stream, follow_redirects=False)
        if not 200 <= response.status_code < 300:
            try:
                if stream:
                    await response.aread()
                raise self._refuse(response)
            finally:
                await response.aclose()
        return response
