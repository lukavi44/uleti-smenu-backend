using API.Hubs;
using API.Middlewares;
using API.Services;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.DataProtection;
using Core.Billing;
using Core.Interfaces;
using Core.Models.Entities;
using Core.Models.Enums;
using Core.Repositories;
using Core.Services;
using Infrastructure.Email;
using Infrastructure.Persistence.Database;
using Infrastructure.Persistence.Database.Repositories;
using Infrastructure.Persistence.Services;
using Infrastructure.Stripe;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using System.Net;
using System.Net.Mail;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile(
        "appsettings.Development.local.json",
        optional: true,
        reloadOnChange: true);
}

static string DescribeDatabaseTarget(string? connectionString)
{
    if (string.IsNullOrWhiteSpace(connectionString))
        return "not configured";

    if (connectionString.Contains("database.windows.net", StringComparison.OrdinalIgnoreCase))
        return "Azure SQL — use appsettings.Development.json for local SQL Server";

    const string serverKey = "Server=";
    var start = connectionString.IndexOf(serverKey, StringComparison.OrdinalIgnoreCase);
    if (start < 0)
        return "local SQL Server";

    start += serverKey.Length;
    var end = connectionString.IndexOf(';', start);
    var server = end < 0 ? connectionString[start..] : connectionString[start..end];
    return $"local SQL Server ({server.Trim()})";
}

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
    builder.WebHost.UseUrls($"http://*:{port}");

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IJobPostRepository, JobPostRepository>();
builder.Services.AddScoped<IApplicationRepository, ApplicationRepository>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();
builder.Services.AddScoped<IWorkExperienceRepository, WorkExperienceRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IRestaurantLocationRepository, RestaurantLocationRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
builder.Services.AddScoped<IPaymentEventRepository, PaymentEventRepository>();
builder.Services.AddScoped<IWalletTransactionRepository, WalletTransactionRepository>();

builder.Services.AddScoped<RoleManager<IdentityRole<Guid>>>();
builder.Services.AddScoped<IApplicationUnitOfWork, ApplicationUnitOfWork>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IJobPostService, JobPostService>();
builder.Services.AddScoped<IApplicationService, ApplicationService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IEmployeeProfileService, EmployeeProfileService>();
builder.Services.AddScoped<IEmployerProfileService, EmployerProfileService>();
builder.Services.AddScoped<IPlatformStatsService, PlatformStatsService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IReviewReminderService, ReviewReminderService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddSingleton<IRealtimeNotifier, RealtimeNotifier>();
builder.Services.AddSignalR();
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddScoped<IBillingCheckoutService, BillingCheckoutService>();
builder.Services.AddScoped<IBillingWebhookProcessor, BillingWebhookProcessor>();
builder.Services.AddScoped<IWalletLedgerService, WalletLedgerService>();
builder.Services.Configure<BillingSettings>(builder.Configuration.GetSection(BillingSettings.SectionName));
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection(StripeSettings.SectionName));

var stripeEnabled = builder.Configuration.GetValue<bool>($"{StripeSettings.SectionName}:Enabled");
if (stripeEnabled)
    builder.Services.AddScoped<IPaymentProvider, StripePaymentProvider>();
else
    builder.Services.AddScoped<IPaymentProvider, DisabledPaymentProvider>();

builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"))
    .AddTransient(serviceProvider =>
    {
        var smtpSettings = serviceProvider.GetRequiredService<IOptions<SmtpSettings>>().Value;

        return new SmtpClient
        {
            Host = smtpSettings.Host,
            Port = smtpSettings.Port,
            Credentials = new NetworkCredential(smtpSettings.Username, smtpSettings.Password),
            EnableSsl = smtpSettings.EnableSsl,
            Timeout = 10000
        };
    });

builder.Services.AddTransient<IEmailService, EmailService>(provider =>
{
    var smtpClient = provider.GetRequiredService<SmtpClient>();
    var smtpSettings = provider.GetRequiredService<IOptions<SmtpSettings>>().Value;
    var fromEmail = !string.IsNullOrWhiteSpace(smtpSettings.FromEmail)
        ? smtpSettings.FromEmail
        : (smtpClient.Credentials as NetworkCredential)?.UserName ?? string.Empty;
    return new EmailService(smtpClient, fromEmail);
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("oauth2", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });

    options.OperationFilter<SecurityRequirementsOperationFilter>();
});

builder.Services.AddDbContext<ApplicationDbContext>(
    options => options.UseSqlServer(
        connectionString: builder.Configuration["ConnectionStrings:UletiSmenu"]
    )
);

builder.Services.AddDataProtection()
    .PersistKeysToDbContext<ApplicationDbContext>();

builder.Services.AddIdentity<User, IdentityRole<Guid>>(options =>
{
    options.SignIn.RequireConfirmedEmail = false;
    options.User.RequireUniqueEmail = true;     
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
})
.AddRoles<IdentityRole<Guid>>()
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddApiEndpoints();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = IdentityConstants.BearerScheme;
    options.DefaultChallengeScheme = IdentityConstants.BearerScheme;
})
.AddBearerToken(IdentityConstants.BearerScheme, options =>
{
    options.Events = new BearerTokenEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        policy =>
            policy
                .WithOrigins(corsOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    var connectionString = app.Configuration["ConnectionStrings:UletiSmenu"];
    app.Logger.LogInformation("Development database target: {DatabaseTarget}", DescribeDatabaseTarget(connectionString));
}

var uploadPath = app.Configuration["FileSettings:UploadPath"]
    ?? Path.Combine(app.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadPath);

await EnsureDatabaseMigratedAsync(app.Services);
await EnsureRolesSeededAsync(app.Services);
await EnsureSubscriptionsSeededAsync(app.Services);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadPath),
    RequestPath = "/uploads"
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors("AllowSpecificOrigin");

if (app.Environment.IsProduction())
    app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapIdentityApi<User>();

app.MapControllers();
app.MapHub<RealtimeHub>("/hubs/realtime");
app.MapGet("/health", () => Results.Ok("ok"));

app.Run();

static async Task EnsureDatabaseMigratedAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseMigration");

    const int maxAttempts = 12;
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await dbContext.Database.MigrateAsync();
            return;
        }
        catch (Exception ex) when (attempt < maxAttempts && IsTransientDatabaseError(ex))
        {
            var delay = TimeSpan.FromSeconds(Math.Min(30, 2 * attempt));
            logger.LogWarning(
                ex,
                "Database unavailable (attempt {Attempt}/{MaxAttempts}). Retrying in {DelaySeconds}s.",
                attempt,
                maxAttempts,
                delay.TotalSeconds);
            await Task.Delay(delay);
        }
    }
}

static bool IsTransientDatabaseError(Exception ex)
{
    for (var current = ex; current is not null; current = current.InnerException)
    {
        if (current is Microsoft.Data.SqlClient.SqlException sqlEx &&
            sqlEx.Number is 40613 or -2 or 40197 or 40501 or 49918 or 49919 or 49920)
        {
            return true;
        }
    }

    return false;
}

static async Task EnsureRolesSeededAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

    var roles = new[]
    {
        UserRolesEnum.Admin.ToString(),
        UserRolesEnum.Employee.ToString(),
        UserRolesEnum.Employer.ToString()
    };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole<Guid>(role));
    }
}

static async Task EnsureSubscriptionsSeededAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var billingService = scope.ServiceProvider.GetRequiredService<IBillingService>();
    var billingSettings = scope.ServiceProvider.GetRequiredService<IOptions<BillingSettings>>().Value;

    if (!await dbContext.Subscriptions.AnyAsync(plan => plan.Id == BillingConstants.BasicSubscriptionPlanId))
    {
        var basicPlan = Subscription.Create(
            BillingConstants.BasicSubscriptionPlanId,
            "Basic",
            "Monthly subscription with up to 10 job posts per month.",
            billingSettings.BasicMonthlyPrice,
            BillingConstants.MonthlyDurationDays,
            0,
            PlanKind.Basic).Value;

        await dbContext.Subscriptions.AddAsync(basicPlan);
    }

    if (!await dbContext.Subscriptions.AnyAsync(plan => plan.Id == BillingConstants.UnlimitedSubscriptionPlanId))
    {
        var unlimitedPlan = Subscription.Create(
            BillingConstants.UnlimitedSubscriptionPlanId,
            "Unlimited",
            "Monthly subscription with unlimited active job posts.",
            billingSettings.UnlimitedMonthlyPrice,
            BillingConstants.MonthlyDurationDays,
            0,
            PlanKind.Unlimited).Value;

        await dbContext.Subscriptions.AddAsync(unlimitedPlan);
    }

    await dbContext.SaveChangesAsync();

    var basicPlanEntity = await dbContext.Subscriptions
        .FirstAsync(plan => plan.Id == BillingConstants.BasicSubscriptionPlanId);
    basicPlanEntity.UpdatePlan(
        "Basic",
        "Monthly subscription with up to 10 job posts per month.",
        billingSettings.BasicMonthlyPrice,
        BillingConstants.MonthlyDurationDays,
        0,
        PlanKind.Basic);

    var unlimitedPlanEntity = await dbContext.Subscriptions
        .FirstAsync(plan => plan.Id == BillingConstants.UnlimitedSubscriptionPlanId);
    unlimitedPlanEntity.UpdatePlan(
        "Unlimited",
        "Monthly subscription with unlimited active job posts.",
        billingSettings.UnlimitedMonthlyPrice,
        BillingConstants.MonthlyDurationDays,
        0,
        PlanKind.Unlimited);

    var trialEmployers = await dbContext.Users
        .OfType<Employer>()
        .Where(employer =>
            employer.SubscriptionId == BillingConstants.TrialPlanId ||
            employer.BillingStatus == BillingStatus.Trialing)
        .ToListAsync();

    foreach (var employer in trialEmployers)
    {
        employer.ClearSubscription();
        if (employer.PostCredits <= 0)
            billingService.GrantRegistrationBonus(employer);
    }

    await dbContext.SaveChangesAsync();
}
