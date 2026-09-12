namespace ParceleMais.Customers.Models;

public sealed record ListCustomersRequest(
    string? Name = null,
    string? Document = null,
    int Page = 1,
    int PageSize = 10);
