using MyDash.Domain.Entities;

namespace MyDash.Application.Repositories;

public interface IUserPreferencesRepository
{
    Task<UserPreferences> GetAsync(CancellationToken ct = default);
    Task UpdateAsync(UserPreferences prefs, CancellationToken ct = default);
}
