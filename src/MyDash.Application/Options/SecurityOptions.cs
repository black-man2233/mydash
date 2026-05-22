namespace MyDash.Application.Options;

public class SecurityOptions
{
    public int PinTtlSeconds { get; set; } = 300;
    public int MaxFailedAttempts { get; set; } = 3;
    public int LockoutSeconds { get; set; } = 900;
    public int SessionCookieHours { get; set; } = 12;
    public string[] TailnetCidrs { get; set; } = Array.Empty<string>();
}
