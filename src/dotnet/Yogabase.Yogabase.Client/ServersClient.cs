using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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

    /// <summary>
    /// Externally reachable addresses of this instance's declared ports. Empty when no member has
    /// observed an address for the instance yet; a client must not connect to such an instance.
    /// </summary>
    public IReadOnlyList<YogabaseServerPort> NetworkPorts { get; set; } = Array.Empty<YogabaseServerPort>();

    /// <summary>
    /// The address a client is handed for this instance, in the Agones shape, or
    /// <see langword="null"/> from a Control Plane that does not send it — today's does not, and adds
    /// it only once a client that accepts it is the minimum. When <see cref="Addresses"/> is also
    /// present it is one of them.
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Every typed address observed for this instance, in the Agones shape (for example
    /// <c>Advertised</c>, the node's <c>ExternalIP</c> or <c>InternalIP</c>, <c>PodIP</c>), at most 16, or
    /// <see langword="null"/> when the Control Plane sends none.
    /// </summary>
    public IReadOnlyList<YogabaseServerAddress>? Addresses { get; set; }

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

    /// <summary>Node address a client connects to, exactly as the member observed it.</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>The <c>host:port</c> a client dials.</summary>
    public string Endpoint => Host + ":" + ExternalPort.ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>
    /// The address a client is handed for this port, in the Agones shape, or <see langword="null"/>
    /// when the Control Plane does not send it. When <see cref="Addresses"/> is also present it is one
    /// of them. <see cref="Host"/> stays the address to dial.
    /// </summary>
    public string? Address { get; set; }

    /// <summary>Every typed address observed for this port, at most 16, or <see langword="null"/>.</summary>
    public IReadOnlyList<YogabaseServerAddress>? Addresses { get; set; }
}

/// <summary>One typed address in the Agones shape: its kind and the address itself.</summary>
public sealed class YogabaseServerAddress
{
    /// <summary>
    /// What kind of address this is, verbatim (for example <c>Advertised</c>, <c>ExternalIP</c>,
    /// <c>InternalIP</c>, <c>ExternalDNS</c>, <c>InternalDNS</c>, <c>Hostname</c> or <c>PodIP</c>). Not
    /// an enumeration, so a kind added later stays readable.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>An IP literal or DNS name.</summary>
    public string Address { get; set; } = string.Empty;
}

/// <summary>Request body of <c>POST /v1/servers/start</c>.</summary>
public sealed class YogabaseServerStartRequest
{
    /// <summary>Canonical id of the hosted service to allocate an instance of.</summary>
    public string Service { get; set; } = string.Empty;

    /// <summary>
    /// Optional bounded string map recorded on the allocation and carried to the instance. It
    /// travels to customer code, so it must never carry a credential or another player's identity.
    /// </summary>
    public IReadOnlyDictionary<string, string>? CustomData { get; set; }
}

/// <summary>
/// Client-facing game-server allocation. Every call is authenticated with the signed-in player's
/// access token against the application gateway; no developer or workload credential is accepted.
/// </summary>
public interface IYogabaseServers
{
    /// <summary>
    /// Allocates a Ready instance of one hosted service and returns its address. A service with no
    /// Ready instance carrying an observed address is refused with <c>server_none_available</c>
    /// rather than answered with an address nothing listens on.
    /// </summary>
    Task<YogabaseServer> StartAsync(YogabaseServerStartRequest request, CancellationToken cancellationToken = default);

    /// <summary>Allocates a Ready instance of one hosted service with no custom data.</summary>
    Task<YogabaseServer> StartAsync(string service, CancellationToken cancellationToken = default);

    /// <summary>
    /// Every instance of the caller's environment, ordered by server id. An instance whose address
    /// no member has observed is listed with empty <see cref="YogabaseServer.NetworkPorts"/>.
    /// </summary>
    Task<IReadOnlyList<YogabaseServer>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Reads one instance. A server id from another project is answered as one that does not exist.</summary>
    Task<YogabaseServer> GetAsync(string serverId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases the allocation this caller holds on one instance and returns the instance's state
    /// after the release. It is never a deletion, and an instance allocated to a different caller
    /// is refused with <c>server_stop_not_available</c>.
    /// </summary>
    Task<YogabaseServer> StopAsync(string serverId, CancellationToken cancellationToken = default);
}

internal sealed class YogabaseServersClient : IYogabaseServers
{
    /// <summary>The service-id grammar the product's bundle contract pins, spelled the same way.</summary>
    private static readonly Regex ServiceIdPattern = new(
        "^[a-z0-9](?:[-a-z0-9]{0,61}[a-z0-9])?$",
        RegexOptions.CultureInvariant);

    private const int MaximumServiceIdLength = 63;
    private const int MaximumServerIdLength = 128;
    private const int MaximumCustomDataEntries = 32;
    private const int MaximumCustomDataKeyLength = 64;
    private const int MaximumCustomDataValueLength = 1024;
    private const int MaximumCustomDataBytes = 4 * 1024;
    private const int MaximumTypedAddresses = 16;

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
        // The contract pins a bare array; the gateway relays the Control Plane's body verbatim.
        var servers = await YogabaseHttpTransport.ReadJsonAsync<YogabaseServer[]>(response, cancellationToken)
            .ConfigureAwait(false);
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

    public async Task<YogabaseServer> StopAsync(string serverId, CancellationToken cancellationToken = default)
    {
        var response = await transport.SendAsync(
            HttpMethod.Post,
            YogabaseRoutes.ServerStop(EscapeServerId(serverId)),
            body: null,
            authenticated: true,
            cancellationToken).ConfigureAwait(false);
        return Validate(await YogabaseHttpTransport.ReadJsonAsync<YogabaseServer>(response, cancellationToken)
            .ConfigureAwait(false));
    }

    private static string EscapeServerId(string serverId)
    {
        if (serverId is null)
        {
            throw new ArgumentNullException(nameof(serverId));
        }
        if (string.IsNullOrWhiteSpace(serverId)
            || serverId.Length > MaximumServerIdLength
            || !string.Equals(serverId, serverId.Trim(), StringComparison.Ordinal))
        {
            throw new ArgumentException("A bounded non-empty server id is required.", nameof(serverId));
        }

        return Uri.EscapeDataString(serverId);
    }

    private static void ValidateService(string service)
    {
        if (string.IsNullOrWhiteSpace(service)
            || service.Length > MaximumServiceIdLength
            || !ServiceIdPattern.IsMatch(service))
        {
            throw new ArgumentException(
                "A canonical hosted-service id is required: lowercase letters, digits and '-', "
                + "starting and ending with a letter or digit, at most 63 characters.",
                nameof(service));
        }
    }

    private static void ValidateCustomData(IReadOnlyDictionary<string, string>? customData)
    {
        if (customData is null || customData.Count == 0)
        {
            return;
        }
        if (customData.Count > MaximumCustomDataEntries)
        {
            throw new ArgumentException("Custom data carries at most 32 entries.", nameof(customData));
        }

        // Ordinal-ordered, exactly as the Control Plane canonicalizes it before measuring, so the
        // 4 KiB bound refused here is the same one the server applies.
        var ordered = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in customData)
        {
            if (string.IsNullOrWhiteSpace(entry.Key)
                || entry.Key.Length > MaximumCustomDataKeyLength
                || entry.Value is null
                || entry.Value.Length > MaximumCustomDataValueLength)
            {
                throw new ArgumentException("A custom-data entry is outside the gateway limits.", nameof(customData));
            }

            ordered[entry.Key] = entry.Value;
        }

        var canonical = JsonSerializer.Serialize(ordered, YogabaseJson.Options);
        if (Encoding.UTF8.GetByteCount(canonical) > MaximumCustomDataBytes)
        {
            throw new ArgumentException("Custom data must be at most 4096 bytes as canonical JSON.", nameof(customData));
        }
    }

    // A malformed allocation is worse than a failed one: a caller that trusts an empty host will
    // dial its own machine. Reject the response instead of returning an unusable address.
    private static YogabaseServer Validate(YogabaseServer server)
    {
        if (string.IsNullOrWhiteSpace(server.ServerId)
            || server.ServerId.Length > MaximumServerIdLength
            || string.IsNullOrWhiteSpace(server.State)
            || server.NetworkPorts is null)
        {
            throw new YogabaseApiException(502, "server_response_invalid", "Yogabase returned an invalid game-server instance.");
        }

        foreach (var port in server.NetworkPorts)
        {
            if (string.IsNullOrWhiteSpace(port.Name)
                || string.IsNullOrWhiteSpace(port.Host)
                || port.ExternalPort is < 1 or > 65535
                || port.InternalPort is < 1 or > 65535
                || port.Protocol is not ("udp" or "tcp")
                || !TypedAddressesAreWellFormed(port.Address, port.Addresses))
            {
                throw new YogabaseApiException(502, "server_response_invalid", "Yogabase returned an invalid game-server address.");
            }
        }

        if (!TypedAddressesAreWellFormed(server.Address, server.Addresses))
        {
            throw new YogabaseApiException(502, "server_response_invalid", "Yogabase returned an invalid game-server address.");
        }

        return server;
    }

    /// <summary>
    /// The Agones-shaped pair is optional, but when present it is bounded, typed and consistent: at
    /// most 16 addresses, each with a type and an address, and a stated address among them.
    /// </summary>
    private static bool TypedAddressesAreWellFormed(string? address, IReadOnlyList<YogabaseServerAddress>? addresses)
    {
        if (address is not null && string.IsNullOrWhiteSpace(address))
        {
            return false;
        }
        if (addresses is null)
        {
            return true;
        }
        if (addresses.Count > MaximumTypedAddresses)
        {
            return false;
        }
        foreach (var typed in addresses)
        {
            if (typed is null || string.IsNullOrWhiteSpace(typed.Type) || string.IsNullOrWhiteSpace(typed.Address))
            {
                return false;
            }
        }
        return address is null || addresses.Any(typed => string.Equals(typed.Address, address, StringComparison.Ordinal));
    }

    private sealed class StartBody
    {
        public string Service { get; set; } = string.Empty;
        public IReadOnlyDictionary<string, string>? CustomData { get; set; }
    }
}
