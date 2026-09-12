using System.Text.Json.Serialization;

namespace ParceleMais.Internal.Generated.Order;

internal sealed record ImportOrderInvoiceRequestWire(
    [property: JsonPropertyName("pedidoId")] Guid PedidoId,
    [property: JsonPropertyName("arquivoBase64")] string ArquivoBase64,
    [property: JsonPropertyName("nomeArquivo")] string NomeArquivo);
