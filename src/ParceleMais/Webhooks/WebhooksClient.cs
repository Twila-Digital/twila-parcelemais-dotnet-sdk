using System.Net.Http.Json;
using ParceleMais.Errors;
using ParceleMais.Internal.Generated.WebHook;
using ParceleMais.Internal.Mapping;
using ParceleMais.Serialization;
using ParceleMais.Webhooks.Models;

namespace ParceleMais.Webhooks;

internal sealed class WebhooksClient(HttpClient httpClient) : IWebhooksClient
{
    public async Task<CreateWebhookResult> CreateAsync(CreateWebhookRequest request, CancellationToken cancellationToken = default)
    {
        var wireRequest = WebHookMapper.ToWire(request);

        using var response = await httpClient
            .PostAsJsonAsync("v1/webhooks", wireRequest, ParceleMaisJsonOptions.Default, cancellationToken)
            .ConfigureAwait(false);

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        var wire = await response.Content
            .ReadFromJsonAsync<CreateWebHookResponseWire>(ParceleMaisJsonOptions.Default, cancellationToken)
            .ConfigureAwait(false);

        return new CreateWebhookResult(wire!.ChaveAssinatura);
    }

    public async Task<IReadOnlyList<Webhook>> ListAsync(CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync("v1/webhooks", cancellationToken).ConfigureAwait(false);

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        var wire = await response.Content
            .ReadFromJsonAsync<List<WebHookWire>>(ParceleMaisJsonOptions.Default, cancellationToken)
            .ConfigureAwait(false);

        return wire!.Select(WebHookMapper.ToPublic).ToList();
    }

    public async Task UpdateAsync(WebHookType type, UpdateWebhookRequest request, CancellationToken cancellationToken = default)
    {
        var wireRequest = WebHookMapper.ToWire(request);

        using var response = await httpClient
            .PutAsJsonAsync($"v1/webhooks/{(int)type}", wireRequest, ParceleMaisJsonOptions.Default, cancellationToken)
            .ConfigureAwait(false);

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(WebHookType type, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync($"v1/webhooks/{(int)type}", cancellationToken).ConfigureAwait(false);

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
            throw await ParceleMaisExceptionFactory.FromResponseAsync(response, cancellationToken).ConfigureAwait(false);
    }
}
