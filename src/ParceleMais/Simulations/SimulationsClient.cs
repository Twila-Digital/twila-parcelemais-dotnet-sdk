using System.Net.Http.Json;
using ParceleMais.Errors;
using ParceleMais.Http;
using ParceleMais.Internal.Generated.Simulations;
using ParceleMais.Internal.Mapping;
using ParceleMais.Serialization;
using ParceleMais.Simulations.Models;

namespace ParceleMais.Simulations;

internal sealed class SimulationsClient(HttpClient httpClient) : ISimulationsClient
{
    public async Task<IReadOnlyList<InstallmentSimulation>> SimulateInstallmentsAsync(SimulateInstallmentsRequest request, CancellationToken cancellationToken = default)
    {
        var path = new QueryStringBuilder()
            .Add("valorSolicitado", request.RequestedAmount)
            .Add("tipoValorCalculo", (int)request.CalculationValueType)
            .Build("v1/order/simulate-installments");

        using var response = await httpClient.GetAsync(path, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
            throw await ParceleMaisExceptionFactory.FromResponseAsync(response, cancellationToken).ConfigureAwait(false);

        var wire = await response.Content
            .ReadFromJsonAsync<List<SimulateInstallmentWire>>(ParceleMaisJsonOptions.Default, cancellationToken)
            .ConfigureAwait(false);

        return wire!.Select(SimulationMapper.ToPublic).ToList();
    }

    public async Task<ValuesSimulation> SimulateValuesAsync(SimulateValuesRequest request, CancellationToken cancellationToken = default)
    {
        var path = new QueryStringBuilder()
            .Add("valor", request.Amount)
            .Add("prazo", request.Term)
            .Add("modeloJuros", 1)
            .Add("tipoValorCalculo", (int)request.CalculationValueType)
            .Build("v1/order/simulate-values");

        using var response = await httpClient.GetAsync(path, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
            throw await ParceleMaisExceptionFactory.FromResponseAsync(response, cancellationToken).ConfigureAwait(false);

        var wire = await response.Content
            .ReadFromJsonAsync<SimulationValuesWire>(ParceleMaisJsonOptions.Default, cancellationToken)
            .ConfigureAwait(false);

        return SimulationMapper.ToPublic(wire!);
    }
}
