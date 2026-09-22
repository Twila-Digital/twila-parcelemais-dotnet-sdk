namespace ParceleMais.Establishments.Models;

/// <param name="Document">CNPJ da loja, somente números.</param>
public sealed record CreateEstablishmentRequest(
    string Document,
    string LegalName,
    string TradeName,
    DisbursementModel DisbursementModel,
    EstablishmentOwner Owner,
    EstablishmentBankAccount BankAccount,
    EstablishmentAddress? Address = null);
