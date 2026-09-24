namespace ParceleMais.Establishments.Models;

public sealed record EstablishmentAddress(
    string Street,
    string Number,
    string District,
    string City,
    string State,
    string ZipCode,
    string? Complement = null,
    string? Country = null);
