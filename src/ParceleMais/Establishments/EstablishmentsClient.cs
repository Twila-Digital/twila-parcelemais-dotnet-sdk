using System.Net.Http.Json;
using ParceleMais.Errors;
using ParceleMais.Http;
using ParceleMais.Internal.Generated.Establishment;
using ParceleMais.Internal.Mapping;
using ParceleMais.Serialization;
using ParceleMais.Establishments.Models;

namespace ParceleMais.Establishments;

internal sealed class EstablishmentsClient(HttpClient httpClient) : IEstablishmentsClient
{
    public async Task<CreateEstablishmentResult> CreateAsync(CreateEstablishmentRequest request, CancellationToken cancellationToken = default)
    {
        var wireRequest = EstablishmentMapper.ToWire(request);

        using var response = await httpClient
            .PostAsJsonAsync("v1/establishment", wireRequest, ParceleMaisJsonOptions.Default, cancellationToken)
            .ConfigureAwait(false);

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        var wire = await response.Content
            .ReadFromJsonAsync<CreateEstablishmentResponseWire>(ParceleMaisJsonOptions.Default, cancellationToken)
            .ConfigureAwait(false);

        return new CreateEstablishmentResult(wire!.EstabelecimentoId);
    }

    public async Task<Establishment> GetAsync(Guid establishmentId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient
            .GetAsync($"v1/establishment/{establishmentId}", cancellationToken)
            .ConfigureAwait(false);

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        var wire = await response.Content
            .ReadFromJsonAsync<EstablishmentWire>(ParceleMaisJsonOptions.Default, cancellationToken)
            .ConfigureAwait(false);

        return EstablishmentMapper.ToPublic(wire!);
    }

    public async Task<IReadOnlyList<Establishment>> ListAsync(ListEstablishmentsRequest? request = null, CancellationToken cancellationToken = default)
    {
        request ??= new ListEstablishmentsRequest();

        var path = new QueryStringBuilder()
            .Add("nomeFantasia", request.TradeName)
            .Add("ativa", request.IsActive?.ToString().ToLowerInvariant())
            .Build("v1/establishment/list");

        using var response = await httpClient.GetAsync(path, cancellationToken).ConfigureAwait(false);

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        var wire = await response.Content
            .ReadFromJsonAsync<List<EstablishmentWire>>(ParceleMaisJsonOptions.Default, cancellationToken)
            .ConfigureAwait(false);

        return wire!.Select(EstablishmentMapper.ToPublic).ToList();
    }

    public async Task UpdateAsync(Guid establishmentId, UpdateEstablishmentRequest request, CancellationToken cancellationToken = default)
    {
        var wireRequest = EstablishmentMapper.ToWire(request);

        using var response = await httpClient
            .PutAsJsonAsync($"v1/establishment/{establishmentId}", wireRequest, ParceleMaisJsonOptions.Default, cancellationToken)
            .ConfigureAwait(false);

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateBankAccountAsync(Guid establishmentId, EstablishmentBankAccount bankAccount, CancellationToken cancellationToken = default)
    {
        var wireRequest = EstablishmentMapper.ToBankAccountWire(bankAccount);

        using var response = await httpClient
            .PutAsJsonAsync($"v1/establishment/{establishmentId}/bank-account", wireRequest, ParceleMaisJsonOptions.Default, cancellationToken)
            .ConfigureAwait(false);

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
    }

    public Task ActivateAsync(Guid establishmentId, CancellationToken cancellationToken = default)
        => SetActiveAsync(establishmentId, true, cancellationToken);

    public Task DeactivateAsync(Guid establishmentId, CancellationToken cancellationToken = default)
        => SetActiveAsync(establishmentId, false, cancellationToken);

    private async Task SetActiveAsync(Guid establishmentId, bool isActive, CancellationToken cancellationToken)
    {
        using var response = await httpClient
            .PutAsJsonAsync($"v1/establishment/{establishmentId}/status", new UpdateEstablishmentStatusRequestWire(isActive), ParceleMaisJsonOptions.Default, cancellationToken)
            .ConfigureAwait(false);

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
            throw await ParceleMaisExceptionFactory.FromResponseAsync(response, cancellationToken).ConfigureAwait(false);
    }
}
