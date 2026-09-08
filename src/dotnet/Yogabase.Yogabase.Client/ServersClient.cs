using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Yogabase.Yogabase;

/// <summary>
/// One allocated hosted-service instance and the addresses a client may connect to.
/// </summary>
public sealed class YogabaseServer
{
    /// <summary>Opaque instance identifier. Treat it as a bounded token, not a parsed value.</summary>
    public string ServerId { get; set; } = string.Empty;

    /// <summary>
    /// Lifecycle state reported by the gateway, verbatim. The client does not translate it into an
    /// enumeration, because a state Yogabase adds later must stay readable rather than throw.
    /// </summary>
    public string State { get; set; } = string.Empty;

    /// <summary>Externally reachable addresses of this instance's declared ports.</summary>
    public IReadOnlyList<YogabaseServerPort> NetworkPorts { get; set; } = Array.Empty<YogabaseServerPort>();

    /// <summary>The named port, or <see langword="null"/> when the instance does not declare it.</summary>
    public YogabaseServerPort? FindPort(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException("A port name is required.", nameof(name));
        }

        foreach (var port in NetworkPorts)
        {
            if (string.Equals(port.Name, name, StringComparison.Ordinal))
            {
                return port;
            }
        }

        return null;
    }
}

/// <summary>One externally reachable port of an allocated instance.</summary>
public sealed class YogabaseServerPort
{
    /// <summary>Port name as declared by the build (for example <c>game</c>).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Port the workload itself listens on inside its Pod.</summary>
    public int InternalPort { get; set; }

    /// <summary>Port a client connects to on <see cref="Host"/>.</summary>
    public int ExternalPort { get; set; }

    /// <summary>Transport protocol, <c>udp</c> or <c>tcp</c>.</summary>
    public string Protocol { get; set; } = string.Empty;

    /// <summary>Node address a client connects to.</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>The <c>host:port</c> a client dials.</summary>
    public string Endpoint => Host + ":" + ExternalPort.ToString(System.Globalization.CultureInfo.InvariantCulture);
}

/// <summary>Request body of <c>POST /v1/servers/start</c>.</summary>
public sealed class YogabaseServerStartRequest
{
    /// <summary>Name of the hosted service to allocate an instance of.</summary>
    public string Service { get; set; } = string.Empty;

    /// <summary>
    /// Optional bounded string map handed to the allocated instance. It travels to customer code,
    /// so it must never carry a credential or another player's identity.
    /// </summary>
    public IReadOnlyDictionary<string, string>? CustomData { get; set; }
}

/// <summary>
/// Client-facing game-server allocation. Every call is authenticated with the signed-in player's
/// access token against the application gateway; no developer or workload credential is accepted.
/// </summary>
public interface IYogabaseServers
{
    /// <summary>Allocates a Ready instance of one hosted service and returns its address.</summary>
    Task<YogabaseServer> StartAsync(YogabaseServerStartRequest request, CancellationToken cancellationToken = default);

    /// <summary>Allocates a Ready instance of one hosted service with no custom data.</summary>
    Task<YogabaseServer> StartAsync(string service, CancellationToken cancellationToken = default);

    /// <summary>Lists the instances this caller may see.</summary>
    Task<IReadOnlyList<YogabaseServer>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Reads one instance.</summary>
    Task<YogabaseServer> GetAsync(string serverId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops one instance. Returns the instance's last reported state, or <see langword="null"/>
    /// when the gateway answers without a body.
    /// </summary>
    Task<YogabaseServer?> StopAsync(string serverId, CancellationToken cancellationToken = default);
}

internal sealed class YogabaseServersClient : IYogabaseServers
{
    private readonly YogabaseHttpTransport transport;

    internal YogabaseServersClient(YogabaseHttpTransport transport)
    {
        this.transport = transport;
    }

    public Task<YogabaseServer> StartAsync(string service, CancellationToken cancellationToken = default) =>
        StartAsync(new YogabaseServerStartRequest { Service = service }, cancellationToken);

    public async Task<YogabaseServer> StartAsync(
        YogabaseServerStartRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        ValidateService(request.Service);
        ValidateCustomData(request.CustomData);
        var response = await transport.SendAsync(
            HttpMethod.Post,
            YogabaseRoutes.ServersStart,
            new StartBody { Service = request.Service, CustomData = request.CustomData },
            authenticated: true,
            cancellationToken).ConfigureAwait(false);
        return Validate(await YogabaseHttpTransport.ReadJsonAsync<YogabaseServer>(response, cancellationToken)
            .ConfigureAwait(false));
    }

    public async Task<IReadOnlyList<YogabaseServer>> ListAsync(CancellationToken cancellationToken = default)
    {
        var response = await transport.SendAsync(
            HttpMethod.Get,
            YogabaseRoutes.Servers,
            body: null,
            authenticated: true,
            cancellationToken).ConfigureAwait(false);
        var page = await YogabaseHttpTransport.ReadJsonAsync<ServerCollection>(response, cancellationToken)
            .ConfigureAwait(false);
        var servers = page.Servers ?? Array.Empty<YogabaseServer>();
        foreach (var server in servers)
        {
            Validate(server);
        }

        return servers;
    }

    public async Task<YogabaseServer> GetAsync(string serverId, CancellationToken cancellationToken = default)
    {
        var response = await transport.SendAsync(
            HttpMethod.Get,
            YogabaseRoutes.Server(EscapeServerId(serverId)),
            body: null,
            authenticated: true,
            cancellationToken).ConfigureAwait(false);
        return Validate(await YogabaseHttpTransport.ReadJsonAsync<YogabaseServer>(response, cancellationToken)
            .ConfigureAwait(false));
    }

    public async Task<YogabaseServer?> StopAsync(string serverId, CancellationToken cancellationToken = default)
    {
        var response = await transport.SendAsync(
            HttpMethod.Post,
            YogabaseRoutes.ServerStop(EscapeServerId(serverId)),
            body: null,
            authenticated: true,
            cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent
            || response.Content.Headers.ContentLength == 0)
        {
            response.Dispose();
            return null;
        }

        return Validate(await YogabaseHttpTransport.ReadJsonAsync<YogabaseServer>(response, cancellationToken)
            .ConfigureAwait(false));
    }

    private static string EscapeServerId(string serverId) =>
        YogabaseHttpTransport.EscapePath(serverId, nameof(serverId));

    private static void ValidateService(string service)
    {
        if (string.IsNullOrWhiteSpace(service)
            || service.Length > 253
            || !string.Equals(service, service.Trim(), StringComparison.Ordinal))
        {
            throw new ArgumentException("A bounded hosted-service name is required.", nameof(service));
        }
    }

    private static void ValidateCustomData(IReadOnlyDictionary<string, string>? customData)
    {
        if (customData is null)
        {
            return;
        }
        if (customData.Count > 32)
        {
            throw new ArgumentException("Custom data carries at most 32 entries.", nameof(customData));
        }

        foreach (var entry in customData)
        {
            if (string.IsNullOrWhiteSpace(entry.Key)
                || entry.Key.Length > 64
                || entry.Value is null
                || entry.Value.Length > 1024)
            {
                throw new ArgumentException("A custom-data entry is outside the gateway limits.", nameof(customData));
            }
        }
    }

    // A malformed allocation is worse than a failed one: a caller that trusts an empty host will
    // dial its own machine. Reject the response instead of returning an unusable address.
    private static YogabaseServer Validate(YogabaseServer server)
    {
        if (string.IsNullOrWhiteSpace(server.ServerId) || string.IsNullOrWhiteSpace(server.State))
        {
            throw new YogabaseApiException(502, "server_response_invalid", "Yogabase returned an invalid game-server instance.");
        }

        foreach (var port in server.NetworkPorts)
        {
            if (string.IsNullOrWhiteSpace(port.Name)
                || string.IsNullOrWhiteSpace(port.Host)
                || port.ExternalPort is < 1 or > 65535
                || port.InternalPort is < 1 or > 65535
                || port.Protocol is not ("udp" or "tcp"))
            {
                throw new YogabaseApiException(502, "server_response_invalid", "Yogabase returned an invalid game-server address.");
            }
        }

        return server;
    }

    private sealed class StartBody
    {
        public string Service { get; set; } = string.Empty;
        public IReadOnlyDictionary<string, string>? CustomData { get; set; }
    }

    /// <summary>
    /// <c>GET /v1/servers</c> is read through a converter rather than a fixed shape, because the
    /// surface is still landing on the server: a bare array and a <c>{ "servers": [...] }</c>
    /// envelope both parse, and anything else is refused rather than silently read as empty.
    /// </summary>
    [System.Text.Json.Serialization.JsonConverter(typeof(ServerCollectionConverter))]
    private sealed class ServerCollection
    {
        public IReadOnlyList<YogabaseServer>? Servers { get; set; }
    }

    private sealed class ServerCollectionConverter : System.Text.Json.Serialization.JsonConverter<ServerCollection>
    {
        public override ServerCollection Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                return new ServerCollection
                {
                    Servers = JsonSerializer.Deserialize<YogabaseServer[]>(ref reader, YogabaseJson.Options),
                };
            }
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("A game-server listing must be an array or an object.");
            }

            IReadOnlyList<YogabaseServer>? servers = null;
            var depth = reader.CurrentDepth;
            while (reader.Read() && !(reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == depth))
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    continue;
                }

                var name = reader.GetString();
                reader.Read();
                if (string.Equals(name, "servers", StringComparison.Ordinal)
                    || string.Equals(name, "items", StringComparison.Ordinal))
                {
                    servers = JsonSerializer.Deserialize<YogabaseServer[]>(ref reader, YogabaseJson.Options);
                }
                else
                {
                    reader.Skip();
                }
            }

            if (servers is null)
            {
                throw new JsonException("A game-server listing object must carry a servers or items array.");
            }

            return new ServerCollection { Servers = servers };
        }

        public override void Write(Utf8JsonWriter writer, ServerCollection value, JsonSerializerOptions options) =>
            throw new NotSupportedException("Game-server listings are never written by this client.");
    }
}
