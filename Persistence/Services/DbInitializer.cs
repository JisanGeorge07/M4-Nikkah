using Application.Constants;
using Domain;
using Domain.Framework;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Persistence.Services;

public static class DbInitializer
{
    public static async Task Seed(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();


        if (context.Email.FirstOrDefault(x => x.Purpose == EnquiryTypes.General) == null)
            await context.AddAsync(new Email { Purpose = EnquiryTypes.General });

        if (context.Email.FirstOrDefault(x => x.Purpose == EnquiryTypes.Registration) == null)
            await context.AddAsync(new Email { Purpose = EnquiryTypes.Registration });

        if (!context.HomeContent.Any())
            context.HomeContent.Add(new HomeContent());

        if (!context.ProfileFor.Any())
            context.ProfileFor.Add(new ProfileFor 
            { 
                Title = "Profile For"
            });
        if (!context.Nationality.Any())
            context.Nationality.Add(new Nationality
            {
                Title = "Nationality"
            });
        if (!context.MaritalStatus.Any())
            context.MaritalStatus.Add(new MaritalStatus
            {
                Title = "Marital Status"
            });
        if (!context.BodyFeatures.Any())
            context.BodyFeatures.Add(new BodyFeatures
            {
                Title = "Height",
                Type = BodyFeature.Height
            });
        if (!context.BodyFeatures.Any())
            context.BodyFeatures.Add(new BodyFeatures
            {
                Title = "Weight",
                Type = BodyFeature.Weight
            });
        if (!context.BodyFeatures.Any())
            context.BodyFeatures.Add(new BodyFeatures
            {
                Title = "Complexion",
                Type = BodyFeature.Complexion
            });
        if (!context.BodyFeatures.Any())
            context.BodyFeatures.Add(new BodyFeatures
            {
                Title = "Body Type",
                Type = BodyFeature.BodyType
            });
        if (!context.Profession.Any())
            context.Profession.Add(new Profession
            {
                Title = "Profession"
            });
        if (!context.MotherTongue.Any())
            context.MotherTongue.Add(new MotherTongue
            {
                Title = "Mother Tongue"
            });
        if (!context.ReligionCaste.Any())
            context.ReligionCaste.Add(new ReligionCaste
            {
                ParentId = 0,
                Title = "Religion"
            });
        if (!context.ReligionCaste.Any())
            context.ReligionCaste.Add(new ReligionCaste
            {
                ParentId = 1,
                Title = "Caste"
            });
        if (!context.Community.Any())
            context.Community.Add(new Community
            {
                Title = "Community"
            });
        if (!context.Religiousness.Any())
            context.Religiousness.Add(new Religiousness
            {
                Title = "Religiousness"
            });
        if (!context.FinancialStatus.Any())
            context.FinancialStatus.Add(new FinancialStatus
            {
                Title = "Financial Status"
            });

		if (!context.About.Any())
			context.About.Add(new About());

		if (!context.Contact.Any())
			context.Contact.Add(new Contact());

		if (!context.TestimonialSettings.Any())
			context.TestimonialSettings.Add(new TestimonialSetting { FirstPromptDays = 7, FollowUpPromptDays = 30 });

		await context.SaveChangesAsync();
    }
}