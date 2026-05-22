using Microsoft.Extensions.Options;
using MyDash.Application.Options;
using MyDash.Application.Repositories;
using MyDash.Application.Services;
using MyDash.Domain.Entities;

namespace MyDash.Application.UseCases;

public record VerifyPin(string ChallengeId, string Code, string ClientIp);

public record VerifyPinResult(bool Ok, int AttemptsRemaining, bool LockedOut, int LockoutSeconds);

public class VerifyPinHandler
{
    private readonly IPinChallengeRepository _challenges;
    private readonly IAuditWriter _audit;
    private readonly SecurityOptions _secOpts;

    public VerifyPinHandler(
        IPinChallengeRepository challenges,
        IAuditWriter audit,
        IOptions<SecurityOptions> secOpts)
    {
        _challenges = challenges;
        _audit = audit;
        _secOpts = secOpts.Value;
    }

    public async Task<VerifyPinResult> Handle(VerifyPin request, CancellationToken ct)
    {
        if (!Guid.TryParse(request.ChallengeId, out var id))
            return new VerifyPinResult(false, 0, false, 0);

        var challenge = await _challenges.GetByIdAsync(id, ct);
        if (challenge is null || !challenge.IsValid)
            return new VerifyPinResult(false, 0, false, 0);

        if (challenge.FailedAttempts >= _secOpts.MaxFailedAttempts)
        {
            await _audit.WriteAsync(AuditEntry.Create(AuditAction.PinFailed, "login", request.ClientIp, "locked_out"), ct);
            return new VerifyPinResult(false, 0, true, _secOpts.LockoutSeconds);
        }

        challenge.AttemptCount++;
        var match = BCrypt.Net.BCrypt.Verify(request.Code, challenge.CodeHash);
        if (!match)
        {
            challenge.FailedAttempts++;
            await _challenges.UpdateAsync(challenge, ct);
            var remaining = _secOpts.MaxFailedAttempts - challenge.FailedAttempts;
            await _audit.WriteAsync(AuditEntry.Create(AuditAction.PinFailed, "login", request.ClientIp, "wrong_code"), ct);
            return new VerifyPinResult(false, remaining, remaining <= 0, remaining <= 0 ? _secOpts.LockoutSeconds : 0);
        }

        challenge.Consume();
        await _challenges.UpdateAsync(challenge, ct);
        await _audit.WriteAsync(AuditEntry.Create(AuditAction.PinVerified, "login", request.ClientIp), ct);

        return new VerifyPinResult(true, _secOpts.MaxFailedAttempts, false, 0);
    }
}
