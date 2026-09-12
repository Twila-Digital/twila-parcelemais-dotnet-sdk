namespace ParceleMais.Customers.Models;

public sealed record Customer(
    Guid Id,
    string Name,
    string Document,
    DateTimeOffset DateOfBirth,
    Address? Address = null,
    string? Email = null,
    string? PhoneNumber = null);
