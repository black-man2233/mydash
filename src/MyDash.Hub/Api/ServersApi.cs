using MyDash.Application.Repositories;
using MyDash.Application.Services;
using MyDash.Domain.Entities;

namespace MyDash.Hub.Api;

public static class ServersApi
{
    public static WebApplication MapServersApi(this WebApplication app)
    {
        var g = app.MapGroup("/api").RequireAuthorization();

        g.MapGet("/servers", async (IServerRepository repo, CancellationToken ct) =>
        {
            var servers = await repo.ListAllAsync(ct);
            return Results.Ok(servers.Select(MapServer));
        });

        g.MapGet("/servers/{id:guid}", async (
            Guid id,
            IServerRepository repo,
            IServiceRepository services,
            CancellationToken ct) =>
        {
            var server = await repo.GetByIdAsync(id, ct);
            if (server is null) return Results.NotFound();
            var svcs = await services.ListByServerAsync(id, ct);
            return Results.Ok(new { server = MapServer(server), services = svcs.Select(MapService) });
        });

        g.MapDelete("/servers/{id:guid}", async (
            Guid id,
            IServerRepository repo,
            IAuditWriter audit,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            await repo.DeleteAsync(id, ct);
            await audit.WriteAsync(AuditEntry.Create(AuditAction.AgentRevoked, id.ToString(),
                ctx.Connection.RemoteIpAddress?.ToString() ?? ""), ct);
            return Results.NoContent();
        });

        g.MapGet("/servers/{id:guid}/services", async (
            Guid id,
            IServiceRepository repo,
            CancellationToken ct) =>
        {
            var services = await repo.ListByServerAsync(id, ct);
            return Results.Ok(services.Select(MapService));
        });

        g.MapPost("/servers/{id:guid}/services", async (
            Guid id,
            AddServiceRequest req,
            IServiceRepository repo,
            IAuditWriter audit,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var svc = Service.Create(id, req.Name, req.Port,
                req.ServiceType?.ToLower() == "docker" ? ServiceType.Docker : ServiceType.Native);
            svc.Description = req.Description ?? "";
            await repo.AddAsync(svc, ct);
            await audit.WriteAsync(AuditEntry.Create(AuditAction.ServiceAdded, req.Name,
                ctx.Connection.RemoteIpAddress?.ToString() ?? ""), ct);
            return Results.Created($"/api/services/{svc.Id}", MapService(svc));
        });

        g.MapDelete("/services/{id:guid}", async (
            Guid id,
            IServiceRepository repo,
            IAuditWriter audit,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            await repo.DeleteAsync(id, ct);
            await audit.WriteAsync(AuditEntry.Create(AuditAction.ServiceRemoved, id.ToString(),
                ctx.Connection.RemoteIpAddress?.ToString() ?? ""), ct);
            return Results.NoContent();
        });

        return app;
    }

    internal static object MapServer(Server s) => new
    {
        id = s.Id,
        name = s.Name,
        fullName = s.FullName,
        status = s.Status.ToString(),
        cpu = s.CpuPercent,
        mem = s.MemPercent,
        disk = s.DiskPercent,
        uptimeSeconds = s.UptimeSeconds,
        os = s.OS,
        agentVersion = s.AgentVersion,
        fingerprint = s.AgentFingerprint,
        lastHeartbeat = s.LastHeartbeat,
        enrolledAt = s.EnrolledAt,
        tailscaleHost = s.TailscaleHost,
        color = s.Color,
        initial = s.InitialChar,
        tags = s.Tags,
    };

    internal static object MapService(Service s) => new
    {
        id = s.Id,
        serverId = s.ServerId,
        name = s.Name,
        port = s.Port,
        serviceType = s.Type.ToString().ToLower(),
        status = s.Status.ToString(),
        dockerImage = s.DockerImage ?? "",
        description = s.Description,
        iconColor = s.IconColor,
        iconGlyph = s.IconGlyph,
        tags = s.Tags,
        lastCheck = s.LastCheck,
    };
}

public record AddServiceRequest(string Name, int Port, string? ServiceType, string? Description);
