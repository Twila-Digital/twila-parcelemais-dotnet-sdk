using System.Text.Json.Serialization;

namespace ParceleMais.Internal.Generated.Simulations;

internal sealed record EstablishmentSimulationValuesWire(
    [property: JsonPropertyName("valorVenda")] decimal ValorVenda,
    [property: JsonPropertyName("valorDesembolso")] decimal ValorDesembolso);

internal sealed record CustomerSimulationValuesWire(
    [property: JsonPropertyName("valorParcela")] decimal ValorParcela);

internal sealed record SimulationValuesWire(
    [property: JsonPropertyName("valoresEstabelecimento")] EstablishmentSimulationValuesWire ValoresEstabelecimento,
    [property: JsonPropertyName("valoresCliente")] CustomerSimulationValuesWire ValoresCliente);
