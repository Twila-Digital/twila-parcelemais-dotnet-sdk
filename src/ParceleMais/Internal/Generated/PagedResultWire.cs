using System.Text.Json.Serialization;

namespace ParceleMais.Internal.Generated;

internal sealed record PagedResultWire<T>(
    [property: JsonPropertyName("itens")] IReadOnlyList<T> Items,
    [property: JsonPropertyName("pagina")] PaginaWire Pagina);
