using ParceleMais.Customers.Models;

namespace ParceleMais.Customers;

public interface ICustomersClient
{
    Task<Customer> GetAsync(Guid customerId, CancellationToken cancellationToken = default);

    Task<PagedResult<Customer>> ListAsync(ListCustomersRequest? request = null, CancellationToken cancellationToken = default);
}
