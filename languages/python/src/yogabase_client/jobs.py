"""Authored async facade for the Yogabase application gateway's durable Jobs routes.

The product's client OpenAPI document describes the Jobs routes, so Kiota also generates request builders for
them (``client.v1.projects.by_project_id(id).jobs``). This client sits beside those builders, not on top of
them: a generated builder cannot hand back a response header, and the result route's integrity check is one
(the bytes are verified here against ``X-Yogabase-Content-Sha256``), and each call is validated before it is
sent and every refusal is named by its code. It is deliberately separate from the generated application
client: like :class:`AsyncDeliveryClient` it takes its own gateway host and access token.
``tests/test_jobs.py`` pins every shape and holds each request to the one the generated builder builds.

A job belongs to the signed-in user who submitted it. Every call is authenticated with that user's
access token against ``/v1/projects/{projectId}/jobs``, behind the project token's ``lambda.invoke`` scope.
"""

from __future__ import annotations

import base64
import hashlib
import json
import re
from collections.abc import Mapping
from dataclasses import dataclass
from typing import Any

import httpx

from ._gateway import (
    CONTROL_CHARACTER,
    TIMESTAMP,
    GatewayAccessToken,
    GatewayChannel,
    is_non_zero_uuid,
    read_problem,
    require_uuid,
)

#: The statuses a job settles in: it will not change again. ``Failed`` is a checkpoint on the way to a
#: retry (``Queued`` again) or to ``DeadLettered``; a job never rests in it, so it is not terminal.
YOGABASE_JOB_TERMINAL_STATUSES = ("Succeeded", "Cancelled", "DeadLettered")
YOGABASE_JOB_STATUSES = ("Queued", "Running", "Succeeded", "Failed", "Cancelled", "DeadLettered")

#: The bounds of the Jobs routes, which the client checks before it sends a request.
YOGABASE_JOBS_MAXIMUM_PAYLOAD_BYTES = 1_048_576
YOGABASE_JOBS_MAXIMUM_ATTEMPTS = 10
YOGABASE_JOBS_MAXIMUM_JOB_TYPE_LENGTH = 128
YOGABASE_JOBS_MAXIMUM_IDEMPOTENCY_KEY_LENGTH = 256
YOGABASE_JOBS_MAXIMUM_RESULT_BYTES = 16 * 1_048_576

#: The codes the Jobs routes refuse with, as ``YogabaseJobsError.code`` carries them. The ``job_*``
#: codes up to ``job_result_reference_invalid`` are the gateway's; the last two are this client's own,
#: for an answer that is not what the route declares.
JOB_TYPE_NOT_REGISTERED = "job_type_not_registered"
JOB_IDEMPOTENCY_CONFLICT = "job_idempotency_conflict"
JOB_PAYLOAD_TOO_LARGE = "job_payload_too_large"
JOB_REQUEST_INVALID = "job_request_invalid"
JOB_NOT_FOUND = "job_not_found"
JOB_ACCESS_DENIED = "job_access_denied"
JOB_UNAVAILABLE = "job_unavailable"
JOB_RESULT_NOT_FOUND = "job_result_not_found"
JOB_RESULT_NOT_READY = "job_result_not_ready"
JOB_RESULT_INTEGRITY_FAILED = "job_result_integrity_failed"
JOB_RESULT_REFERENCE_INVALID = "job_result_reference_invalid"
JOB_RESPONSE_INVALID = "job_response_invalid"
JOB_RESULT_DIGEST_MISMATCH = "job_result_digest_mismatch"

_RESULT_DIGEST_HEADER = "X-Yogabase-Content-Sha256"
_JOB_TYPE = re.compile(r"[A-Za-z0-9._:-]{1,128}")
_FAILURE_CODE = re.compile(r"[a-z0-9_]{1,128}")
_RESULT_DIGEST = re.compile(r"sha256:[0-9a-f]{64}")
_REFERENCE_FORBIDDEN = re.compile(r"[\s\u0000-\u001f\u007f-\u009f]")

JobsAccessToken = GatewayAccessToken


@dataclass(frozen=True)
class YogabaseJob:
    """One durable job as the gateway reads it back. The payload never travels back."""

    job_id: str
    #: The id of the function the job runs.
    job_type: str
    #: ``Queued``, ``Running``, ``Succeeded``, ``Failed``, ``Cancelled`` or ``DeadLettered``.
    status: str
    #: How many attempts have started; 0 while the job has never run.
    attempt: int
    max_attempts: int
    #: How far the function says it has got, 0 through 100.
    progress_percent: int
    #: True once a cancel has been asked for; a running job is stopped by its worker, so this can
    #: precede ``Cancelled``.
    cancellation_requested: bool
    #: A name for the stored result, not a capability: read the bytes with ``result()``. None while there is none.
    result_reference: str | None
    #: Why the job last failed, as the function or the worker named it, or None.
    failure_code: str | None
    created_at: str
    updated_at: str
    completed_at: str | None

    @property
    def is_terminal(self) -> bool:
        """True once the job has settled: ``Succeeded``, ``Cancelled`` or ``DeadLettered``."""
        return self.status in YOGABASE_JOB_TERMINAL_STATUSES


@dataclass(frozen=True)
class YogabaseJobResult:
    """The stored result of a job that succeeded with one: exactly the bytes the function returned."""

    content: bytes
    #: ``sha256:`` and sixty-four lowercase hexadecimal digits, as sent in ``X-Yogabase-Content-Sha256``
    #: and as ``content`` hashes to.
    digest: str


@dataclass(frozen=True)
class JobsClientOptions:
    #: The installation's application-gateway origin, such as ``https://apps.example.test``.
    gateway_host: str
    #: The project's id, a canonical UUID (the bootstrap document's ``projectId``).
    project_id: str
    #: The signed-in user's access token, or a (possibly async) function that returns the current one.
    access_token: JobsAccessToken
    timeout: float = 30.0
    headers: Mapping[str, str] | None = None
    #: Accept a plain ``http`` gateway origin when it is a loopback host.
    allow_insecure_loopback: bool = False


class YogabaseJobsError(Exception):
    """A non-successful Jobs response, or an answer this client refuses, with the gateway's problem details."""

    def __init__(
        self,
        message: str,
        status: int,
        code: str,
        problem: dict[str, Any] | None = None,
        correlation_id: str | None = None,
        limit_bytes: int | None = None,
        max_body_bytes: int | None = None,
    ) -> None:
        super().__init__(message)
        self.status = status
        #: The gateway's stable code, or one derived from the status when the body names none.
        self.code = code
        self.problem = problem
        self.correlation_id = correlation_id
        #: The most bytes the refused payload may have, before base64, when a 413 named it (``limitBytes``).
        self.limit_bytes = limit_bytes
        #: The most bytes the whole request body may have, when a 413 named it (``maxBodyBytes``).
        self.max_body_bytes = max_body_bytes


class AsyncJobsClient:
    """Async client for the application gateway's durable Jobs routes.

    Submit a run of a deployed stateless function, read it, cancel it and fetch what it returned. No
    capability gates it: the bootstrap document carries no capability identifier for Jobs, so the client
    does not pretend to know whether an installation serves them. An installation without them answers a
    plain 404 with code ``not_found``; a missing job is ``job_not_found``, a different code.
    """

    def __init__(
        self,
        options: JobsClientOptions | None = None,
        *,
        gateway_host: str | None = None,
        project_id: str | None = None,
        access_token: JobsAccessToken | None = None,
        timeout: float = 30.0,
        headers: Mapping[str, str] | None = None,
        allow_insecure_loopback: bool = False,
        http_client: httpx.AsyncClient | None = None,
    ) -> None:
        if options is not None and any(value is not None for value in (gateway_host, project_id, access_token, headers)):
            raise TypeError("pass either options or keyword client settings, not both")
        if options is None:
            if gateway_host is None or project_id is None or access_token is None:
                raise TypeError("gateway_host, project_id and access_token are required")
            options = JobsClientOptions(gateway_host, project_id, access_token, timeout, headers, allow_insecure_loopback)
        self._channel = GatewayChannel(
            "jobs",
            gateway_host=options.gateway_host,
            project_id=options.project_id,
            access_token=options.access_token,
            timeout=options.timeout,
            headers=options.headers,
            allow_insecure_loopback=options.allow_insecure_loopback,
            http_client=http_client,
            refuse=_jobs_error,
        )
        self.gateway_host = self._channel.gateway_host
        self.project_id = self._channel.project_id

    async def __aenter__(self) -> "AsyncJobsClient":
        return self

    async def __aexit__(self, *_: object) -> None:
        await self.aclose()

    async def aclose(self) -> None:
        await self._channel.aclose()

    async def submit(
        self,
        *,
        job_type: str,
        idempotency_key: str,
        payload: bytes | None = None,
        max_attempts: int | None = None,
    ) -> YogabaseJob:
        """Starts a job and returns it as the gateway accepted it (202), normally ``Queued``.

        Idempotent per user and ``idempotency_key``: the same request again returns the job it already made,
        whatever that job has become; a different request under that key is refused with
        ``job_idempotency_conflict``. The key belongs to the signed-in user. Keep one key across the retries
        of one submit and use a new one for a new job.

        ``job_type`` is the id of a deployed stateless function (1 to 128 ASCII letters, digits, ``.``, ``-``,
        ``_`` or ``:``); ``payload`` is at most 1 MiB; ``max_attempts`` is 1 through 10 (the gateway's default is 5).
        """
        _validate_submit(job_type, idempotency_key, payload, max_attempts)
        # The member order and the names are the gateway's submission, which refuses any other member. A job
        # with no payload and one with an empty payload are the same job to the gateway.
        body: dict[str, Any] = {"jobType": job_type, "idempotencyKey": idempotency_key}
        if payload:
            body["payloadBase64"] = base64.b64encode(bytes(payload)).decode("ascii")
        if max_attempts is not None:
            body["maxAttempts"] = max_attempts
        response = await self._send("POST", "", json.dumps(body, separators=(",", ":")).encode("utf-8"))
        return _read_job(response)

    async def get(self, job_id: str) -> YogabaseJob:
        """Reads the caller's own job. Poll this until it ``is_terminal``."""
        return _read_job(await self._send("GET", f"/{require_uuid(job_id, 'job_id')}"))

    async def cancel(self, job_id: str) -> YogabaseJob:
        """Asks for the caller's own job to stop, idempotently, and returns it.

        A queued job is cancelled at once, a running one is flagged (``cancellation_requested``) and stopped
        by its worker, and a settled one is returned as it is.
        """
        return _read_job(await self._send("POST", f"/{require_uuid(job_id, 'job_id')}/cancel"))

    async def result(self, job_id: str) -> YogabaseJobResult:
        """Reads the result of the caller's own job.

        The bytes are hashed here and compared with the ``X-Yogabase-Content-Sha256`` the gateway sent; a
        result that does not match, or that arrives without exactly one well-formed ``sha256:`` digest, is
        refused and never returned.
        """
        path = f"/{require_uuid(job_id, 'job_id')}/result"
        response = await self._send("GET", path, accept="application/octet-stream, application/problem+json", stream=True)
        try:
            declared_values = response.headers.get_list(_RESULT_DIGEST_HEADER)
            if response.status_code != 200 or len(declared_values) != 1 or not _RESULT_DIGEST.fullmatch(declared_values[0]):
                raise _invalid("Yogabase returned an invalid job result.", response)
            declared = declared_values[0]
            length = response.headers.get("Content-Length")
            if length is not None and length.isdigit() and int(length) > YOGABASE_JOBS_MAXIMUM_RESULT_BYTES:
                raise _invalid("Yogabase returned an oversized job result.", response)
            chunks: list[bytes] = []
            total = 0
            async for chunk in response.aiter_bytes():
                total += len(chunk)
                if total > YOGABASE_JOBS_MAXIMUM_RESULT_BYTES:
                    raise _invalid("Yogabase returned an oversized job result.", response)
                chunks.append(chunk)
        finally:
            await response.aclose()
        content = b"".join(chunks)
        computed = "sha256:" + hashlib.sha256(content).hexdigest()
        if computed != declared:
            raise YogabaseJobsError(
                "The job result does not match the digest Yogabase sent with it, so none of it was returned.",
                502,
                JOB_RESULT_DIGEST_MISMATCH,
                None,
                response.headers.get("X-Correlation-ID"),
            )
        return YogabaseJobResult(content, declared)

    async def _send(
        self,
        method: str,
        suffix: str,
        body: bytes | None = None,
        *,
        accept: str = "application/json, application/problem+json",
        stream: bool = False,
    ) -> httpx.Response:
        return await self._channel.send(
            method,
            f"/v1/projects/{self.project_id}/jobs{suffix}",
            body,
            accept=accept,
            stream=stream,
        )


def _validate_submit(job_type: str, idempotency_key: str, payload: bytes | None, max_attempts: int | None) -> None:
    if not isinstance(job_type, str) or not _JOB_TYPE.fullmatch(job_type):
        raise ValueError("a job type is 1 to 128 ASCII letters, digits, '.', '-', '_' or ':', with no surrounding whitespace")
    if (
        not isinstance(idempotency_key, str)
        or not idempotency_key.strip()
        or len(idempotency_key) > YOGABASE_JOBS_MAXIMUM_IDEMPOTENCY_KEY_LENGTH
        or idempotency_key != idempotency_key.strip()
        or CONTROL_CHARACTER.search(idempotency_key)
    ):
        raise ValueError("an idempotency key is 1 to 256 characters, trimmed, with no control character")
    if payload is not None:
        if not isinstance(payload, (bytes, bytearray, memoryview)):
            raise TypeError("payload must be bytes")
        if len(payload) > YOGABASE_JOBS_MAXIMUM_PAYLOAD_BYTES:
            raise ValueError("a job payload is limited to one MiB before base64 encoding")
    if max_attempts is not None and (
        isinstance(max_attempts, bool)
        or not isinstance(max_attempts, int)
        or not 1 <= max_attempts <= YOGABASE_JOBS_MAXIMUM_ATTEMPTS
    ):
        raise ValueError("a job allows between 1 and 10 attempts")


def _invalid(message: str, response: httpx.Response) -> YogabaseJobsError:
    return YogabaseJobsError(message, 502, JOB_RESPONSE_INVALID, None, response.headers.get("X-Correlation-ID"))


def _jobs_error(response: httpx.Response) -> YogabaseJobsError:
    refusal = read_problem(response)
    return YogabaseJobsError(
        refusal.message,
        refusal.status,
        refusal.code,
        refusal.problem,
        refusal.correlation_id,
        refusal.limit_bytes,
        refusal.max_body_bytes,
    )


# A job the gateway describes wrongly is worse than one it refuses: a caller that polls for a state it was
# never told about waits for ever. Reject the answer instead of returning a guess. A member this client does
# not know is ignored, as the generated clients keep unknown members aside.
def _read_job(response: httpx.Response) -> YogabaseJob:
    def bad() -> YogabaseJobsError:
        return YogabaseJobsError("Yogabase returned an invalid job.", 502, JOB_RESPONSE_INVALID, None, response.headers.get("X-Correlation-ID"))

    try:
        document = response.json()
    except (ValueError, json.JSONDecodeError):
        raise bad() from None
    if not isinstance(document, dict):
        raise bad()

    def text(name: str) -> str | None:
        value = document.get(name)
        return value if isinstance(value, str) else None

    def whole(name: str) -> int | None:
        value = document.get(name)
        return value if isinstance(value, int) and not isinstance(value, bool) else None

    def nullable_text(name: str) -> str | None | bool:
        # False marks a member that is neither null nor text.
        value = document.get(name)
        return None if value is None else value if isinstance(value, str) else False

    job_id = text("jobId")
    job_type = text("jobType")
    status = text("status")
    attempt = whole("attempt")
    max_attempts = whole("maxAttempts")
    progress = whole("progressPercent")
    cancellation = document.get("cancellationRequested")
    result_reference = nullable_text("resultReference")
    failure_code = nullable_text("failureCode")
    created_at = text("createdAt")
    updated_at = text("updatedAt")
    completed_at = nullable_text("completedAt")
    if (
        job_id is None or not is_non_zero_uuid(job_id)
        or job_type is None or not _JOB_TYPE.fullmatch(job_type)
        or status is None or status not in YOGABASE_JOB_STATUSES
        or attempt is None or attempt < 0
        or max_attempts is None or not 1 <= max_attempts <= YOGABASE_JOBS_MAXIMUM_ATTEMPTS
        or progress is None or not 0 <= progress <= 100
        or not isinstance(cancellation, bool)
        or result_reference is False
        or (isinstance(result_reference, str) and not _is_result_reference(result_reference))
        or failure_code is False
        or (isinstance(failure_code, str) and not _FAILURE_CODE.fullmatch(failure_code))
        or created_at is None or not TIMESTAMP.fullmatch(created_at)
        or updated_at is None or not TIMESTAMP.fullmatch(updated_at)
        or completed_at is False
        or (isinstance(completed_at, str) and not TIMESTAMP.fullmatch(completed_at))
    ):
        raise bad()
    return YogabaseJob(
        job_id,
        job_type,
        status,
        attempt,
        max_attempts,
        progress,
        cancellation,
        result_reference,
        failure_code,
        created_at,
        updated_at,
        completed_at,
    )


def _is_result_reference(value: str) -> bool:
    # The reference is opaque to a client: bounded, an object:// name, and nothing that could be read as
    # more than text. Its grammar belongs to the gateway and is not restated here.
    return 9 < len(value) <= 2048 and value.startswith("object://") and not _REFERENCE_FORBIDDEN.search(value)
