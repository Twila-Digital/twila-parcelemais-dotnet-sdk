using ParceleMais.Errors;
using ParceleMais.Orders.Models;
using ParceleMais.Webhooks;

namespace ParceleMais.UnitTests.Webhooks;

public class ParceleMaisWebhookEventTests
{
    private const string SamplePayload = """{"id_pedido":"3c90c3cc-0d44-4b50-8888-8dd25736052a","enum_status":2,"status":"Approved"}""";

    [Fact]
    public void Parse_SemAssinatura_DesserializaOPayloadDePedido()
    {
        var result = ParceleMaisWebhookEvent.Parse(SamplePayload);

        Assert.Equal(Guid.Parse("3c90c3cc-0d44-4b50-8888-8dd25736052a"), result.OrderId);
        Assert.Equal(OrderStatus.Approved, result.Status);
        Assert.Equal(2, result.StatusRaw);
        Assert.Equal("Approved", result.StatusName);
    }

    [Fact]
    public void Parse_ComStatusDesconhecido_MapeiaParaUnknown_SemLancar()
    {
        const string payload = """{"id_pedido":"3c90c3cc-0d44-4b50-8888-8dd25736052a","enum_status":99,"status":"FuturoStatus"}""";

        var result = ParceleMaisWebhookEvent.Parse(payload);

        Assert.Equal(OrderStatus.Unknown, result.Status);
        Assert.Equal(99, result.StatusRaw);
    }

    [Fact]
    public void ComputeSignature_ReproduzExatamenteOVetorDeReferenciaCalculadoIndependentemente()
    {
        // Vetor gerado independentemente via Python (hmac + hashlib), não pelo próprio SDK:
        // hmac.new(b"minha-chave-secreta-de-teste", f"{timestamp}.{body}".encode(), hashlib.sha256).hexdigest()
        const string secret = "minha-chave-secreta-de-teste";
        const long timestamp = 1700000000;
        const string expectedSignature = "489bdf9c68245e520f9795f904471cf892ef69daea620f2b951a1d13c341eb18";

        var actualSignature = ParceleMaisWebhookEvent.ComputeSignature(secret, timestamp, SamplePayload);

        Assert.Equal(expectedSignature, actualSignature);
    }

    [Fact]
    public void Parse_ComAssinaturaValidaDentroDaJanela_Funciona()
    {
        const string secret = "minha-chave-secreta-de-teste";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signature = ParceleMaisWebhookEvent.ComputeSignature(secret, timestamp, SamplePayload);
        var header = $"t={timestamp},v1={signature}";

        var result = ParceleMaisWebhookEvent.Parse(SamplePayload, header, secret);

        Assert.Equal(OrderStatus.Approved, result.Status);
    }

    [Fact]
    public void Parse_ComAssinaturaAdulterada_Lanca()
    {
        const string secret = "minha-chave-secreta-de-teste";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signature = ParceleMaisWebhookEvent.ComputeSignature(secret, timestamp, SamplePayload);
        var lastChar = signature[signature.Length - 1];
        var tamperedSignature = signature.Substring(0, signature.Length - 1) + (lastChar == '0' ? '1' : '0');
        var header = $"t={timestamp},v1={tamperedSignature}";

        Assert.Throws<ParceleMaisWebhookSignatureException>(() => ParceleMaisWebhookEvent.Parse(SamplePayload, header, secret));
    }

    [Fact]
    public void Parse_ComSegredoErrado_Lanca()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signature = ParceleMaisWebhookEvent.ComputeSignature("segredo-correto", timestamp, SamplePayload);
        var header = $"t={timestamp},v1={signature}";

        Assert.Throws<ParceleMaisWebhookSignatureException>(() => ParceleMaisWebhookEvent.Parse(SamplePayload, header, "segredo-errado"));
    }

    [Fact]
    public void Parse_ComTimestampForaDaJanelaDeTolerancia_Lanca()
    {
        const string secret = "minha-chave-secreta-de-teste";
        var timestamp = DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeSeconds();
        var signature = ParceleMaisWebhookEvent.ComputeSignature(secret, timestamp, SamplePayload);
        var header = $"t={timestamp},v1={signature}";

        var exception = Assert.Throws<ParceleMaisWebhookSignatureException>(() => ParceleMaisWebhookEvent.Parse(SamplePayload, header, secret));
        Assert.Contains("janela", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("header-malformado")]
    [InlineData("t=123")]
    [InlineData("v1=abc")]
    public void Parse_ComCabecalhoMalformado_Lanca(string malformedHeader)
    {
        Assert.Throws<ParceleMaisWebhookSignatureException>(() => ParceleMaisWebhookEvent.Parse(SamplePayload, malformedHeader, "qualquer-segredo"));
    }
}
