using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Yogabase.Yogabase;

internal sealed class YogabaseDataClient : IYogabaseData
{
    private readonly YogabaseClientConfiguration configuration;
    private readonly YogabaseHttpTransport transport;

    internal YogabaseDataClient(YogabaseClientConfiguration configuration, YogabaseHttpTransport transport)
    {
        this.configuration = configuration;
        this.transport = transport;
    }

    public Task<YogabaseDataResult> SelectAsync(
        string resource,
        YogabaseSelectRequest request,
        CancellationToken cancellationToken = default) => SendAsync(resource, "select", request, cancellationToken);

    public Task<YogabaseDataResult> InsertAsync(
        string resource,
        YogabaseMutationRequest request,
        CancellationToken cancellationToken = default) => SendAsync(resource, "insert", request, cancellationToken);

    public Task<YogabaseDataResult> UpdateAsync(
        string resource,
        YogabaseMutationRequest request,
        CancellationToken cancellationToken = default) => SendAsync(resource, "update", request, cancellationToken);

    public Task<YogabaseDataResult> DeleteAsync(
        string resource,
        YogabaseMutationRequest request,
        CancellationToken cancellationToken = default) => SendAsync(resource, "delete", request, cancellationToken);

    private async Task<YogabaseDataResult> SendAsync(
        string resource,
        string operation,
        object request,
        CancellationToken cancellationToken)
    {
        YogabaseClient.EnsureCapability(configuration, "data-rest");
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }
        var path = $"v1/data/{YogabaseHttpTransport.EscapePath(resource, nameof(resource))}/{operation}";
        var response = await transport.SendAsync(
            HttpMethod.Post,
            path,
            request,
            authenticated: true,
            cancellationToken).ConfigureAwait(false);
        var wire = await YogabaseHttpTransport.ReadJsonAsync<DataResponse>(response, cancellationToken).ConfigureAwait(false);
        return new YogabaseDataResult
        {
            Rows = wire.Rows ?? Array.Empty<JsonElement>(),
            AffectedRows = wire.AffectedRows,
        };
    }

    private sealed class DataResponse
    {
        public JsonElement[]? Rows { get; set; }
        public int AffectedRows { get; set; }
    }
}
