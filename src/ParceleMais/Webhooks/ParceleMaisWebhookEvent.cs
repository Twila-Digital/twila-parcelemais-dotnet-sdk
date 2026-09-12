using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ParceleMais.Errors;
using ParceleMais.Internal;
using ParceleMais.Internal.Generated.WebHook;
using ParceleMais.Serialization;
using ParceleMais.Webhooks.Models;

namespace ParceleMais.Webhooks;

public static class ParceleMaisWebhookEvent
{
    private static readonly TimeSpan ReplayTolerance = TimeSpan.FromMinutes(5);

    public static OrderWebhookEvent Parse(string rawJson)
    {
        var wire = JsonSerializer.Deserialize<OrderWebhookEventWire>(rawJson, ParceleMaisJsonOptions.Default)
            ?? throw new ParceleMaisWebhookSignatureException("O corpo do webhook está vazio ou não é um JSON válido.");

        return new OrderWebhookEvent(
            wire.IdPedido,
            EnumMapping.FromWireValue<Orders.Models.OrderStatus>(wire.EnumStatus),
            wire.EnumStatus,
            wire.Status);
    }

    public static OrderWebhookEvent Parse(string rawJson, string signatureHeader, string signingSecret)
    {
        VerifySignature(rawJson, signatureHeader, signingSecret);
        return Parse(rawJson);
    }

    private static void VerifySignature(string rawJson, string signatureHeader, string signingSecret)
    {
        var (timestamp, expectedSignature) = ParseSignatureHeader(signatureHeader);
        var computedSignature = ComputeSignature(signingSecret, timestamp, rawJson);

        if (!CryptoUtility.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computedSignature),
                Encoding.UTF8.GetBytes(expectedSignature)))
            throw new ParceleMaisWebhookSignatureException("A assinatura do webhook não confere.");

        var eventTime = DateTimeOffset.FromUnixTimeSeconds(timestamp);
        if ((DateTimeOffset.UtcNow - eventTime).Duration() > ReplayTolerance)
            throw new ParceleMaisWebhookSignatureException("O timestamp do webhook está fora da janela de tolerância — possível replay.");
    }

    internal static string ComputeSignature(string signingSecret, long timestamp, string payload)
    {
        var signedContent = $"{timestamp}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(signingSecret));
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedContent));
        return CryptoUtility.ToHexStringLower(computedHash);
    }

    private static (long Timestamp, string Signature) ParseSignatureHeader(string signatureHeader)
    {
        long? timestamp = null;
        string? signature = null;

        foreach (var part in signatureHeader.Split(','))
        {
            var pair = part.Split(new[] { '=' }, 2);
            if (pair.Length != 2)
                continue;

            switch (pair[0].Trim())
            {
                case "t":
                    if (long.TryParse(pair[1].Trim(), out var parsedTimestamp))
                        timestamp = parsedTimestamp;
                    break;
                case "v1":
                    signature = pair[1].Trim().ToLowerInvariant();
                    break;
            }
        }

        if (timestamp is null || signature is null)
            throw new ParceleMaisWebhookSignatureException($"Cabeçalho de assinatura malformado: '{signatureHeader}'.");

        return (timestamp.Value, signature);
    }
}
