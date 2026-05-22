using MyDash.Domain.Entities;

namespace MyDash.Application.Repositories;

public interface IPinChallengeRepository
{
    Task<PinChallenge?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(PinChallenge challenge, CancellationToken ct = default);
    Task UpdateAsync(PinChallenge challenge, CancellationToken ct = default);
    Task<int> CountRecentByIpAsync(string ip, TimeSpan window, CancellationToken ct = default);
    Task PurgeExpiredAsync(CancellationToken ct = default);
}
