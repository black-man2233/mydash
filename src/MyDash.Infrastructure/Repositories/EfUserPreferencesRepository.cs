using Microsoft.EntityFrameworkCore;
using MyDash.Application.Repositories;
using MyDash.Domain.Entities;
using MyDash.Infrastructure.Data;

namespace MyDash.Infrastructure.Repositories;

public class EfUserPreferencesRepository : IUserPreferencesRepository
{
    private readonly AppDbContext _db;

    public EfUserPreferencesRepository(AppDbContext db) => _db = db;

    public async Task<UserPreferences> GetAsync(CancellationToken ct = default)
    {
        var prefs = await _db.UserPreferences.FirstOrDefaultAsync(ct);
        return prefs ?? new UserPreferences();
    }

    public async Task UpdateAsync(UserPreferences prefs, CancellationToken ct = default)
    {
        _db.UserPreferences.Update(prefs);
        await _db.SaveChangesAsync(ct);
    }
}
