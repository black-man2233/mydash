using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Options;
using MyDash.Application.Options;
using MyDash.Application.Services;

namespace MyDash.Infrastructure.Sms;

public class ClickSendSmsSender : ISmsSender
{
    private readonly HttpClient _http;
    private readonly SmsOptions _opts;

    public ClickSendSmsSender(HttpClient http, IOptions<SmsOptions> opts)
    {
        _http = http;
        _opts = opts.Value;
    }

    public string ProviderName => "ClickSend";

    public async Task<SmsSendResult> SendAsync(string toE164, string body, CancellationToken ct = default)
    {
        var cred = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_opts.From}:{_opts.ApiKey}"));
        var req = new HttpRequestMessage(HttpMethod.Post, "https://rest.clicksend.com/v3/sms/send");
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic", cred);

        var payload = new
        {
            messages = new[] { new { source = "MyDash", to = toE164, body } }
        };
        req.Content = JsonContent.Create(payload);

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
