using Application.Helpers;
using Application.Interfaces.Persistence;
using Application.Models;
using Application.Models.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Repositories;
using Persistence.Services;

namespace Persistence;

public static class PersistenceServicesRegistration
{
    public static IServiceCollection ConfigurePersistenceServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("ConnectionString");
            options.UseMySql(connectionString!, ServerVersion.AutoDetect(connectionString),
                mySqlOptions =>
                {
                    mySqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                    mySqlOptions.EnableStringComparisonTranslations();
                });
            options.EnableSensitiveDataLogging();
            options.UseSnakeCaseNamingConvention();
        },
         ServiceLifetime.Scoped);
        services.Configure<SmtpSettings>(configuration.GetSection("SmtpSettings"));
        services.Configure<AdminAuthSettings>(configuration.GetSection("AdminAuth"));
        services.AddTransient<EmailNotificationHelper>();
        services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
       // services.AddScoped(typeof(IFileService), typeof(FileService));
        services.AddScoped(typeof(IMatchingProfileRepo), typeof(MatchingProfilesRepo));
        services.AddScoped<IRegistrationRepository, RegistrationRepository>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddTransient<IFileService, FileService>();
       
        services.AddTransient<ITransactionRepository, TransactionRepository>();
        services.AddTransient<INotifcationRepository, NotificationRepository>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<ICallService, CallService>();
        

        return services;
    }
}