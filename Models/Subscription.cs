using BudgetMasterFinal.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace BudgetMasterFinal.Models
{
    public class Subscription : IArchivable
    {
        public int Id { get; set; }

        public int TenantId { get; set; }
        public Tenant Tenant { get; set; } = null!;

        public int SubscriptionPlanId { get; set; }
        public SubscriptionPlan SubscriptionPlan { get; set; } = null!;

        [Required, MaxLength(20)]
        public string BillingCycle { get; set; } = "Monthly"; // Monthly, Yearly, Usage

        public decimal PriceAmount { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Active"; // Active, Suspended, Cancelled, Trial, PastDue

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime? TrialEndDate { get; set; }
        public DateTime? NextBillingDate { get; set; }

        // Usage tracking for usage-based billing
        public int CurrentUsers { get; set; } = 0;
        public int CurrentDepartments { get; set; } = 0;
        public int CurrentTransactions { get; set; } = 0;

        // Billing details
        public decimal? DiscountAmount { get; set; }
        public decimal? CreditBalance { get; set; } = 0;
        public int? BillingDiscountId { get; set; }
        public BillingDiscount? BillingDiscount { get; set; }

        [MaxLength(200)]
        public string? PaymentReference { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        // Suspension details
        public DateTime? SuspendedAt { get; set; }
        public string? SuspensionReason { get; set; }
        public string? SuspendedByUserId { get; set; }
        public ApplicationUser? SuspendedByUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Archive functionality
        public bool IsArchived { get; set; } = false;
        public DateTime? ArchivedAt { get; set; }
        public string? ArchivedBy { get; set; }
        public string? ArchiveReason { get; set; }

        // Navigation
        public ICollection<BillingTransaction> BillingTransactions { get; set; } = new List<BillingTransaction>();
        public ICollection<BillingCredit> BillingCredits { get; set; } = new List<BillingCredit>();

        // Computed properties
        public bool IsActive => Status == "Active";
        public bool IsTrial => Status == "Trial" && TrialEndDate > DateTime.UtcNow;
        public bool IsExpired => EndDate < DateTime.UtcNow;
        public int DaysUntilExpiry => (EndDate - DateTime.UtcNow).Days;
    }
}