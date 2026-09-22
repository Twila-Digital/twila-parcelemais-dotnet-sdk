using System.Text.Json.Serialization;

namespace ParceleMais.Internal.Generated.Establishment;

internal sealed record EstablishmentWire(
    [property: JsonPropertyName("estabelecimentoId")] Guid EstabelecimentoId,
    [property: JsonPropertyName("documento")] string Documento,
    [property: JsonPropertyName("razaoSocial")] string RazaoSocial,
    [property: JsonPropertyName("nomeFantasia")] string NomeFantasia,
    [property: JsonPropertyName("ativa")] bool Ativa,
    [property: JsonPropertyName("responsavel")] EstablishmentOwnerWire Responsavel,
    [property: JsonPropertyName("modeloDesembolso")] int? ModeloDesembolso = null,
    [property: JsonPropertyName("contaBancaria")] EstablishmentBankAccountWire? ContaBancaria = null,
    [property: JsonPropertyName("endereco")] EstablishmentAddressWire? Endereco = null);
