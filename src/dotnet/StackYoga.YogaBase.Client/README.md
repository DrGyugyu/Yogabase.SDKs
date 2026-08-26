# StackYoga.YogaBase.Client

Preview .NET client for the versioned YogaBase application-gateway wire contract. The package
targets .NET Standard 2.1 and .NET 10; Unity remains an unqualified candidate platform pending
IL2CPP/AOT and dependency-closure testing. It supports non-secret publishable-key bootstrap,
OIDC authorization-code/PKCE, the bounded Data API, mediated object
storage, Lambda invocation, and SignalR topic subscriptions.

The publishable key identifies a client configuration only. It is never sent as application
authorization; authenticated calls use an OIDC access token. The default session store is memory
only. Supply an `IYogaBaseSessionStore` backed by the host platform's secure credential facility if
sessions must survive process restarts.

```csharp
using StackYoga.YogaBase;

await using var yogaBase = await YogaBaseClient.CreateAsync(
    new Uri("https://api.example.com"),
    "sy_pub_example_project");

if (yogaBase.Configuration.IsAvailable("data-rest"))
{
    var rows = await yogaBase.Data.SelectAsync(
        "profiles",
        new YogaBaseSelectRequest { Columns = new[] { "display_name" }, PageSize = 25 });
}
```

YogaBase and this package remain preview capabilities until the repository's YogaBase release
gates pass. The current runtime deliberately reports authentication and all bearer-dependent
capabilities unavailable until public-client registration, redirect binding, and the browser login
journey are provisioned and qualified. Unknown capability identifiers are treated as unavailable.
