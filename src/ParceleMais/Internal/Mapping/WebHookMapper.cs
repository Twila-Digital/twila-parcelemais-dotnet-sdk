using ParceleMais.Internal.Generated.WebHook;
using ParceleMais.Serialization;
using ParceleMais.Webhooks.Models;

namespace ParceleMais.Internal.Mapping;

internal static class WebHookMapper
{
    public static Webhook ToPublic(WebHookWire wire) => new(
        EnumMapping.FromWireValue<WebHookType>(wire.Tipo),
        wire.Url,
        EnumMapping.FromWireValue<WebHookAuthenticationType>(wire.TipoAutenticacao));

    public static WebhookAudit ToPublic(AuditWebHookWire wire) => new(
        wire.Id,
        EnumMapping.FromWireValue<WebHookType>(wire.Tipo),
        wire.Requisicao,
        wire.Resposta,
        wire.StatusCode,
        wire.DataCriacao);

    public static CreateWebHookRequestWire ToWire(CreateWebhookRequest request) => new(
        (int)request.Type,
        request.Url,
        (int)request.AuthenticationType,
        request.Credential);

    public static UpdateWebHookRequestWire ToWire(UpdateWebhookRequest request) => new(
        request.Url,
        (int)request.AuthenticationType,
        request.Credential);
}
