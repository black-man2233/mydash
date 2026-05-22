using Microsoft.Extensions.Options;
using MyDash.Application.Options;
using MyDash.Application.Services;

namespace MyDash.Infrastructure.Sms;

public class SmsApiSmsSender : ISmsSender
{
    private readonly HttpClient _http;
    private readonly SmsOptions _opts;

    public SmsApiSmsSender(HttpClient http, IOptions<SmsOptions> opts)
    {
        _http = http;
        _opts = opts.Value;
    }

    public string ProviderName => "SMSAPI";

    public async Task<SmsSendResult> SendAsync(string toE164, string body, CancellationToken ct = default)
    {
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["to"] = toE164,
            ["message"] = body,
            ["from"] = _opts.From,
        });

        var req = new HttpRequestMessage(HttpMethod.Post, "https://api.smsapi.com/sms.do");
        req.Headers.Add("Authorization", $"Bearer {_opts.ApiKey}");
        req.Content = form;

        try
        {
            var resp = await _http.SendAsync(req, ct);
            resp.EnsureSuccessStatusCode();
            return new SmsSendResult(true, null, null);
        }
        catch (Exception ex)
        {
            return new SmsSendResult(false, null, ex.Message);
        }
    }
}
