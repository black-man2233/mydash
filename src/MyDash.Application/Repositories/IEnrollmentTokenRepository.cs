using MyDash.Domain.Entities;

namespace MyDash.Application.Repositories;

public interface IEnrollmentTokenRepository
{
    Task<EnrollmentToken?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<EnrollmentToken?> FindValidByHashAsync(string tokenHash, CancellationToken ct = default);
    Task<List<EnrollmentToken>> ListAllAsync(CancellationToken ct = default);
    Task AddAsync(EnrollmentToken token, CancellationToken ct = default);
    Task UpdateAsync(EnrollmentToken token, CancellationToken ct = default);
    Task<string> IssueAsync(string serverName, string[] tags, TimeSpan ttl, CancellationToken ct = default);
    Task PurgeExpiredAsync(CancellationToken ct = default);
}
