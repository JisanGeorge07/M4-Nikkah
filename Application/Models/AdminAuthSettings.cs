namespace Application.Models;

public class AdminAuthSettings
{
    public int OtpExpirationMinutes { get; set; } = 5;
    public int ResendCooldownSeconds { get; set; } = 60;
    public int MaxFailedAttempts { get; set; } = 5;
    public int MaxResends { get; set; } = 5;
}
