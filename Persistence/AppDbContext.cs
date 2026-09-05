using Application.Models;
using Application.Models.Transactions;
using Domain;
using Domain.Common;
using Domain.Framework;
using Microsoft.EntityFrameworkCore;

namespace Persistence;

    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

    public DbSet<PageSettings> PageSettings { get; set; }
    public DbSet<Email> Email { get; set; }
    public DbSet<SocialMedia> SocialMedia { get; set; }
    public DbSet<HomeContent> HomeContent { get; set; }
    public DbSet<ImageGallery> ImageGallery { get; set;}
    public DbSet<Registration> Registration { get; set; }
    public DbSet<ProfileFor> ProfileFor { get; set; }
    public DbSet<Nationality> Nationality { get; set; }
    public DbSet<MaritalStatus> MaritalStatus { get; set; }
    public DbSet<BodyFeatures> BodyFeatures { get; set; }
    public DbSet<Profession> Profession { get; set; }
    public DbSet<MotherTongue> MotherTongue { get; set; }
    public DbSet<ReligionCaste> ReligionCaste { get; set; }
    public DbSet<UserFavouriteProfile> Userfavoriteprofile { get; set; }
    public DbSet<UserStarProfile> Userstaredprofile { get; set; }
    public DbSet<UserContactView> UserContactViews { get; set; }
    public DbSet<UserReport> UserReports { get; set; }
    public DbSet<UserNotLikeProfile> UserNotLikeProfiles { get; set; }
    public DbSet<Notification> Notification { get; set; }
    public DbSet<ManagePreference> AccountmanageAlert { get; set; }
    public DbSet<Transaction> Transaction { get; set; }
    public DbSet<PlanPurchase> PlanPurchases { get; set; }
    public DbSet<Community> Community { get; set; }
    public DbSet<Religiousness> Religiousness { get; set; }
    public DbSet<FinancialStatus> FinancialStatus { get; set; }
    public DbSet<HomeBanner> HomeBanner { get; set; }
    public DbSet<About> About { get; set; }
    public DbSet<Contact> Contact { get; set; }
    public DbSet<MatchingProfiles> MatchingProfiles { get; set; }
    public DbSet<ContactEnquiry> ContactEnquiry { get; set; }
    public DbSet<SupportRequest> SupportRequests { get; set; }
    public DbSet<Images> Images { get; set; }
    public DbSet<State> States { get; set; }
    public DbSet<District> Districts { get; set; }
    public DbSet<City> Cities { get; set; }
    public DbSet<UserReportReason> UserReportReasons { get; set; }
    public DbSet<PhotoUnlockRequest> PhotoUnlockRequests { get; set; }
    public DbSet<MatchStatusUpdate> MatchStatusUpdates { get; set; }
    public DbSet<SuccessStory> SuccessStories { get; set; }
    public DbSet<TestimonialSetting> TestimonialSettings { get; set; }
    public DbSet<DeleteReason> DeleteReasons { get; set; }
    public DbSet<StaffProfileAssignment> StaffProfileAssignments { get; set; }
    public DbSet<FollowUp> FollowUps { get; set; }
    public DbSet<FollowUpTimeline> FollowUpTimelines { get; set; }
    public DbSet<FollowUpAdminApproval> FollowUpAdminApprovals { get; set; }
    public DbSet<ColdLead> ColdLeads { get; set; }
    public DbSet<StaffDetail> StaffDetails { get; set; }
    public DbSet<PremiumPackage> PremiumPackages { get; set; }
    public DbSet<StaffSalaryConfig> StaffSalaryConfigs { get; set; }
    public DbSet<StaffBoysIncentiveConfig> StaffBoysIncentiveConfigs { get; set; }
    public DbSet<StaffGirlsIncentiveConfig> StaffGirlsIncentiveConfigs { get; set; }
    public DbSet<StaffDailyTargetConfig> StaffDailyTargetConfigs { get; set; }
    public DbSet<StaffTargetSlab> StaffTargetSlabs { get; set; }
    public DbSet<StaffVerificationIncentiveConfig> StaffVerificationIncentiveConfigs { get; set; }
    public DbSet<StaffLeaveDeductionConfig> StaffLeaveDeductionConfigs { get; set; }
    public DbSet<StaffIncentiveConfig> StaffIncentiveConfigs { get; set; }
    public DbSet<StaffPerformanceTargetConfig> StaffPerformanceTargetConfigs { get; set; }
    public DbSet<StaffPayroll> StaffPayrolls { get; set; }
    public DbSet<StaffPayrollAuditLog> StaffPayrollAuditLogs { get; set; }
    public DbSet<StaffPayrollAdminIncentiveItem> StaffPayrollAdminIncentiveItems { get; set; }
    public DbSet<StaffLeaveRecord> StaffLeaveRecords { get; set; }
    public DbSet<StaffComplaintRecord> StaffComplaintRecords { get; set; }
    public DbSet<VerificationDocument> VerificationDocuments { get; set; }
    public DbSet<LoginOtpVerification> LoginOtpVerifications { get; set; }



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Registration)
            .WithMany()
            .HasForeignKey(t => t.userId);

        modelBuilder.Entity<FollowUp>()
            .HasIndex(f => new { f.ProfileId, f.FollowUpType });

        modelBuilder.Entity<FollowUp>()
            .HasOne(f => f.Profile)
            .WithMany()
            .HasForeignKey(f => f.ProfileId);

        modelBuilder.Entity<FollowUpTimeline>()
            .HasOne(t => t.FollowUp)
            .WithMany(f => f.Timelines)
            .HasForeignKey(t => t.FollowUpId);

        modelBuilder.Entity<FollowUpAdminApproval>()
            .HasOne(a => a.FollowUp)
            .WithMany(f => f.AdminApprovals)
            .HasForeignKey(a => a.FollowUpId);

        modelBuilder.Entity<StaffPayrollAuditLog>()
            .HasOne(a => a.StaffPayroll)
            .WithMany()
            .HasForeignKey(a => a.StaffPayrollId);

        modelBuilder.Entity<StaffPayrollAdminIncentiveItem>()
            .HasOne<StaffPayroll>()
            .WithMany(p => p.AdminIncentiveItems)
            .HasForeignKey(i => i.StaffPayrollId);
    }


    public virtual async Task<int> SaveChangesAsync(string username = "SYSTEM")
    {
        foreach (var entry in base.ChangeTracker.Entries<BaseEntity>()
                     .Where(q => q.State is EntityState.Added or EntityState.Modified))
        {
            entry.Entity.ModifiedOn = DateTime.UtcNow;
            entry.Entity.ModifiedBy = username;


            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedOn = DateTime.UtcNow;
                entry.Entity.CreatedBy = username;
            }
        }

        var result = await base.SaveChangesAsync();

        return result;
    }

}