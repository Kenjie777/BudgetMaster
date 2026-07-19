using BudgetMasterFinal.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace BudgetMasterFinal.Models
{
    public class BillingDiscount : IArchivable
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty; // Coupon code

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required, MaxLength(20)]
        public string DiscountType { get; set; } = string.Empty; // Percentage, Fixed, FreeTrial

        [Required]
        public decimal DiscountValue { get; set; } // Percentage (0-100) or Fixed amount

        public int? MaxUsageCount { get; set; } // Null = unlimited
        public int CurrentUsageCount { get; set; } = 0;

        public int? MaxUsagePerTenant { get; set; } // Null = unlimited per tenant

        public decimal? MinimumOrderAmount { get; set; } // Minimum order amount to apply discount

        [MaxLength(20)]
        public string ApplicableTo { get; set; } = "All"; // All, SpecificPlans, NewCustomers

        public string? ApplicablePlanIds { get; set; } // JSON array of plan IDs

        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidUntil { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Active"; // Active, Inactive, Expired

        public bool IsVisible { get; set; } = true; // Show in public promotions

        public string CreatedByUserId { get; set; } = string.Empty;
        public ApplicationUser CreatedByUser { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Archive functionality
        public bool IsArchived { get; set; } = false;
        public DateTime? ArchivedAt { get; set; }
        public string? ArchivedBy { get; set; }
        public string? ArchiveReason { get; set; }

        // Navigation
        public ICollection<DiscountUsage> DiscountUsages { get; set; } = new List<DiscountUsage>();
        public ICollection<BillingTransaction> BillingTransactions { get; set; } = new List<BillingTransaction>();
    }
}