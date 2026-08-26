using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using StackYoga.YogaBase;

namespace YogaBase.Tests;

public sealed class YogaBaseClientTests
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task DataUsesTheExactRouteAndBearerToken()
    {
        HttpRequestMessage? observed = null;
        string? body = null;
        var handler = new ScriptedHandler(async (request, cancellationToken) =>
        {
            observed = CloneRequest(request);
            body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return Json(HttpStatusCode.OK, "{\"rows\":[],\"affectedRows\":0}");
        });
        var store = await AuthenticatedStoreAsync();
        await using var client = Client(handler, store);

        var result = await client.Data.SelectAsync(
            "profiles",
            new YogaBaseSelectRequest { Columns = ["display_name"], PageSize = 25 },
            TestContext.Current.CancellationToken);

        Assert.Empty(result.Rows);
        Assert.Equal(HttpMethod.Post, observed!.Method);
        Assert.Equal("http://localhost/v1/data/profiles/select", observed.RequestUri!.AbsoluteUri);
        Assert.Equal(new AuthenticationHeaderValue("Bearer", "access-token"), observed.Headers.Authorization);
        Assert.Contains("\"columns\":[\"display_name\"]", body, StringComparison.Ordinal);
    }

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
            requestDigest = request.Headers.GetValues("X-StackYoga-Content-Sha256").Single();
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
            new YogaBaseInvocationRequest
            {
                IdempotencyKey = "request-1",
                CorrelationId = "correlation-1",
                Deadline = DateTimeOffset.UtcNow.AddSeconds(20),
                Mode = YogaBaseInvocationMode.Asynchronous,
                InlinePayload = [1, 2, 3],
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(
            "http://localhost/v1/projects/11111111-1111-4111-8111-111111111111/functions/resize/versions/1/invoke",
            route!.AbsoluteUri);
        Assert.Equal(YogaBaseInvocationOutcome.Accepted, result.Outcome);
        Assert.Contains("\"inlinePayloadBase64\":\"AQID\"", body, StringComparison.Ordinal);
        Assert.Contains("\"payloadDigest\":\"sha256:", body, StringComparison.Ordinal);
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

        var exception = await Assert.ThrowsAsync<YogaBaseApiException>(() => client.Functions.InvokeAsync(
            "resize",
            "1",
            new YogaBaseInvocationRequest
            {
                IdempotencyKey = "request-1",
                CorrelationId = "correlation-1",
                Deadline = DateTimeOffset.UtcNow.AddSeconds(20),
                Mode = YogaBaseInvocationMode.Asynchronous,
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

        var exception = await Assert.ThrowsAsync<YogaBaseApiException>(() => YogaBaseClient.CreateAsync(
            new Uri("http://localhost/"),
            "sy_pub_0123456789abcdef",
            new YogaBaseClientOptions
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

        var exception = await Assert.ThrowsAsync<YogaBaseApiException>(() => YogaBaseClient.CreateAsync(
            new Uri("http://localhost/"),
            "sy_pub_0123456789abcdef",
            new YogaBaseClientOptions
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
        using var client = YogaBaseClient.FromConfiguration(
            configuration,
            new YogaBaseClientOptions
            {
                AllowInsecureLoopback = true,
                TransportHandler = new ScriptedHandler((_, _) =>
                    Task.FromResult(Json(HttpStatusCode.OK, "{}"))),
            });

        scopes[0] = "mutated";
        capabilities[0] = new YogaBaseCapability { Id = "unknown", Available = true };

        Assert.Equal("openid", client.Configuration.OidcScopes[0]);
        Assert.True(client.Configuration.IsAvailable("auth"));
        Assert.False(client.Configuration.IsAvailable("unknown"));
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
        await using var client = Client(handler, new InMemoryYogaBaseSessionStore());

        var session = await client.Auth.SignInWithPkceAsync(
            new YogaBasePkceOptions
            {
                RedirectUri = new Uri("com.stackyoga.game:/oauth/callback"),
                OpenBrowserAsync = (authorize, _) =>
                {
                    var state = ParseQuery(authorize.Query)["state"];
                    return Task.FromResult(new Uri($"com.stackyoga.game:/oauth/callback?code=code-1&state={state}"));
                },
            },
            TestContext.Current.CancellationToken);

        Assert.Equal("access-token", session.AccessToken);

        var forged = await Assert.ThrowsAsync<YogaBaseApiException>(() => client.Auth.SignInWithPkceAsync(
            new YogaBasePkceOptions
            {
                RedirectUri = new Uri("com.stackyoga.game:/oauth/callback"),
                OpenBrowserAsync = (_, _) => Task.FromResult(
                    new Uri("com.stackyoga.game:/oauth/callback?error=access_denied")),
            },
            TestContext.Current.CancellationToken));
        Assert.Equal("authorization_response_invalid", forged.Code);
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
            return Task.FromResult(Json(HttpStatusCode.OK, "{\"rows\":[],\"affectedRows\":0}"));
        });
        var store = new InMemoryYogaBaseSessionStore();
        await store.SaveAsync(new YogaBaseTokenSet
        {
            AccessToken = "expired",
            RefreshToken = "old-refresh",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1),
        }, TestContext.Current.CancellationToken);
        await using var client = Client(handler, store);

        await Task.WhenAll(
            client.Data.SelectAsync("profiles", new YogaBaseSelectRequest(), TestContext.Current.CancellationToken),
            client.Data.SelectAsync("profiles", new YogaBaseSelectRequest(), TestContext.Current.CancellationToken));

        Assert.Equal(1, tokenCalls);
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
            new YogaBasePkceOptions
            {
                RedirectUri = new Uri("com.stackyoga.game:/oauth/callback"),
                OpenBrowserAsync = (authorize, _) =>
                {
                    var state = ParseQuery(authorize.Query)["state"];
                    return Task.FromResult(new Uri($"com.stackyoga.game:/oauth/callback?code=new-user-code&state={state}"));
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
            new YogaBasePkceOptions
            {
                RedirectUri = new Uri("com.stackyoga.game:/oauth/callback"),
                OpenBrowserAsync = async (authorize, cancellationToken) =>
                {
                    browserStarted.TrySetResult();
                    await releaseBrowser.Task.WaitAsync(cancellationToken);
                    var state = ParseQuery(authorize.Query)["state"];
                    return new Uri($"com.stackyoga.game:/oauth/callback?code=stale-code&state={state}");
                },
            },
            TestContext.Current.CancellationToken);
        await browserStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

        await client.Auth.ClearLocalSessionAsync(TestContext.Current.CancellationToken);
        releaseBrowser.TrySetResult();
        var exception = await Assert.ThrowsAsync<YogaBaseApiException>(() => signIn);

        Assert.Equal("authentication_state_changed", exception.Code);
        Assert.Equal(0, tokenCalls);
        Assert.Null(await store.LoadAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public void RedirectCapableHandlersAreRejected()
    {
        using var handler = new HttpClientHandler { AllowAutoRedirect = true };
        var exception = Assert.Throws<ArgumentException>(() => YogaBaseClient.FromConfiguration(
            Configuration(),
            new YogaBaseClientOptions
            {
                AllowInsecureLoopback = true,
                TransportHandler = handler,
            }));
        Assert.Contains("redirect", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static YogaBaseClient Client(HttpMessageHandler handler, IYogaBaseSessionStore store) =>
        YogaBaseClient.FromConfiguration(
            Configuration(),
            new YogaBaseClientOptions
            {
                AllowInsecureLoopback = true,
                SessionStore = store,
                TransportHandler = handler,
            });

    private static async Task<InMemoryYogaBaseSessionStore> AuthenticatedStoreAsync()
    {
        var store = new InMemoryYogaBaseSessionStore();
        await store.SaveAsync(new YogaBaseTokenSet
        {
            AccessToken = "access-token",
            RefreshToken = "refresh-token",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
        }, TestContext.Current.CancellationToken);
        return store;
    }

    private static async Task<InMemoryYogaBaseSessionStore> ExpiredStoreAsync()
    {
        var store = new InMemoryYogaBaseSessionStore();
        await store.SaveAsync(new YogaBaseTokenSet
        {
            AccessToken = "expired",
            RefreshToken = "old-family",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1),
        }, TestContext.Current.CancellationToken);
        return store;
    }

    private static YogaBaseClientConfiguration Configuration(
        IReadOnlyList<string>? scopes = null,
        IReadOnlyList<YogaBaseCapability>? capabilities = null) => new()
        {
            Schema = YogaBaseClient.ClientConfigSchema,
            ProjectId = Guid.Parse("11111111-1111-4111-8111-111111111111"),
            EnvironmentId = Guid.Parse("22222222-2222-4222-8222-222222222222"),
            GatewayEndpoint = "http://localhost/",
            OidcAuthority = "http://localhost/p/11111111-1111-4111-8111-111111111111",
            OidcDiscoveryEndpoint = "http://localhost/.well-known/openid-configuration",
            OidcPublicClientId = "stackyoga-native-preview",
            OidcScopes = scopes ?? ["openid", "offline_access"],
            OidcRedirectUris = ["com.stackyoga.game:/oauth/callback"],
            RealtimePath = "/v1/realtime",
            Capabilities = capabilities ?? Capabilities(),
            IssuedAt = DateTimeOffset.Parse("2026-08-26T00:00:00+00:00", System.Globalization.CultureInfo.InvariantCulture),
        };

    private static YogaBaseClientConfiguration WithGateway(
        YogaBaseClientConfiguration source,
        string gateway) => new()
        {
            Schema = source.Schema,
            ProjectId = source.ProjectId,
            EnvironmentId = source.EnvironmentId,
            GatewayEndpoint = gateway,
            OidcAuthority = "https://other.example/p/11111111-1111-4111-8111-111111111111",
            OidcDiscoveryEndpoint = "https://other.example/.well-known/openid-configuration",
            OidcPublicClientId = source.OidcPublicClientId,
            OidcScopes = source.OidcScopes,
            OidcRedirectUris = source.OidcRedirectUris,
            RealtimePath = source.RealtimePath,
            Capabilities = source.Capabilities,
            IssuedAt = source.IssuedAt,
        };

    private static IReadOnlyList<YogaBaseCapability> Capabilities() =>
    [
        new() { Id = "auth", Available = true },
        new() { Id = "data-rest", Available = true },
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
