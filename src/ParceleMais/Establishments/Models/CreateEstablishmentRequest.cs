namespace ParceleMais.Establishments.Models;

public sealed record CreateEstablishmentRequest(
    string Document,
    string LegalName,
    string TradeName,
    DisbursementModel DisbursementModel,
    EstablishmentOwner Owner,
    EstablishmentBankAccount BankAccount,
    EstablishmentAddress Address);
