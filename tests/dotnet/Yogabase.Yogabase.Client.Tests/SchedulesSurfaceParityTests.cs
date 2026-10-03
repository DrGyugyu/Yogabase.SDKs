namespace Yogabase.Tests;

/// <summary>
/// The schedule surface of every language, held to the same names. Where a language wraps the Kiota builders
/// (Java, Dart) the route tails and the request members are spelled by the generated builders and models,
/// which are read with the authored client; where it is standalone (.NET, TypeScript, Python) its own file
/// spells them. The twin of <see cref="JobsSurfaceParityTests"/>: the operations are held to the contract by
/// <see cref="ContractOperationParityTests"/> and the limits and codes by
/// <see cref="ContractLimitsAndCodesTests"/>, this holds the five spellings to each other. It reads the
/// sources as text, which is the only check Dart can have here.
/// </summary>
public sealed class SchedulesSurfaceParityTests
{
    private const string SolutionFileName = "Yogabase.SDKs.slnx";

    // An entry that ends in a slash is every source file under that directory.
    private static readonly Dictionary<string, string[]> Sources = new(StringComparer.Ordinal)
    {
        [".NET"] =
        [
            "src/dotnet/Yogabase.Yogabase.Client/SchedulesClient.cs",
            "src/dotnet/Yogabase.Yogabase.Client/YogabaseRoutes.cs",
            "src/dotnet/Yogabase.Yogabase.Client/StatusErrors.cs",
        ],
        ["TypeScript"] =
        [
            "languages/typescript/src/schedules.ts",
            "languages/typescript/src/gateway.ts",
        ],
        ["Python"] =
        [
            "languages/python/src/stackyoga_yogabase/schedules.py",
            "languages/python/src/stackyoga_yogabase/_gateway.py",
        ],
        ["Java"] =
        [
            "languages/java/src/main/java/io/stackyoga/yogabase/YogabaseSchedulesClient.java",
            "languages/java/src/main/java/io/stackyoga/yogabase/YogabaseScheduleErrorCodes.java",
            "languages/java/src/main/java/io/stackyoga/yogabase/YogabaseScheduleMisfirePolicy.java",
            "languages/java/src/main/java/io/stackyoga/yogabase/YogabaseScheduleConcurrencyPolicy.java",
            "languages/java/src/main/java/io/stackyoga/yogabase/YogabaseStatusErrorCodes.java",
            "languages/java/src/main/java/io/stackyoga/yogabase/YogabaseGatewayChannel.java",
            "languages/java/src/main/java/io/stackyoga/yogabase/v1/projects/item/schedules/",
            "languages/java/src/main/java/io/stackyoga/yogabase/models/ScheduleDefinitionRequest.java",
            "languages/java/src/main/java/io/stackyoga/yogabase/models/ScheduleTriggerRequest.java",
        ],
        ["Dart"] =
        [
            "languages/dart/lib/src/yogabase_schedules_client.dart",
            "languages/dart/lib/src/yogabase_gateway_support.dart",
            "languages/dart/lib/src/v1/projects/item/schedules/",
            "languages/dart/lib/src/models/schedule_definition_request.dart",
            "languages/dart/lib/src/models/schedule_trigger_request.dart",
        ],
    };

    // Members, route tails and the two conditional headers, compared without regard to case because .NET writes
    // its members as PascalCase properties that its serializer names in camelCase.
    private static readonly string[] WireMembers =
    [
        "scheduleId",
        "functionId",
        "functionVersion",
        "cron",
        "timeZone",
        "misfirePolicy",
        "concurrencyPolicy",
        "maxCatchUp",
        "maxAttempts",
        "deadlineSeconds",
        "payloadBase64",
        "oneShotAtUtc",
        "idempotencyKey",
        "deadline",
        "invocationId",
        "outcome",
        "/trigger",
        "If-Match",
        "If-None-Match",
        "schedule-v",
    ];

    // Names the gateway reads, writes or refuses with exactly: the policy names, the one code the gateway names,
    // the code this client names, and the status-derived codes a schedule write meets that a job route does not.
    private static readonly string[] ExactNames =
    [
        "RunImmediately",
        "SkipToNextOccurrence",
        "RunAllMissed",
        "Allow",
        "Forbid",
        "Replace",
        "schedule_payload_too_large",
        "schedule_response_invalid",
        "precondition_failed",
        "precondition_required",
        "unsupported_media_type",
        "payload_too_large",
        "not_found",
        "conflict",
    ];

    public static TheoryData<string> Languages() => new(Sources.Keys);

    [Theory]
    [MemberData(nameof(Languages))]
    public async Task EveryLanguagesScheduleSurfaceSpellsTheSameWireNames(string language)
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
    public async Task EveryLanguagesScheduleSurfaceHoldsTheSameBoundsAndRoute(string language)
    {
        // Digit separators differ between languages; the numbers do not.
        var text = (await ReadAsync(language)).Replace("_", string.Empty, StringComparison.Ordinal);

        foreach (var bound in new[] { "1048576", "86400", "3600", "128", "256", "64", "16" })
        {
            Assert.True(text.Contains(bound, StringComparison.Ordinal), $"{language} no longer states the bound {bound}.");
        }

        Assert.True(
            text.Contains("v1/projects/", StringComparison.Ordinal) && text.Contains("/schedules", StringComparison.Ordinal),
            $"{language} no longer addresses /v1/projects/{{projectId}}/schedules.");
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
