using Microsoft.EntityFrameworkCore;
using MyDash.Application.Repositories;
using MyDash.Infrastructure.Data;

namespace MyDash.Infrastructure.Repositories;

public class EfServiceRepository : IServiceRepository
{
    private readonly AppDbContext _db;

    public EfServiceRepository(AppDbContext db) => _db = db;

    public Task<Domain.Entities.Service?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Services.FindAsync(new object[] { id }, ct).AsTask();

    public Task<List<Domain.Entities.Service>> ListByServerAsync(Guid serverId, CancellationToken ct = default) =>
        _db.Services.Where(s => s.ServerId == serverId).ToListAsync(ct);

    public async Task AddAsync(Domain.Entities.Service service, CancellationToken ct = default)
    {
        _db.Services.Add(service);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Domain.Entities.Service service, CancellationToken ct = default)
    {
        _db.Services.Update(service);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var svc = await _db.Services.FindAsync(new object[] { id }, ct);
        if (svc is not null)
        {
            _db.Services.Remove(svc);
            await _db.SaveChangesAsync(ct);
        }
    }
}
