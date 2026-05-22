using Microsoft.EntityFrameworkCore;
using MyDash.Application.Repositories;
using MyDash.Domain.Entities;
using MyDash.Infrastructure.Data;

namespace MyDash.Infrastructure.Repositories;

public class EfEnrollmentTokenRepository : IEnrollmentTokenRepository
{
    private readonly AppDbContext _db;

    public EfEnrollmentTokenRepository(AppDbContext db) => _db = db;

    public Task<EnrollmentToken?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.EnrollmentTokens.FindAsync(new object[] { id }, ct).AsTask();

    public Task<EnrollmentToken?> FindValidByHashAsync(string tokenHash, CancellationToken ct = default) =>
        _db.EnrollmentTokens.FirstOrDefaultAsync(t =>
            t.TokenHash == tokenHash && t.ConsumedAt == null && t.RevokedAt == null, ct);

    public Task<List<EnrollmentToken>> ListAllAsync(CancellationToken ct = default) =>
        _db.EnrollmentTokens.OrderByDescending(t => t.CreatedAt).ToListAsync(ct);

    public async Task AddAsync(EnrollmentToken token, CancellationToken ct = default)
    {
        _db.EnrollmentTokens.Add(token);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(EnrollmentToken token, CancellationToken ct = default)
    {
        _db.EnrollmentTokens.Update(token);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<string> IssueAsync(string serverName, string[] tags, TimeSpan ttl, CancellationToken ct = default)
    {
        var plaintext = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        var hash = BCrypt.Net.BCrypt.HashPassword(plaintext);
        var token = EnrollmentToken.Create(serverName, hash, tags.ToList(), ttl);
        await AddAsync(token, ct);
        return plaintext;
    }

    public async Task PurgeExpiredAsync(CancellationToken ct = default)
    {
        var expired = await _db.EnrollmentTokens
            .Where(t => t.ExpiresAt < DateTimeOffset.UtcNow && t.ConsumedAt == null)
            .ToListAsync(ct);
        _db.EnrollmentTokens.RemoveRange(expired);
        await _db.SaveChangesAsync(ct);
    }
}
