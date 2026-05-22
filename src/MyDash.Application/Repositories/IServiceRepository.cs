using MyDash.Domain.Entities;

namespace MyDash.Application.Repositories;

public interface IServiceRepository
{
    Task<Domain.Entities.Service?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Domain.Entities.Service>> ListByServerAsync(Guid serverId, CancellationToken ct = default);
    Task AddAsync(Domain.Entities.Service service, CancellationToken ct = default);
    Task UpdateAsync(Domain.Entities.Service service, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
