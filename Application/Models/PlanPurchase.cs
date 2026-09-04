using Application.Models.Common;
using Domain;
namespace Application.Models;

public class PlanPurchaseDto : BaseDto
{
    public long UserId { get; set; }
    public Registration? User { get; set; }
    
    public short ViewCreditsPurchased { get; set; }
    public short ViewCreditsUsed { get; set; }

    public DateTime ExpiresAt { get; set; }
    
    public bool ExpiryNotificationSent { get; set; }
    public bool LowCreditNotificationSent { get; set; }
}
