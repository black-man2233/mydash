using MyDash.Application.Repositories;
using MyDash.Application.Services;
using MyDash.Domain.Entities;

namespace MyDash.Infrastructure.Services;

public class AuditWriter : IAuditWriter
{
    private readonly IAuditRepository _repo;

    public AuditWriter(IAuditRepository repo) => _repo = repo;

    public Task WriteAsync(AuditEntry entry, CancellationToken ct = default) =>
        _repo.AddAsync(entry, ct);
}
