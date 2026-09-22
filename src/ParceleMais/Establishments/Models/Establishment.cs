namespace ParceleMais.Establishments.Models;

/// <param name="Document">CNPJ da loja.</param>
/// <param name="IsActive">Uma loja inativa não aceita novos pedidos nem edição.</param>
/// <param name="BankAccount"><c>null</c> quando a loja ainda não tem conta bancária cadastrada.</param>
/// <param name="Address"><c>null</c> quando a loja ainda não tem endereço cadastrado.</param>
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
