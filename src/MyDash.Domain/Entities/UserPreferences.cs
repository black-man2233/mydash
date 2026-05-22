namespace MyDash.Domain.Entities;

public class UserPreferences
{
    public int Id { get; private set; } = 1;
    public string Theme { get; set; } = "Dark";
    public string Accent { get; set; } = "#594AE2";
    public string DefaultServiceView { get; set; } = "cards";
    public string Density { get; set; } = "normal";
    public bool NotifyOnNewPorts { get; set; }
    public bool NotifyOnDisconnect { get; set; }
    public string? ScanSchedule { get; set; }
    public string? PhoneE164 { get; set; }
}
