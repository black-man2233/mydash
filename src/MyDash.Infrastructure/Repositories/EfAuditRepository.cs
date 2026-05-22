using Microsoft.EntityFrameworkCore;
using MyDash.Application.Repositories;
using MyDash.Domain.Entities;
using MyDash.Infrastructure.Data;

namespace MyDash.Infrastructure.Repositories;

public class EfAuditRepository : IAuditRepository
{
    private readonly AppDbContext _db;

    public EfAuditRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(AuditEntry entry, CancellationToken ct = default)
    {
        _db.AuditEntries.Add(entry);
        await _db.SaveChangesAsync(ct);
    }

    public Task<List<AuditEntry>> ListAsync(int limit = 100, DateTimeOffset? since = null, CancellationToken ct = default)
    {
        var q = _db.AuditEntries.AsQueryable();
        if (since.HasValue)
            q = q.Where(a => a.At > since.Value);
        return q.OrderByDescending(a => a.At).Take(limit).ToListAsync(ct);
    }
}
