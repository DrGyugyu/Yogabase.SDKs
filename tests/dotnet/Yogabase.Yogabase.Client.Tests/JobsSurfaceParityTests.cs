namespace Yogabase.Tests;

/// <summary>
/// Every language reaches the durable Jobs routes through code this repository writes, and nothing generated
/// keeps those five implementations agreeing on the names a caller meets, so this test does: each language's
/// Jobs sources must spell the same route tails, request members, status names, error codes, limits and
/// result-digest header. Where a language wraps the Kiota builders (Java, Dart) the route tails and the
/// request members are spelled by the generated builders and models, which are read with the authored
/// client; where it is standalone (.NET, TypeScript, Python) its own file spells them. The operations
/// themselves, and every limit and code against the contract, are held by
/// <see cref="ContractOperationParityTests"/> and <see cref="ContractLimitsAndCodesTests"/>. It reads the
/// sources as text, which is the only check a language this repository cannot run (Dart) can have here;
/// each language's own tests pin behaviour.
/// </summary>
public sealed class JobsSurfaceParityTests
{
    private const string SolutionFileName = "Yogabase.SDKs.slnx";

    // Each language's Jobs sources. A language that spreads the surface over several files lists them all, and
    // the checks run over their text together; an entry that ends in a slash is every source file under that
    // directory (the generated Jobs builders), and the checks run over those too.
    private static readonly Dictionary<string, string[]> Sources = new(StringComparer.Ordinal)
    {
        [".NET"] =
        [
            "src/dotnet/Yogabase.Yogabase.Client/JobsClient.cs",
            "src/dotnet/Yogabase.Yogabase.Client/YogabaseRoutes.cs",
            "src/dotnet/Yogabase.Yogabase.Client/StatusErrors.cs",
        ],
        ["TypeScript"] =
        [
            "languages/typescript/src/jobs.ts",
            "languages/typescript/src/gateway.ts",
        ],
        ["Python"] =
        [
            "languages/python/src/stackyoga_yogabase/jobs.py",
            "languages/python/src/stackyoga_yogabase/_gateway.py",
        ],
        ["Java"] =
        [
            "languages/java/src/main/java/io/stackyoga/yogabase/YogabaseJobsClient.java",
            "languages/java/src/main/java/io/stackyoga/yogabase/YogabaseJobStatus.java",
            "languages/java/src/main/java/io/stackyoga/yogabase/YogabaseJobErrorCodes.java",
            "languages/java/src/main/java/io/stackyoga/yogabase/YogabaseStatusErrorCodes.java",
            "languages/java/src/main/java/io/stackyoga/yogabase/YogabaseGatewayChannel.java",
            "languages/java/src/main/java/io/stackyoga/yogabase/v1/projects/item/jobs/",
            "languages/java/src/main/java/io/stackyoga/yogabase/models/JobSubmitRequest.java",
        ],
        ["Dart"] = ["languages/dart/lib/src/yogabase_jobs_client.dart"],
    };

    // Members and route tails: .NET writes its members as PascalCase properties that its serializer names
    // in camelCase, so these compare without regard to case.
    private static readonly string[] WireMembers =
    [
        "jobType",
        "idempotencyKey",
        "payloadBase64",
        "maxAttempts",
        "/cancel",
        "/result",
    ];

    // Names the gateway writes exactly: the six statuses the ledger's state machine has, the codes the
    // gateway refuses with (which clients match on), the code each client gives an answer that is not what
    // the route declares, the statuses a bare refusal reads as, and the result digest header.
    private static readonly string[] ExactNames =
    [
        "Queued",
        "Running",
        "Succeeded",
        "Failed",
        "Cancelled",
        "DeadLettered",
        "job_type_not_registered",
        "job_idempotency_conflict",
        "job_payload_too_large",
        "job_request_invalid",
        "job_not_found",
        "job_access_denied",
        "job_unavailable",
        "job_result_not_found",
        "job_result_not_ready",
        "job_result_integrity_failed",
        "job_result_reference_invalid",
        "job_response_invalid",
        "payload_too_large",
        "not_found",
        // Every language but Dart checks the result against it; Dart's source says why it cannot.
        "X-Yogabase-Content-Sha256",
    ];

    public static TheoryData<string> Languages() => new(Sources.Keys);

    [Theory]
    [MemberData(nameof(Languages))]
    public async Task EveryLanguagesAuthoredJobsSurfaceSpellsTheSameWireNames(string language)
    {
        var text = await ReadAsync(language);

        foreach (var member in WireMembers)
        {
            Assert.True(text.Contains(member, StringComparison.OrdinalIgnoreCase), $"{language} no longer spells '{member}'.");
        }

        foreach (var name in ExactNames)
        {
            Assert.True(text.Contains(name, StringComparison.Ordinal), $"{language} no longer spells '{name}'.");
        }
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public async Task EveryLanguagesAuthoredJobsSurfaceHoldsTheSameBoundsAndRoute(string language)
    {
        // Digit separators and the way each language writes 16 * 1 MiB differ; the numbers do not.
        var text = (await ReadAsync(language)).Replace("_", string.Empty, StringComparison.Ordinal);

        Assert.Contains("1048576", text, StringComparison.Ordinal);
        Assert.True(
            text.Contains("16 * 1048576", StringComparison.Ordinal) || text.Contains("16*1048576", StringComparison.Ordinal),
            $"{language} no longer bounds a result at 16 MiB.");
        Assert.True(
            text.Contains("128", StringComparison.Ordinal) && text.Contains("256", StringComparison.Ordinal),
            $"{language} no longer states the job type and key bounds.");
        Assert.True(
            text.Contains("v1/projects/", StringComparison.Ordinal) && text.Contains("/jobs", StringComparison.Ordinal),
            $"{language} no longer addresses /v1/projects/{{projectId}}/jobs.");
    }

    private static async Task<string> ReadAsync(string language)
    {
        var root = FindRepoRoot();
        var parts = new List<string>();
        foreach (var relativePath in Sources[language])
        {
            var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (relativePath.EndsWith('/'))
            {
                foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
                {
                    parts.Add(await File.ReadAllTextAsync(file, TestContext.Current.CancellationToken));
                }

                continue;
            }

            parts.Add(await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
        }

        return string.Join('\n', parts);
    }

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
