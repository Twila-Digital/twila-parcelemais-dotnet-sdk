namespace ParceleMais.Customers.Models;

public sealed record Address(
    string? Street = null,
    string? Number = null,
    string? Neighborhood = null,
    string? City = null,
    string? State = null,
    string? PostalCode = null,
    string? Country = null,
    string? Complement = null);
