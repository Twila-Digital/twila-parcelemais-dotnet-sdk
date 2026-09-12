namespace ParceleMais.Orders.Models;

public sealed record ListOrdersRequest(
    OrderStatus? Status = null,
    string? CustomerDocument = null,
    DateTimeOffset? StartDate = null,
    DateTimeOffset? EndDate = null,
    long? Number = null,
    string? EstablishmentDocument = null,
    string? Description = null,
    int Page = 1,
    int PageSize = 10);
