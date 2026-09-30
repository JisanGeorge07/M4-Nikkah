using Domain.Common;
namespace Domain;

public class PlanPurchase : BaseEntity
{
    public long UserId { get; set; }
    public virtual Registration? User { get; set; }
    
    public short ViewCreditsPurchased { get; set; }
    public short ViewCreditsUsed { get; set; }
    public short MessageCreditsPurchased { get; set; }
    public short MessageCreditsUsed { get; set; }
    public short AudioCallContactsPurchased { get; set; }
    public short AudioCallContactsUsed { get; set; }
    public short VideoCallMinutesPurchased { get; set; }
    public short VideoCallMinutesUsed { get; set; }

    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow + new TimeSpan(180, 0, 0, 0);
    
    public bool ExpiryNotificationSent { get; set; }
    public bool LowCreditNotificationSent { get; set; }
}
