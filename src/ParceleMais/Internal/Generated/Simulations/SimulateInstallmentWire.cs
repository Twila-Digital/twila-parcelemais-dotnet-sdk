using System.Text.Json.Serialization;

namespace ParceleMais.Internal.Generated.Simulations;

internal sealed record SimulateInstallmentWire(
    [property: JsonPropertyName("valorTotalDebito")] decimal ValorTotalDebito,
    [property: JsonPropertyName("prazo")] int Prazo,
    [property: JsonPropertyName("valorParcela")] decimal ValorParcela);
