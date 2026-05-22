namespace MyDash.Application.Services;

public interface ISmsSender
{
    string ProviderName { get; }
    Task<SmsSendResult> SendAsync(string toE164, string body, CancellationToken ct = default);
}

public record SmsSendResult(bool Success, string? ProviderMessageId, string? Error);
