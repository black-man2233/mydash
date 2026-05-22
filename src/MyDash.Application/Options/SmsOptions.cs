namespace MyDash.Application.Options;

public class SmsOptions
{
    public string Provider { get; set; } = "TextBelt";
    public string PhoneE164 { get; set; } = string.Empty;
    public string ApiKey { get; set; } = "textbelt";
    public string From { get; set; } = "MyDash";
}
