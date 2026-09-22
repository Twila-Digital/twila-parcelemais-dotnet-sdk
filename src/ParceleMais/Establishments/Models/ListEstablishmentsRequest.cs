namespace ParceleMais.Establishments.Models;

public sealed record ListEstablishmentsRequest(
    string? TradeName = null,
    bool? IsActive = null);
