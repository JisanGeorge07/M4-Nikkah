using Domain.Common;
namespace Domain;

public class PlanPurchase : BaseEntity
{
    public long UserId { get; set; }
    public virtual Registration? User { get; set; }
    
    public short ViewCreditsPurchased { get; set; }
    public short ViewCreditsUsed { get; set; }

    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow + new TimeSpan(180, 0, 0, 0);
    
    public bool ExpiryNotificationSent { get; set; }
    public bool LowCreditNotificationSent { get; set; }
}
