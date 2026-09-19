using Application;
using AspNetCore.ReCaptcha;
using Identity;
using Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Persistence;
using Persistence.Services;
using Serilog;
using System.Text;
using URMARRY.Hubs;
using URMARRY.Services;

var builder = WebApplication.CreateBuilder(args);

// Load configuration from appsettings.json
builder.Host.ConfigureAppConfiguration((hostingContext, config) =>
{
    var settings = config.Build();
    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(settings)
        .Enrich.FromLogContext()
        .CreateLogger();
})
.UseSerilog();

// Set up Serilog to log to a specified external file path
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Error()
    .MinimumLevel.Warning()
    .WriteTo.File("Logs/webapp-log.txt")
    .CreateLogger();

builder.Host.UseSerilog(); // Attach Serilog to the app

//builder.WebHost.UseKestrel(options => { options.Listen(IPAddress.Loopback, 5027); });

builder.Services.ConfigureApplicationServices(builder.Configuration);
builder.Services.ConfigureInfrastructureServices(builder.Configuration);
builder.Services.ConfigureIdentityServices(builder.Configuration);
builder.Services.ConfigurePersistenceServices(builder.Configuration);

builder.Services.AddSingleton<PresenceTracker>();
builder.Services.AddSignalR();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<CookieHelper>();
builder.Services.AddScoped<UserStatusFilter>();
builder.Services.AddHostedService<PremiumNotificationService>();
builder.Services.AddHostedService<RelationshipStatusNotificationService>();
builder.Services.AddHostedService<PresenceCleanupService>();
builder.Services.AddScoped<IRenewalFollowUpProcessor, RenewalFollowUpProcessor>();
builder.Services.AddHostedService<AutoRenewalFollowUpService>();

builder.Services.AddReCaptcha(builder.Configuration.GetSection("ReCaptcha"));
// it sets the login path to /account/login, so unauthenticated users are redirected there when authentication is required.
builder.Services.ConfigureApplicationCookie(options => options.LoginPath = "/account/login");
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
})
.AddCookie("Cookies")
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    var secret = builder.Configuration["JwtSettings:Secret"];
    var key = Encoding.ASCII.GetBytes(secret ?? string.Empty);
    
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(5) // Original production value
        // ClockSkew = TimeSpan.Zero // For testing
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // 1. Try to read from Authorization header first
            var authorization = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                context.Token = authorization.Substring("Bearer ".Length).Trim();
            }
            // 2. Fallback to reading from the "auth_session" cookie
            else if (context.Request.Cookies.TryGetValue("auth_session", out var cookieToken))
            {
                context.Token = cookieToken;
            }
            // 3. Fallback to reading from "access_token" query parameter for SignalR WebSocket connections
            else if (context.Request.Query.TryGetValue("access_token", out var accessToken)
                && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddControllersWithViews(options => { options.EnableEndpointRouting = false; options.Filters.Add<UserStatusFilter>(); });
builder.Services.AddEndpointsApiExplorer();

// CORS policy for Staff API (external staff website)
builder.Services.AddCors(options =>
{
    options.AddPolicy("StaffApiCors", policy =>
    {
        policy.AllowAnyOrigin()  // TODO: Restrict to staff website domain in production
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});


var app = builder.Build();


// Ensure the database is migrated to the latest version
using (var scope = app.Services.CreateScope())
{
    using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
   // dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    await DbInitializer.Seed(app);
}
else
{
    //app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Enable forwarded headers so the app respects X-Forwarded-Proto (HTTPS) 
// from reverse proxies (IIS, Nginx, load balancers, etc.)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseCors("StaffApiCors");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<PresenceHub>("/hubs/presence");
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<CallHub>("/hubs/call");

app.MapControllerRoute(
    "area",
    "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    "default",
    "{controller=Home}/{action=Index}/{id?}");

try
{
    Log.Information("Starting web host");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
