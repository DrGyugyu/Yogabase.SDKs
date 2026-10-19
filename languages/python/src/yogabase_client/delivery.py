"""Authored async facade for the separate Yogabase workload Delivery API.

The application gateway client remains generated in :mod:`yogabase_client`. This module is
maintained from ``contracts/yogabase-delivery-v1.openapi.json`` and deliberately exposes no
application-gateway or provider-specific compatibility operations.
"""

from __future__ import annotations

import asyncio
import inspect
import json
import re
import ssl
from collections.abc import AsyncGenerator, Awaitable, Callable, Mapping
from dataclasses import dataclass
from typing import Any, Generic, TypeVar, TypedDict, cast
from urllib.parse import quote, urlsplit, urlunsplit

import httpx

Uuid = str
Sha256Digest = str
YOGABASE_DELIVERY_CONTRACT_SHA256 = "d44f95c0364d49071aa29e0d555687c942d91acbc9215cb71bd1eefb294d9c39"
DELIVERY_TERMINAL_OPERATION_STATES = ("Succeeded", "Failed", "Abandoned", "Cancelled", "Blocked")
AccessToken = str | Callable[[], str | Awaitable[str]]
T = TypeVar("T")


def platform_ssl_context() -> ssl.SSLContext:
    """A verifying TLS context over the platform's certificate store, not certifi's bundle.

    ``ssl.create_default_context()`` with no ``cafile`` loads the interpreter's default trust: the
    OpenSSL default verify paths, plus the system stores on Windows. It requires a valid peer
    certificate and a matching host name. httpx builds its own default context from certifi's
    bundle, so every client this package creates passes this one instead; pass it to any
    ``httpx.AsyncClient`` you hand to a Kiota ``HttpxRequestAdapter`` as ``verify=``. certifi stays
    installed because httpx and httpcore require it, but its bundle is not what these clients trust.
    """
    return ssl.create_default_context()


class DeliveryEnvironmentPreviewRequest(TypedDict):
    name: str
    templateId: Uuid
    templateVersion: int


class DeliveryArtifactPreviewRequest(TypedDict):
    sourceLocator: str
    bundleDigest: Sha256Digest
    manifestDigest: Sha256Digest
    sbomDigest: Sha256Digest
    scanEvidenceDigest: Sha256Digest
    artifactVersion: int
    sourceSizeBytes: int
    sourceMediaType: str
    manifestUri: str
    sbomUri: str
    sbomFormat: str
    scanUri: str
    scanner: str
    scannedAt: str
    criticalFindings: int
    highFindings: int


class _DeliveryReleaseConfigurationRequired(TypedDict):
    statelessFunctions: bool
    scheduler: bool
    jobs: bool


class DeliveryReleaseConfiguration(_DeliveryReleaseConfigurationRequired, total=False):
    maxConcurrentExecutions: int
    maxCatchUp: int
    # The longest a durable job's function may run, in seconds (1 through 900), valid only with
    # ``jobs``. A release carries the whole configuration, so one that omits it returns the
    # environment to the 30-second default, as it does ``maxCatchUp``.
    jobsMaxRunSeconds: int
    allowedTimeZones: list[str]


class DeliveryReleasePreviewRequest(TypedDict):
    artifactAdmissionIds: list[Uuid]
    configuration: DeliveryReleaseConfiguration
    migrationEvidenceDigest: Sha256Digest


class DeliveryPromotionPreviewRequest(TypedDict):
    sourceEnvironmentId: Uuid
    sourceReleaseId: Uuid
    migrationEvidenceDigest: Sha256Digest


class DeliveryRollbackPreviewRequest(TypedDict):
    releaseId: Uuid
    migrationEvidenceDigest: Sha256Digest


class DeliverySubmitRequest(TypedDict):
    previewId: Uuid
    previewDigest: Sha256Digest


class DeliveryPreviewResponse(TypedDict):
    previewId: Uuid
    previewDigest: Sha256Digest
    action: str
    expectedRevision: int
    approvalRequired: bool
    expiresAt: str


class _DeliveryTemplateResponseRequired(TypedDict):
    id: Uuid
    name: str
    version: int
    state: str
    targetId: Uuid
    templateDigest: Sha256Digest
    revision: int


class DeliveryTemplateResponse(_DeliveryTemplateResponseRequired, total=False):
    hostedServicePolicyDigest: Sha256Digest | None


class _DeliveryEnvironmentResponseRequired(TypedDict):
    id: Uuid
    name: str
    slug: str
    lifecycle: str
    isProduction: bool
    templateId: Uuid
    templateVersion: int
    runtimeRevision: int
    revision: int


class DeliveryEnvironmentResponse(_DeliveryEnvironmentResponseRequired, total=False):
    currentReleaseId: Uuid | None


class DeliveryArtifactResponse(TypedDict):
    id: Uuid
    status: str
    artifactVersion: int
    bundleDigest: Sha256Digest
    manifestDigest: Sha256Digest
    sbomDigest: Sha256Digest
    scanResultDigest: Sha256Digest
    signerId: str
    keyId: str
    revision: int


class _DeliveryReleaseResponseRequired(TypedDict):
    id: Uuid
    releaseType: str
    desiredRuntimeRevision: int
    artifactSetDigest: Sha256Digest
    configurationDigest: Sha256Digest
    migrationEvidenceDigest: Sha256Digest
    status: str
    revision: int


class DeliveryReleaseResponse(_DeliveryReleaseResponseRequired, total=False):
    sourceReleaseId: Uuid | None


class _DeliveryOperationEnvelopeRequired(TypedDict):
    operationId: Uuid
    state: str
    kind: str
    revision: int
    updatedAt: str


class DeliveryOperationEnvelope(_DeliveryOperationEnvelopeRequired, total=False):
    resourceType: str | None
    resourceId: Uuid | None


class DeliveryOperationEvent(TypedDict):
    operationId: Uuid
    state: str
    detail: str
    occurredAt: str


class _DeliveryOperationStreamEventRequired(TypedDict):
    data: DeliveryOperationEvent


class DeliveryOperationStreamEvent(_DeliveryOperationStreamEventRequired, total=False):
    """One SSE event; ``id`` is opaque and is never persisted or interpreted by the client."""

    id: str
    event: str


class _DeliveryTemplateListRequired(TypedDict):
    items: list[DeliveryTemplateResponse]


class DeliveryTemplateList(_DeliveryTemplateListRequired, total=False):
    nextCursor: str | None


class _DeliveryEnvironmentListRequired(TypedDict):
    items: list[DeliveryEnvironmentResponse]


class DeliveryEnvironmentList(_DeliveryEnvironmentListRequired, total=False):
    nextCursor: str | None


class _DeliveryArtifactListRequired(TypedDict):
    items: list[DeliveryArtifactResponse]


class DeliveryArtifactList(_DeliveryArtifactListRequired, total=False):
    nextCursor: str | None


class _DeliveryReleaseListRequired(TypedDict):
    items: list[DeliveryReleaseResponse]


class DeliveryReleaseList(_DeliveryReleaseListRequired, total=False):
    nextCursor: str | None


class _DeliveryOperationListRequired(TypedDict):
    items: list[DeliveryOperationEnvelope]


class DeliveryOperationList(_DeliveryOperationListRequired, total=False):
    nextCursor: str | None


class _ProblemDetailsRequired(TypedDict):
    title: str
    status: int
    code: str
    correlationId: str


class ProblemDetails(_ProblemDetailsRequired, total=False):
    type: str
    detail: str
    instance: str


@dataclass(frozen=True)
class DeliveryClientOptions:
    delivery_host: str
    access_token: AccessToken
    timeout: float = 30.0
    headers: Mapping[str, str] | None = None


@dataclass(frozen=True)
class DeliveryResponse(Generic[T]):
    data: T
    status: int
    etag: str | None = None
    location: str | None = None
    retry_after: int | None = None
    correlation_id: str | None = None


class YogabaseDeliveryError(Exception):
    """A non-successful Delivery API response with its HTTP/problem details."""

    def __init__(
        self,
        message: str,
        status: int,
        problem: ProblemDetails | None = None,
        correlation_id: str | None = None,
    ) -> None:
        super().__init__(message)
        self.status = status
        self.problem = problem
        self.correlation_id = correlation_id


class DeliveryPollingTimeoutError(TimeoutError):
    """The operation remained non-terminal within the caller's explicit polling bounds."""

    def __init__(self, last_operation: DeliveryOperationEnvelope | None = None) -> None:
        super().__init__("The Yogabase delivery operation did not reach a terminal state within the polling bounds.")
        self.last_operation = last_operation


class AsyncDeliveryClient:
    """Async client for all 25 operations in the Yogabase Delivery API contract."""

    def __init__(
        self,
        options: DeliveryClientOptions | None = None,
        *,
        delivery_host: str | None = None,
        access_token: AccessToken | None = None,
        timeout: float = 30.0,
        headers: Mapping[str, str] | None = None,
        http_client: httpx.AsyncClient | None = None,
    ) -> None:
        if options is not None and any(value is not None for value in (delivery_host, access_token, headers)):
            raise TypeError("pass either options or keyword client settings, not both")
        if options is None:
            if delivery_host is None or access_token is None:
                raise TypeError("delivery_host and access_token are required")
            options = DeliveryClientOptions(delivery_host, access_token, timeout, headers)
        self.delivery_host = _normalize_host(options.delivery_host)
        self._token = options.access_token
        self._timeout = _bounded_timeout(options.timeout)
        self._headers = dict(options.headers or {})
        self._http = http_client or httpx.AsyncClient(verify=platform_ssl_context())
        self._owns_http = http_client is None

    async def __aenter__(self) -> "AsyncDeliveryClient":
        return self

    async def __aexit__(self, *_: object) -> None:
        await self.aclose()

    async def aclose(self) -> None:
        if self._owns_http:
            await self._http.aclose()

    async def list_templates(self, project_id: Uuid, *, limit: int | None = None, cursor: str | None = None) -> DeliveryResponse[DeliveryTemplateList]:
        return await self._request("GET", f"/projects/{_segment(project_id)}/templates", params=_list_params(limit, cursor))

    async def get_template(self, project_id: Uuid, template_id: Uuid) -> DeliveryResponse[DeliveryTemplateResponse]:
        return await self._request("GET", f"/projects/{_segment(project_id)}/templates/{_segment(template_id)}")

    async def list_environments(self, project_id: Uuid, *, limit: int | None = None, cursor: str | None = None) -> DeliveryResponse[DeliveryEnvironmentList]:
        return await self._request("GET", f"/projects/{_segment(project_id)}/environments", params=_list_params(limit, cursor))

    async def preview_environment(self, project_id: Uuid, body: DeliveryEnvironmentPreviewRequest, *, if_match: str) -> DeliveryResponse[DeliveryPreviewResponse]:
        return await self._request("POST", f"/projects/{_segment(project_id)}/environments/previews", body, {"If-Match": _etag(if_match)})

    async def submit_environment(self, project_id: Uuid, body: DeliverySubmitRequest, *, idempotency_key: str) -> DeliveryResponse[DeliveryOperationEnvelope]:
        return await self._request("POST", f"/projects/{_segment(project_id)}/environments", body, {"Idempotency-Key": _idempotency_key(idempotency_key)})

    async def get_environment(self, project_id: Uuid, environment_id: Uuid) -> DeliveryResponse[DeliveryEnvironmentResponse]:
        return await self._request("GET", f"/projects/{_segment(project_id)}/environments/{_segment(environment_id)}")

    async def list_artifacts(self, project_id: Uuid, environment_id: Uuid, *, limit: int | None = None, cursor: str | None = None) -> DeliveryResponse[DeliveryArtifactList]:
        return await self._request("GET", f"/projects/{_segment(project_id)}/environments/{_segment(environment_id)}/artifacts", params=_list_params(limit, cursor))

    async def preview_artifact_admission(self, project_id: Uuid, environment_id: Uuid, body: DeliveryArtifactPreviewRequest, *, if_match: str) -> DeliveryResponse[DeliveryPreviewResponse]:
        return await self._request("POST", f"/projects/{_segment(project_id)}/environments/{_segment(environment_id)}/artifacts/previews", body, {"If-Match": _etag(if_match)})

    async def submit_artifact_admission(self, project_id: Uuid, environment_id: Uuid, body: DeliverySubmitRequest, *, idempotency_key: str) -> DeliveryResponse[DeliveryOperationEnvelope]:
        return await self._request("POST", f"/projects/{_segment(project_id)}/environments/{_segment(environment_id)}/artifacts", body, {"Idempotency-Key": _idempotency_key(idempotency_key)})

    async def get_artifact(self, project_id: Uuid, environment_id: Uuid, admission_id: Uuid) -> DeliveryResponse[DeliveryArtifactResponse]:
        return await self._request("GET", f"/projects/{_segment(project_id)}/environments/{_segment(environment_id)}/artifacts/{_segment(admission_id)}")

    async def list_releases(self, project_id: Uuid, environment_id: Uuid, *, limit: int | None = None, cursor: str | None = None) -> DeliveryResponse[DeliveryReleaseList]:
        return await self._request("GET", f"/projects/{_segment(project_id)}/environments/{_segment(environment_id)}/releases", params=_list_params(limit, cursor))

    async def preview_release(self, project_id: Uuid, environment_id: Uuid, body: DeliveryReleasePreviewRequest, *, if_match: str) -> DeliveryResponse[DeliveryPreviewResponse]:
        return await self._request("POST", f"/projects/{_segment(project_id)}/environments/{_segment(environment_id)}/releases/previews", body, {"If-Match": _etag(if_match)})

    async def submit_release(self, project_id: Uuid, environment_id: Uuid, body: DeliverySubmitRequest, *, idempotency_key: str) -> DeliveryResponse[DeliveryOperationEnvelope]:
        return await self._request("POST", f"/projects/{_segment(project_id)}/environments/{_segment(environment_id)}/releases", body, {"Idempotency-Key": _idempotency_key(idempotency_key)})

    async def get_release(self, project_id: Uuid, environment_id: Uuid, release_id: Uuid) -> DeliveryResponse[DeliveryReleaseResponse]:
        return await self._request("GET", f"/projects/{_segment(project_id)}/environments/{_segment(environment_id)}/releases/{_segment(release_id)}")

    async def preview_promotion(self, project_id: Uuid, environment_id: Uuid, body: DeliveryPromotionPreviewRequest, *, if_match: str) -> DeliveryResponse[DeliveryPreviewResponse]:
        return await self._request("POST", f"/projects/{_segment(project_id)}/environments/{_segment(environment_id)}/promotions/previews", body, {"If-Match": _etag(if_match)})

    async def submit_promotion(self, project_id: Uuid, environment_id: Uuid, body: DeliverySubmitRequest, *, idempotency_key: str) -> DeliveryResponse[DeliveryOperationEnvelope]:
        return await self._request("POST", f"/projects/{_segment(project_id)}/environments/{_segment(environment_id)}/promotions", body, {"Idempotency-Key": _idempotency_key(idempotency_key)})

    async def preview_rollback(self, project_id: Uuid, environment_id: Uuid, body: DeliveryRollbackPreviewRequest, *, if_match: str) -> DeliveryResponse[DeliveryPreviewResponse]:
        return await self._request("POST", f"/projects/{_segment(project_id)}/environments/{_segment(environment_id)}/rollbacks/previews", body, {"If-Match": _etag(if_match)})

    async def submit_rollback(self, project_id: Uuid, environment_id: Uuid, body: DeliverySubmitRequest, *, idempotency_key: str) -> DeliveryResponse[DeliveryOperationEnvelope]:
        return await self._request("POST", f"/projects/{_segment(project_id)}/environments/{_segment(environment_id)}/rollbacks", body, {"Idempotency-Key": _idempotency_key(idempotency_key)})

    async def preview_decommission(self, project_id: Uuid, environment_id: Uuid, *, if_match: str) -> DeliveryResponse[DeliveryPreviewResponse]:
        return await self._request("POST", f"/projects/{_segment(project_id)}/environments/{_segment(environment_id)}/decommissions/previews", headers={"If-Match": _etag(if_match)})

    async def submit_decommission(self, project_id: Uuid, environment_id: Uuid, body: DeliverySubmitRequest, *, idempotency_key: str) -> DeliveryResponse[DeliveryOperationEnvelope]:
        return await self._request("POST", f"/projects/{_segment(project_id)}/environments/{_segment(environment_id)}/decommissions", body, {"Idempotency-Key": _idempotency_key(idempotency_key)})

    async def list_operations(self, project_id: Uuid, *, limit: int | None = None, cursor: str | None = None) -> DeliveryResponse[DeliveryOperationList]:
        return await self._request("GET", f"/projects/{_segment(project_id)}/operations", params=_list_params(limit, cursor))

    async def get_operation(self, project_id: Uuid, operation_id: Uuid) -> DeliveryResponse[DeliveryOperationEnvelope]:
        return await self._request("GET", f"/projects/{_segment(project_id)}/operations/{_segment(operation_id)}")

    async def preview_abandon(self, project_id: Uuid, operation_id: Uuid, *, if_match: str) -> DeliveryResponse[DeliveryPreviewResponse]:
        return await self._request("POST", f"/projects/{_segment(project_id)}/operations/{_segment(operation_id)}/abandon/previews", headers={"If-Match": _etag(if_match)})

    async def submit_abandon(self, project_id: Uuid, operation_id: Uuid, body: DeliverySubmitRequest, *, idempotency_key: str) -> DeliveryResponse[DeliveryOperationEnvelope]:
        return await self._request("POST", f"/projects/{_segment(project_id)}/operations/{_segment(operation_id)}/abandon", body, {"Idempotency-Key": _idempotency_key(idempotency_key)})

    async def stream_operation_events(
        self,
        project_id: Uuid,
        *,
        environment_id: Uuid | None = None,
        operation_id: Uuid | None = None,
        last_event_id: str | None = None,
    ) -> AsyncGenerator[DeliveryOperationStreamEvent, None]:
        token = await _resolve_token(self._token)
        if not token:
            raise TypeError("The delivery access token provider returned an empty token")
        headers = self._headers_for(token, {"Accept": "text/event-stream"})
        if last_event_id is not None:
            if not last_event_id or len(last_event_id) > 4096:
                raise ValueError("last_event_id must contain 1 through 4096 characters")
            headers["Last-Event-ID"] = last_event_id
        params = {key: value for key, value in {"environmentId": environment_id, "operationId": operation_id}.items() if value is not None}
        async with self._http.stream(
            "GET",
            self._url(f"/projects/{_segment(project_id)}/operations/events"),
            params=params,
            headers=headers,
            timeout=None,
            follow_redirects=False,
        ) as response:
            if response.status_code < 200 or response.status_code >= 300:
                await response.aread()
                raise _delivery_error(response)
            content_type = response.headers.get("content-type", "").lower()
            if not content_type.startswith("text/event-stream"):
                raise YogabaseDeliveryError("The delivery event endpoint did not return text/event-stream", response.status_code)
            event_lines: list[str] = []
            async for line in response.aiter_lines():
                if line == "":
                    event = _parse_sse_event(event_lines)
                    event_lines = []
                    if event is not None:
                        yield event
                else:
                    event_lines.append(line)
            event = _parse_sse_event(event_lines)
            if event is not None:
                yield event

    async def wait_for_operation(
        self,
        project_id: Uuid,
        operation_id: Uuid,
        *,
        terminal_states: tuple[str, ...] | list[str] = DELIVERY_TERMINAL_OPERATION_STATES,
        interval: float = 1.0,
        timeout: float = 300.0,
        max_attempts: int = 60,
        cancel_event: asyncio.Event | None = None,
    ) -> DeliveryOperationEnvelope:
        if not terminal_states:
            raise ValueError("terminal_states must not be empty")
        interval = _bounded_interval(interval)
        timeout = _bounded_timeout(timeout)
        if not isinstance(max_attempts, int) or max_attempts < 1:
            raise ValueError("max_attempts must be a positive integer")
        deadline = asyncio.get_running_loop().time() + timeout
        last: DeliveryOperationEnvelope | None = None

        async def get_operation_or_cancel() -> DeliveryResponse[DeliveryOperationEnvelope]:
            if cancel_event is None:
                return await self.get_operation(project_id, operation_id)
            request_task = asyncio.create_task(self.get_operation(project_id, operation_id))
            cancellation_task = asyncio.create_task(cancel_event.wait())
            try:
                done, _ = await asyncio.wait(
                    (request_task, cancellation_task),
                    return_when=asyncio.FIRST_COMPLETED,
                )
                if cancellation_task in done:
                    request_task.cancel()
                    raise asyncio.CancelledError
                return await request_task
            finally:
                for task in (request_task, cancellation_task):
                    if not task.done():
                        task.cancel()
                await asyncio.gather(request_task, cancellation_task, return_exceptions=True)

        for attempt in range(max_attempts):
            if cancel_event is not None and cancel_event.is_set():
                raise asyncio.CancelledError
            remaining = deadline - asyncio.get_running_loop().time()
            if remaining <= 0:
                raise DeliveryPollingTimeoutError(last)
            try:
                result = await asyncio.wait_for(
                    get_operation_or_cancel(),
                    timeout=remaining,
                )
            except asyncio.TimeoutError as exception:
                raise DeliveryPollingTimeoutError(last) from exception
            last = result.data
            if asyncio.get_running_loop().time() >= deadline:
                raise DeliveryPollingTimeoutError(last)
            if last.get("state") in terminal_states:
                return last
            remaining = deadline - asyncio.get_running_loop().time()
            if attempt + 1 < max_attempts and remaining > 0:
                sleep_for = min(interval, remaining)
                if cancel_event is None:
                    await asyncio.sleep(sleep_for)
                else:
                    try:
                        await asyncio.wait_for(cancel_event.wait(), timeout=sleep_for)
                    except asyncio.TimeoutError:
                        pass
                    if cancel_event.is_set():
                        raise asyncio.CancelledError
        raise DeliveryPollingTimeoutError(last)

    async def _request(
        self,
        method: str,
        path: str,
        body: Any = None,
        headers: Mapping[str, str] | None = None,
        *,
        params: Mapping[str, str | int] | None = None,
    ) -> DeliveryResponse[Any]:
        token = await _resolve_token(self._token)
        if not token:
            raise TypeError("The delivery access token provider returned an empty token")
        request_headers = self._headers_for(token, headers)
        response = await self._http.request(
            method,
            self._url(path),
            params=params,
            headers=request_headers,
            json=body if body is not None else None,
            timeout=self._timeout,
            follow_redirects=False,
        )
        if response.status_code < 200 or response.status_code >= 300:
            raise _delivery_error(response)
        data = None if response.status_code == 204 else response.json()
        return DeliveryResponse(data, response.status_code, response.headers.get("ETag"), response.headers.get("Location"), _retry_after(response.headers.get("Retry-After")), response.headers.get("X-Yogabase-Correlation-Id"))

    def _url(self, path: str) -> str:
        return f"{self.delivery_host}/api/v1/delivery{path}"

    def _headers_for(self, token: str, headers: Mapping[str, str] | None = None) -> dict[str, str]:
        result = {"Accept": "application/json", **self._headers, "Authorization": f"Bearer {token}"}
        if headers:
            result.update(headers)
        return result


async def _resolve_token(token: AccessToken) -> str:
    value = token() if callable(token) else token
    if inspect.isawaitable(value):
        value = await value
    if not isinstance(value, str):
        raise TypeError("The delivery access token provider must return a string")
    return value


def _normalize_host(value: str) -> str:
    if not value:
        raise TypeError("delivery_host is required")
    parsed = urlsplit(value)
    if parsed.scheme != "https" or not parsed.netloc or parsed.username or parsed.password or parsed.query or parsed.fragment or parsed.path not in ("", "/"):
        raise ValueError("delivery_host must be an origin-only HTTPS URL")
    return urlunsplit((parsed.scheme, parsed.netloc, "", "", ""))


def _segment(value: str) -> str:
    if not isinstance(value, str) or not value:
        raise TypeError("path identifiers must not be empty")
    return quote(value, safe="")


def _list_params(limit: int | None, cursor: str | None) -> dict[str, str | int]:
    if limit is not None and (not isinstance(limit, int) or limit < 1 or limit > 100):
        raise ValueError("limit must be an integer from 1 through 100")
    if cursor is not None and len(cursor) > 4096:
        raise ValueError("cursor exceeds the contract limit")
    return {key: value for key, value in {"limit": limit, "cursor": cursor}.items() if value is not None}


def _etag(value: str) -> str:
    if not re.fullmatch(r'W/"[0-9]+"', value):
        raise ValueError('if_match must match W/"<revision>"')
    return value


def _idempotency_key(value: str) -> str:
    if not isinstance(value, str) or not 1 <= len(value) <= 128:
        raise ValueError("idempotency_key must contain 1 through 128 characters")
    return value


def _bounded_timeout(value: float) -> float:
    if not isinstance(value, (int, float)) or not 0 < value <= 900:
        raise ValueError("timeout must be greater than 0 and no more than 900 seconds")
    return float(value)


def _bounded_interval(value: float) -> float:
    if not isinstance(value, (int, float)) or not 0 <= value <= 60:
        raise ValueError("interval must be between 0 and 60 seconds")
    return float(value)


def _retry_after(value: str | None) -> int | None:
    if value is None:
        return None
    try:
        parsed = int(value)
    except ValueError:
        return None
    return parsed if parsed >= 0 else None


def _delivery_error(response: httpx.Response) -> YogabaseDeliveryError:
    try:
        problem = cast(ProblemDetails, response.json())
    except (ValueError, json.JSONDecodeError):
        problem = None
    message = (problem or {}).get("detail") or (problem or {}).get("title") or f"Yogabase delivery request failed with HTTP {response.status_code}"
    correlation = (problem or {}).get("correlationId") or response.headers.get("X-Yogabase-Correlation-Id")
    return YogabaseDeliveryError(message, response.status_code, problem, correlation)


def _parse_sse_event(lines: list[str]) -> DeliveryOperationStreamEvent | None:
    if not lines:
        return None
    event: DeliveryOperationStreamEvent = {}
    data_lines: list[str] = []
    for line in lines:
        if line.startswith(":"):
            continue
        if line.startswith("id:"):
            event["id"] = _sse_field_value(line[3:])
        elif line.startswith("event:"):
            event["event"] = _sse_field_value(line[6:])
        elif line.startswith("data:"):
            data_lines.append(_sse_field_value(line[5:]))
    if not data_lines:
        return None
    event["data"] = cast(DeliveryOperationEvent, json.loads("\n".join(data_lines)))
    return event


def _sse_field_value(value: str) -> str:
    # The SSE grammar removes a single U+0020 after the field colon. Resume ids are opaque, so any
    # further leading whitespace belongs to the value and must round-trip unchanged.
    return value[1:] if value.startswith(" ") else value
