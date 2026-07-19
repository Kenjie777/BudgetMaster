using BudgetMasterFinal.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BudgetMasterFinal.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // Multi-Tenant SaaS
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<PlanFeature> PlanFeatures { get; set; }
        public DbSet<BillingTransaction> BillingTransactions { get; set; }
        public DbSet<BillingCredit> BillingCredits { get; set; }
        public DbSet<CreditUsage> CreditUsages { get; set; }
        public DbSet<BillingDiscount> BillingDiscounts { get; set; }
        public DbSet<DiscountUsage> DiscountUsages { get; set; }

        // ERP Financial Modules
        public DbSet<Department> Departments { get; set; }
        public DbSet<Budget> Budgets { get; set; }
        public DbSet<BudgetRequest> BudgetRequests { get; set; }
        public DbSet<BudgetApproval> BudgetApprovals { get; set; }
        public DbSet<Forecast> Forecasts { get; set; }
        public DbSet<ScenarioPlan> ScenarioPlans { get; set; }
        public DbSet<ActualTransaction> ActualTransactions { get; set; }

        // System
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<ArchivedItem> ArchivedItems { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Rename ASP.NET Identity tables
            builder.Entity<ApplicationUser>().ToTable("Users");
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityRole>().ToTable("Roles");
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>().ToTable("UserRoles");
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<string>>().ToTable("UserClaims");
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>().ToTable("UserLogins");
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>>().ToTable("RoleClaims");
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>().ToTable("UserTokens");

            // Tenant - configure all child cascade behaviors here to avoid SQL Server cycle errors
            builder.Entity<Tenant>(e =>
            {
                e.HasIndex(t => t.CompanyName);
                e.HasMany(t => t.Users).WithOne(u => u.Tenant).HasForeignKey(u => u.TenantId).OnDelete(DeleteBehavior.Restrict);
                e.HasMany(t => t.Departments).WithOne(d => d.Tenant).HasForeignKey(d => d.TenantId).OnDelete(DeleteBehavior.Cascade);
                // NoAction on these to avoid cycles: Tenant->Dept->Budget AND Tenant->Budget
                e.HasMany(t => t.Budgets).WithOne(b => b.Tenant).HasForeignKey(b => b.TenantId).OnDelete(DeleteBehavior.NoAction);
                e.HasMany(t => t.BudgetRequests).WithOne(br => br.Tenant).HasForeignKey(br => br.TenantId).OnDelete(DeleteBehavior.NoAction);
                e.HasMany(t => t.ActualTransactions).WithOne(at => at.Tenant).HasForeignKey(at => at.TenantId).OnDelete(DeleteBehavior.NoAction);
                e.HasMany(t => t.Forecasts).WithOne(f => f.Tenant).HasForeignKey(f => f.TenantId).OnDelete(DeleteBehavior.Restrict);
                e.HasMany(t => t.ScenarioPlans).WithOne(s => s.Tenant).HasForeignKey(s => s.TenantId).OnDelete(DeleteBehavior.Restrict);
                e.HasMany(t => t.Notifications).WithOne(n => n.Tenant).HasForeignKey(n => n.TenantId).OnDelete(DeleteBehavior.SetNull);
                e.HasOne(t => t.Subscription).WithOne(s => s.Tenant).HasForeignKey<Subscription>(s => s.TenantId).OnDelete(DeleteBehavior.Cascade);
            });

            // Department
            builder.Entity<Department>(e =>
            {
                e.Property(d => d.BudgetAllocation).HasPrecision(18, 2);
                e.HasIndex(d => new { d.TenantId, d.Name });
                e.HasMany(d => d.Budgets).WithOne(b => b.Department).HasForeignKey(b => b.DepartmentId).OnDelete(DeleteBehavior.SetNull);
                e.HasMany(d => d.BudgetRequests).WithOne(br => br.Department).HasForeignKey(br => br.DepartmentId).OnDelete(DeleteBehavior.SetNull);
                e.HasMany(d => d.ActualTransactions).WithOne(at => at.Department).HasForeignKey(at => at.DepartmentId).OnDelete(DeleteBehavior.SetNull);
            });

            // Budget - TenantId NoAction configured via Tenant entity
            builder.Entity<Budget>(e =>
            {
                e.Property(b => b.AllocatedAmount).HasPrecision(18, 2);
                e.Property(b => b.UsedAmount).HasPrecision(18, 2);
                e.HasMany(b => b.Approvals).WithOne(a => a.Budget).HasForeignKey(a => a.BudgetId).OnDelete(DeleteBehavior.SetNull);
                e.HasOne(b => b.CreatedByUser).WithMany().HasForeignKey(b => b.CreatedByUserId).OnDelete(DeleteBehavior.SetNull);
            });

            // BudgetRequest - TenantId NoAction configured via Tenant entity
            builder.Entity<BudgetRequest>(e =>
            {
                e.Property(br => br.RequestedAmount).HasPrecision(18, 2);
                e.HasOne(br => br.RequestedByUser).WithMany().HasForeignKey(br => br.RequestedByUserId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(br => br.ReviewedByUser).WithMany().HasForeignKey(br => br.ReviewedByUserId).OnDelete(DeleteBehavior.SetNull);
                
                // Two-stage approval relationships - use NoAction to avoid cascade cycles
                e.HasOne(br => br.OperationallyApprovedByUser).WithMany().HasForeignKey(br => br.OperationallyApprovedByUserId).OnDelete(DeleteBehavior.NoAction);
                e.HasOne(br => br.FinanciallyApprovedByUser).WithMany().HasForeignKey(br => br.FinanciallyApprovedByUserId).OnDelete(DeleteBehavior.NoAction);
                e.HasOne(br => br.FinanciallyRejectedByUser).WithMany().HasForeignKey(br => br.FinanciallyRejectedByUserId).OnDelete(DeleteBehavior.NoAction);
                
                e.HasMany(br => br.Approvals).WithOne(a => a.BudgetRequest).HasForeignKey(a => a.BudgetRequestId).OnDelete(DeleteBehavior.Cascade);
            });

            // BudgetApproval
            builder.Entity<BudgetApproval>(e =>
            {
                e.HasOne(a => a.ApprovedByUser).WithMany().HasForeignKey(a => a.ApprovedByUserId).OnDelete(DeleteBehavior.Restrict);
            });

            // Forecast
            builder.Entity<Forecast>(e =>
            {
                e.Property(f => f.ProjectedRevenue).HasPrecision(18, 2);
                e.Property(f => f.ProjectedExpenses).HasPrecision(18, 2);
                e.Property(f => f.GrowthRatePercent).HasPrecision(8, 4);
                e.Property(f => f.ConfidencePercent).HasPrecision(8, 4);
                e.HasOne(f => f.CreatedByUser).WithMany().HasForeignKey(f => f.CreatedByUserId).OnDelete(DeleteBehavior.SetNull);
                e.Ignore(f => f.ProjectedNetIncome);
            });

            // ScenarioPlan
            builder.Entity<ScenarioPlan>(e =>
            {
                e.Property(s => s.BaseRevenue).HasPrecision(18, 2);
                e.Property(s => s.BaseExpenses).HasPrecision(18, 2);
                e.Property(s => s.RevenueAdjustmentPercent).HasPrecision(8, 4);
                e.Property(s => s.ExpenseAdjustmentPercent).HasPrecision(8, 4);
                e.Ignore(s => s.AdjustedRevenue);
                e.Ignore(s => s.AdjustedExpenses);
                e.Ignore(s => s.NetImpact);
                e.HasOne(s => s.CreatedByUser).WithMany().HasForeignKey(s => s.CreatedByUserId).OnDelete(DeleteBehavior.SetNull);
            });

            // ActualTransaction - TenantId NoAction configured via Tenant entity
            builder.Entity<ActualTransaction>(e =>
            {
                e.Property(at => at.Amount).HasPrecision(18, 2);
                e.HasIndex(at => new { at.TenantId, at.FiscalYear, at.TransactionDate });
                e.HasOne(at => at.CreatedByUser).WithMany().HasForeignKey(at => at.CreatedByUserId).OnDelete(DeleteBehavior.SetNull);
            });

            // Subscription
            builder.Entity<Subscription>(e =>
            {
                e.Property(s => s.PriceAmount).HasPrecision(18, 2);
                e.Property(s => s.DiscountAmount).HasPrecision(18, 2);
                e.Property(s => s.CreditBalance).HasPrecision(18, 2);
                e.HasIndex(s => new { s.TenantId, s.Status });
                e.HasOne(s => s.SubscriptionPlan).WithMany(p => p.Subscriptions).HasForeignKey(s => s.SubscriptionPlanId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(s => s.BillingDiscount).WithMany().HasForeignKey(s => s.BillingDiscountId).OnDelete(DeleteBehavior.SetNull);
                e.HasOne(s => s.SuspendedByUser).WithMany().HasForeignKey(s => s.SuspendedByUserId).OnDelete(DeleteBehavior.SetNull);
            });

            // SubscriptionPlan
            builder.Entity<SubscriptionPlan>(e =>
            {
                e.Property(p => p.MonthlyPrice).HasPrecision(18, 2);
                e.Property(p => p.YearlyPrice).HasPrecision(18, 2);
                e.Property(p => p.UsageBasedPrice).HasPrecision(18, 2);
                e.HasIndex(p => p.Name).IsUnique();
            });

            // PlanFeature
            builder.Entity<PlanFeature>(e =>
            {
                e.HasOne(f => f.SubscriptionPlan).WithMany(p => p.Features).HasForeignKey(f => f.SubscriptionPlanId).OnDelete(DeleteBehavior.Cascade);
            });

            // BillingTransaction
            builder.Entity<BillingTransaction>(e =>
            {
                e.Property(t => t.Amount).HasPrecision(18, 2);
                e.Property(t => t.NetAmount).HasPrecision(18, 2);
                e.HasIndex(t => new { t.TenantId, t.Status, t.TransactionDate });
                e.HasOne(t => t.Tenant).WithMany().HasForeignKey(t => t.TenantId).OnDelete(DeleteBehavior.NoAction);
                e.HasOne(t => t.Subscription).WithMany(s => s.BillingTransactions).HasForeignKey(t => t.SubscriptionId).OnDelete(DeleteBehavior.NoAction);
                e.HasOne(t => t.ProcessedByUser).WithMany().HasForeignKey(t => t.ProcessedByUserId).OnDelete(DeleteBehavior.SetNull);
            });

            // BillingCredit
            builder.Entity<BillingCredit>(e =>
            {
                e.Property(c => c.Amount).HasPrecision(18, 2);
                e.Property(c => c.UsedAmount).HasPrecision(18, 2);
                e.HasOne(c => c.IssuedByUser).WithMany().HasForeignKey(c => c.IssuedByUserId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(c => c.BillingTransaction).WithMany(t => t.Credits).HasForeignKey(c => c.BillingTransactionId).OnDelete(DeleteBehavior.SetNull);
            });

            // CreditUsage
            builder.Entity<CreditUsage>(e =>
            {
                e.Property(u => u.AmountUsed).HasPrecision(18, 2);
                e.HasOne(u => u.BillingCredit).WithMany(c => c.CreditUsages).HasForeignKey(u => u.BillingCreditId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(u => u.BillingTransaction).WithMany().HasForeignKey(u => u.BillingTransactionId).OnDelete(DeleteBehavior.Restrict);
            });

            // BillingDiscount
            builder.Entity<BillingDiscount>(e =>
            {
                e.Property(d => d.DiscountValue).HasPrecision(18, 2);
                e.Property(d => d.MinimumOrderAmount).HasPrecision(18, 2);
                e.HasIndex(d => d.Code).IsUnique();
                e.HasOne(d => d.CreatedByUser).WithMany().HasForeignKey(d => d.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            });

            // DiscountUsage
            builder.Entity<DiscountUsage>(e =>
            {
                e.Property(u => u.DiscountAmount).HasPrecision(18, 2);
                e.HasOne(u => u.BillingDiscount).WithMany(d => d.DiscountUsages).HasForeignKey(u => u.BillingDiscountId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(u => u.BillingTransaction).WithMany(t => t.DiscountUsages).HasForeignKey(u => u.BillingTransactionId).OnDelete(DeleteBehavior.Restrict);
            });

            // Budget ignore computed
            builder.Entity<Budget>().Ignore(b => b.RemainingAmount);

            // ApplicationUser
            builder.Entity<ApplicationUser>(e =>
            {
                e.Ignore(u => u.FullName);
            });
        }
    }
}
