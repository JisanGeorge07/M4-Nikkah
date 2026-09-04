using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<IdentityRole<long>>
{
    public void Configure(EntityTypeBuilder<IdentityRole<long>> builder)
    {
        builder.HasData(
            new IdentityRole<long>
            {
                Id = 1,
                Name = "Super Administrator",
                NormalizedName = "SUPER ADMINISTRATOR"
            },
            new IdentityRole<long>
            {
                Id = 2,
                Name = "Staff",
                NormalizedName = "STAFF"
            }
        );
    }
}