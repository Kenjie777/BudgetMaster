using BudgetMasterFinal.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace BudgetMasterFinal.Models
{
    public class BillingTransaction : IArchivable
    {
        public int Id { get; set; }

        public int TenantId { get; set; }
        public Tenant Tenant { get; set; } = null!;

        public int SubscriptionId { get; set; }
        public Subscription Subscription { get; set; } = null!;

        [Required, MaxLength(50)]
        public string TransactionType { get; set; } = string.Empty; // Payment, Refund, Credit, Discount

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public decimal NetAmount { get; set; } // After discounts/credits

        [MaxLength(10)]
        public string Currency { get; set; } = "PHP";

        [Required, MaxLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Completed, Failed, Cancelled

        [MaxLength(100)]
        public string? PaymentMethod { get; set; } // Credit Card, Bank Transfer, etc.

        [MaxLength(200)]
        public string? PaymentReference { get; set; }

        [MaxLength(200)]
        public string? ExternalTransactionId { get; set; } // Payment gateway ID

        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        [MaxLength(1000)]
        public string? FailureReason { get; set; }

        public string? ProcessedByUserId { get; set; }
        public ApplicationUser? ProcessedByUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Archive functionality
        public bool IsArchived { get; set; } = false;
        public DateTime? ArchivedAt { get; set; }
        public string? ArchivedBy { get; set; }
        public string? ArchiveReason { get; set; }

        // Navigation
        public ICollection<BillingCredit> Credits { get; set; } = new List<BillingCredit>();
        public ICollection<DiscountUsage> DiscountUsages { get; set; } = new List<DiscountUsage>();
    }
}