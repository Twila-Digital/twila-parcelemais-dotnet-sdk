namespace ParceleMais.Orders.Models;

public sealed record Address(
    string Street,
    string Number,
    string Neighborhood,
    string City,
    string State,
    string PostalCode,
    string? Complement = null);
