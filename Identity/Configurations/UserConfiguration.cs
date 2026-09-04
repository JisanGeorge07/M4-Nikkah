using Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        var hasher = new PasswordHasher<ApplicationUser>();
        builder.HasData(
            new ApplicationUser
            {
                Id = 1,
                Email = "admin@urmarry.com",
                NormalizedEmail = "ADMIN@URMARRY.COM",
                UserName = "admin",
                NormalizedUserName = "ADMIN",
                PasswordHash = hasher.HashPassword(null!, "1"),
                EmailConfirmed = true,
                SecurityStamp = "F3A1877B-B990-4F13-8E87-C90DF070B2C9"
            }
        );
    }
}