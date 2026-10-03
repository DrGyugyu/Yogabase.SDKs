using System.Net;
using System.Text;
using System.Text.Json;
using Yogabase.Yogabase;

namespace Yogabase.Tests;

/// <summary>
/// A delivery release carries the whole environment configuration, and the longest a durable job's
/// function may run (<c>jobsMaxRunSeconds</c>, 1 to 900, owner decision 2026-10-03) is part of it:
/// a release that omits the field returns the environment to the 30-second default. The client
/// sends exactly what it is given, so these tests pin that a stated value reaches the wire and that
/// an unstated one is not invented.
/// </summary>
public sealed class DeliveryReleaseConfigurationTests
{
    private static readonly Guid ProjectId = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static readonly Guid EnvironmentId = Guid.Parse("22222222-2222-4222-8222-222222222222");
    private static readonly Guid ArtifactId = Guid.Parse("33333333-3333-4333-8333-333333333333");

    [Fact]
    public async Task AReleasePreviewCarriesTheLongestJobRunItWasGiven()
    {
        var body = await PreviewBodyAsync(new YogabaseDeliveryReleaseConfiguration
        {
            StatelessFunctions = true,
            Jobs = true,
            JobsMaxRunSeconds = 120,
        });

        using var document = JsonDocument.Parse(body);
        var configuration = document.RootElement.GetProperty("configuration");
        Assert.Equal(120, configuration.GetProperty("jobsMaxRunSeconds").GetInt32());
        Assert.True(configuration.GetProperty("jobs").GetBoolean());
    }

    [Fact]
    public async Task AReleasePreviewDoesNotInventALongestJobRun()
    {
        var body = await PreviewBodyAsync(new YogabaseDeliveryReleaseConfiguration
        {
            StatelessFunctions = true,
            Jobs = true,
        });

        using var document = JsonDocument.Parse(body);
        Assert.False(document.RootElement.GetProperty("configuration").TryGetProperty("jobsMaxRunSeconds", out _));
    }

    private static async Task<string> PreviewBodyAsync(YogabaseDeliveryReleaseConfiguration configuration)
    {
        string? body = null;
        var handler = new ScriptedHandler(async (request, cancellationToken) =>
        {
            body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new
                    {
                        previewId = "77777777-7777-4777-8777-777777777777",
                        previewDigest = "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                        action = "CreateRelease",
                        expectedRevision = 4,
                        approvalRequired = false,
                        expiresAt = "2026-09-01T00:10:00Z",
                    }),
                    Encoding.UTF8,
                    "application/json"),
            };
        });
        using var delivery = new YogabaseDeliveryClient(
            new Uri("http://localhost:7000/"),
            _ => Task.FromResult<string?>("delivery-token"),
            new YogabaseDeliveryClientOptions { AllowInsecureLoopback = true, TransportHandler = handler });

        await delivery.PreviewReleaseAsync(
            ProjectId,
            EnvironmentId,
            new YogabaseDeliveryReleasePreviewRequest
            {
                ArtifactAdmissionIds = [ArtifactId],
                Configuration = configuration,
                MigrationEvidenceDigest = "sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
            },
            "W/\"4\"",
            TestContext.Current.CancellationToken);
        return body!;
    }

    private sealed class ScriptedHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => send(request, cancellationToken);
    }
}
