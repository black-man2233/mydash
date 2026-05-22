using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using MyDash.Application.Options;
using MyDash.Application.Services;

namespace MyDash.Infrastructure.Sms;

public class TextBeltSmsSender : ISmsSender
{
    private readonly HttpClient _http;
    private readonly SmsOptions _opts;

    public TextBeltSmsSender(HttpClient http, IOptions<SmsOptions> opts)
    {
        _http = http;
        _opts = opts.Value;
    }

    public string ProviderName => "TextBelt";

    public async Task<SmsSendResult> SendAsync(string toE164, string body, CancellationToken ct = default)
    {
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["phone"] = toE164,
            ["message"] = body,
            ["key"] = _opts.ApiKey,
        });

        HttpResponseMessage resp;
        try
        {
            resp = await _http.PostAsync("https://textbelt.com/text", form, ct);
        }
        catch (Exception ex)
        {
            return new SmsSendResult(false, null, ex.Message);
        }

        var json = await resp.Content.ReadFromJsonAsync<TextBeltResponse>(ct);
        return json is { Success: true }
            ? new SmsSendResult(true, json.TextId, null)
            : new SmsSendResult(false, null, json?.Error ?? "unknown");
    }

    private sealed record TextBeltResponse(bool Success, string? TextId, string? Error);
}
