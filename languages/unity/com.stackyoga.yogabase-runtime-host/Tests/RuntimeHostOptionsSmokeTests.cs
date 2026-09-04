// Minimal smoke coverage for the Unity package wrapper around Yogabase.Yogabase.RuntimeHost.
// This intentionally does NOT port the full .NET test suite (see
// src/dotnet/Yogabase.Yogabase.RuntimeHost's own tests for that, 60/60 passing there). It only
// checks that the copied sources compile and behave sanely inside a Unity assembly, since that
// is the one thing the .NET suite cannot verify for us.
//
// This has not been run inside the Unity Test Runner — no Unity Editor is available in the
// environment that authored this package. See the package README's "Honest limits" section.

using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Yogabase.Yogabase.RuntimeHost;

namespace Yogabase.Yogabase.RuntimeHost.Tests
{
    internal sealed class RuntimeHostOptionsSmokeTests
    {
        [Test]
        public void DefaultOptions_ResolveLoopbackEndpoint()
        {
            var options = new YogabaseRuntimeHostOptions();

            Assert.AreEqual(new Uri("http://127.0.0.1:8090"), options.Endpoint);
            Assert.AreEqual(TimeSpan.FromSeconds(2), options.HeartbeatInterval);
            Assert.AreEqual(TimeSpan.FromSeconds(10), options.RequestTimeout);
        }

        [Test]
        public async Task Client_ConstructsAndDisposesWithoutThrowing()
        {
            var options = new YogabaseRuntimeHostOptions
            {
                Endpoint = new Uri("http://127.0.0.1:8090"),
            };

            var client = new YogabaseRuntimeHostClient(options);
            Assert.IsNotNull(client);

            await client.DisposeAsync();
        }
    }
}
