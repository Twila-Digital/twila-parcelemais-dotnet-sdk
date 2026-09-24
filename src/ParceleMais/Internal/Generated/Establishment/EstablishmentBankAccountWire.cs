using System.Text.Json.Serialization;

namespace ParceleMais.Internal.Generated.Establishment;

internal sealed record EstablishmentBankAccountWire(
    [property: JsonPropertyName("banco")] string Banco,
    [property: JsonPropertyName("agencia")] string Agencia,
    [property: JsonPropertyName("digitoAgencia")] string DigitoAgencia,
    [property: JsonPropertyName("conta")] string Conta,
    [property: JsonPropertyName("digitoConta")] string DigitoConta,
    [property: JsonPropertyName("tipoConta")] int TipoConta,
    [property: JsonPropertyName("nomeTitular")] string? NomeTitular = null,
    [property: JsonPropertyName("documentoTitular")] string? DocumentoTitular = null);
