using System.ComponentModel.DataAnnotations;

namespace BudgetMasterFinal.Models
{
    public class CreditUsage
    {
        public int Id { get; set; }

        public int BillingCreditId { get; set; }
        public BillingCredit BillingCredit { get; set; } = null!;

        public int BillingTransactionId { get; set; }
        public BillingTransaction BillingTransaction { get; set; } = null!;

        [Required]
        public decimal AmountUsed { get; set; }

        public DateTime UsedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(200)]
        public string? Notes { get; set; }
    }
}