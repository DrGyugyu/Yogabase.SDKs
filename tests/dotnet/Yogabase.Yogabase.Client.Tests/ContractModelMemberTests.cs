using System.Reflection;
using System.Text.Json;
using Yogabase.Yogabase;

namespace Yogabase.Tests;

/// <summary>
/// Holds the authored .NET response models to the members the mirrored documents declare for them. The
/// transport reads every answer with <c>UnmappedMemberHandling.Disallow</c>, so a member the product adds to a
/// schema that the model does not name turns a good answer into <c>response_invalid</c> instead of being
/// ignored. Re-mirroring a document that grew a member therefore fails here, naming the member, until the
/// model declares it.
/// </summary>
public sealed class ContractModelMemberTests
{
    private const string SolutionFileName = "Yogabase.SDKs.slnx";

    [Theory]
    [InlineData("yogabase-builds-v1.openapi.json", "Build", typeof(YogabaseBuild))]
    [InlineData("yogabase-builds-v1.openapi.json", "Check", typeof(YogabaseBuildCheck))]
    [InlineData("yogabase-builds-v1.openapi.json", "Finding", typeof(YogabaseBuildFinding))]
    [InlineData("yogabase-builds-v1.openapi.json", "Refusal", typeof(YogabaseBuildRefusal))]
    [InlineData("yogabase-client-v1.openapi.json", "GameServer", typeof(YogabaseServer))]
    [InlineData("yogabase-client-v1.openapi.json", "GameServerPort", typeof(YogabaseServerPort))]
    public async Task EveryMemberOfAMirroredSchemaIsDeclaredByTheAuthoredModel(
        string contractFileName,
        string schemaName,
        Type model)
    {
        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(
            Path.Combine(FindRepoRoot(), "contracts", contractFileName),
            TestContext.Current.CancellationToken));
        var schema = document.RootElement.GetProperty("components").GetProperty("schemas").GetProperty(schemaName);

        var undeclared = schema.GetProperty("properties").EnumerateObject()
            .Select(static member => member.Name)
            .Where(member => model.GetProperty(
                string.Concat(char.ToUpperInvariant(member[0]).ToString(), member.AsSpan(1)),
                BindingFlags.Public | BindingFlags.Instance)?.SetMethod?.IsPublic != true)
            .ToArray();

        Assert.True(
            undeclared.Length == 0,
            $"{model.Name} does not declare {schemaName} member(s) {string.Join(", ", undeclared)} of {contractFileName}; "
            + "the transport refuses an answer that carries a member its model leaves out.");
    }

    /// <summary>
    /// The game-server instance's <c>address</c> is optional, a bounded non-empty string, and a member of the
    /// answer an authored client must read (it is how a client that was handed no host reaches the instance).
    /// </summary>
    [Fact]
    public async Task TheMirroredGameServerDeclaresAnOptionalBoundedAddress()
    {
        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(
            Path.Combine(FindRepoRoot(), "contracts", "yogabase-client-v1.openapi.json"),
            TestContext.Current.CancellationToken));
        var schema = document.RootElement.GetProperty("components").GetProperty("schemas").GetProperty("GameServer");
        var address = schema.GetProperty("properties").GetProperty("address");

        Assert.Equal("string", address.GetProperty("type").GetString());
        Assert.Equal(1, address.GetProperty("minLength").GetInt32());
        Assert.Equal(253, address.GetProperty("maxLength").GetInt32());
        Assert.DoesNotContain(
            "address",
            schema.GetProperty("required").EnumerateArray().Select(static name => name.GetString()));
    }

    /// <summary>
    /// The builds document's <c>Check.reportedByClusterAgent</c> is an optional boolean: an answer that has no
    /// cluster-agent report leaves it out, so the model must read both its absence and its value.
    /// </summary>
    [Fact]
    public async Task TheMirroredBuildCheckDeclaresAnOptionalBooleanClusterAgentReport()
    {
        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(
            Path.Combine(FindRepoRoot(), "contracts", "yogabase-builds-v1.openapi.json"),
            TestContext.Current.CancellationToken));
        var schema = document.RootElement.GetProperty("components").GetProperty("schemas").GetProperty("Check");
        var member = schema.GetProperty("properties").GetProperty("reportedByClusterAgent");

        Assert.Equal("boolean", member.GetProperty("type").GetString());
        Assert.DoesNotContain(
            "reportedByClusterAgent",
            schema.GetProperty("required").EnumerateArray().Select(static name => name.GetString()));
        Assert.Equal(typeof(bool), typeof(YogabaseBuildCheck).GetProperty(nameof(YogabaseBuildCheck.ReportedByClusterAgent))!.PropertyType);
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
