namespace ParceleMais.Establishments.Models;

public sealed record EstablishmentBankAccount(
    string BankNumber,
    string AgencyNumber,
    string AccountNumber,
    string AccountDigit,
    BankAccountType AccountType,
    string? AgencyDigit = null,
    string? HolderName = null,
    string? HolderDocument = null);
