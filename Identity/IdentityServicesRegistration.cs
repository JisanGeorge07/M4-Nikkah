using System.Reflection;
using System.Text.Json;
using Application.Constants;
using Application.Interfaces.Identity;
using Application.Models.Identity;
using Application.Models.Transactions;
using Identity.Models;
using Identity.Profiles;
using Identity.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Identity;

public static class IdentityServicesRegistration
{
    public static IServiceCollection ConfigureIdentityServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
        services.AddAutoMapper(typeof(MappingProfile));

        services.AddDbContext<IdentityDbContext>(
     options =>
     {
         var connectionString = configuration.GetConnectionString("ConnectionString");
         options.UseMySql(connectionString!, ServerVersion.AutoDetect(connectionString),
             b => b.MigrationsAssembly(typeof(IdentityDbContext).Assembly.FullName));
         options.EnableSensitiveDataLogging();
         options.UseSnakeCaseNamingConvention();
     },
     ServiceLifetime.Scoped);

        services
            .AddIdentity<ApplicationUser, IdentityRole<long>>()
            .AddEntityFrameworkStores<IdentityDbContext>()
            .AddDefaultTokenProviders();

        services.AddTransient<IAccountService, AccountService>();
        services.AddTransient<IRoleService, RoleService>();

        /*services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    ValidIssuer = configuration["JwtSettings:Issuer"],
                    ValidAudience = configuration["JwtSettings:Audience"],
                    IssuerSigningKey =
                        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JwtSettings:Key"]))
                };
            });*/

        services.Configure<IdentityOptions>(options =>
        {
            options.Password.RequiredLength = 1;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireDigit = false;
            options.Password.RequireNonAlphanumeric = false;
        });

        services.AddAuthorization(options =>
        {
            var permissions = GetPermissions();

            foreach (var permission in permissions)
                options.AddPolicy(permission.Name!,
                    policy => policy.RequireClaim(CustomClaimTypes.Permission, permission.Name!));
        });
        services.Configure<PayUSettings>(configuration.GetSection("PayU"));
        return services;
    }

    private static List<PermissionDto> GetPermissions()
    {
        var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var permissionsData = File.ReadAllText(path + @"/Data/permissions.json");

        return JsonSerializer.Deserialize<List<PermissionDto>>(permissionsData,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<PermissionDto>()
            .OrderBy(x => x.Id).ToList();
    }
}