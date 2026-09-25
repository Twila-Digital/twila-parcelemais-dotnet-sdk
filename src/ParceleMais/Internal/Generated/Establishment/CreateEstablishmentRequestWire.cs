using System.Text.Json.Serialization;

namespace ParceleMais.Internal.Generated.Establishment;

internal sealed record CreateEstablishmentRequestWire(
    [property: JsonPropertyName("documento")] string Documento,
    [property: JsonPropertyName("razaoSocial")] string RazaoSocial,
    [property: JsonPropertyName("nomeFantasia")] string NomeFantasia,
    [property: JsonPropertyName("modeloDesembolso")] int ModeloDesembolso,
    [property: JsonPropertyName("responsavel")] EstablishmentOwnerWire Responsavel,
    [property: JsonPropertyName("contaBancaria")] EstablishmentBankAccountWire ContaBancaria,
    [property: JsonPropertyName("endereco")] EstablishmentAddressWire Endereco);
