using Grpc.Core;
using Microsoft.Extensions.Logging;
using MyDash.Application.Repositories;
using MyDash.Application.Services;
using MyDash.Contracts.Agent.V1;
using MyDash.Domain.Entities;
using AuditAction = MyDash.Domain.Entities.AuditAction;

namespace MyDash.Infrastructure.GrpcServices;

public class AgentGrpcService : AgentService.AgentServiceBase
{
    private readonly IServerRepository _servers;
    private readonly IServiceRepository _services;
    private readonly IEnrollmentTokenRepository _tokens;
    private readonly IAuditWriter _audit;
    private readonly ILogger<AgentGrpcService> _logger;

    public AgentGrpcService(
        IServerRepository servers,
        IServiceRepository services,
        IEnrollmentTokenRepository tokens,
        IAuditWriter audit,
        ILogger<AgentGrpcService> logger)
    {
        _servers = servers;
        _services = services;
        _tokens = tokens;
        _audit = audit;
        _logger = logger;
    }

    public override async Task<EnrollResponse> Enroll(EnrollRequest request, ServerCallContext context)
    {
        var allTokens = await _tokens.ListAllAsync(context.CancellationToken);
        EnrollmentToken? matchedToken = null;
        foreach (var t in allTokens.Where(t => t.IsValid))
        {
            if (BCrypt.Net.BCrypt.Verify(request.Token, t.TokenHash))
            {
                matchedToken = t;
                break;
            }
        }

        if (matchedToken is null)
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid or expired enrollment token"));

        var server = Server.NewEnrolled(
            matchedToken.ServerName,
            request.Hostname,
            request.Hostname,
            request.Os);

        server.AgentVersion = request.AgentVersion;
        server.AgentFingerprint = $"SHA256:{Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(request.PublicKey)))[..16]}";
        server.Tags = matchedToken.Tags.ToList();

        await _servers.AddAsync(server, context.CancellationToken);

        matchedToken.Consume();
        await _tokens.UpdateAsync(matchedToken, context.CancellationToken);

        await _audit.WriteAsync(AuditEntry.Create(AuditAction.AgentEnrolled, server.Name, context.Peer), context.CancellationToken);
        await FleetGrpcService.NotifyEnrollment(matchedToken.Id.ToString(), server.Id.ToString(), server.Name);

        _logger.LogInformation("Agent enrolled: {Name} ({Id})", server.Name, server.Id);

        return new EnrollResponse
        {
            ServerId = server.Id.ToString(),
            ClientCertificate = Google.Protobuf.ByteString.Empty,
            ClientKey = Google.Protobuf.ByteString.Empty,
            Fingerprint = server.AgentFingerprint,
            HeartbeatSeconds = 5,
        };
    }

    public override async Task Stream(IAsyncStreamReader<AgentMessage> requestStream, IServerStreamWriter<HubMessage> responseStream, ServerCallContext context)
    {
        Server? server = null;

        await foreach (var msg in requestStream.ReadAllAsync(context.CancellationToken))
        {
            if (msg.PayloadCase == AgentMessage.PayloadOneofCase.Heartbeat)
            {
                var hb = msg.Heartbeat;

                if (server is null && Guid.TryParse(hb.ServerId, out var sid))
                    server = await _servers.GetByIdAsync(sid, context.CancellationToken);

                if (server is not null)
                {
                    server.CpuPercent = hb.CpuPercent;
                    server.MemPercent = hb.MemPercent;
                    server.DiskPercent = hb.DiskPercent;
                    server.UptimeSeconds = hb.UptimeSeconds;
                    server.AgentVersion = hb.Version;
                    server.LastHeartbeat = DateTimeOffset.UtcNow;
                    server.Status = ServerStatus.Up;
                    await _servers.UpdateAsync(server, context.CancellationToken);
                }
            }
            else if (msg.PayloadCase == AgentMessage.PayloadOneofCase.Services && server is not null)
            {
                await ProcessServiceReport(server, msg.Services, context.CancellationToken);
            }
        }
    }

    private async Task ProcessServiceReport(Server server, ServiceReport report, CancellationToken ct)
    {
        var existing = await _services.ListByServerAsync(server.Id, ct);
        var existingPorts = existing.ToDictionary(s => s.Port);

        foreach (var ds in report.Services)
        {
            if (!existingPorts.ContainsKey(ds.Port))
            {
                var svc = Domain.Entities.Service.Create(server.Id, ds.Name, ds.Port,
                    ds.ServiceType == "docker" ? ServiceType.Docker : ServiceType.Native);
                svc.DockerImage = ds.DockerImage;
                svc.DockerContainerId = ds.DockerContainerId;
                svc.Tags = ds.Tags.ToList();
                svc.Status = ServerStatus.Up;
                await _services.AddAsync(svc, ct);
            }
        }
    }
}
