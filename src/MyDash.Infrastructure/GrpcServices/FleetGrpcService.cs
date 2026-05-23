using System.Collections.Concurrent;
using Grpc.Core;
using MyDash.Application.Repositories;
using MyDash.Application.Services;
using MyDash.Contracts.Fleet.V1;
using MyDash.Domain.Entities;
using DomainAuditEntry = MyDash.Domain.Entities.AuditEntry;
using AuditAction = MyDash.Domain.Entities.AuditAction;

namespace MyDash.Infrastructure.GrpcServices;

public class FleetGrpcService : FleetService.FleetServiceBase
{
    private readonly IServerRepository _servers;
    private readonly IServiceRepository _services;
    private readonly IEnrollmentTokenRepository _tokens;
    private readonly IAuditRepository _audit;
    private readonly IAuditWriter _auditWriter;

    private static readonly ConcurrentDictionary<string, List<IServerStreamWriter<EnrollmentEvent>>> _enrollmentStreams = new();

    public FleetGrpcService(
        IServerRepository servers,
        IServiceRepository services,
        IEnrollmentTokenRepository tokens,
        IAuditRepository audit,
        IAuditWriter auditWriter)
    {
        _servers = servers;
        _services = services;
        _tokens = tokens;
        _audit = audit;
        _auditWriter = auditWriter;
    }

    public override async Task<ListServersResponse> ListServers(ListServersRequest request, ServerCallContext context)
    {
        var servers = await _servers.ListAllAsync(context.CancellationToken);
        var resp = new ListServersResponse();
        resp.Servers.AddRange(servers.Select(MapServer));
        return resp;
    }

    public override async Task<Contracts.Fleet.V1.Server> GetServer(GetServerRequest request, ServerCallContext context)
    {
        var server = await _servers.GetByIdAsync(Guid.Parse(request.Id), context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Server not found"));
        return MapServer(server);
    }

    public override async Task<ListServicesResponse> ListServices(ListServicesRequest request, ServerCallContext context)
    {
        var services = await _services.ListByServerAsync(Guid.Parse(request.ServerId), context.CancellationToken);
        if (!string.IsNullOrEmpty(request.NameFilter))
            services = services.Where(s => s.Name.Contains(request.NameFilter, StringComparison.OrdinalIgnoreCase)).ToList();
        if (!string.IsNullOrEmpty(request.TagFilter))
            services = services.Where(s => s.Tags.Contains(request.TagFilter)).ToList();

        var resp = new ListServicesResponse();
        resp.Services.AddRange(services.Select(MapService));
        return resp;
    }

    public override async Task<Contracts.Fleet.V1.Service> AddService(AddServiceRequest request, ServerCallContext context)
    {
        var svc = Domain.Entities.Service.Create(
            Guid.Parse(request.ServerId),
            request.Service.Name,
            request.Service.Port,
            request.Service.ServiceType == "docker" ? ServiceType.Docker : ServiceType.Native);

        svc.Description = request.Service.Description;
        svc.IconColor = request.Service.IconColor;
        svc.IconGlyph = request.Service.IconGlyph;
        svc.Tags = request.Service.Tags.ToList();

        await _services.AddAsync(svc, context.CancellationToken);
        await _auditWriter.WriteAsync(DomainAuditEntry.Create(AuditAction.ServiceAdded, svc.Name, ""), context.CancellationToken);
        return MapService(svc);
    }

    public override async Task<Contracts.Fleet.V1.Empty> RemoveService(RemoveServiceRequest request, ServerCallContext context)
    {
        await _services.DeleteAsync(Guid.Parse(request.Id), context.CancellationToken);
        await _auditWriter.WriteAsync(DomainAuditEntry.Create(AuditAction.ServiceRemoved, request.Id, ""), context.CancellationToken);
        return new Contracts.Fleet.V1.Empty();
    }

    public override async Task<CreateEnrollmentResponse> CreateEnrollment(CreateEnrollmentRequest request, ServerCallContext context)
    {
        var ttl = TimeSpan.FromMinutes(request.TtlMinutes > 0 ? request.TtlMinutes : 60);
        var plaintext = await _tokens.IssueAsync(request.Name, request.Tags.ToArray(), ttl, context.CancellationToken);
        return new CreateEnrollmentResponse
        {
            TokenPlaintext = plaintext,
            ExpiresAtUnix = DateTimeOffset.UtcNow.Add(ttl).ToUnixTimeSeconds(),
        };
    }

    public override async Task<ListEnrollmentsResponse> ListEnrollments(Contracts.Fleet.V1.Empty request, ServerCallContext context)
    {
        var tokens = await _tokens.ListAllAsync(context.CancellationToken);
        var resp = new ListEnrollmentsResponse();
        resp.Tokens.AddRange(tokens.Select(t => new EnrollmentTokenInfo
        {
            Id = t.Id.ToString(),
            Name = t.ServerName,
            CreatedAtUnix = t.CreatedAt.ToUnixTimeSeconds(),
            ExpiresAtUnix = t.ExpiresAt.ToUnixTimeSeconds(),
            Consumed = t.IsConsumed,
        }));
        return resp;
    }

    public override async Task<Contracts.Fleet.V1.Empty> RevokeEnrollment(RevokeEnrollmentRequest request, ServerCallContext context)
    {
        var token = await _tokens.GetByIdAsync(Guid.Parse(request.Id), context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Token not found"));
        token.Revoke();
        await _tokens.UpdateAsync(token, context.CancellationToken);
        return new Contracts.Fleet.V1.Empty();
    }

    public override async Task<Contracts.Fleet.V1.Empty> RevokeAgent(RevokeAgentRequest request, ServerCallContext context)
    {
        await _servers.DeleteAsync(Guid.Parse(request.ServerId), context.CancellationToken);
        await _auditWriter.WriteAsync(DomainAuditEntry.Create(AuditAction.AgentRevoked, request.ServerId, ""), context.CancellationToken);
        return new Contracts.Fleet.V1.Empty();
    }

    public override Task<Contracts.Fleet.V1.Empty> TriggerScan(TriggerScanRequest request, ServerCallContext context)
    {
        return Task.FromResult(new Contracts.Fleet.V1.Empty());
    }

    public override async Task StreamScan(StreamScanRequest request, IServerStreamWriter<ScanLogLine> responseStream, ServerCallContext context)
    {
        await responseStream.WriteAsync(new ScanLogLine
        {
            TimestampSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Level = "info",
            Message = $"Scan triggered for server {request.ServerId} on ports {request.FromPort}-{request.ToPort}",
        });
    }

    public override async Task<ListAuditResponse> ListAuditEntries(ListAuditRequest request, ServerCallContext context)
    {
        DateTimeOffset? since = request.SinceUnix > 0 ? DateTimeOffset.FromUnixTimeSeconds(request.SinceUnix) : null;
        var entries = await _audit.ListAsync(request.Limit > 0 ? request.Limit : 100, since, context.CancellationToken);
        var resp = new ListAuditResponse();
        resp.Entries.AddRange(entries.Select(e => new Contracts.Fleet.V1.AuditEntry
        {
            Id = e.Id.ToString(),
            AtUnix = e.At.ToUnixTimeSeconds(),
            Actor = e.Actor,
            Action = e.Action.ToString(),
            Target = e.Target,
            Ip = e.Ip,
            Outcome = e.Outcome,
        }));
        return resp;
    }

    public override async Task StreamEnrollment(StreamEnrollmentRequest request, IServerStreamWriter<EnrollmentEvent> responseStream, ServerCallContext context)
    {
        var key = request.TokenId;
        var writers = _enrollmentStreams.GetOrAdd(key, _ => new List<IServerStreamWriter<EnrollmentEvent>>());
        lock (writers) writers.Add(responseStream);

        try
        {
            await Task.Delay(Timeout.Infinite, context.CancellationToken);
        }
        catch (OperationCanceledException) { }
        finally
        {
            lock (writers) writers.Remove(responseStream);
        }
    }

    public static async Task NotifyEnrollment(string tokenId, string serverId, string serverName)
    {
        if (_enrollmentStreams.TryGetValue(tokenId, out var writers))
        {
            List<IServerStreamWriter<EnrollmentEvent>> snapshot;
            lock (writers) snapshot = writers.ToList();

            foreach (var w in snapshot)
            {
                try
                {
                    await w.WriteAsync(new EnrollmentEvent
                    {
                        EventType = "enrolled",
                        ServerId = serverId,
                        ServerName = serverName,
                    });
                }
                catch { }
            }
        }
    }

    private static Contracts.Fleet.V1.Server MapServer(Domain.Entities.Server s) => new()
    {
        Id = s.Id.ToString(),
        Name = s.Name,
        FullName = s.FullName,
        TailscaleHost = s.TailscaleHost,
        Color = s.Color,
        Initial = s.InitialChar,
        Os = s.OS,
        Status = s.Status.ToString(),
        EnrolledAtUnix = s.EnrolledAt.ToUnixTimeSeconds(),
        AgentVersion = s.AgentVersion,
        Fingerprint = s.AgentFingerprint,
        LastHeartbeatUnix = s.LastHeartbeat?.ToUnixTimeSeconds() ?? 0,
        Cpu = s.CpuPercent,
        Mem = s.MemPercent,
        Disk = s.DiskPercent,
        UptimeSeconds = s.UptimeSeconds,
    };

    private static Contracts.Fleet.V1.Service MapService(Domain.Entities.Service s) => new()
    {
        Id = s.Id.ToString(),
        ServerId = s.ServerId.ToString(),
        Name = s.Name,
        Port = s.Port,
        ServiceType = s.Type.ToString().ToLower(),
        DockerImage = s.DockerImage ?? "",
        Description = s.Description,
        IconColor = s.IconColor,
        IconGlyph = s.IconGlyph,
        Status = s.Status.ToString(),
        LastCheckUnix = s.LastCheck?.ToUnixTimeSeconds() ?? 0,
    };
}
