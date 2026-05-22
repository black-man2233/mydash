using MyDash.Domain.Entities;

namespace MyDash.Application.Repositories;

public interface IAuditRepository
{
    Task AddAsync(AuditEntry entry, CancellationToken ct = default);
    Task<List<AuditEntry>> ListAsync(int limit = 100, DateTimeOffset? since = null, CancellationToken ct = default);
}
