using System.Net.Http.Json;
using ParceleMais.Errors;
using ParceleMais.Http;
using ParceleMais.Internal.Generated;
using ParceleMais.Internal.Generated.Customer;
using ParceleMais.Internal.Mapping;
using ParceleMais.Customers.Models;
using ParceleMais.Serialization;

namespace ParceleMais.Customers;

internal sealed class CustomersClient(HttpClient httpClient) : ICustomersClient
{
    public async Task<Customer> GetAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"v1/customer/{customerId}", cancellationToken).ConfigureAwait(false);

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        var wire = await response.Content
            .ReadFromJsonAsync<CustomerWire>(ParceleMaisJsonOptions.Default, cancellationToken)
            .ConfigureAwait(false);

        return CustomerMapper.ToPublic(wire!);
    }

    public async Task<PagedResult<Customer>> ListAsync(ListCustomersRequest? request = null, CancellationToken cancellationToken = default)
    {
        request ??= new ListCustomersRequest();

        var path = new QueryStringBuilder()
            .Add("nome", request.Name)
            .Add("documento", request.Document)
            .Add("pagina", request.Page)
            .Add("tamanhoPagina", request.PageSize)
            .Build("v1/customer/paged");

        using var response = await httpClient.GetAsync(path, cancellationToken).ConfigureAwait(false);

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        var wire = await response.Content
            .ReadFromJsonAsync<PagedResultWire<CustomerWire>>(ParceleMaisJsonOptions.Default, cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<Customer>(
            wire!.Items.Select(CustomerMapper.ToPublic).ToList(),
            wire.Pagina.TemProximo,
            wire.Pagina.TemAnterior,
            wire.Pagina.Numero,
            wire.Pagina.Tamanho,
            wire.Pagina.Total);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
            throw await ParceleMaisExceptionFactory.FromResponseAsync(response, cancellationToken).ConfigureAwait(false);
    }
}
