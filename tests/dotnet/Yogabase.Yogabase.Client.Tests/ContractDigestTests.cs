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
