namespace ParceleMais.Webhooks.Models;

public sealed record UpdateWebhookRequest(string Url, WebHookAuthenticationType AuthenticationType, string? Credential = null);
