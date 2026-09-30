using Application.Interfaces.Persistence;
using Domain;
namespace URMARRY.Services;

public class UserService : IUserService
{
    private readonly IRepository<PlanPurchase> _planPurchaseRepo;
    private readonly IRepository<UserContactView> _userContactViewRepository;
    
    
    public UserService(IRepository<PlanPurchase> planPurchaseRepo, 
        IRepository<UserContactView> userContactViewRepository)
    {
        _planPurchaseRepo = planPurchaseRepo;
        _userContactViewRepository = userContactViewRepository;
    }


    public async Task<bool> IsPremiumUser(long userId)
    {
        var planPurchases = await _planPurchaseRepo.WhereActive(x => x.UserId == userId && x.ExpiresAt > DateTime.UtcNow);
        return planPurchases.Any();
    }
    public async Task<long> GetRemainingContactViewCredits(long userId)
    {
        var planPurchases = await _planPurchaseRepo.WhereActive(x => x.UserId == userId && x.ExpiresAt > DateTime.UtcNow);

        return planPurchases.Sum(x => x.ViewCreditsPurchased) - planPurchases.Sum(x => x.ViewCreditsUsed);
    }

    public async Task<long> GetRemainingMessageCredits(long userId)
    {
        var planPurchases = await _planPurchaseRepo.WhereActive(x => x.UserId == userId && x.ExpiresAt > DateTime.UtcNow);

        return planPurchases.Sum(x => x.MessageCreditsPurchased) - planPurchases.Sum(x => x.MessageCreditsUsed);
    }

    public async Task<PlanPurchase?> GetActivePlanPurchase(long userId)
    {
        var planPurchases = await _planPurchaseRepo
            .WhereActive(x => x.UserId == userId && x.ExpiresAt > DateTime.UtcNow);

        // Prioritize plans that have remaining credits
        return planPurchases
                   .Where(x => x.ViewCreditsUsed < x.ViewCreditsPurchased || x.MessageCreditsUsed < x.MessageCreditsPurchased)
                   .OrderBy(x => x.ExpiresAt)
                   .FirstOrDefault() 
               ?? planPurchases.OrderByDescending(x => x.ExpiresAt).FirstOrDefault();
    }

    public async Task<bool> SpendContactViewCredit(long userId)
    {
        var planPurchases = await _planPurchaseRepo
            .WhereActive(x => x.UserId == userId && x.ExpiresAt > DateTime.UtcNow && x.ViewCreditsUsed < x.ViewCreditsPurchased);

        PlanPurchase? planPurchase = planPurchases.OrderBy(x => x.ExpiresAt).FirstOrDefault();

        if (planPurchase is null)
            return false;
        
        planPurchase.ViewCreditsUsed++;
        await _planPurchaseRepo.Update(planPurchase);
        await _planPurchaseRepo.SaveChanges();
        return true;
    }

    public async Task<bool> RefundContactViewCredit(long userId)
    {
        var planPurchase = (await _planPurchaseRepo
            .WhereActive(x => x.UserId == userId))
            .OrderByDescending(x => x.CreatedOn)
            .FirstOrDefault();

        if (planPurchase is null || planPurchase.ViewCreditsUsed <= 0)
            return false;

        planPurchase.ViewCreditsUsed--;
        await _planPurchaseRepo.Update(planPurchase);
        await _planPurchaseRepo.SaveChanges();
        return true;
    }

    public async Task<bool> SpendMessageCredit(long userId)
    {
        var planPurchases = await _planPurchaseRepo
            .WhereActive(x => x.UserId == userId && x.ExpiresAt > DateTime.UtcNow && x.MessageCreditsUsed < x.MessageCreditsPurchased);

        PlanPurchase? planPurchase = planPurchases.OrderBy(x => x.ExpiresAt).FirstOrDefault();

        if (planPurchase is null)
            return false;
        
        planPurchase.MessageCreditsUsed++;
        await _planPurchaseRepo.Update(planPurchase);
        await _planPurchaseRepo.SaveChanges();
        return true;
    }

    public async Task<bool> RefundMessageCredit(long userId)
    {
        var planPurchase = (await _planPurchaseRepo
            .WhereActive(x => x.UserId == userId))
            .OrderByDescending(x => x.CreatedOn)
            .FirstOrDefault();

        if (planPurchase is null || planPurchase.MessageCreditsUsed <= 0)
            return false;

        planPurchase.MessageCreditsUsed--;
        await _planPurchaseRepo.Update(planPurchase);
        await _planPurchaseRepo.SaveChanges();
        return true;
    }

    public async Task<bool> AreUserContactDetailsUnlocked(long viewerUserId, long viewedUserId)
    {
        return await _userContactViewRepository.FirstOrDefaultActive(x => x.ViewerUserId == viewerUserId && x.ViewedUserId == viewedUserId)
               != null;
    }
}
