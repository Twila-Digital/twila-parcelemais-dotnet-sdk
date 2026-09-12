using ParceleMais.Internal.Generated.Order;
using ParceleMais.Orders.Models;
using ParceleMais.Serialization;

namespace ParceleMais.Internal.Mapping;

internal static class OrderMapper
{
    public static Order ToPublic(OrderWire wire) => new(
        wire.Id,
        wire.Numero,
        EnumMapping.FromWireValue<OrderStatus>(wire.Status.Value),
        wire.Status.Description,
        wire.DocumentoCliente,
        wire.RazaoSocialEstabelecimento,
        wire.DocumentoEstabelecimento,
        wire.CriadoEm,
        wire.Total,
        wire.NomeCliente,
        wire.Prazo,
        wire.Descricao,
        wire.ValorAprovado,
        wire.Desembolsado,
        wire.DesembolsadoEm,
        wire.ValorSolicitado);

    public static AddressWire ToWire(Address address) => new(
        address.Street,
        address.Number,
        address.Neighborhood,
        address.City,
        address.State,
        address.PostalCode,
        address.Complement);

    public static CreateOrderRequestWire ToWire(CreateOrderRequest request) => new(
        request.Cpf,
        request.PhoneNumber,
        request.EstablishmentDocument,
        request.RequestedAmount,
        request.Name,
        request.Email,
        request.DateOfBirth,
        ToWire(request.Address));
}
