namespace ParceleMais.Establishments.Models;

/// <param name="HolderName">Obrigatório quando o modelo de desembolso é <see cref="DisbursementModel.External"/>.</param>
/// <param name="HolderDocument">Obrigatório quando o modelo de desembolso é <see cref="DisbursementModel.External"/>.</param>
public sealed record EstablishmentBankAccount(
    string BankNumber,
    string AgencyNumber,
    string AccountNumber,
    string AccountDigit,
    BankAccountType AccountType,
    string? AgencyDigit = null,
    string? HolderName = null,
    string? HolderDocument = null);
