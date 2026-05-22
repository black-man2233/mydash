using MyDash.Domain.Entities;

namespace MyDash.Application.Services;

public interface IAuditWriter
{
    Task WriteAsync(AuditEntry entry, CancellationToken ct = default);
}
