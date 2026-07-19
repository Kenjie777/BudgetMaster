using System.ComponentModel.DataAnnotations;

namespace BudgetMasterFinal.Models
{
    public class BillingCredit
    {
        public int Id { get; set; }

        public int TenantId { get; set; }
        public Tenant Tenant { get; set; } = null!;

        [Required]
        public decimal Amount { get; set; }

        public decimal UsedAmount { get; set; } = 0;

        public decimal RemainingAmount => Amount - UsedAmount;

        [Required, MaxLength(20)]
        public string CreditType { get; set; } = string.Empty; // Refund, Promotional, Adjustment, Goodwill

        [Required, MaxLength(200)]
        public string Reason { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Notes { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Active"; // Active, Used, Expired, Cancelled

        public DateTime? ExpiresAt { get; set; }

        public string IssuedByUserId { get; set; } = string.Empty;
        public ApplicationUser IssuedByUser { get; set; } = null!;

        public int? BillingTransactionId { get; set; }
        public BillingTransaction? BillingTransaction { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public ICollection<CreditUsage> CreditUsages { get; set; } = new List<CreditUsage>();
    }
}