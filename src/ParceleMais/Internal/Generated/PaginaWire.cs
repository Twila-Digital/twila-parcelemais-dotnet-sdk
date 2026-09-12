using System.Text.Json.Serialization;

namespace ParceleMais.Internal.Generated;

internal sealed record PaginaWire(
    [property: JsonPropertyName("tem_proximo")] bool TemProximo,
    [property: JsonPropertyName("tem_anterior")] bool TemAnterior,
    [property: JsonPropertyName("numero")] int Numero,
    [property: JsonPropertyName("tamanho")] int Tamanho,
    [property: JsonPropertyName("total")] int Total);
