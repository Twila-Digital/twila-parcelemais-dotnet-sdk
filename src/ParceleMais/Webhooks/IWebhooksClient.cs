using ParceleMais.Webhooks.Models;

namespace ParceleMais.Webhooks;

public interface IWebhooksClient
{
    Task<CreateWebhookResult> CreateAsync(CreateWebhookRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Webhook>> ListAsync(CancellationToken cancellationToken = default);

    Task<PagedResult<WebhookAudit>> ListAuditAsync(ListWebhookAuditRequest? request = null, CancellationToken cancellationToken = default);

    Task UpdateAsync(WebHookType type, UpdateWebhookRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(WebHookType type, CancellationToken cancellationToken = default);
}
