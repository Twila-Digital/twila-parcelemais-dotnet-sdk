namespace ParceleMais.Establishments.Models;

public sealed record UpdateEstablishmentRequest(
    string TradeName,
    DisbursementModel? DisbursementModel = null,
    EstablishmentAddress? Address = null);
