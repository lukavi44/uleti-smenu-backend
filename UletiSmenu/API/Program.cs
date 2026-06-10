using API.Hubs;
using API.Middlewares;
using API.Services;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Core.Interfaces;
using Core.Models.Entities;
using Core.Models.Enums;
using Core.Repositories;
using Core.Services;
using Infrastructure.Email;
using Infrastructure.Persistence.Database;
using Infrastructure.Persistence.Database.Repositories;
using Infrastructure.Persistence.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using System.Net;
using System.Net.Mail;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IJobPostRepository, JobPostRepository>();
builder.Services.AddScoped<IApplicationRepository, ApplicationRepository>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();
builder.Services.AddScoped<IWorkExperienceRepository, WorkExperienceRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IRestaurantLocationRepository, RestaurantLocationRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();

builder.Services.AddScoped<RoleManager<IdentityRole<Guid>>>();
builder.Services.AddScoped<IApplicationUnitOfWork, ApplicationUnitOfWork>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IJobPostService, JobPostService>();
builder.Services.AddScoped<IApplicationService, ApplicationService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IEmployeeProfileService, EmployeeProfileService>();
builder.Services.AddScoped<IEmployerProfileService, EmployerProfileService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IReviewReminderService, ReviewReminderService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddSingleton<IRealtimeNotifier, RealtimeNotifier>();
builder.Services.AddSignalR();
builder.Services.AddScoped<IBillingService, BillingService>();

builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"))
    .AddTransient(serviceProvider =>
    {
        var smtpSettings = serviceProvider.GetRequiredService<IOptions<SmtpSettings>>().Value;

        return new SmtpClient
        {
            Host = smtpSettings.Host,
            Port = smtpSettings.Port,
            Credentials = new NetworkCredential(smtpSettings.Username, smtpSettings.Password),
            EnableSsl = smtpSettings.EnableSsl
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
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors("AllowSpecificOrigin");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapIdentityApi<User>();

app.MapControllers();
app.MapHub<RealtimeHub>("/hubs/realtime");

app.Run();

static async Task EnsureDatabaseMigratedAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
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

    var trialPlanExists = await dbContext.Subscriptions
        .AnyAsync(plan => plan.Id == Core.Billing.BillingConstants.TrialPlanId);

    if (!trialPlanExists)
    {
        var trialPlan = Subscription.Create(
            Core.Billing.BillingConstants.TrialPlanId,
            "3-Month Free Trial",
            "Full access to post shifts for the first 3 months.",
            0,
            Core.Billing.BillingConstants.TrialDurationDays,
            0).Value;

        await dbContext.Subscriptions.AddAsync(trialPlan);
    }

    if (!await dbContext.Subscriptions.AnyAsync(plan => plan.Id == Core.Billing.BillingConstants.MonthlyStarterPlanId))
    {
        var monthlyPlan = Subscription.Create(
            Core.Billing.BillingConstants.MonthlyStarterPlanId,
            "Starter Monthly",
            "Unlimited job posts, applicants, chat, and reviews.",
            Core.Billing.BillingConstants.MonthlyStarterPriceRsd,
            30,
            0).Value;

        await dbContext.Subscriptions.AddAsync(monthlyPlan);
    }

    if (!await dbContext.Subscriptions.AnyAsync(plan => plan.Id == Core.Billing.BillingConstants.YearlyStarterPlanId))
    {
        var yearlyPlan = Subscription.Create(
            Core.Billing.BillingConstants.YearlyStarterPlanId,
            "Starter Yearly",
            "Unlimited job posts for one year. Save compared to monthly billing.",
            Core.Billing.BillingConstants.YearlyStarterPriceRsd,
            365,
            0).Value;

        await dbContext.Subscriptions.AddAsync(yearlyPlan);
    }

    await dbContext.SaveChangesAsync();

    var employersWithoutSubscription = await dbContext.Users
        .OfType<Employer>()
        .Where(employer => employer.SubscriptionId == null)
        .ToListAsync();

    if (employersWithoutSubscription.Count == 0)
        return;

    foreach (var employer in employersWithoutSubscription)
    {
        billingService.AssignTrialToEmployer(employer);
    }

    await dbContext.SaveChangesAsync();
}
