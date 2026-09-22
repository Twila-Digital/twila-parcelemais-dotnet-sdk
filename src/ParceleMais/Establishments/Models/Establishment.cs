namespace ParceleMais.Establishments.Models;

public sealed record Establishment(
    Guid EstablishmentId,
    string Document,
    string LegalName,
    string TradeName,
    bool IsActive,
    EstablishmentOwner Owner,
    DisbursementModel? DisbursementModel = null,
    EstablishmentBankAccount? BankAccount = null,
    EstablishmentAddress? Address = null);
