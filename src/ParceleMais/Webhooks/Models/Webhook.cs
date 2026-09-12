namespace ParceleMais.Webhooks.Models;

public sealed record Webhook(WebHookType Type, string Url, WebHookAuthenticationType AuthenticationType);
