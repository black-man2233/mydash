using MyDash.Domain.Entities;

namespace MyDash.Application.Repositories;

public interface IServerRepository
{
    Task<Server?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Server>> ListAllAsync(CancellationToken ct = default);
    Task AddAsync(Server server, CancellationToken ct = default);
    Task UpdateAsync(Server server, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
