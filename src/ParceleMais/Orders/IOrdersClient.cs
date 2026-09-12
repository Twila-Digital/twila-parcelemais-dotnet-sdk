using ParceleMais.Orders.Models;

namespace ParceleMais.Orders;

public interface IOrdersClient
{
    Task<Guid> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);

    Task<Order> GetAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<PagedResult<Order>> ListAsync(ListOrdersRequest? request = null, CancellationToken cancellationToken = default);

    Task<CheckoutLink> StartCdcSaleAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task ImportInvoiceAsync(Guid orderId, InvoiceFile file, CancellationToken cancellationToken = default);
}
