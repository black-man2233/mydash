using Microsoft.Extensions.Options;
using MyDash.Application.Options;
using MyDash.Application.Services;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace MyDash.Infrastructure.Sms;

public class TwilioSmsSender : ISmsSender
{
    private readonly SmsOptions _opts;

    public TwilioSmsSender(IOptions<SmsOptions> opts)
    {
        _opts = opts.Value;
        TwilioClient.Init(_opts.ApiKey, _opts.ApiKey);
    }

    public string ProviderName => "Twilio";

    public async Task<SmsSendResult> SendAsync(string toE164, string body, CancellationToken ct = default)
    {
        try
        {
            var msg = await MessageResource.CreateAsync(
                to: new Twilio.Types.PhoneNumber(toE164),
                from: new Twilio.Types.PhoneNumber(_opts.From),
                body: body);
            return new SmsSendResult(msg.ErrorCode is null, msg.Sid, msg.ErrorMessage);
        }
        catch (Exception ex)
        {
            return new SmsSendResult(false, null, ex.Message);
        }
    }
}
