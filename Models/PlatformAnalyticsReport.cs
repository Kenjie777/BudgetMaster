namespace BudgetMasterFinal.Models
{
    public class PlatformAnalyticsReport
    {
        // Tenant Metrics
        public int TotalTenants { get; set; }
        public int ActiveTenants { get; set; }
        public int InactiveTenants { get; set; }
        public int NewTenantsThisMonth { get; set; }
        public decimal TenantGrowthRate { get; set; }

        // User Metrics
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int NewUsersThisMonth { get; set; }
        public decimal UserGrowthRate { get; set; }

        // Subscription Metrics
        public int TotalSubscriptions { get; set; }
        public int ActiveSubscriptions { get; set; }
        public int TrialSubscriptions { get; set; }
        public int ExpiredSubscriptions { get; set; }
        public int ExpiringThisMonth { get; set; }

        // Revenue Metrics
        public decimal MonthlyRecurringRevenue { get; set; }
        public decimal AnnualRecurringRevenue { get; set; }
        public decimal TotalRevenueThisMonth { get; set; }
        public decimal TotalRevenueThisYear { get; set; }
        public decimal AverageRevenuePerTenant { get; set; }

        // Plan Distribution
        public Dictionary<string, int> SubscriptionsByPlan { get; set; } = new();
        public Dictionary<string, decimal> RevenueByPlan { get; set; } = new();

        // System Usage
        public int TotalLogins { get; set; }
        public int LoginsThisMonth { get; set; }
        public int FailedLoginsThisMonth { get; set; }
        public Dictionary<string, int> LoginsByRole { get; set; } = new();

        // Audit Metrics
        public int TotalAuditLogs { get; set; }
        public int AuditLogsThisMonth { get; set; }
        public Dictionary<string, int> AuditLogsByAction { get; set; } = new();

        // Report Metadata
        public DateTime GeneratedAt { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? FilteredByPlan { get; set; }
        public string? FilteredByStatus { get; set; }
    }

    public class TenantGrowthMetric
    {
        public string Period { get; set; } = string.Empty;
        public int NewTenants { get; set; }
        public int TotalTenants { get; set; }
        public decimal GrowthRate { get; set; }
    }

    public class RevenueMetric
    {
        public string Period { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int Subscriptions { get; set; }
        public decimal AveragePerSubscription { get; set; }
    }

    public class SystemUsageMetric
    {
        public string Date { get; set; } = string.Empty;
        public int Logins { get; set; }
        public int ActiveUsers { get; set; }
        public int FailedLogins { get; set; }
    }

    public class AuditLogSummary
    {
        public string Action { get; set; } = string.Empty;
        public int Count { get; set; }
        public DateTime LastOccurrence { get; set; }
    }
}
