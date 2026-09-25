namespace ParceleMais.Webhooks.Models;

public sealed record ListWebhookAuditRequest(
    DateTimeOffset? StartDate = null,
    DateTimeOffset? EndDate = null,
    Guid? OrderId = null,
    long? OrderNumber = null,
    int? StatusCode = null,
    int Page = 1,
    int PageSize = 10);
