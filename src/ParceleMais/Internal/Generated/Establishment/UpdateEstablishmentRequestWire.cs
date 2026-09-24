using System.Text.Json.Serialization;

namespace ParceleMais.Internal.Generated.Establishment;

internal sealed record UpdateEstablishmentRequestWire(
    [property: JsonPropertyName("nomeFantasia")] string NomeFantasia,
    [property: JsonPropertyName("modeloDesembolso")] int? ModeloDesembolso = null,
    [property: JsonPropertyName("endereco")] EstablishmentAddressWire? Endereco = null);
