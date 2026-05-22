using Microsoft.EntityFrameworkCore;
using MyDash.Application.Repositories;
using MyDash.Domain.Entities;
using MyDash.Infrastructure.Data;

namespace MyDash.Infrastructure.Repositories;

public class EfPinChallengeRepository : IPinChallengeRepository
{
    private readonly AppDbContext _db;

    public EfPinChallengeRepository(AppDbContext db) => _db = db;

    public Task<PinChallenge?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.PinChallenges.FindAsync(new object[] { id }, ct).AsTask();

    public async Task AddAsync(PinChallenge challenge, CancellationToken ct = default)
    {
        _db.PinChallenges.Add(challenge);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(PinChallenge challenge, CancellationToken ct = default)
    {
        _db.PinChallenges.Update(challenge);
        await _db.SaveChangesAsync(ct);
    }

    public Task<int> CountRecentByIpAsync(string ip, TimeSpan window, CancellationToken ct = default)
    {
        var since = DateTimeOffset.UtcNow - window;
        return _db.PinChallenges.CountAsync(c => c.IssuedAt > since, ct);
    }

    public async Task PurgeExpiredAsync(CancellationToken ct = default)
    {
        var expired = await _db.PinChallenges
            .Where(c => c.ExpiresAt < DateTimeOffset.UtcNow)
            .ToListAsync(ct);
        _db.PinChallenges.RemoveRange(expired);
        await _db.SaveChangesAsync(ct);
    }
}
