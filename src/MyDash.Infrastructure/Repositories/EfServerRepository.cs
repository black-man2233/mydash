using Microsoft.EntityFrameworkCore;
using MyDash.Application.Repositories;
using MyDash.Domain.Entities;
using MyDash.Infrastructure.Data;

namespace MyDash.Infrastructure.Repositories;

public class EfServerRepository : IServerRepository
{
    private readonly AppDbContext _db;

    public EfServerRepository(AppDbContext db) => _db = db;

    public Task<Server?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Servers.Include(s => s.Services).FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<List<Server>> ListAllAsync(CancellationToken ct = default) =>
        _db.Servers.Include(s => s.Services).ToListAsync(ct);

    public async Task AddAsync(Server server, CancellationToken ct = default)
    {
        _db.Servers.Add(server);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Server server, CancellationToken ct = default)
    {
        _db.Servers.Update(server);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var server = await _db.Servers.FindAsync(new object[] { id }, ct);
        if (server is not null)
        {
            _db.Servers.Remove(server);
            await _db.SaveChangesAsync(ct);
        }
    }
}
