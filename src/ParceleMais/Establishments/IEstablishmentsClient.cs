using ParceleMais.Establishments.Models;

namespace ParceleMais.Establishments;

public interface IEstablishmentsClient
{
    Task<CreateEstablishmentResult> CreateAsync(CreateEstablishmentRequest request, CancellationToken cancellationToken = default);

    Task<Establishment> GetAsync(Guid establishmentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Establishment>> ListAsync(ListEstablishmentsRequest? request = null, CancellationToken cancellationToken = default);

    Task UpdateAsync(Guid establishmentId, UpdateEstablishmentRequest request, CancellationToken cancellationToken = default);

    Task UpdateBankAccountAsync(Guid establishmentId, EstablishmentBankAccount bankAccount, CancellationToken cancellationToken = default);

    Task ActivateAsync(Guid establishmentId, CancellationToken cancellationToken = default);

    Task DeactivateAsync(Guid establishmentId, CancellationToken cancellationToken = default);
}
