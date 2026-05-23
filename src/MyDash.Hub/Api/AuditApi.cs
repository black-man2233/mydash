using MyDash.Application.Repositories;

namespace MyDash.Hub.Api;

public static class AuditApi
{
    public static WebApplication MapAuditApi(this WebApplication app)
    {
        app.MapGet("/api/audit", async (
            IAuditRepository repo,
            int limit = 50,
            long sinceUnix = 0,
            CancellationToken ct = default) =>
        {
            DateTimeOffset? since = sinceUnix > 0 ? DateTimeOffset.FromUnixTimeSeconds(sinceUnix) : null;
            var entries = await repo.ListAsync(limit, since, ct);
            return Results.Ok(entries.Select(e => new
            {
                id = e.Id,
                atUnix = e.At.ToUnixTimeSeconds(),
                actor = e.Actor,
                action = e.Action.ToString(),
                target = e.Target,
                ip = e.Ip,
                outcome = e.Outcome,
            }));
        }).RequireAuthorization();

        return app;
    }
}
