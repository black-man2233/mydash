using Microsoft.Extensions.Options;
using MyDash.Application.Options;
using MyDash.Application.Repositories;
using MyDash.Application.Services;
using MyDash.Domain.Entities;

namespace MyDash.Application.UseCases;

public record RequestPin(string ClientIp);

public record RequestPinResult(string ChallengeId, int ExpiresInSeconds, string PhoneMasked, string Provider);

public class RequestPinHandler
{
    private readonly IPinChallengeRepository _challenges;
    private readonly ISmsSender _sms;
    private readonly IAuditWriter _audit;
    private readonly SmsOptions _smsOpts;
    private readonly SecurityOptions _secOpts;

    public RequestPinHandler(
        IPinChallengeRepository challenges,
        ISmsSender sms,
        IAuditWriter audit,
        IOptions<SmsOptions> smsOpts,
        IOptions<SecurityOptions> secOpts)
    {
        _challenges = challenges;
        _sms = sms;
        _audit = audit;
        _smsOpts = smsOpts.Value;
        _secOpts = secOpts.Value;
    }

    public async Task<RequestPinResult> Handle(RequestPin request, CancellationToken ct)
    {
        var recentCount = await _challenges.CountRecentByIpAsync(request.ClientIp, TimeSpan.FromHours(1), ct);
        if (recentCount >= 5)
            throw new InvalidOperationException("Rate limit exceeded.");

        var code = GenerateCode();
        var hash = BCrypt.Net.BCrypt.HashPassword(code);
        var challenge = PinChallenge.Create(_smsOpts.PhoneE164, hash, TimeSpan.FromSeconds(_secOpts.PinTtlSeconds));

        await _challenges.AddAsync(challenge, ct);

        var body = $"Your {_smsOpts.From} code is: {code}";
        var result = await _sms.SendAsync(_smsOpts.PhoneE164, body, ct);

        await _audit.WriteAsync(AuditEntry.Create(AuditAction.PinRequested, "login", request.ClientIp,
            result.Success ? "ok" : "sms_failed"), ct);

        var masked = MaskPhone(_smsOpts.PhoneE164);
        return new RequestPinResult(challenge.Id.ToString(), _secOpts.PinTtlSeconds, masked, _sms.ProviderName);
    }

    private static string GenerateCode()
    {
        return Random.Shared.Next(100000, 999999).ToString();
    }

    private static string MaskPhone(string phone)
    {
        if (phone.Length < 4) return "••••";
        return phone[..3] + " ••• ••• " + phone[^4..];
    }
}
