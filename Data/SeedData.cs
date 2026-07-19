using BudgetMasterFinal.Models;
using Microsoft.AspNetCore.Identity;

namespace BudgetMasterFinal.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // Ensure DB
            context.Database.EnsureCreated();

            // Seed Roles
            string[] roles = { "SuperAdmin", "CompanyAdmin", "Tenant", "FinanceManager", "Accountant", "DepartmentHead", "Auditor", "Employee" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // Seed SuperAdmin
            const string superAdminEmail = "superadmin@budgetmaster.com";
            if (await userManager.FindByEmailAsync(superAdminEmail) == null)
            {
                var superAdmin = new ApplicationUser
                {
                    UserName = superAdminEmail,
                    Email = superAdminEmail,
                    FirstName = "Super",
                    LastName = "Admin",
                    Role = "SuperAdmin",
                    IsActive = true,
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(superAdmin, "Admin@123");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");
            }

            // Seed Subscription Plans
            if (!context.SubscriptionPlans.Any())
            {
                var plans = new[]
                {
                    new SubscriptionPlan
                    {
                        Name = "Starter",
                        Description = "Perfect for small businesses getting started with budget management",
                        MonthlyPrice = 99m,
                        YearlyDiscountPercentage = 20m,
                        YearlyPrice = 950.40m, // Auto-calculated: 99 * 12 * 0.8
                        UsageBasedPrice = 0m,
                        BillingModel = "Fixed",
                        MaxUsers = 5,
                        MaxDepartments = 3,
                        IncludesForecasting = true,
                        IncludesScenarioPlanning = false,
                        IncludesAdvancedReports = false,
                        IncludesApiAccess = false,
                        IncludesCustomBranding = false,
                        IncludesPrioritySupport = false,
                        Status = "Active",
                        IsVisible = true,
                        SortOrder = 1
                    },
                    new SubscriptionPlan
                    {
                        Name = "Professional",
                        Description = "Comprehensive solution for growing businesses with advanced features",
                        MonthlyPrice = 299m,
                        YearlyDiscountPercentage = 20m,
                        YearlyPrice = 2870.40m, // Auto-calculated: 299 * 12 * 0.8
                        UsageBasedPrice = 0m,
                        BillingModel = "Fixed",
                        MaxUsers = 25,
                        MaxDepartments = 10,
                        IncludesForecasting = true,
                        IncludesScenarioPlanning = true,
                        IncludesAdvancedReports = true,
                        IncludesApiAccess = false,
                        IncludesCustomBranding = false,
                        IncludesPrioritySupport = true,
                        Status = "Active",
                        IsVisible = true,
                        SortOrder = 2
                    },
                    new SubscriptionPlan
                    {
                        Name = "Enterprise",
                        Description = "Full-featured solution for large organizations with unlimited access",
                        MonthlyPrice = 999m,
                        YearlyDiscountPercentage = 20m,
                        YearlyPrice = 9590.40m, // Auto-calculated: 999 * 12 * 0.8
                        UsageBasedPrice = 0m,
                        BillingModel = "Fixed",
                        MaxUsers = -1, // Unlimited
                        MaxDepartments = -1, // Unlimited
                        IncludesForecasting = true,
                        IncludesScenarioPlanning = true,
                        IncludesAdvancedReports = true,
                        IncludesApiAccess = true,
                        IncludesCustomBranding = true,
                        IncludesPrioritySupport = true,
                        Status = "Active",
                        IsVisible = true,
                        SortOrder = 3
                    }
                };
                context.SubscriptionPlans.AddRange(plans);
                await context.SaveChangesAsync();
            }

            // No demo tenants or admin users will be created
            // Only SuperAdmin and subscription plans are seeded
            // Tenants will create their own Admin users during subscription registration

            // Seed system settings
            if (!context.SystemSettings.Any())
            {
                var settingsList = new[]
                {
                    new SystemSetting { Key = "AppName", Value = "BudgetMaster", Group = "General", Description = "Application Name" },
                    new SystemSetting { Key = "SupportEmail", Value = "support@budgetmaster.com", Group = "General" },
                    new SystemSetting { Key = "MaxLoginAttempts", Value = "5", Group = "Security" },
                    new SystemSetting { Key = "FiscalYearStart", Value = "January", Group = "Finance" }
                };
                context.SystemSettings.AddRange(settingsList);
                await context.SaveChangesAsync();
            }
        }
    }
}
