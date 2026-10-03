using System.Security.Cryptography;

namespace Yogabase.Tests;

/// <summary>
/// Verifies that every checked-in OpenAPI contract under <c>contracts/</c> still matches its
/// recorded SHA-256 digest, and that the recorded digest is the same value published in
/// <c>MANIFEST.json</c>, <c>README.md</c>, and <c>README.ko.md</c>. Nothing else in the repository
/// checks this, so a contract edited without regenerating its <c>.sha256</c> sidecar (or without
/// updating the published digests) would otherwise go unnoticed until a consumer's checksum
/// verification failed at regeneration time.
/// </summary>
public sealed class ContractDigestTests
{
    private const string SolutionFileName = "Yogabase.SDKs.slnx";

    [Theory]
    [InlineData("yogabase-client-v1.openapi.json")]
    [InlineData("yogabase-builds-v1.openapi.json")]
    [InlineData("yogabase-delivery-v1.openapi.json")]
    [InlineData("yogabase-runtime-host-v1.openapi.json")]
    public async Task ContractFileMatchesItsSha256Sidecar(string contractFileName)
    {
        var repoRoot = FindRepoRoot();
        var contractPath = Path.Combine(repoRoot, "contracts", contractFileName);
        var sidecarPath = Path.ChangeExtension(contractPath, ".sha256");

        Assert.True(File.Exists(contractPath), $"Missing contract file: {contractPath}");
        Assert.True(File.Exists(sidecarPath), $"Missing sha256 sidecar: {sidecarPath}");

        var actualDigest = await ComputeSha256Async(contractPath);
        var sidecarLine = (await File.ReadAllTextAsync(sidecarPath, TestContext.Current.CancellationToken)).Trim();
        var recordedDigest = ParseShasumLine(sidecarLine);

        Assert.Equal(contractFileName, ParseShasumFileName(sidecarLine));
        Assert.Equal(recordedDigest, actualDigest, StringComparer.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("yogabase-client-v1.openapi.json")]
    [InlineData("yogabase-builds-v1.openapi.json")]
    [InlineData("yogabase-delivery-v1.openapi.json")]
    [InlineData("yogabase-runtime-host-v1.openapi.json")]
    public async Task RecordedDigestAppearsInManifestAndBothReadmes(string contractFileName)
    {
        var repoRoot = FindRepoRoot();
        var sidecarPath = Path.ChangeExtension(Path.Combine(repoRoot, "contracts", contractFileName), ".sha256");
        var sidecarLine = (await File.ReadAllTextAsync(sidecarPath, TestContext.Current.CancellationToken)).Trim();
        var recordedDigest = ParseShasumLine(sidecarLine);

        var manifestText = await File.ReadAllTextAsync(Path.Combine(repoRoot, "MANIFEST.json"), TestContext.Current.CancellationToken);
        var readmeText = await File.ReadAllTextAsync(Path.Combine(repoRoot, "README.md"), TestContext.Current.CancellationToken);
        var readmeKoText = await File.ReadAllTextAsync(Path.Combine(repoRoot, "README.ko.md"), TestContext.Current.CancellationToken);

        Assert.Contains(recordedDigest, manifestText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(recordedDigest, readmeText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(recordedDigest, readmeKoText, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Every document under <c>contracts/</c> is a byte-identical snapshot of the product's
    /// <c>docs/api/</c> file of the same name. The former <c>contracts/planned/</c> documents were
    /// written here while the product implemented the build and game-server surfaces; the product
    /// has published both (game servers inside the application contract, builds as its own
    /// document), so the planned files were deleted rather than reconciled, and nothing may
    /// reintroduce a planned marker under <c>contracts/</c>.
    /// </summary>
    [Fact]
    public async Task NoContractIsMarkedPlannedAndThePlannedDirectoryIsGone()
    {
        var repoRoot = FindRepoRoot();
        var contracts = Path.Combine(repoRoot, "contracts");

        Assert.False(Directory.Exists(Path.Combine(contracts, "planned")), "contracts/planned must not exist.");
        foreach (var contract in Directory.GetFiles(contracts, "*.openapi.json"))
        {
            var text = await File.ReadAllTextAsync(contract, TestContext.Current.CancellationToken);
            Assert.DoesNotContain("x-yogabase-status", text, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// The application contract carries the game-server surface and the builds contract carries
    /// the three build routes, exactly as the product's endpoints map them. Pinned so a re-sync
    /// that silently dropped a route the authored .NET client calls is caught here rather than by
    /// a consumer.
    /// </summary>
    [Fact]
    public async Task TheMirroredContractsDeclareTheRoutesTheAuthoredClientsCall()
    {
        var repoRoot = FindRepoRoot();
        var client = await File.ReadAllTextAsync(
            Path.Combine(repoRoot, "contracts", "yogabase-client-v1.openapi.json"),
            TestContext.Current.CancellationToken);
        var builds = await File.ReadAllTextAsync(
            Path.Combine(repoRoot, "contracts", "yogabase-builds-v1.openapi.json"),
            TestContext.Current.CancellationToken);

        Assert.Contains("\"/v1/servers/start\"", client, StringComparison.Ordinal);
        Assert.Contains("\"/v1/servers\"", client, StringComparison.Ordinal);
        Assert.Contains("\"/v1/servers/{serverId}\"", client, StringComparison.Ordinal);
        Assert.Contains("\"/v1/servers/{serverId}/stop\"", client, StringComparison.Ordinal);
        Assert.Contains("\"/api/v1/projects/{projectId}/environments/{environmentId}/builds\"", builds, StringComparison.Ordinal);
        Assert.Contains("\"/api/v1/projects/{projectId}/environments/{environmentId}/builds/{buildId}\"", builds, StringComparison.Ordinal);
        Assert.Contains("\"/api/v1/projects/{projectId}/environments/{environmentId}/builds/{buildId}/recheck\"", builds, StringComparison.Ordinal);
    }

    /// <summary>
    /// The application contract declares the restored client Data API (the product implemented it on
    /// 2026-09-23) and its requested-only <c>data-rest</c> capability, and the generated TypeScript,
    /// Python, Java and Dart clients are produced from exactly that. The authored .NET client has no
    /// Data client and does not request the row, which is a decision recorded in the READMEs rather
    /// than something this contract implies, so the test pins the contract's side of it. Read
    /// structurally rather than as text: the document explains the 2026-09-08 withdrawal in its
    /// description, and that prose must not count as a declaration.
    /// </summary>
    [Fact]
    public async Task TheMirroredApplicationContractDeclaresTheRestoredDataApiAndItsRequestedOnlyCapability()
    {
        var repoRoot = FindRepoRoot();
        using var document = System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(
            Path.Combine(repoRoot, "contracts", "yogabase-client-v1.openapi.json"),
            TestContext.Current.CancellationToken));
        var root = document.RootElement;

        var paths = root.GetProperty("paths").EnumerateObject().Select(static path => path.Name).ToArray();
        foreach (var operation in new[] { "select", "insert", "upsert", "update", "delete" })
        {
            Assert.Contains($"/v1/data/{{table}}/{operation}", paths);
        }

        var schemas = root.GetProperty("components").GetProperty("schemas");
        foreach (var schema in new[] { "DataSelectRequest", "DataInsertRequest", "DataUpsertRequest", "DataUpdateRequest", "DataDeleteRequest", "DataResult" })
        {
            Assert.True(schemas.TryGetProperty(schema, out _), $"Data schema missing: {schema}");
        }

        var capabilityIds = schemas.GetProperty("Capability")
            .GetProperty("properties").GetProperty("id").GetProperty("enum")
            .EnumerateArray().Select(static value => value.GetString()).ToArray();
        // realtime-presence and data-rest are requested-only: a gateway adds their rows after the four
        // core rows only for a client that names them in X-Yogabase-Accept-Capabilities, and the
        // authored .NET client names only realtime-presence.
        Assert.Equal(["auth", "storage", "functions-rest", "realtime-subscribe", "realtime-presence", "data-rest"], capabilityIds);
        Assert.True(root.TryGetProperty("x-yogabase-data-api", out _));
    }

    /// <summary>
    /// The function invoke route declares the 1 MiB inline limit the authored clients check before
    /// transport and answers an over-limit request with 413 <c>payload_too_large</c> naming
    /// <c>limitBytes</c> and <c>maxBodyBytes</c>. Pinned so a re-sync that dropped the refusal, or moved
    /// the limit the SDKs hard-code, is caught here and not by a consumer's 400.
    /// </summary>
    [Fact]
    public async Task TheMirroredApplicationContractDeclaresTheInlinePayloadLimitAndItsPayloadTooLargeRefusal()
    {
        var repoRoot = FindRepoRoot();
        using var document = System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(
            Path.Combine(repoRoot, "contracts", "yogabase-client-v1.openapi.json"),
            TestContext.Current.CancellationToken));
        var root = document.RootElement;

        var limits = root.GetProperty("x-yogabase-inline-payload");
        Assert.Equal(1_048_576, limits.GetProperty("limits").GetProperty("maximumPayloadBytes").GetInt32());
        Assert.Equal(1_398_104, limits.GetProperty("limits").GetProperty("maximumPayloadBase64Characters").GetInt32());
        Assert.Equal(8_392_166, limits.GetProperty("limits").GetProperty("maximumRequestBodyBytes").GetInt32());
        var refusal = limits.GetProperty("refusal");
        Assert.Equal(413, refusal.GetProperty("status").GetInt32());
        Assert.Equal("payload_too_large", refusal.GetProperty("code").GetString());
        Assert.Equal(["limitBytes", "maxBodyBytes"], refusal.GetProperty("members").EnumerateArray().Select(static value => value.GetString()).ToArray());

        var invoke = root.GetProperty("paths")
            .GetProperty("/v1/projects/{projectId}/functions/{functionId}/versions/{functionVersion}/invoke")
            .GetProperty("post").GetProperty("responses");
        Assert.Equal("#/components/responses/PayloadTooLarge", invoke.GetProperty("413").GetProperty("$ref").GetString());
        Assert.True(root.GetProperty("components").GetProperty("responses").TryGetProperty("PayloadTooLarge", out _));
    }

    /// <summary>
    /// The product serves durable Jobs to application clients (owner decision, 2026-10-03) but its
    /// client document does not declare the routes (checked against product main <c>ab2eca1f</c>), so
    /// no generator emits them and every language's Jobs surface is authored here from the gateway's
    /// source. This is the tripwire for the day the document does declare them: the failing test
    /// says to re-mirror, regenerate, and decide per language whether the authored surface stays the
    /// front door beside the generated builders or is rebuilt over them. It reads paths structurally.
    /// </summary>
    [Fact]
    public async Task TheMirroredApplicationContractDeclaresNoJobsRouteSoTheAuthoredJobsSurfaceIsTheOnlyOne()
    {
        var repoRoot = FindRepoRoot();
        using var document = System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(
            Path.Combine(repoRoot, "contracts", "yogabase-client-v1.openapi.json"),
            TestContext.Current.CancellationToken));

        foreach (var path in document.RootElement.GetProperty("paths").EnumerateObject())
        {
            Assert.False(
                path.Name.Contains("/jobs", StringComparison.Ordinal),
                $"The mirrored contract now declares {path.Name}: re-run scripts/regenerate-clients.sh and revisit the authored Jobs surfaces.");
        }
    }

    /// <summary>
    /// The additive surfaces the authored clients now read are in the mirrored documents: the
    /// Lambda execution declaration on a Builds upload, the requested-only realtime-presence row
    /// and its Hub metadata in the application contract, and the runtime-host page head.
    /// </summary>
    [Fact]
    public async Task TheMirroredContractsDeclareTheAdditiveSurfacesTheAuthoredClientsRead()
    {
        var repoRoot = FindRepoRoot();
        using var builds = System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(
            Path.Combine(repoRoot, "contracts", "yogabase-builds-v1.openapi.json"),
            TestContext.Current.CancellationToken));
        var buildsText = builds.RootElement.GetRawText();
        Assert.Contains("\"executionKind\"", buildsText, StringComparison.Ordinal);
        Assert.Contains("\"realtimeIngressBindings\"", buildsText, StringComparison.Ordinal);
        Assert.Contains("\"realtimeEgressBindings\"", buildsText, StringComparison.Ordinal);
        Assert.Contains("BUILD_LAMBDA_DECLARATION_REQUIRED", buildsText, StringComparison.Ordinal);
        // The registering-bundle field the Builds client and the Unity publisher send.
        Assert.Contains("\"registeredLambdaIds\"", buildsText, StringComparison.Ordinal);
        Assert.Contains("BUILD_LAMBDA_REGISTRATION_REQUIRED", buildsText, StringComparison.Ordinal);

        using var client = System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(
            Path.Combine(repoRoot, "contracts", "yogabase-client-v1.openapi.json"),
            TestContext.Current.CancellationToken));
        var clientText = client.RootElement.GetRawText();
        Assert.Contains("\"X-Yogabase-Accept-Capabilities\"", clientText, StringComparison.Ordinal);
        foreach (var method in new[] { "TrackPresence", "UntrackPresence", "GetPresence" })
        {
            Assert.Contains($"\"{method}\"", clientText, StringComparison.Ordinal);
        }

        using var runtimeHost = System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(
            Path.Combine(repoRoot, "contracts", "yogabase-runtime-host-v1.openapi.json"),
            TestContext.Current.CancellationToken));
        var runtimeHostText = runtimeHost.RootElement.GetRawText();
        Assert.Contains("\"headState\"", runtimeHostText, StringComparison.Ordinal);
        Assert.Contains("\"headSequence\"", runtimeHostText, StringComparison.Ordinal);
    }

    /// <summary>
    /// The TypeScript and Python delivery clients each export the delivery contract's SHA-256 as a
    /// public constant so a consumer can assert which contract revision the client was authored
    /// against. Those constants are hand-maintained and live outside <c>contracts/</c>, which is
    /// exactly why they went stale the last time the contract was re-synced: nothing compared them
    /// to the sidecar. This test pins them.
    /// </summary>
    [Theory]
    [InlineData("languages/typescript/src/delivery.ts")]
    [InlineData("languages/python/src/stackyoga_yogabase/delivery.py")]
    public async Task EmbeddedDeliveryContractConstantMatchesTheRecordedDigest(string relativePath)
    {
        var repoRoot = FindRepoRoot();
        var sidecarPath = Path.Combine(repoRoot, "contracts", "yogabase-delivery-v1.openapi.sha256");
        var sidecarLine = (await File.ReadAllTextAsync(sidecarPath, TestContext.Current.CancellationToken)).Trim();
        var recordedDigest = ParseShasumLine(sidecarLine);

        var sourcePath = Path.Combine(repoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(sourcePath), $"Missing authored delivery client source: {sourcePath}");

        var sourceText = await File.ReadAllTextAsync(sourcePath, TestContext.Current.CancellationToken);
        Assert.Contains("YOGABASE_DELIVERY_CONTRACT_SHA256", sourceText, StringComparison.Ordinal);
        Assert.Contains(recordedDigest, sourceText, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string> ComputeSha256Async(string path)
    {
        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream, TestContext.Current.CancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    // A `shasum -a 256` line has the form "<digest>  <filename>" (two spaces for a binary-mode
    // check, one space for text mode). Split on runs of whitespace so either form parses.
    private static string ParseShasumLine(string line) =>
        line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)[0];

    private static string ParseShasumFileName(string line) =>
        line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)[1];

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"Could not locate {SolutionFileName} by walking up from {AppContext.BaseDirectory}.");
    }
}
