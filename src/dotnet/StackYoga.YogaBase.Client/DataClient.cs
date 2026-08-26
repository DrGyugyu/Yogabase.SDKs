using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace StackYoga.YogaBase;

internal sealed class YogaBaseDataClient : IYogaBaseData
{
    private readonly YogaBaseClientConfiguration configuration;
    private readonly YogaBaseHttpTransport transport;

    internal YogaBaseDataClient(YogaBaseClientConfiguration configuration, YogaBaseHttpTransport transport)
    {
        this.configuration = configuration;
        this.transport = transport;
    }

    public Task<YogaBaseDataResult> SelectAsync(
        string resource,
        YogaBaseSelectRequest request,
        CancellationToken cancellationToken = default) => SendAsync(resource, "select", request, cancellationToken);

    public Task<YogaBaseDataResult> InsertAsync(
        string resource,
        YogaBaseMutationRequest request,
        CancellationToken cancellationToken = default) => SendAsync(resource, "insert", request, cancellationToken);

    public Task<YogaBaseDataResult> UpdateAsync(
        string resource,
        YogaBaseMutationRequest request,
        CancellationToken cancellationToken = default) => SendAsync(resource, "update", request, cancellationToken);

    public Task<YogaBaseDataResult> DeleteAsync(
        string resource,
        YogaBaseMutationRequest request,
        CancellationToken cancellationToken = default) => SendAsync(resource, "delete", request, cancellationToken);

    private async Task<YogaBaseDataResult> SendAsync(
        string resource,
        string operation,
        object request,
        CancellationToken cancellationToken)
    {
        YogaBaseClient.EnsureCapability(configuration, "data-rest");
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }
        var path = $"v1/data/{YogaBaseHttpTransport.EscapePath(resource, nameof(resource))}/{operation}";
        var response = await transport.SendAsync(
            HttpMethod.Post,
            path,
            request,
            authenticated: true,
            cancellationToken).ConfigureAwait(false);
        var wire = await YogaBaseHttpTransport.ReadJsonAsync<DataResponse>(response, cancellationToken).ConfigureAwait(false);
        return new YogaBaseDataResult
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
