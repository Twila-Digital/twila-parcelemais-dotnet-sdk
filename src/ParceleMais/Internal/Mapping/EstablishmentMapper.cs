using ParceleMais.Internal.Generated.Establishment;
using ParceleMais.Establishments.Models;
using ParceleMais.Serialization;

namespace ParceleMais.Internal.Mapping;

internal static class EstablishmentMapper
{
    public static CreateEstablishmentRequestWire ToWire(CreateEstablishmentRequest request) => new(
        request.Document,
        request.LegalName,
        request.TradeName,
        (int)request.DisbursementModel,
        ToWire(request.Owner),
        ToWire(request.BankAccount),
        ToWire(request.Address));

    public static UpdateEstablishmentRequestWire ToWire(UpdateEstablishmentRequest request) => new(
        request.TradeName,
        request.DisbursementModel is null ? null : (int)request.DisbursementModel,
        request.Address is null ? null : ToWire(request.Address));

    public static EstablishmentBankAccountWire ToBankAccountWire(EstablishmentBankAccount bankAccount) => ToWire(bankAccount);

    public static Establishment ToPublic(EstablishmentWire wire) => new(
        wire.EstabelecimentoId,
        wire.Documento,
        wire.RazaoSocial,
        wire.NomeFantasia,
        wire.Ativa,
        new EstablishmentOwner(wire.Responsavel.Nome, wire.Responsavel.Email, wire.Responsavel.Celular),
        wire.ModeloDesembolso is null ? null : EnumMapping.FromWireValue<DisbursementModel>(wire.ModeloDesembolso.Value),
        wire.ContaBancaria is null
            ? null
            : new EstablishmentBankAccount(
                wire.ContaBancaria.Banco,
                wire.ContaBancaria.Agencia,
                wire.ContaBancaria.Conta,
                wire.ContaBancaria.DigitoConta,
                EnumMapping.FromWireValue<BankAccountType>(wire.ContaBancaria.TipoConta),
                wire.ContaBancaria.DigitoAgencia,
                wire.ContaBancaria.NomeTitular,
                wire.ContaBancaria.DocumentoTitular),
        wire.Endereco is null
            ? null
            : new EstablishmentAddress(
                wire.Endereco.Rua,
                wire.Endereco.Numero,
                wire.Endereco.Bairro,
                wire.Endereco.Cidade,
                wire.Endereco.Estado,
                wire.Endereco.Cep,
                wire.Endereco.Complemento,
                wire.Endereco.Pais));

    private static EstablishmentOwnerWire ToWire(EstablishmentOwner owner) => new(owner.Name, owner.Email, owner.Phone);

    private static EstablishmentBankAccountWire ToWire(EstablishmentBankAccount bankAccount) => new(
        bankAccount.BankNumber,
        bankAccount.AgencyNumber,
        bankAccount.AgencyDigit ?? string.Empty,
        bankAccount.AccountNumber,
        bankAccount.AccountDigit,
        (int)bankAccount.AccountType,
        bankAccount.HolderName,
        bankAccount.HolderDocument);

    private static EstablishmentAddressWire ToWire(EstablishmentAddress address) => new(
        address.Street,
        address.Number,
        address.District,
        address.City,
        address.State,
        address.ZipCode,
        address.Complement,
        address.Country);
}
