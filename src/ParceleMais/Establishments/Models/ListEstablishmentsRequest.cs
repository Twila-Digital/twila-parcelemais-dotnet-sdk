namespace ParceleMais.Establishments.Models;

/// <param name="TradeName">Filtra pelo nome fantasia. Busca parcial, sem diferenciar maiúsculas de minúsculas.</param>
/// <param name="IsActive">Quando <c>null</c>, retorna lojas ativas e inativas.</param>
public sealed record ListEstablishmentsRequest(
    string? TradeName = null,
    bool? IsActive = null);
