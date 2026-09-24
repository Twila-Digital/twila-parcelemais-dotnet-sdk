namespace ParceleMais.Webhooks.Models;

public sealed record WebhookAudit(
    Guid Id,
    WebHookType Type,
    string Request,
    string Response,
    int StatusCode,
    DateTimeOffset CreatedAt);
