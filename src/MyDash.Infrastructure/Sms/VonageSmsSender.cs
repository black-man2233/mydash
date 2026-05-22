using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using MyDash.Application.Options;
using MyDash.Application.Services;

namespace MyDash.Infrastructure.Sms;

public class VonageSmsSender : ISmsSender
{
    private readonly HttpClient _http;
    private readonly SmsOptions _opts;

    public VonageSmsSender(HttpClient http, IOptions<SmsOptions> opts)
    {
        _http = http;
        _opts = opts.Value;
    }

    public string ProviderName => "Vonage";

    public async Task<SmsSendResult> SendAsync(string toE164, string body, CancellationToken ct = default)
    {
        var payload = new
        {
            from = _opts.From,
            to = toE164.TrimStart('+'),
            text = body,
            api_key = _opts.ApiKey,
            api_secret = _opts.ApiKey,
        };
        try
        {
            var resp = await _http.PostAsJsonAsync("https://rest.nexmo.com/sms/json", payload, ct);
            resp.EnsureSuccessStatusCode();
            return new SmsSendResult(true, null, null);
        }
        catch (Exception ex)
        {
            return new SmsSendResult(false, null, ex.Message);
        }
    }
}
