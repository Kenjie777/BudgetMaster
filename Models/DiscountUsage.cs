using System.ComponentModel.DataAnnotations;

namespace BudgetMasterFinal.Models
{
    public class DiscountUsage
    {
        public int Id { get; set; }

        public int BillingDiscountId { get; set; }
        public BillingDiscount BillingDiscount { get; set; } = null!;

        public int TenantId { get; set; }
        public Tenant Tenant { get; set; } = null!;

        public int BillingTransactionId { get; set; }
        public BillingTransaction BillingTransaction { get; set; } = null!;

        [Required]
        public decimal DiscountAmount { get; set; }

        public DateTime UsedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(200)]
        public string? Notes { get; set; }
    }
}