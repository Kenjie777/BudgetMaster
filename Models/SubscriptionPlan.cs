using BudgetMasterFinal.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace BudgetMasterFinal.Models
{
    public class SubscriptionPlan : IArchivable
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty; // Starter, Professional, Enterprise

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        public decimal MonthlyPrice { get; set; }

        [Required]
        public decimal YearlyPrice { get; set; }

        // Yearly discount percentage (e.g., 20 for 20% discount)
        [Range(0, 100)]
        public decimal YearlyDiscountPercentage { get; set; } = 20;

        public decimal? UsageBasedPrice { get; set; } // Per user/transaction

        [MaxLength(20)]
        public string BillingModel { get; set; } = "Fixed"; // Fixed, Usage, Hybrid

        // Feature Limits
        public int MaxUsers { get; set; } = 5;
        public int MaxDepartments { get; set; } = 3;

        // Feature Access
        public bool IncludesForecasting { get; set; } = true;
        public bool IncludesScenarioPlanning { get; set; } = false;
        public bool IncludesAdvancedReports { get; set; } = false;
        public bool IncludesApiAccess { get; set; } = false;
        public bool IncludesCustomBranding { get; set; } = false;
        public bool IncludesPrioritySupport { get; set; } = false;

        [MaxLength(20)]
        public string Status { get; set; } = "Active"; // Active, Inactive, Deprecated

        public bool IsVisible { get; set; } = true; // Show in public pricing
        public int SortOrder { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Archive functionality
        public bool IsArchived { get; set; } = false;
        public DateTime? ArchivedAt { get; set; }
        public string? ArchivedBy { get; set; }
        public string? ArchiveReason { get; set; }

        // Navigation
        public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
        public ICollection<PlanFeature> Features { get; set; } = new List<PlanFeature>();
    }
}