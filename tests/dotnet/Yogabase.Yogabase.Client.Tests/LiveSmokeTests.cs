using System.Text;
using Yogabase.Yogabase;

namespace Yogabase.Tests;

/// <summary>
/// Signs in to a real installation and uses what the token opens. Skipped unless these are set:
/// YOGABASE_SMOKE_GATEWAY (the gateway origin), YOGABASE_SMOKE_PUBLISHABLE_KEY, YOGABASE_SMOKE_EMAIL and
/// YOGABASE_SMOKE_PASSWORD (an existing, confirmed user without MFA), YOGABASE_SMOKE_FUNCTION_ID and
/// YOGABASE_SMOKE_FUNCTION_VERSION (a deployed stateless function that accepts <c>{}</c>). Optional:
/// YOGABASE_SMOKE_TOPIC, a realtime topic that function publishes to, adds a subscribe-and-receive step.
/// </summary>
public sealed class LiveSmokeTests
{
    [Fact]
    public async Task SignInThenInvokeStoreAndReceive()
    {
        static string? Env(string name) => Environment.GetEnvironmentVariable("YOGABASE_SMOKE_" + name);
        string[] required = ["GATEWAY", "PUBLISHABLE_KEY", "EMAIL", "PASSWORD", "FUNCTION_ID", "FUNCTION_VERSION"];
        Assert.SkipWhen(required.Any(name => string.IsNullOrEmpty(Env(name))), "Set the YOGABASE_SMOKE_* variables to run against an installation.");
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var client = await YogabaseClient.CreateAsync(new Uri(Env("GATEWAY")!), Env("PUBLISHABLE_KEY")!, cancellationToken: cancellationToken);
        var session = await client.Auth.SignInWithPasswordAsync(Env("EMAIL")!, Env("PASSWORD")!, cancellationToken: cancellationToken);
        Assert.False(string.IsNullOrEmpty(session.AccessToken));

        // Subscribe before the function runs so its message cannot be missed.
        var topic = Env("TOPIC");
        var received = new TaskCompletionSource<YogabaseRealtimeEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        client.Realtime.MessageReceived += message =>
        {
            received.TrySetResult(message);
            return Task.CompletedTask;
        };
        IAsyncDisposable? subscription = null;
        if (!string.IsNullOrEmpty(topic))
        {
            await client.Realtime.ConnectAsync(cancellationToken);
            subscription = await client.Realtime.SubscribeAsync(topic, cancellationToken);
        }

        await using var held = subscription;

        var invoked = await client.Functions.InvokeAsync(Env("FUNCTION_ID")!, Env("FUNCTION_VERSION")!, new YogabaseInvocationRequest
        {
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            CorrelationId = "live-smoke",
            Deadline = DateTimeOffset.UtcNow.AddSeconds(20),
            Mode = YogabaseInvocationMode.Synchronous,
            InlinePayload = Encoding.UTF8.GetBytes("{}"),
        }, cancellationToken);
        Assert.Equal(YogabaseInvocationOutcome.Completed, invoked.Outcome);

        var bytes = Encoding.UTF8.GetBytes("live smoke " + Guid.NewGuid());
        var key = $"smoke/{Guid.NewGuid():N}.txt";
        await client.Storage.PutBytesAsync(key, bytes, "text/plain", Guid.NewGuid().ToString("N"), cancellationToken);
        await using var read = await client.Storage.GetAsync(key, cancellationToken);
        using var content = new MemoryStream();
        await read.Content.CopyToAsync(content, cancellationToken);
        Assert.Equal(bytes, content.ToArray());

        if (!string.IsNullOrEmpty(topic))
        {
            await received.Task.WaitAsync(TimeSpan.FromSeconds(20), cancellationToken);
        }
    }
}
