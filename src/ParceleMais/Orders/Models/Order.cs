namespace ParceleMais.Orders.Models;

public sealed record Order(
    Guid Id,
    long Number,
    OrderStatus Status,
    string StatusDescription,
    string CustomerDocument,
    string EstablishmentLegalName,
    string EstablishmentDocument,
    DateTimeOffset CreatedAt,
    decimal? Total = null,
    string? CustomerName = null,
    int? Term = null,
    string? Description = null,
    decimal? ApprovedAmount = null,
    bool? Disbursed = null,
    DateTimeOffset? DisbursedAt = null,
    decimal? RequestedAmount = null);
