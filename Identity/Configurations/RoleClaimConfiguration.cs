using System.Reflection;
using System.Text.Json;
using Application.Constants;
using Application.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Configurations;

public class RoleClaimConfiguration : IEntityTypeConfiguration<IdentityRoleClaim<long>>
{
    public void Configure(EntityTypeBuilder<IdentityRoleClaim<long>> builder)
    {
        var claims = GetPermissions().Select(
            permission => new IdentityRoleClaim<long>
            {
                Id = (int)permission.Id,
                RoleId = 1,
                ClaimType = CustomClaimTypes.Permission,
                ClaimValue = permission.Name
            }).ToList();

        builder.HasData(claims);
    }

    private static IEnumerable<PermissionDto> GetPermissions()
    {
        var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var permissionsData = File.ReadAllText(path + @"/Data/permissions.json");

        return JsonSerializer.Deserialize<List<PermissionDto>>(permissionsData,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<PermissionDto>()
            .OrderBy(x => x.Id).ToList();
    }
}