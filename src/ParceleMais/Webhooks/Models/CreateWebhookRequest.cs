namespace ParceleMais.Webhooks.Models;

public sealed record CreateWebhookRequest(WebHookType Type, string Url, WebHookAuthenticationType AuthenticationType, string? Credential = null);
