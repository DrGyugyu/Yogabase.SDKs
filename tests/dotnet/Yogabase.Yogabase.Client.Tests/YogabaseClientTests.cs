using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Yogabase.Yogabase;

namespace Yogabase.Tests;

public sealed class YogabaseClientTests
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task StorageSendsIntegrityHeadersChecksReceiptAndKeepsCallerStreamOpen()
    {
        var bytes = Encoding.UTF8.GetBytes("hello");
        var digest = "sha256:" + LowerHex(SHA256.HashData(bytes));
        string? requestDigest = null;
        string? idempotencyKey = null;
        byte[]? uploaded = null;
        var handler = new ScriptedHandler(async (request, cancellationToken) =>
        {
            requestDigest = request.Headers.GetValues("X-Yogabase-Content-Sha256").Single();
            idempotencyKey = request.Headers.GetValues("Idempotency-Key").Single();
            uploaded = await request.Content!.ReadAsByteArrayAsync(cancellationToken);
            return Json(HttpStatusCode.Created, JsonSerializer.Serialize(new
            {
                key = "avatars/a.txt",
                digest,
                sizeBytes = 5,
                encryption = "AES256",
                alreadyExisted = false,
            }));
        });
        var store = await AuthenticatedStoreAsync();
        await using var client = Client(handler, store);
        using var content = new MemoryStream(bytes);

        var result = await client.Storage.PutAsync(
            "avatars/a.txt",
            content,
            bytes.Length,
            "text/plain",
            digest,
            "request-1",
            TestContext.Current.CancellationToken);

        Assert.Equal("avatars/a.txt", result.Key);
        Assert.Equal(digest, requestDigest);
        Assert.Equal("request-1", idempotencyKey);
        Assert.Equal(bytes, uploaded);
        Assert.True(content.CanRead);
    }

    [Theory]
    [InlineData("projects/tenant/object")]
    [InlineData("a%2fb")]
    [InlineData("a\\b")]
    [InlineData("a//b")]
    [InlineData("a/../b")]
    public async Task StorageRejectsAmbiguousLogicalKeysBeforeTransport(string key)
    {
        var calls = 0;
        var handler = new ScriptedHandler((_, _) =>
        {
            calls++;
            return Task.FromResult(Json(HttpStatusCode.OK, "{}"));
        });
        var store = await AuthenticatedStoreAsync();
        await using var client = Client(handler, store);

        await Assert.ThrowsAsync<ArgumentException>(() => client.Storage.PutBytesAsync(
            key,
            [1],
            "application/octet-stream",
            "request-1",
            TestContext.Current.CancellationToken));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task FunctionsUseTheVersionedProjectRouteAndBoundedWireShape()
    {
        Uri? route = null;
        string? body = null;
        var invocationId = "33333333-3333-8333-8333-333333333333";
        var handler = new ScriptedHandler(async (request, cancellationToken) =>
        {
            route = request.RequestUri;
            body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return Json(HttpStatusCode.Accepted, JsonSerializer.Serialize(new
            {
                outcome = 0,
                invocationId,
                correlationId = "correlation-1",
            }));
        });
        var store = await AuthenticatedStoreAsync();
        await using var client = Client(handler, store);

        var result = await client.Functions.InvokeAsync(
            "resize",
            "1",
            new YogabaseInvocationRequest
            {
                IdempotencyKey = "request-1",
                CorrelationId = "correlation-1",
                Deadline = DateTimeOffset.UtcNow.AddSeconds(20),
                Mode = YogabaseInvocationMode.Asynchronous,
                InlinePayload = [1, 2, 3],
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(
            "http://localhost/v1/projects/11111111-1111-4111-8111-111111111111/functions/resize/versions/1/invoke",
            route!.AbsoluteUri);
        Assert.Equal(YogabaseInvocationOutcome.Accepted, result.Outcome);
        Assert.Contains("\"inlinePayloadBase64\":\"AQID\"", body, StringComparison.Ordinal);
        Assert.Contains("\"payloadDigest\":\"sha256:", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ActorsUseTheVersionedInstanceRouteAndValidateTheDurableResult()
    {
        Uri? route = null;
        string? body = null;
        var invocationId = "44444444-4444-8444-8444-444444444444";
        var handler = new ScriptedHandler(async (request, cancellationToken) =>
        {
            route = request.RequestUri;
            body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return Json(HttpStatusCode.OK, JsonSerializer.Serialize(new
            {
                status = "Completed",
                invocationId,
                correlationId = "actor-correlation",
                resultReference = "object://results/actor",
                stateVersion = 7,
            }));
        });
        var store = await AuthenticatedStoreAsync();
        await using var client = Client(handler, store);

        var result = await client.Actors.InvokeAsync(
            "counter",
            "1",
            "customer:42",
            new YogabaseActorInvocationRequest
            {
                IdempotencyKey = "actor-request-1",
                CorrelationId = "actor-correlation",
                Deadline = DateTimeOffset.UtcNow.AddSeconds(20),
                InlinePayload = [1, 2, 3],
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(
            "http://localhost/v1/projects/11111111-1111-4111-8111-111111111111/actors/counter/versions/1/instances/customer%3A42/invoke",
            route!.AbsoluteUri);
        Assert.Equal(YogabaseActorInvocationStatus.Completed, result.Status);
        Assert.Equal(7, result.StateVersion);
        Assert.Contains("\"inlinePayloadBase64\":\"AQID\"", body, StringComparison.Ordinal);
        Assert.Contains("\"payloadDigest\":\"sha256:", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WorkflowsUseTheVersionedStartAndCanonicalCancelRoutes()
    {
        var paths = new List<string>();
        var bodies = new List<string>();
        var workflowId = "55555555-5555-4555-8555-555555555555";
        var handler = new ScriptedHandler(async (request, cancellationToken) =>
        {
            paths.Add(request.RequestUri!.AbsolutePath);
            bodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
            return request.RequestUri.AbsolutePath.EndsWith("/start", StringComparison.Ordinal)
                ? Json(HttpStatusCode.OK, $"{{\"workflowInstanceId\":\"{workflowId}\"}}")
                : new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        var store = await AuthenticatedStoreAsync();
        await using var client = Client(handler, store);

        var started = await client.Workflows.StartAsync(
            "onboard",
            "12",
            new YogabaseWorkflowStartRequest
            {
                IdempotencyKey = "workflow-start-1",
                InlineInput = [4, 5],
            },
            TestContext.Current.CancellationToken);
        await client.Workflows.CancelAsync(
            started.WorkflowInstanceId,
            new YogabaseWorkflowCancelRequest
            {
                IdempotencyKey = "workflow-cancel-1",
                InlinePayload = [],
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(Guid.Parse(workflowId), started.WorkflowInstanceId);
        Assert.Equal(
            "/v1/projects/11111111-1111-4111-8111-111111111111/workflows/onboard/versions/12/start",
            paths[0]);
        Assert.Equal(
            $"/v1/projects/11111111-1111-4111-8111-111111111111/workflows/instances/{workflowId}/cancel",
            paths[1]);
        Assert.Contains("\"inlineInputBase64\":\"BAU=\"", bodies[0], StringComparison.Ordinal);
        Assert.Contains("\"inlinePayloadBase64\":\"\"", bodies[1], StringComparison.Ordinal);
    }

    [Fact]
    public async Task InteractiveAuthUsesThePublishedContractRoutesAndStatuses()
    {
        var observed = new List<(string Path, string? Body, AuthenticationHeaderValue? Authorization)>();
        var handler = new ScriptedHandler(async (request, cancellationToken) =>
        {
            var body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            observed.Add((request.RequestUri!.AbsolutePath, body, request.Headers.Authorization));
            return request.RequestUri.AbsolutePath switch
            {
                "/auth/login" => new HttpResponseMessage(HttpStatusCode.Accepted),
                "/auth/mfa/setup" => Json(HttpStatusCode.OK, "{\"sharedKey\":\"enrollment-secret\"}"),
                "/auth/mfa/recovery/regenerate" => Json(
                    HttpStatusCode.OK,
                    "{\"recoveryCodes\":[\"code-one\",\"code-two\"]}"),
                "/auth/password/reset/request" => new HttpResponseMessage(HttpStatusCode.Accepted),
                _ => new HttpResponseMessage(HttpStatusCode.NoContent),
            };
        });
        var store = await AuthenticatedStoreAsync();
        await using var client = Client(handler, store);
        var userId = Guid.Parse("66666666-6666-4666-8666-666666666666");

        await client.Auth.SignUpAsync(
            "user@example.com",
            "correct horse battery staple",
            "invitation-token",
            TestContext.Current.CancellationToken);
        var login = await client.Auth.LoginAsync(
            "user@example.com",
            "correct horse battery staple",
            TestContext.Current.CancellationToken);
        await client.Auth.LogoutAsync(TestContext.Current.CancellationToken);
        await client.Auth.ConfirmEmailAsync(userId, "email-token", TestContext.Current.CancellationToken);
        await client.Auth.RequestPasswordResetAsync("user@example.com", TestContext.Current.CancellationToken);
        await client.Auth.CompletePasswordResetAsync(
            userId,
            "reset-token",
            "replacement password",
            TestContext.Current.CancellationToken);
        var setup = await client.Auth.SetupMfaAsync(TestContext.Current.CancellationToken);
        await client.Auth.ConfirmMfaAsync("123456", TestContext.Current.CancellationToken);
        await client.Auth.VerifyMfaAsync("123456", TestContext.Current.CancellationToken);
        var recoveryCodes = await client.Auth.RegenerateRecoveryCodesAsync(TestContext.Current.CancellationToken);
        await client.Auth.UseRecoveryCodeAsync("code-one", TestContext.Current.CancellationToken);

        Assert.Equal(YogabaseInteractiveSignInOutcome.MfaRequired, login);
        Assert.Equal("enrollment-secret", setup.SharedKey);
        Assert.Equal(["code-one", "code-two"], recoveryCodes);
        Assert.Equal(
            [
                "/auth/signup",
                "/auth/login",
                "/auth/logout",
                "/auth/email/confirm",
                "/auth/password/reset/request",
                "/auth/password/reset/complete",
                "/auth/mfa/setup",
                "/auth/mfa/confirm",
                "/auth/mfa/verify",
                "/auth/mfa/recovery/regenerate",
                "/auth/mfa/recovery/use",
            ],
            observed.Select(item => item.Path));
        Assert.All(observed, item => Assert.Null(item.Authorization));
        Assert.Contains("\"invitationToken\":\"invitation-token\"", observed[0].Body, StringComparison.Ordinal);
        Assert.Contains("\"userId\":\"66666666-6666-4666-8666-666666666666\"", observed[3].Body, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(403)]
    [InlineData(503)]
    public async Task FunctionsSurfaceProblemResponsesWithoutParsingThemAsInvocationResults(int statusCode)
    {
        var handler = new ScriptedHandler((_, _) => Task.FromResult(new HttpResponseMessage(
            (HttpStatusCode)statusCode)
        {
            Content = new StringContent(
                "{\"code\":\"invocation_unavailable\",\"detail\":\"The invocation was not admitted.\"}",
                Encoding.UTF8,
                "application/problem+json"),
        }));
        var store = await AuthenticatedStoreAsync();
        await using var client = Client(handler, store);

        var exception = await Assert.ThrowsAsync<YogabaseApiException>(() => client.Functions.InvokeAsync(
            "resize",
            "1",
            new YogabaseInvocationRequest
            {
                IdempotencyKey = "request-1",
                CorrelationId = "correlation-1",
                Deadline = DateTimeOffset.UtcNow.AddSeconds(20),
                Mode = YogabaseInvocationMode.Asynchronous,
                InlinePayload = [1, 2, 3],
            },
            TestContext.Current.CancellationToken));

        Assert.Equal(statusCode, exception.StatusCode);
        Assert.Equal("invocation_unavailable", exception.Code);
        Assert.Equal("The invocation was not admitted.", exception.Message);
    }

    [Fact]
    public async Task BootstrapRejectsAnotherOrigin()
    {
        var payload = JsonSerializer.Serialize(
            WithGateway(Configuration(), "https://other.example/"),
            WebJson);
        var handler = new ScriptedHandler((_, _) => Task.FromResult(Json(HttpStatusCode.OK, payload)));

        var exception = await Assert.ThrowsAsync<YogabaseApiException>(() => YogabaseClient.CreateAsync(
            new Uri("http://localhost/"),
            "sy_pub_0123456789abcdef",
            new YogabaseClientOptions
            {
                AllowInsecureLoopback = true,
                TransportHandler = handler,
            },
            TestContext.Current.CancellationToken));

        Assert.Equal("client_config_origin_mismatch", exception.Code);
    }

    [Fact]
    public async Task BootstrapRejectsUnknownFields()
    {
        var serialized = JsonSerializer.Serialize(Configuration(), WebJson);
        var payload = serialized[..^1] + ",\"unexpected\":true}";
        var handler = new ScriptedHandler((_, _) => Task.FromResult(Json(HttpStatusCode.OK, payload)));

        var exception = await Assert.ThrowsAsync<YogabaseApiException>(() => YogabaseClient.CreateAsync(
            new Uri("http://localhost/"),
            "sy_pub_0123456789abcdef",
            new YogabaseClientOptions
            {
                AllowInsecureLoopback = true,
                TransportHandler = handler,
            },
            TestContext.Current.CancellationToken));

        Assert.Equal("client_config_invalid", exception.Code);
    }

    [Fact]
    public void ConfigurationIsDefensivelyCopiedAndUnknownCapabilitiesStayUnavailable()
    {
        var scopes = new List<string> { "openid", "offline_access" };
        var capabilities = Capabilities().ToList();
        var configuration = Configuration(scopes, capabilities);
        using var client = YogabaseClient.FromConfiguration(
            configuration,
            new YogabaseClientOptions
            {
                AllowInsecureLoopback = true,
                TransportHandler = new ScriptedHandler((_, _) =>
                    Task.FromResult(Json(HttpStatusCode.OK, "{}"))),
            });

        scopes[0] = "mutated";
        capabilities[0] = new YogabaseCapability { Id = "unknown", Available = true };

        Assert.Equal("openid", client.Configuration.OidcScopes[0]);
        Assert.Equal("http://localhost:5001/", client.Configuration.RealtimeEndpoint);
        Assert.True(client.Configuration.IsAvailable("auth"));
        Assert.False(client.Configuration.IsAvailable("unknown"));
    }

    [Fact]
    public void ConfigurationRejectsAnInsecureNonLoopbackRealtimeOrigin()
    {
        var source = Configuration();
        var invalid = new YogabaseClientConfiguration
        {
            Schema = source.Schema,
            ProjectId = source.ProjectId,
            EnvironmentId = source.EnvironmentId,
            GatewayEndpoint = source.GatewayEndpoint,
            RealtimeEndpoint = "http://realtime.example/",
            OidcAuthority = source.OidcAuthority,
            OidcDiscoveryEndpoint = source.OidcDiscoveryEndpoint,
            OidcPublicClientId = source.OidcPublicClientId,
            OidcScopes = source.OidcScopes,
            OidcRedirectUris = source.OidcRedirectUris,
            RealtimePath = source.RealtimePath,
            Capabilities = source.Capabilities,
            IssuedAt = source.IssuedAt,
        };

        Assert.Throws<ArgumentException>(() => YogabaseClient.FromConfiguration(
            invalid,
            new YogabaseClientOptions
            {
                AllowInsecureLoopback = true,
                TransportHandler = new ScriptedHandler((_, _) =>
                    Task.FromResult(Json(HttpStatusCode.OK, "{}"))),
            }));
    }

    [Fact]
    public async Task PkceRequiresMatchingStateBeforeAcceptingAResult()
    {
        var handler = new ScriptedHandler((request, _) =>
        {
            if (request.Method == HttpMethod.Get)
            {
                return Task.FromResult(Json(HttpStatusCode.OK, Discovery()));
            }
            return Task.FromResult(Json(
                HttpStatusCode.OK,
                "{\"access_token\":\"access-token\",\"token_type\":\"Bearer\",\"expires_in\":3600,\"refresh_token\":\"refresh-token\"}"));
        });
        await using var client = Client(handler, new InMemoryYogabaseSessionStore());

        var session = await client.Auth.SignInWithPkceAsync(
            new YogabasePkceOptions
            {
                RedirectUri = new Uri("com.stackyoga.app:/oauth/callback"),
                OpenBrowserAsync = (authorize, _) =>
                {
                    var state = ParseQuery(authorize.Query)["state"];
                    return Task.FromResult(new Uri($"com.stackyoga.app:/oauth/callback?code=code-1&state={state}"));
                },
            },
            TestContext.Current.CancellationToken);

        Assert.Equal("access-token", session.AccessToken);

        var forged = await Assert.ThrowsAsync<YogabaseApiException>(() => client.Auth.SignInWithPkceAsync(
            new YogabasePkceOptions
            {
                RedirectUri = new Uri("com.stackyoga.app:/oauth/callback"),
                OpenBrowserAsync = (_, _) => Task.FromResult(
                    new Uri("com.stackyoga.app:/oauth/callback?error=access_denied")),
            },
            TestContext.Current.CancellationToken));
        Assert.Equal("authorization_response_invalid", forged.Code);
    }

    [Fact]
    public async Task PasswordSignInLogsInThenAuthorizesWithAChallengeAndStoresTheTokens()
    {
        var seen = new List<(string Path, string Query, string Body)>();
        var store = new InMemoryYogabaseSessionStore();
        await using var client = Client(SignInGateway(seen, () => new(HttpStatusCode.NoContent), state => Found($"{Callback}?code=code-1&state={state}")), store);

        var session = await client.Auth.SignInWithPasswordAsync("user@example.com", "pw", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("access-1", session.AccessToken);
        Assert.Equal("refresh-1", (await store.LoadAsync(TestContext.Current.CancellationToken))!.RefreshToken);
        Assert.Equal(["/auth/login", "/.well-known/openid-configuration", "/connect/authorize", "/connect/token"], seen.Select(call => call.Path).ToArray());
        var authorize = ParseQuery(seen[2].Query);
        var token = ParseQuery(seen[3].Body);
        Assert.Equal(("S256", "code-1", Callback), (authorize["code_challenge_method"], token["code"], token["redirect_uri"]));
        var verifier = token["code_verifier"];
        Assert.Equal(authorize["code_challenge"], Convert.ToBase64String(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))).TrimEnd('=').Replace('+', '-').Replace('/', '_'));
        Assert.DoesNotContain(verifier, seen[2].Query, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "invalid_credentials")]
    [InlineData(HttpStatusCode.Forbidden, "mfa_enrollment_required")]
    [InlineData(HttpStatusCode.TooManyRequests, "authentication_rate_limited")]
    public async Task PasswordSignInSurfacesTheLoginRefusalAndGoesNoFurther(HttpStatusCode status, string code)
    {
        var seen = new List<(string Path, string Query, string Body)>();
        var store = new InMemoryYogabaseSessionStore();
        await using var client = Client(SignInGateway(seen, () => Json(status, $"{{\"code\":\"{code}\"}}"), _ => Found(Callback)), store);

        var refused = await Assert.ThrowsAsync<YogabaseApiException>(() => client.Auth.SignInWithPasswordAsync("user@example.com", "wrong", cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(((int)status, code), (refused.StatusCode, refused.Code));
        Assert.Equal(["/auth/login"], seen.Select(call => call.Path).ToArray());
        Assert.Null(await store.LoadAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(null, "interactive_login_required")]
    [InlineData("com.stackyoga.app:/oauth/callback?code=code-1&state=forged", "authorization_response_invalid")]
    [InlineData("com.stackyoga.app:/oauth/callback?state={state}", "authorization_response_invalid")]
    [InlineData("com.stackyoga.app:/oauth/callback?error=access_denied&state={state}", "access_denied")]
    [InlineData("https://other.example/cb?code=code-1&state={state}", "authorization_redirect_mismatch")]
    public async Task PasswordSignInMakesNoTokenRequestUnlessAuthorizeAnswersWithTheCodeAndState(string? location, string code)
    {
        var seen = new List<(string Path, string Query, string Body)>();
        var store = new InMemoryYogabaseSessionStore();
        await using var client = Client(SignInGateway(seen, () => new(HttpStatusCode.NoContent), state => location is null
            ? Json(HttpStatusCode.Unauthorized, "{\"code\":\"interactive_login_required\"}")
            : Found(location.Replace("{state}", state, StringComparison.Ordinal))), store);

        var refused = await Assert.ThrowsAsync<YogabaseApiException>(() => client.Auth.SignInWithPasswordAsync("user@example.com", "pw", cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(code, refused.Code);
        Assert.DoesNotContain(seen, call => call.Path == "/connect/token");
        Assert.Null(await store.LoadAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PasswordSignInNamesMfaAndCompletesItWhenTheCodeIsPassed()
    {
        var seen = new List<(string Path, string Query, string Body)>();
        await using var client = Client(SignInGateway(seen, () => Json(HttpStatusCode.Accepted, "{\"code\":\"mfa_required\"}"), state => Found($"{Callback}?code=code-1&state={state}")), new InMemoryYogabaseSessionStore());

        var required = await Assert.ThrowsAsync<YogabaseApiException>(() => client.Auth.SignInWithPasswordAsync("user@example.com", "pw", cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal((202, "mfa_required"), (required.StatusCode, required.Code));
        Assert.Equal(["/auth/login"], seen.Select(call => call.Path).ToArray());

        seen.Clear();
        var session = await client.Auth.SignInWithPasswordAsync("user@example.com", "pw", "123456", TestContext.Current.CancellationToken);
        Assert.Equal("access-1", session.AccessToken);
        Assert.Equal(["/auth/login", "/auth/mfa/verify"], seen.Take(2).Select(call => call.Path).ToArray());
        Assert.Contains("123456", seen[1].Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PasswordSignInNeedsAHandlerThatKeepsTheLoginCookie()
    {
        // A real loopback gateway: only a request carrying the cookie /auth/login set is authorized.
        var tokenCalls = 0;
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        await using var gateway = builder.Build();
        gateway.MapPost("/auth/login", (HttpContext context) =>
        {
            context.Response.Cookies.Append("yogabase.project.identity", "signed-in");
            return Results.NoContent();
        });
        gateway.MapGet("/.well-known/openid-configuration", (HttpContext context) => Results.Text(
            $"{{\"issuer\":\"http://{context.Request.Host}/p/11111111-1111-4111-8111-111111111111\","
            + $"\"authorization_endpoint\":\"http://{context.Request.Host}/connect/authorize\","
            + $"\"token_endpoint\":\"http://{context.Request.Host}/connect/token\"}}", "application/json"));
        gateway.MapGet("/connect/authorize", (HttpContext context) => context.Request.Cookies.ContainsKey("yogabase.project.identity")
            ? Results.Redirect($"{Callback}?code=code-1&state={context.Request.Query["state"]}")
            : Results.Json(new { code = "interactive_login_required" }, statusCode: 401));
        gateway.MapPost("/connect/token", () =>
        {
            tokenCalls++;
            return Results.Text(TokenBody, "application/json");
        });
        await gateway.StartAsync(TestContext.Current.CancellationToken);
        var origin = gateway.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();

        await using var defaultHandler = YogabaseClient.FromConfiguration(Configuration(origin: origin), new YogabaseClientOptions { AllowInsecureLoopback = true });
        var session = await defaultHandler.Auth.SignInWithPasswordAsync("user@example.com", "pw", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(("access-1", 1), (session.AccessToken, tokenCalls));

        await using var noCookies = YogabaseClient.FromConfiguration(Configuration(origin: origin), new YogabaseClientOptions
        {
            AllowInsecureLoopback = true,
            TransportHandler = new HttpClientHandler { UseCookies = false, AllowAutoRedirect = false },
        });
        var refused = await Assert.ThrowsAsync<YogabaseApiException>(() => noCookies.Auth.SignInWithPasswordAsync("user@example.com", "pw", cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal((401, "interactive_login_required", 1), (refused.StatusCode, refused.Code, tokenCalls));
    }

    [Fact]
    public async Task ExternalSignInSendsAChallengeThenTheVerifierAndStoresTheTokens()
    {
        var seen = new List<(string Path, string Query, string Body)>();
        var store = new InMemoryYogabaseSessionStore();
        await using var client = Client(SignInGateway(seen, () => new(HttpStatusCode.NoContent), state => Found($"{Callback}?code=code-1&state={state}")), store);

        var pending = await client.Auth.StartExternalSignInAsync("google", Callback, TestContext.Current.CancellationToken);
        var session = await client.Auth.CompleteExternalSignInAsync(pending, new Uri($"{Callback}?code=provider-code&state=gw-state-1"), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("https://accounts.example/auth?state=gw-state-1", pending.AuthorizationUrl.AbsoluteUri);
        Assert.Equal(("access-1", "refresh-1"), (session.AccessToken, (await store.LoadAsync(TestContext.Current.CancellationToken))!.RefreshToken));
        Assert.Equal(
            ["/auth/external/google/start", "/auth/external/google/callback", "/.well-known/openid-configuration", "/connect/authorize", "/connect/token"],
            seen.Select(call => call.Path).ToArray());
        var start = JsonSerializer.Deserialize<Dictionary<string, string>>(seen[0].Body)!;
        var callback = JsonSerializer.Deserialize<Dictionary<string, string>>(seen[1].Body)!;
        var verifier = callback["codeVerifier"];
        Assert.InRange(verifier.Length, 43, 128);
        Assert.Equal((Callback, S256(verifier)), (start["redirectUri"], start["codeChallenge"]));
        Assert.DoesNotContain(verifier, seen[0].Body, StringComparison.Ordinal);
        Assert.Equal(("provider-code", "gw-state-1"), (callback["code"], callback["state"]));
    }

    [Theory]
    [InlineData("com.stackyoga.app:/oauth/callback?code=c&state=forged", "authorization_response_invalid")]
    [InlineData("com.stackyoga.app:/oauth/callback?error=access_denied&state=gw-state-1", "access_denied")]
    [InlineData("com.stackyoga.app:/oauth/callback?state=gw-state-1", "authorization_response_invalid")]
    [InlineData("https://other.example/cb?code=c&state=gw-state-1", "authorization_redirect_mismatch")]
    public async Task ExternalSignInMakesNoCallbackRequestForAForgedOrRefusedRedirect(string redirect, string code)
    {
        var seen = new List<(string Path, string Query, string Body)>();
        var store = new InMemoryYogabaseSessionStore();
        await using var client = Client(SignInGateway(seen, () => new(HttpStatusCode.NoContent), state => Found($"{Callback}?code=code-1&state={state}")), store);
        var pending = await client.Auth.StartExternalSignInAsync("google", Callback, TestContext.Current.CancellationToken);

        var refused = await Assert.ThrowsAsync<YogabaseApiException>(() => client.Auth.CompleteExternalSignInAsync(pending, new Uri(redirect), cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(code, refused.Code);
        Assert.Equal(["/auth/external/google/start"], seen.Select(call => call.Path).ToArray());
        Assert.Null(await store.LoadAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ExternalSignInOnlyCompletesFromTheRedirectOfItsOwnStart()
    {
        var seen = new List<(string Path, string Query, string Body)>();
        await using var client = Client(SignInGateway(seen, () => new(HttpStatusCode.NoContent), state => Found($"{Callback}?code=code-1&state={state}")), new InMemoryYogabaseSessionStore());
        var first = await client.Auth.StartExternalSignInAsync("google", Callback, TestContext.Current.CancellationToken);
        var second = await client.Auth.StartExternalSignInAsync("google", Callback, TestContext.Current.CancellationToken);

        var crossed = await Assert.ThrowsAsync<YogabaseApiException>(() => client.Auth.CompleteExternalSignInAsync(first, new Uri($"{Callback}?code=c&state=gw-state-2"), cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal("authorization_response_invalid", crossed.Code);
        Assert.DoesNotContain(seen, call => call.Path.EndsWith("/callback", StringComparison.Ordinal));
        Assert.Equal("access-1", (await client.Auth.CompleteExternalSignInAsync(second, new Uri($"{Callback}?code=c&state=gw-state-2"), cancellationToken: TestContext.Current.CancellationToken)).AccessToken);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "external_identity_invalid")]
    [InlineData(HttpStatusCode.Conflict, "external_identity_email_in_use")]
    [InlineData(HttpStatusCode.Forbidden, "external_email_unverified")]
    public async Task ExternalSignInSurfacesTheCallbackRefusalAndGoesNoFurther(HttpStatusCode status, string code)
    {
        var seen = new List<(string Path, string Query, string Body)>();
        var store = new InMemoryYogabaseSessionStore();
        await using var client = Client(SignInGateway(seen, () => Json(status, $"{{\"code\":\"{code}\"}}"), _ => Found(Callback)), store);
        var pending = await client.Auth.StartExternalSignInAsync("google", Callback, TestContext.Current.CancellationToken);

        var refused = await Assert.ThrowsAsync<YogabaseApiException>(() => client.Auth.CompleteExternalSignInAsync(pending, new Uri($"{Callback}?code=c&state=gw-state-1"), cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(((int)status, code), (refused.StatusCode, refused.Code));
        Assert.Equal(["/auth/external/google/start", "/auth/external/google/callback"], seen.Select(call => call.Path).ToArray());
        Assert.Null(await store.LoadAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ExternalSignInNamesMfaAndFinishesWithoutTheSpentCodeWhenTheAuthenticatorCodeIsPassed()
    {
        var seen = new List<(string Path, string Query, string Body)>();
        await using var client = Client(SignInGateway(seen, () => Json(HttpStatusCode.Accepted, "{\"code\":\"mfa_required\"}"), state => Found($"{Callback}?code=code-1&state={state}")), new InMemoryYogabaseSessionStore());
        var pending = await client.Auth.StartExternalSignInAsync("google", Callback, TestContext.Current.CancellationToken);
        var redirect = new Uri($"{Callback}?code=c&state=gw-state-1");

        var required = await Assert.ThrowsAsync<YogabaseApiException>(() => client.Auth.CompleteExternalSignInAsync(pending, redirect, cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal((202, "mfa_required"), (required.StatusCode, required.Code));
        Assert.Equal(["/auth/external/google/start", "/auth/external/google/callback"], seen.Select(call => call.Path).ToArray());

        var session = await client.Auth.CompleteExternalSignInAsync(pending, redirect, "123456", TestContext.Current.CancellationToken);
        Assert.Equal("access-1", session.AccessToken);
        Assert.Equal(["/auth/mfa/verify", "/.well-known/openid-configuration", "/connect/authorize", "/connect/token"], seen.Skip(2).Select(call => call.Path).ToArray());
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "{\"code\":\"external_redirect_uri_invalid\"}", "external_redirect_uri_invalid")]
    [InlineData(HttpStatusCode.OK, "{\"authorizationUrl\":\"http://accounts.example/auth\",\"state\":\"s\"}", "external_start_invalid")]
    public async Task ExternalSignInStartSurfacesARefusalAndNeverHandsBackAnInsecureAddress(HttpStatusCode status, string body, string code)
    {
        await using var client = Client(new ScriptedHandler((_, _) => Task.FromResult(Json(status, body))), new InMemoryYogabaseSessionStore());

        var refused = await Assert.ThrowsAsync<YogabaseApiException>(() => client.Auth.StartExternalSignInAsync("google", Callback, TestContext.Current.CancellationToken));

        Assert.Equal(code, refused.Code);
    }

    [Fact]
    public async Task ConcurrentExpiredCallsRotateTheRefreshTokenOnce()
    {
        var tokenCalls = 0;
        var handler = new ScriptedHandler((request, _) =>
        {
            if (request.RequestUri!.AbsolutePath == "/.well-known/openid-configuration")
            {
                return Task.FromResult(Json(HttpStatusCode.OK, Discovery()));
            }
            if (request.RequestUri.AbsolutePath == "/connect/token")
            {
                Interlocked.Increment(ref tokenCalls);
                return Task.FromResult(Json(
                    HttpStatusCode.OK,
                    "{\"access_token\":\"new-token\",\"token_type\":\"Bearer\",\"expires_in\":3600,\"refresh_token\":\"new-refresh\"}"));
            }
            return Task.FromResult(Json(HttpStatusCode.OK, "[]"));
        });
        var store = new InMemoryYogabaseSessionStore();
        await store.SaveAsync(new YogabaseTokenSet
        {
            AccessToken = "expired",
            RefreshToken = "old-refresh",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1),
        }, TestContext.Current.CancellationToken);
        await using var client = Client(handler, store);

        await Task.WhenAll(
            client.Servers.ListAsync(TestContext.Current.CancellationToken),
            client.Servers.ListAsync(TestContext.Current.CancellationToken));

        Assert.Equal(1, tokenCalls);
    }

    [Fact]
    public async Task InitializeReturnsAPersistedUsableSessionWithoutSendingAGatewayRequest()
    {
        var calls = 0;
        var handler = new ScriptedHandler((_, _) =>
        {
            calls++;
            return Task.FromResult(Json(HttpStatusCode.OK, "{}"));
        });
        var store = await AuthenticatedStoreAsync();
        await using var client = Client(handler, store);

        var session = await client.InitializeAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(session);
        Assert.Equal("access-token", session.AccessToken);
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task ValidSessionReturnsNullForAnExpiredSessionWithoutARefreshToken()
    {
        var calls = 0;
        var handler = new ScriptedHandler((_, _) =>
        {
            calls++;
            return Task.FromResult(Json(HttpStatusCode.OK, "{}"));
        });
        var store = new InMemoryYogabaseSessionStore();
        await store.SaveAsync(new YogabaseTokenSet
        {
            AccessToken = "expired",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1),
        }, TestContext.Current.CancellationToken);
        await using var client = Client(handler, store);

        var session = await client.Auth.GetValidSessionAsync(TestContext.Current.CancellationToken);

        Assert.Null(session);
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task LogoutClearsThePersistedSessionOnlyAfterTheServerAcceptsIt()
    {
        var store = await AuthenticatedStoreAsync();
        var handler = new ScriptedHandler((request, _) => Task.FromResult(
            request.RequestUri!.AbsolutePath == "/auth/logout"
                ? new HttpResponseMessage(HttpStatusCode.NoContent)
                : Json(HttpStatusCode.OK, "{}")));
        await using var client = Client(handler, store);

        await client.Auth.LogoutAsync(TestContext.Current.CancellationToken);

        Assert.Null(await store.LoadAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task FailedLogoutPreservesThePersistedSessionForRecovery()
    {
        var store = await AuthenticatedStoreAsync();
        var handler = new ScriptedHandler((_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        await using var client = Client(handler, store);

        await Assert.ThrowsAsync<YogabaseApiException>(() => client.Auth.LogoutAsync(
            TestContext.Current.CancellationToken));

        var remaining = await store.LoadAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(remaining);
        Assert.Equal("access-token", remaining.AccessToken);
    }

    [Fact]
    public async Task DeliveryUsesItsOwnOriginDeliveryBearerAndConcurrencyHeaders()
    {
        HttpRequestMessage? observed = null;
        var handler = new ScriptedHandler((request, _) =>
        {
            observed = CloneRequest(request);
            return Task.FromResult(Json(HttpStatusCode.OK, JsonSerializer.Serialize(new
            {
                previewId = "77777777-7777-4777-8777-777777777777",
                previewDigest = "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                action = "CreateEnvironment",
                expectedRevision = 4,
                approvalRequired = false,
                expiresAt = "2026-09-01T00:10:00Z",
            })));
        });
        using var delivery = Delivery(handler);

        var preview = await delivery.PreviewEnvironmentAsync(
            Guid.Parse("11111111-1111-4111-8111-111111111111"),
            new YogabaseDeliveryEnvironmentPreviewRequest
            {
                Name = "staging",
                TemplateId = Guid.Parse("22222222-2222-4222-8222-222222222222"),
                TemplateVersion = 1,
            },
            "W/\"4\"",
            TestContext.Current.CancellationToken);

        Assert.Equal("http://localhost:7000/api/v1/delivery/projects/11111111-1111-4111-8111-111111111111/environments/previews", observed!.RequestUri!.AbsoluteUri);
        Assert.Equal(new AuthenticationHeaderValue("Bearer", "delivery-token"), observed.Headers.Authorization);
        Assert.Equal("W/\"4\"", observed.Headers.GetValues("If-Match").Single());
        Assert.Equal("CreateEnvironment", preview.Action);
    }

    [Fact]
    public async Task DeliverySubmitPreservesPreviewBindingAndReturnsPollingHints()
    {
        HttpRequestMessage? observed = null;
        var handler = new ScriptedHandler((request, _) =>
        {
            observed = CloneRequest(request);
            var response = Json(HttpStatusCode.Accepted, "{\"operationId\":\"88888888-8888-4888-8888-888888888888\",\"state\":\"Queued\",\"kind\":\"CreateEnvironment\",\"revision\":1,\"updatedAt\":\"2026-09-01T00:00:00Z\"}");
            response.Headers.TryAddWithoutValidation("Location", "/api/v1/delivery/projects/11111111-1111-4111-8111-111111111111/operations/88888888-8888-4888-8888-888888888888");
            response.Headers.TryAddWithoutValidation("Retry-After", "2");
            return Task.FromResult(response);
        });
        using var delivery = Delivery(handler);

        var accepted = await delivery.SubmitEnvironmentAsync(
            Guid.Parse("11111111-1111-4111-8111-111111111111"),
            new YogabaseDeliveryPreviewRequest
            {
                PreviewId = Guid.Parse("99999999-9999-4999-8999-999999999999"),
                PreviewDigest = "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            },
            "submission-1",
            TestContext.Current.CancellationToken);

        Assert.Equal("submission-1", observed!.Headers.GetValues("Idempotency-Key").Single());
        Assert.Equal(TimeSpan.FromSeconds(2), accepted.RetryAfter);
        Assert.Equal("http://localhost:7000/api/v1/delivery/projects/11111111-1111-4111-8111-111111111111/operations/88888888-8888-4888-8888-888888888888", accepted.Location.AbsoluteUri);
    }

    [Fact]
    public async Task DeliveryPollingUsesBoundedBackoffUntilATerminalOperationState()
    {
        var calls = 0;
        var handler = new ScriptedHandler((_, _) =>
        {
            calls++;
            var state = calls == 1 ? "Queued" : "Succeeded";
            var response = Json(HttpStatusCode.OK, $"{{\"operationId\":\"aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa\",\"state\":\"{state}\",\"kind\":\"CreateEnvironment\",\"revision\":{calls},\"updatedAt\":\"2026-09-01T00:00:00Z\"}}");
            response.Headers.TryAddWithoutValidation("ETag", $"W/\"{calls}\"");
            return Task.FromResult(response);
        });
        using var delivery = Delivery(handler);

        var operation = await delivery.WaitForOperationAsync(
            Guid.Parse("11111111-1111-4111-8111-111111111111"),
            Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"),
            new YogabaseDeliveryPollingOptions
            {
                Timeout = TimeSpan.FromSeconds(1),
                InitialDelay = TimeSpan.FromMilliseconds(1),
                MaximumDelay = TimeSpan.FromMilliseconds(2),
            },
            TestContext.Current.CancellationToken);

        Assert.Equal("Succeeded", operation.State);
        Assert.Equal(2, calls);
    }

    [Theory]
    [InlineData("Failed")]
    [InlineData("Abandoned")]
    [InlineData("Cancelled")]
    [InlineData("Blocked")]
    public async Task DeliveryPollingRecognizesEveryNonSuccessTerminalState(string terminalState)
    {
        var calls = 0;
        var handler = new ScriptedHandler((_, _) =>
        {
            calls++;
            var response = Json(HttpStatusCode.OK,
                $"{{\"operationId\":\"aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa\",\"state\":\"{terminalState}\",\"kind\":\"Release\",\"revision\":1,\"updatedAt\":\"2026-09-01T00:00:00Z\"}}");
            response.Headers.TryAddWithoutValidation("ETag", "W/\"1\"");
            return Task.FromResult(response);
        });
        using var delivery = Delivery(handler);

        var operation = await delivery.WaitForOperationAsync(
            Guid.Parse("11111111-1111-4111-8111-111111111111"),
            Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"),
            new YogabaseDeliveryPollingOptions
            {
                Timeout = TimeSpan.FromSeconds(1),
                InitialDelay = TimeSpan.FromMilliseconds(1),
                MaximumDelay = TimeSpan.FromMilliseconds(2),
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(terminalState, operation.State);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task DeliveryPollingTimeoutCancelsAnInFlightRequest()
    {
        var requestCancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = new ScriptedHandler(async (_, cancellationToken) =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
                throw new InvalidOperationException("The delivery request should have been cancelled.");
            }
            catch (OperationCanceledException)
            {
                requestCancelled.TrySetResult();
                throw;
            }
        });
        using var delivery = Delivery(handler);

        await Assert.ThrowsAsync<TimeoutException>(() => delivery.WaitForOperationAsync(
            Guid.Parse("11111111-1111-4111-8111-111111111111"),
            Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"),
            new YogabaseDeliveryPollingOptions
            {
                Timeout = TimeSpan.FromMilliseconds(25),
                InitialDelay = TimeSpan.FromMilliseconds(1),
                MaximumDelay = TimeSpan.FromMilliseconds(2),
            },
            TestContext.Current.CancellationToken));

        await requestCancelled.Task.WaitAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DeliveryEventStreamUsesDeliveryBearerAndParsesServerSentEvents()
    {
        HttpRequestMessage? observed = null;
        var handler = new ScriptedHandler((request, _) =>
        {
            observed = CloneRequest(request);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "id: resume-2\nevent: delivery-operation\ndata: {\"operationId\":\"aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa\",\"state\":\"Running\",\"detail\":\"dispatched\",\"occurredAt\":\"2026-09-01T00:00:00Z\"}\n\n",
                    Encoding.UTF8,
                    "text/event-stream"),
            };
            return Task.FromResult(response);
        });
        using var delivery = Delivery(handler);

        await foreach (var item in delivery.StreamOperationEventsAsync(
            Guid.Parse("11111111-1111-4111-8111-111111111111"),
            lastEventId: "resume-1",
            cancellationToken: TestContext.Current.CancellationToken))
        {
            Assert.Equal("Running", item.State);
            Assert.Equal("resume-2", item.ResumeToken);
            break;
        }

        Assert.Equal(new AuthenticationHeaderValue("Bearer", "delivery-token"), observed!.Headers.Authorization);
        Assert.Equal("resume-1", observed.Headers.GetValues("Last-Event-ID").Single());
        Assert.Equal("text/event-stream", observed.Headers.Accept.Single().MediaType);
    }

    [Fact]
    public async Task DeliveryTemplateWithoutHostedServicePolicyDigestDeserializesAsNull()
    {
        var handler = new ScriptedHandler((_, _) =>
        {
            var response = Json(HttpStatusCode.OK, JsonSerializer.Serialize(new
            {
                id = "22222222-2222-4222-8222-222222222222",
                name = "web",
                version = 1,
                state = "Active",
                targetId = "33333333-3333-4333-8333-333333333333",
                templateDigest = "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                revision = 1,
            }));
            response.Headers.TryAddWithoutValidation("ETag", "W/\"1\"");
            return Task.FromResult(response);
        });
        using var delivery = Delivery(handler);

        var template = await delivery.GetTemplateAsync(
            Guid.Parse("11111111-1111-4111-8111-111111111111"),
            Guid.Parse("22222222-2222-4222-8222-222222222222"),
            TestContext.Current.CancellationToken);

        Assert.Null(template.Value.HostedServicePolicyDigest);
    }

    [Fact]
    public async Task DeliveryTemplateWithHostedServicePolicyDigestSurfacesTheExactValue()
    {
        var handler = new ScriptedHandler((_, _) =>
        {
            var response = Json(HttpStatusCode.OK, JsonSerializer.Serialize(new
            {
                id = "22222222-2222-4222-8222-222222222222",
                name = "web",
                version = 1,
                state = "Active",
                targetId = "33333333-3333-4333-8333-333333333333",
                templateDigest = "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                hostedServicePolicyDigest = "sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
                revision = 1,
            }));
            response.Headers.TryAddWithoutValidation("ETag", "W/\"1\"");
            return Task.FromResult(response);
        });
        using var delivery = Delivery(handler);

        var template = await delivery.GetTemplateAsync(
            Guid.Parse("11111111-1111-4111-8111-111111111111"),
            Guid.Parse("22222222-2222-4222-8222-222222222222"),
            TestContext.Current.CancellationToken);

        Assert.Equal("sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb", template.Value.HostedServicePolicyDigest);
    }

    [Fact]
    public async Task ClearWaitsForAnInFlightRefreshAndRemainsFinal()
    {
        var refreshStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseRefresh = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = new ScriptedHandler(async (request, cancellationToken) =>
        {
            if (request.RequestUri!.AbsolutePath == "/.well-known/openid-configuration")
            {
                return Json(HttpStatusCode.OK, Discovery());
            }

            refreshStarted.TrySetResult();
            await releaseRefresh.Task.WaitAsync(cancellationToken);
            return Json(
                HttpStatusCode.OK,
                "{\"access_token\":\"stale-refresh\",\"token_type\":\"Bearer\",\"expires_in\":3600,\"refresh_token\":\"stale-family\"}");
        });
        var store = await ExpiredStoreAsync();
        await using var client = Client(handler, store);

        var refresh = client.Auth.RefreshAsync(TestContext.Current.CancellationToken);
        await refreshStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        var clear = client.Auth.ClearLocalSessionAsync(TestContext.Current.CancellationToken);
        var clearFinishedBeforeRefresh = clear.IsCompleted;

        releaseRefresh.TrySetResult();
        if (clearFinishedBeforeRefresh)
        {
            await clear;
            await refresh;
        }
        else
        {
            await refresh;
            await clear;
        }

        Assert.Null(await store.LoadAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task NewSignInCannotBeOverwrittenByAnOlderRefresh()
    {
        var refreshStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseRefresh = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var signInExchangeStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseSignInExchange = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = new ScriptedHandler(async (request, cancellationToken) =>
        {
            if (request.RequestUri!.AbsolutePath == "/.well-known/openid-configuration")
            {
                return Json(HttpStatusCode.OK, Discovery());
            }

            var body = await request.Content!.ReadAsStringAsync(cancellationToken);
            if (body.Contains("grant_type=refresh_token", StringComparison.Ordinal))
            {
                refreshStarted.TrySetResult();
                await releaseRefresh.Task.WaitAsync(cancellationToken);
                return Json(
                    HttpStatusCode.OK,
                    "{\"access_token\":\"old-session-refresh\",\"token_type\":\"Bearer\",\"expires_in\":3600,\"refresh_token\":\"old-family-next\"}");
            }

            signInExchangeStarted.TrySetResult();
            await releaseSignInExchange.Task.WaitAsync(cancellationToken);
            return Json(
                HttpStatusCode.OK,
                "{\"access_token\":\"new-user-session\",\"token_type\":\"Bearer\",\"expires_in\":3600,\"refresh_token\":\"new-user-family\"}");
        });
        var store = await ExpiredStoreAsync();
        await using var client = Client(handler, store);

        var refresh = client.Auth.RefreshAsync(TestContext.Current.CancellationToken);
        await refreshStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        var signIn = client.Auth.SignInWithPkceAsync(
            new YogabasePkceOptions
            {
                RedirectUri = new Uri("com.stackyoga.app:/oauth/callback"),
                OpenBrowserAsync = (authorize, _) =>
                {
                    var state = ParseQuery(authorize.Query)["state"];
                    return Task.FromResult(new Uri($"com.stackyoga.app:/oauth/callback?code=new-user-code&state={state}"));
                },
            },
            TestContext.Current.CancellationToken);

        if (signInExchangeStarted.Task.IsCompleted)
        {
            releaseSignInExchange.TrySetResult();
            await signIn;
            releaseRefresh.TrySetResult();
            await refresh;
        }
        else
        {
            releaseRefresh.TrySetResult();
            await refresh;
            await signInExchangeStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
            releaseSignInExchange.TrySetResult();
            await signIn;
        }

        var finalTokens = await store.LoadAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(finalTokens);
        Assert.Equal("new-user-session", finalTokens.AccessToken);
        Assert.Equal("new-user-family", finalTokens.RefreshToken);
    }

    [Fact]
    public async Task ClearInvalidatesABrowserSignInThatStartedEarlier()
    {
        var browserStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseBrowser = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tokenCalls = 0;
        var handler = new ScriptedHandler((request, _) =>
        {
            if (request.RequestUri!.AbsolutePath == "/.well-known/openid-configuration")
            {
                return Task.FromResult(Json(HttpStatusCode.OK, Discovery()));
            }

            Interlocked.Increment(ref tokenCalls);
            return Task.FromResult(Json(
                HttpStatusCode.OK,
                "{\"access_token\":\"unexpected\",\"token_type\":\"Bearer\",\"expires_in\":3600}"));
        });
        var store = await AuthenticatedStoreAsync();
        await using var client = Client(handler, store);

        var signIn = client.Auth.SignInWithPkceAsync(
            new YogabasePkceOptions
            {
                RedirectUri = new Uri("com.stackyoga.app:/oauth/callback"),
                OpenBrowserAsync = async (authorize, cancellationToken) =>
                {
                    browserStarted.TrySetResult();
                    await releaseBrowser.Task.WaitAsync(cancellationToken);
                    var state = ParseQuery(authorize.Query)["state"];
                    return new Uri($"com.stackyoga.app:/oauth/callback?code=stale-code&state={state}");
                },
            },
            TestContext.Current.CancellationToken);
        await browserStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

        await client.Auth.ClearLocalSessionAsync(TestContext.Current.CancellationToken);
        releaseBrowser.TrySetResult();
        var exception = await Assert.ThrowsAsync<YogabaseApiException>(() => signIn);

        Assert.Equal("authentication_state_changed", exception.Code);
        Assert.Equal(0, tokenCalls);
        Assert.Null(await store.LoadAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public void RedirectCapableHandlersAreRejected()
    {
        using var handler = new HttpClientHandler { AllowAutoRedirect = true };
        var exception = Assert.Throws<ArgumentException>(() => YogabaseClient.FromConfiguration(
            Configuration(),
            new YogabaseClientOptions
            {
                AllowInsecureLoopback = true,
                TransportHandler = handler,
            }));
        Assert.Contains("redirect", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static YogabaseClient Client(HttpMessageHandler handler, IYogabaseSessionStore store) =>
        YogabaseClient.FromConfiguration(
            Configuration(),
            new YogabaseClientOptions
            {
                AllowInsecureLoopback = true,
                SessionStore = store,
                TransportHandler = handler,
            });

    private static YogabaseDeliveryClient Delivery(HttpMessageHandler handler) => new(
        new Uri("http://localhost:7000/"),
        _ => Task.FromResult<string?>("delivery-token"),
        new YogabaseDeliveryClientOptions
        {
            AllowInsecureLoopback = true,
            TransportHandler = handler,
        });

    private static async Task<InMemoryYogabaseSessionStore> AuthenticatedStoreAsync()
    {
        var store = new InMemoryYogabaseSessionStore();
        await store.SaveAsync(new YogabaseTokenSet
        {
            AccessToken = "access-token",
            RefreshToken = "refresh-token",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
        }, TestContext.Current.CancellationToken);
        return store;
    }

    private static async Task<InMemoryYogabaseSessionStore> ExpiredStoreAsync()
    {
        var store = new InMemoryYogabaseSessionStore();
        await store.SaveAsync(new YogabaseTokenSet
        {
            AccessToken = "expired",
            RefreshToken = "old-family",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1),
        }, TestContext.Current.CancellationToken);
        return store;
    }

    private const string Callback = "com.stackyoga.app:/oauth/callback";
    private const string TokenBody = "{\"access_token\":\"access-1\",\"token_type\":\"Bearer\",\"expires_in\":3600,\"refresh_token\":\"refresh-1\"}";

    // The gateway's headless sign-in routes, recording each call. `authorize` receives the state it was sent. `login`
    // answers /auth/login and the external callback alike (both set the cookie); each external start gets the next state.
    private static ScriptedHandler SignInGateway(
        List<(string Path, string Query, string Body)> seen,
        Func<HttpResponseMessage> login,
        Func<string, HttpResponseMessage> authorize)
    {
        var starts = 0;
        return new(async (request, cancellationToken) =>
        {
            var uri = request.RequestUri!;
            seen.Add((uri.AbsolutePath, uri.Query, request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken)));
            return uri.AbsolutePath switch
            {
                "/auth/external/google/start" => Json(HttpStatusCode.OK, ExternalStart($"gw-state-{++starts}")),
                "/auth/login" or "/auth/external/google/callback" => login(),
                "/auth/mfa/verify" => new HttpResponseMessage(HttpStatusCode.NoContent),
                "/.well-known/openid-configuration" => Json(HttpStatusCode.OK, Discovery()),
                "/connect/authorize" => authorize(ParseQuery(uri.Query)["state"]),
                _ => Json(HttpStatusCode.OK, TokenBody),
            };
        });
    }

    private static string ExternalStart(string state) =>
        $"{{\"authorizationUrl\":\"https://accounts.example/auth?state={state}\",\"state\":\"{state}\"}}";

    private static string S256(string verifier) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static HttpResponseMessage Found(string location) => new(HttpStatusCode.Found) { Headers = { Location = new Uri(location) } };

    private static YogabaseClientConfiguration Configuration(
        IReadOnlyList<string>? scopes = null,
        IReadOnlyList<YogabaseCapability>? capabilities = null,
        string origin = "http://localhost") => new()
        {
            Schema = YogabaseClient.ClientConfigSchema,
            ProjectId = Guid.Parse("11111111-1111-4111-8111-111111111111"),
            EnvironmentId = Guid.Parse("22222222-2222-4222-8222-222222222222"),
            GatewayEndpoint = origin + "/",
            RealtimeEndpoint = "http://localhost:5001/",
            OidcAuthority = origin + "/p/11111111-1111-4111-8111-111111111111",
            OidcDiscoveryEndpoint = origin + "/.well-known/openid-configuration",
            OidcPublicClientId = "stackyoga-native-preview",
            OidcScopes = scopes ?? ["openid", "offline_access"],
            OidcRedirectUris = ["com.stackyoga.app:/oauth/callback"],
            RealtimePath = "/v1/realtime",
            Capabilities = capabilities ?? Capabilities(),
            IssuedAt = DateTimeOffset.Parse("2026-08-26T00:00:00+00:00", System.Globalization.CultureInfo.InvariantCulture),
        };

    private static YogabaseClientConfiguration WithGateway(
        YogabaseClientConfiguration source,
        string gateway) => new()
        {
            Schema = source.Schema,
            ProjectId = source.ProjectId,
            EnvironmentId = source.EnvironmentId,
            GatewayEndpoint = gateway,
            RealtimeEndpoint = source.RealtimeEndpoint,
            OidcAuthority = "https://other.example/p/11111111-1111-4111-8111-111111111111",
            OidcDiscoveryEndpoint = "https://other.example/.well-known/openid-configuration",
            OidcPublicClientId = source.OidcPublicClientId,
            OidcScopes = source.OidcScopes,
            OidcRedirectUris = source.OidcRedirectUris,
            RealtimePath = source.RealtimePath,
            Capabilities = source.Capabilities,
            IssuedAt = source.IssuedAt,
        };

    // The four capabilities an installation publishes now that the Data API module is withdrawn.
    internal static IReadOnlyList<YogabaseCapability> Capabilities() =>
    [
        new() { Id = "auth", Available = true },
        new() { Id = "storage", Available = true },
        new() { Id = "functions-rest", Available = true },
        new() { Id = "realtime-subscribe", Available = false, UnavailableReason = "preview-disabled" },
    ];

    private static string Discovery() =>
        "{\"issuer\":\"http://localhost/p/11111111-1111-4111-8111-111111111111\","
        + "\"authorization_endpoint\":\"http://localhost/connect/authorize\","
        + "\"token_endpoint\":\"http://localhost/connect/token\"}";

    private static Dictionary<string, string> ParseQuery(string query) => query
        .TrimStart('?')
        .Split('&', StringSplitOptions.RemoveEmptyEntries)
        .Select(segment => segment.Split('=', 2))
        .ToDictionary(
            parts => Uri.UnescapeDataString(parts[0]),
            parts => Uri.UnescapeDataString(parts[1]),
            StringComparer.Ordinal);

    private static HttpResponseMessage Json(HttpStatusCode status, string json) => new(status)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private static HttpRequestMessage CloneRequest(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);
        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        return clone;
    }

    private static string LowerHex(byte[] bytes) => Convert.ToHexString(bytes).ToLowerInvariant();

    private sealed class ScriptedHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => send(request, cancellationToken);
    }
}
