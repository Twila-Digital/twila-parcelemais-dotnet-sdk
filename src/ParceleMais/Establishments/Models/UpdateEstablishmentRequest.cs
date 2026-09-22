namespace ParceleMais.Establishments.Models;

/// <param name="DisbursementModel">Quando <c>null</c>, mantém o modelo atual.</param>
/// <param name="Address">Quando <c>null</c>, mantém o endereço atual.</param>
public sealed record UpdateEstablishmentRequest(
    string TradeName,
    DisbursementModel? DisbursementModel = null,
    EstablishmentAddress? Address = null);
