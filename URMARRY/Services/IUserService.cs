namespace URMARRY.Services;

public interface IUserService
{
    Task<bool> IsPremiumUser(long userId);
    Task<long> GetRemainingContactViewCredits(long userId);
    Task<long> GetRemainingMessageCredits(long userId);
    Task<Domain.PlanPurchase?> GetActivePlanPurchase(long userId);
    
    Task<bool> SpendContactViewCredit(long userId);
    Task<bool> RefundContactViewCredit(long userId);
    Task<bool> SpendMessageCredit(long userId);
    Task<bool> RefundMessageCredit(long userId);
    Task<bool> AreUserContactDetailsUnlocked(long viewerUserId, long viewedUserId);
}
