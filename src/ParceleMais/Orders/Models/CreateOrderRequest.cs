namespace ParceleMais.Orders.Models;

public sealed record CreateOrderRequest(
    string Cpf,
    string PhoneNumber,
    string EstablishmentDocument,
    decimal RequestedAmount,
    string Name,
    string Email,
    DateTimeOffset DateOfBirth,
    Address Address);
