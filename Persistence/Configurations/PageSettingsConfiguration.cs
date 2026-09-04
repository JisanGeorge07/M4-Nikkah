using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class PageSettingsConfiguration : IEntityTypeConfiguration<PageSettings>
{
    public void Configure(EntityTypeBuilder<PageSettings> builder)
    {
        builder.HasData(
            // Main Pages
            new PageSettings
            {
                Id = 1,
                Name = "index",
                Title = "Home",
                DisplayOrder = 1
            },
            new PageSettings
            {
                Id = 2,
                Name = "result",
                ParentName = "index",
                Title = "Result",
                DisplayOrder = 2
            },
            new PageSettings
            {
                Id = 3,
                Name = "error",
                ParentName = "index",
                Title = "Error",
                DisplayOrder = 3
            }
        );
    }
}