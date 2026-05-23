using MyDash.Application.Repositories;

namespace MyDash.Hub.Api;

public static class EnrollmentApi
{
    public static WebApplication MapEnrollmentApi(this WebApplication app)
    {
        var g = app.MapGroup("/api/enrollment").RequireAuthorization();

        g.MapGet("/tokens", async (IEnrollmentTokenRepository repo, CancellationToken ct) =>
        {
            var tokens = await repo.ListAllAsync(ct);
            return Results.Ok(tokens.Select(t => new
            {
                id = t.Id,
                name = t.ServerName,
                createdAtUnix = t.CreatedAt.ToUnixTimeSeconds(),
                expiresAtUnix = t.ExpiresAt.ToUnixTimeSeconds(),
                consumed = t.IsConsumed,
                expired = t.IsExpired,
                revoked = t.IsRevoked,
            }));
        });

        g.MapPost("/tokens", async (CreateTokenRequest req, IEnrollmentTokenRepository repo, CancellationToken ct) =>
        {
            var ttl = TimeSpan.FromMinutes(req.TtlMinutes > 0 ? req.TtlMinutes : 60);
            var plaintext = await repo.IssueAsync(req.Name, req.Tags ?? [], ttl, ct);
            return Results.Ok(new
            {
                tokenPlaintext = plaintext,
                expiresAtUnix = DateTimeOffset.UtcNow.Add(ttl).ToUnixTimeSeconds(),
                name = req.Name,
            });
        });

        g.MapDelete("/tokens/{id:guid}", async (Guid id, IEnrollmentTokenRepository repo, CancellationToken ct) =>
        {
            var token = await repo.GetByIdAsync(id, ct);
            if (token is null) return Results.NotFound();
            token.Revoke();
            await repo.UpdateAsync(token, ct);
            return Results.NoContent();
        });

        return app;
    }
}

public record CreateTokenRequest(string Name, int TtlMinutes = 60, string[]? Tags = null);
