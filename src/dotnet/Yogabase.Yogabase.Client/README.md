# Yogabase.Yogabase.Client

Preview .NET client for the versioned Yogabase application-gateway wire contract. The package
targets .NET Standard 2.1 and .NET 10; Unity remains an unqualified candidate platform pending
IL2CPP/AOT and dependency-closure testing. It supports non-secret publishable-key bootstrap,
OIDC authorization-code/PKCE, the bounded Data API, mediated object
storage, stateless Function invocation, Actor and Workflow Lambdas, and SignalR realtime.

The publishable key identifies a client configuration only. It is never sent as application
authorization; authenticated calls use an OIDC access token. The default session store is memory
only. Supply an `IYogabaseSessionStore` backed by the host platform's secure credential facility if
sessions must survive process restarts.

```csharp
using Yogabase.Yogabase;

await using var yogabase = await YogabaseClient.CreateAsync(
    new Uri("https://api.example.com"),
    "sy_pub_example_project");

if (yogabase.Configuration.IsAvailable("data-rest"))
{
    var rows = await yogabase.Data.SelectAsync(
        "profiles",
        new YogabaseSelectRequest { Columns = new[] { "display_name" }, PageSize = 25 });
}
```

`YogabaseClientConfiguration` carries separate `GatewayEndpoint` and `RealtimeEndpoint` origins.
The client rejects malformed or insecure non-loopback origins and never derives realtime traffic
from the REST gateway. Storage uploads send `X-Yogabase-Content-Sha256`; Actor and Workflow inline
payloads are digest-checked before transport. Realtime Lambda completions are at-least-once and the
client suppresses duplicate event IDs within a bounded in-memory window.

Interactive login/MFA methods use the protected cookie session maintained by the client's HTTP
handler. PKCE methods manage bearer-token sessions through `IYogabaseSessionStore`. Hosts that
replace the default handler are responsible for equivalent cookie behavior and redirect safety.

Yogabase and this package remain preview capabilities until the repository's release gates pass.
The signed bootstrap capability list is authoritative for each deployment; unknown capability
identifiers are treated as unavailable.
