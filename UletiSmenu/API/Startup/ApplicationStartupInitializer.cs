using Core.Admin;
using Core.Billing;
using Core.Interfaces;
using Core.Models.Entities;
using Core.Models.Enums;
using Core.Services;
using Infrastructure.Persistence.Database;
using Infrastructure.Persistence.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace API.Startup;

public static class ApplicationStartupInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await EnsureDatabaseMigratedAsync(services, cancellationToken);
        await EnsureGeographySeededAsync(services, cancellationToken);
        await EnsureRolesSeededAsync(services, cancellationToken);
        await EnsureAdminUserSeededAsync(services, cancellationToken);
        await EnsureSubscriptionsSeededAsync(services, cancellationToken);
    }

    private static async Task EnsureDatabaseMigratedAsync(
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseMigration");

        const int maxAttempts = 12;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await dbContext.Database.MigrateAsync(cancellationToken);
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
                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    private static async Task EnsureGeographySeededAsync(
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await GeographyCatalogSeeder.SeedAsync(dbContext, cancellationToken);
    }

    private static async Task EnsureRolesSeededAsync(
        IServiceProvider services,
        CancellationToken cancellationToken)
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
            cancellationToken.ThrowIfCancellationRequested();

            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }
    }

    private static async Task EnsureAdminUserSeededAsync(
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var settings = scope.ServiceProvider.GetRequiredService<IOptions<AdminSeedSettings>>().Value;
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("AdminSeed");

        if (!settings.Enabled)
            return;

        if (string.IsNullOrWhiteSpace(settings.Email) || string.IsNullOrWhiteSpace(settings.Password))
        {
            logger.LogInformation(
                "Admin seed is enabled but Email or Password is missing. Set AdminSeed__Email and AdminSeed__Password.");
            return;
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        var normalizedEmail = settings.Email.Trim();
        var existingUser = await userManager.FindByEmailAsync(normalizedEmail);
        if (existingUser != null)
        {
            if (!await userManager.IsInRoleAsync(existingUser, UserRolesEnum.Admin.ToString()))
                await userManager.AddToRoleAsync(existingUser, UserRolesEnum.Admin.ToString());

            return;
        }

        var adminResult = User.Create(
            Guid.NewGuid(),
            normalizedEmail,
            normalizedEmail,
            settings.PhoneNumber.Trim());

        if (adminResult.IsFailure)
        {
            logger.LogWarning("Admin seed skipped: {Error}", adminResult.Error);
            return;
        }

        var admin = adminResult.Value;
        admin.EmailConfirmed = true;

        var createResult = await userManager.CreateAsync(admin, settings.Password);
        if (!createResult.Succeeded)
        {
            logger.LogWarning(
                "Admin seed failed: {Errors}",
                string.Join(", ", createResult.Errors.Select(error => error.Description)));
            return;
        }

        var roleResult = await userManager.AddToRoleAsync(admin, UserRolesEnum.Admin.ToString());
        if (!roleResult.Succeeded)
        {
            logger.LogWarning(
                "Admin role assignment failed: {Errors}",
                string.Join(", ", roleResult.Errors.Select(error => error.Description)));
            return;
        }

        logger.LogInformation("Admin user seeded for {Email}", normalizedEmail);
    }

    private static async Task EnsureSubscriptionsSeededAsync(
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var billingService = scope.ServiceProvider.GetRequiredService<IBillingService>();
        var billingSettings = scope.ServiceProvider.GetRequiredService<IOptions<BillingSettings>>().Value;

        if (!await dbContext.Subscriptions.AnyAsync(plan => plan.Id == BillingConstants.BasicSubscriptionPlanId, cancellationToken))
        {
            var basicPlan = Subscription.Create(
                BillingConstants.BasicSubscriptionPlanId,
                "Basic",
                "Monthly subscription with up to 10 job posts per month.",
                billingSettings.BasicMonthlyPrice,
                BillingConstants.MonthlyDurationDays,
                0,
                PlanKind.Basic).Value;

            await dbContext.Subscriptions.AddAsync(basicPlan, cancellationToken);
        }

        if (!await dbContext.Subscriptions.AnyAsync(plan => plan.Id == BillingConstants.UnlimitedSubscriptionPlanId, cancellationToken))
        {
            var unlimitedPlan = Subscription.Create(
                BillingConstants.UnlimitedSubscriptionPlanId,
                "Unlimited",
                "Monthly subscription with unlimited active job posts.",
                billingSettings.UnlimitedMonthlyPrice,
                BillingConstants.MonthlyDurationDays,
                0,
                PlanKind.Unlimited).Value;

            await dbContext.Subscriptions.AddAsync(unlimitedPlan, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var basicPlanEntity = await dbContext.Subscriptions
            .FirstAsync(plan => plan.Id == BillingConstants.BasicSubscriptionPlanId, cancellationToken);
        basicPlanEntity.UpdatePlan(
            "Basic",
            "Monthly subscription with up to 10 job posts per month.",
            billingSettings.BasicMonthlyPrice,
            BillingConstants.MonthlyDurationDays,
            0,
            PlanKind.Basic);

        var unlimitedPlanEntity = await dbContext.Subscriptions
            .FirstAsync(plan => plan.Id == BillingConstants.UnlimitedSubscriptionPlanId, cancellationToken);
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
            .ToListAsync(cancellationToken);

        foreach (var employer in trialEmployers)
        {
            employer.ClearSubscription();
            if (employer.PostCredits <= 0)
                billingService.GrantRegistrationBonus(employer);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static bool IsTransientDatabaseError(Exception ex)
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
}
