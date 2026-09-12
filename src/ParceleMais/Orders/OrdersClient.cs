using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using ParceleMais.Errors;
using ParceleMais.Http;
using ParceleMais.Internal.Generated;
using ParceleMais.Internal.Generated.Order;
using ParceleMais.Internal.Mapping;
using ParceleMais.Orders.Models;
using ParceleMais.Serialization;

namespace ParceleMais.Orders;

internal sealed class OrdersClient(HttpClient httpClient) : IOrdersClient
{
    public async Task<Guid> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        var wireRequest = OrderMapper.ToWire(request);

        using var response = await httpClient
            .PostAsJsonAsync("v1/order", wireRequest, ParceleMaisJsonOptions.Default, cancellationToken)
            .ConfigureAwait(false);

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        var identifier = await response.Content
            .ReadFromJsonAsync<IdentifierResponseWire>(ParceleMaisJsonOptions.Default, cancellationToken)
            .ConfigureAwait(false);

        return identifier!.PedidoId;
    }

    public async Task<Order> GetAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"v1/order/{orderId}", cancellationToken).ConfigureAwait(false);

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        var wire = await response.Content
            .ReadFromJsonAsync<OrderWire>(ParceleMaisJsonOptions.Default, cancellationToken)
            .ConfigureAwait(false);

        return OrderMapper.ToPublic(wire!);
    }

    public async Task<PagedResult<Order>> ListAsync(ListOrdersRequest? request = null, CancellationToken cancellationToken = default)
    {
        request ??= new ListOrdersRequest();

        var path = new QueryStringBuilder()
            .Add("status", request.Status is null ? null : (int)request.Status)
            .Add("documentoCliente", request.CustomerDocument)
            .Add("dataInicio", request.StartDate)
            .Add("dataFim", request.EndDate)
            .Add("numero", request.Number)
            .Add("documentoLoja", request.EstablishmentDocument)
            .Add("descricao", request.Description)
            .Add("pagina", request.Page)
            .Add("tamanhoPagina", request.PageSize)
            .Build("v1/order/paged");

        using var response = await httpClient.GetAsync(path, cancellationToken).ConfigureAwait(false);

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        var wire = await response.Content
            .ReadFromJsonAsync<PagedResultWire<OrderWire>>(ParceleMaisJsonOptions.Default, cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<Order>(
            wire!.Items.Select(OrderMapper.ToPublic).ToList(),
            wire.Pagina.TemProximo,
            wire.Pagina.TemAnterior,
            wire.Pagina.Numero,
            wire.Pagina.Tamanho,
            wire.Pagina.Total);
    }

    public async IAsyncEnumerable<Order> ListAllAsync(ListOrdersRequest? request = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        request ??= new ListOrdersRequest();
        var page = request.Page;

        while (true)
        {
            var result = await ListAsync(request with { Page = page }, cancellationToken).ConfigureAwait(false);

            foreach (var item in result.Items)
                yield return item;

            if (!result.HasNext)
                yield break;

            page++;
        }
    }

    public async Task<CheckoutLink> StartCdcSaleAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var wireRequest = new StartCdcSaleRequestWire(orderId);

        using var response = await httpClient
            .PostAsJsonAsync("v1/order/start-cdc-sale", wireRequest, ParceleMaisJsonOptions.Default, cancellationToken)
            .ConfigureAwait(false);

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        var wire = await response.Content
            .ReadFromJsonAsync<LinkPaymentResponseWire>(ParceleMaisJsonOptions.Default, cancellationToken)
            .ConfigureAwait(false);

        return new CheckoutLink(wire!.LinkPagamento);
    }

    public async Task ImportInvoiceAsync(Guid orderId, InvoiceFile file, CancellationToken cancellationToken = default)
    {
        var wireRequest = new ImportOrderInvoiceRequestWire(orderId, file.Base64Content, file.FileName);

        using var response = await httpClient
            .PostAsJsonAsync("v1/order/invoice", wireRequest, ParceleMaisJsonOptions.Default, cancellationToken)
            .ConfigureAwait(false);

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
            throw await ParceleMaisExceptionFactory.FromResponseAsync(response, cancellationToken).ConfigureAwait(false);
    }
}
