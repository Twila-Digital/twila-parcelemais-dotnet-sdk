using ParceleMais.Internal.Generated.Customer;
using ParceleMais.Customers.Models;

namespace ParceleMais.Internal.Mapping;

internal static class CustomerMapper
{
    public static Customer ToPublic(CustomerWire wire) => new(
        wire.Id,
        wire.Nome,
        wire.Documento,
        wire.DataDeNascimento,
        wire.Endereco is null ? null : ToPublic(wire.Endereco),
        wire.Email,
        wire.Celular);

    public static Address ToPublic(CustomerAddressWire wire) => new(
        wire.Rua,
        wire.Numero,
        wire.Bairro,
        wire.Cidade,
        wire.Estado,
        wire.Cep,
        wire.Pais,
        wire.Complemento);
}
