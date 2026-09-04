using Domain.Common;
using System;

namespace Domain;

public class LoginOtpVerification : BaseEntity
{
    public long UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string OtpCode { get; set; } = string.Empty;
    public string Purpose { get; set; } = "AdminLogin";
    public string TempToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; } = false;
    public int AttemptCount { get; set; } = 0;
    public int ResendCount { get; set; } = 0;
}
