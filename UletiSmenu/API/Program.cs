using API.Endpoints;
using API.Health;
using API.Hubs;
using API.Middlewares;
using API.Security;
using API.Services;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.DataProtection;
using Core.Admin;
using Core.Billing;
using Core.Interfaces;
using Core.JobPosts;
using Core.Models.Entities;
using Core.Models.Enums;
using Core.Repositories;
using Core.Services;
using Infrastructure.Email;
using Infrastructure.Persistence.Database;
using Infrastructure.Persistence.Database.Repositories;
using Infrastructure.Persistence.Services;
using Infrastructure.Stripe;
using Infrastructure.FileService;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Serilog;
using Swashbuckle.AspNetCore.Filters;
using System.Net;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Production/staging config comes from env vars; avoid inotify file watchers on Render.
if (!builder.Environment.IsDevelopment())
{
    foreach (var source in builder.Configuration.Sources.OfType<JsonConfigurationSource>())
    {
        source.ReloadOnChange = false;
    }
}

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "UletiSmenu.API")
    .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName));

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
builder.Services.AddFileStorage(builder.Configuration);
builder.Services.AddScoped<IJobPostService, JobPostService>();
builder.Services.AddScoped<IApplicationService, ApplicationService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IChatAccessService, ChatAccessService>();
builder.Services.AddScoped<IEmployeeProfileService, EmployeeProfileService>();
builder.Services.AddScoped<IEmployerProfileService, EmployerProfileService>();
builder.Services.AddScoped<IPlatformStatsService, PlatformStatsService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IAccountDeletionService, AccountDeletionService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IReviewReminderService, ReviewReminderService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IGeographyService, GeographyService>();
builder.Services.AddSingleton<IRealtimeNotifier, RealtimeNotifier>();
builder.Services.AddSignalR();
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddScoped<IBillingCheckoutService, BillingCheckoutService>();
builder.Services.AddScoped<IBillingWebhookProcessor, BillingWebhookProcessor>();
builder.Services.AddScoped<IWalletLedgerService, WalletLedgerService>();
builder.Services.Configure<BillingSettings>(builder.Configuration.GetSection(BillingSettings.SectionName));
builder.Services.Configure<AdminSeedSettings>(builder.Configuration.GetSection(AdminSeedSettings.SectionName));
builder.Services.Configure<JobPostLifecycleSettings>(builder.Configuration.GetSection(JobPostLifecycleSettings.SectionName));
builder.Services.AddScoped<IJobPostLifecycleService, JobPostLifecycleService>();
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection(StripeSettings.SectionName));

var stripeEnabled = builder.Configuration.GetValue<bool>($"{StripeSettings.SectionName}:Enabled");
if (stripeEnabled)
    builder.Services.AddScoped<IPaymentProvider, StripePaymentProvider>();
else
    builder.Services.AddScoped<IPaymentProvider, DisabledPaymentProvider>();

builder.Services.AddUletiSmenuEmail(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;

    if (IsRenderProxyEnabled(builder.Configuration))
    {
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    }
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        var endpointPolicy = context.HttpContext
            .GetEndpoint()?
            .Metadata
            .GetMetadata<EnableRateLimitingAttribute>()?
            .PolicyName ?? "global";
        if (context.Lease.TryGetMetadata(
                MetadataName.RetryAfter,
                out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
        }

        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("RateLimit");
        logger.LogWarning(
            "Rate limit rejected request. Policy={Policy} Method={Method} Path={Path} UserId={UserId} ClientIp={ClientIp} TraceId={TraceId}",
            endpointPolicy,
            context.HttpContext.Request.Method,
            context.HttpContext.Request.Path,
            context.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            context.HttpContext.Connection.RemoteIpAddress,
            context.HttpContext.TraceIdentifier);

        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(
            new
            {
                message = "Too many requests. Please try again later.",
                traceId = context.HttpContext.TraceIdentifier
            },
            cancellationToken);
    };

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        httpContext => RateLimitPartition.GetTokenBucketLimiter(
            GetRateLimitPartitionKey(httpContext),
            _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 120,
                TokensPerPeriod = 120,
                ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy(
        RateLimitPolicies.Identity,
        httpContext => RateLimitPartition.GetFixedWindowLimiter(
            $"{GetRateLimitPartitionKey(httpContext)}:{GetIdentityRateLimitBucket(httpContext)}",
            partitionKey => new FixedWindowRateLimiterOptions
            {
                PermitLimit = partitionKey.EndsWith(":login", StringComparison.Ordinal)
                    ? 5
                    : 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy(
        RateLimitPolicies.Registration,
        httpContext => RateLimitPartition.GetFixedWindowLimiter(
            GetRateLimitPartitionKey(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy(
        RateLimitPolicies.PasswordRecovery,
        httpContext => RateLimitPartition.GetFixedWindowLimiter(
            GetRateLimitPartitionKey(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(15),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy(
        RateLimitPolicies.ProfileUpload,
        httpContext => RateLimitPartition.GetFixedWindowLimiter(
            GetRateLimitPartitionKey(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy(
        RateLimitPolicies.Contact,
        httpContext => RateLimitPartition.GetFixedWindowLimiter(
            GetRateLimitPartitionKey(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});
builder.Services.AddHostedService<ApplicationStartupHostedService>();
builder.Services.AddHostedService<JobPostLifecycleHostedService>();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database");
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
        connectionString: builder.Configuration["ConnectionStrings:UletiSmenu"],
        sqlServerOptionsAction: SqlServerTransientRetry.Configure
    )
);

builder.Services.AddDataProtection()
    .PersistKeysToDbContext<ApplicationDbContext>();

builder.Services.AddIdentity<User, IdentityRole<Guid>>(options =>
{
    options.SignIn.RequireConfirmedEmail = false;
    options.User.RequireUniqueEmail = true;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 10;
    options.Password.RequiredUniqueChars = 4;
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
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
    options.BearerTokenExpiration = TimeSpan.FromHours(1);
    options.RefreshTokenExpiration = TimeSpan.FromDays(14);
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

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        IdentityEndpointSecurity.DisabledRegistrationPolicy,
        policy => policy.RequireAssertion(_ => false));
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

SmtpConfigurationGuard.ValidateAtStartup(app.Environment, app.Services);

if (app.Environment.IsDevelopment())
{
    var connectionString = app.Configuration["ConnectionStrings:UletiSmenu"];
    app.Logger.LogInformation("Development database target: {DatabaseTarget}", DescribeDatabaseTarget(connectionString));
}

var fileSettings = app.Configuration
    .GetSection(FileSettings.SectionName)
    .Get<FileSettings>() ?? new FileSettings();

if (!fileSettings.UseAzureBlob)
{
    var uploadPath = fileSettings.UploadPath
        ?? Path.Combine(app.Environment.ContentRootPath, "uploads");
    Directory.CreateDirectory(uploadPath);

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(uploadPath),
        RequestPath = "/uploads"
    });
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (IsRenderProxyEnabled(app.Configuration))
    app.UseForwardedHeaders();

app.UseMiddleware<SecurityHeadersMiddleware>();

app.UseRouting();
app.UseCors("AllowSpecificOrigin");

app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("UserId", httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
    };
});

if (app.Environment.IsProduction())
    app.UseHttpsRedirection();

app.UseAuthentication();
app.UseRateLimiter();
app.UseMiddleware<AuthenticationAuditMiddleware>();
app.UseAuthorization();

app.UseMiddleware<ExceptionHandlingMiddleware>();

var identityApi = app.MapIdentityApi<User>();
identityApi.Add(IdentityEndpointSecurity.Apply);
identityApi.RequireRateLimiting(RateLimitPolicies.Identity);

app.MapControllers();
app.MapUploads(fileSettings);
app.MapHub<RealtimeHub>("/hubs/realtime");
app.MapLivenessHealth()
    .DisableRateLimiting();
app.MapHealthChecks("/health/ready")
    .DisableRateLimiting();

try
{
    app.Run();
}
finally
{
    Log.CloseAndFlush();
}

static string GetRateLimitPartitionKey(HttpContext context)
{
    var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (!string.IsNullOrWhiteSpace(userId))
        return $"user:{userId}";

    return $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
}

static string GetIdentityRateLimitBucket(HttpContext context) =>
    context.Request.Path.Value?.ToLowerInvariant() switch
    {
        "/login" => "login",
        "/refresh" => "refresh",
        _ => "account"
    };

static bool IsRenderProxyEnabled(IConfiguration configuration)
{
    var provider = configuration["Proxy:Provider"];
    if (string.IsNullOrWhiteSpace(provider) ||
        provider.Equals("None", StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

    if (!provider.Equals("Render", StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException($"Unsupported proxy provider '{provider}'.");

    if (!string.Equals(
            Environment.GetEnvironmentVariable("RENDER"),
            "true",
            StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException(
            "Proxy provider is Render, but the Render runtime marker is missing. " +
            "Refusing to trust forwarded headers.");
    }

    return true;
}
