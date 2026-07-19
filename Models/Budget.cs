using BudgetMasterFinal.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BudgetMasterFinal.Models
{
    public class Budget : IArchivable
    {
        public int Id { get; set; }

        public int TenantId { get; set; }
        public Tenant Tenant { get; set; } = null!;

        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        // ── BUDGET PLANNING DATA ──────────────────────────────────────
        [Required]
        public decimal AllocatedAmount { get; set; }

        // ── COMMITTED BUDGET USAGE (from approved requests) ───────────
        // Represents approved/committed budget requests that consume allocated funds
        public decimal UsedAmount { get; set; } = 0;

        // ── ACTUAL OPERATIONAL SPENDING (from expense tracking) ───────
        // Represents real expenses recorded against this budget
        public decimal ActualSpend { get; set; } = 0;

        // ── CALCULATED METRICS ────────────────────────────────────────
        [NotMapped]
        public decimal RemainingAmount => AllocatedAmount - UsedAmount;

        [NotMapped]
        public decimal UsagePercentage => AllocatedAmount > 0 ? (UsedAmount / AllocatedAmount) * 100 : 0;

        [NotMapped]
        public decimal VarianceAmount => AllocatedAmount - ActualSpend;

        [NotMapped]
        public decimal VariancePercentage => AllocatedAmount > 0 ? ((AllocatedAmount - ActualSpend) / AllocatedAmount) * 100 : 0;

        [Required]
        public int FiscalYear { get; set; }

        [MaxLength(20)]
        public string? Quarter { get; set; } // Q1, Q2, Q3, Q4

        [MaxLength(20)]
        public string Status { get; set; } = "Draft"; // Draft, Active, Closed

        public string? CreatedByUserId { get; set; }
        public ApplicationUser? CreatedByUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Archive functionality
        public bool IsArchived { get; set; } = false;
        public DateTime? ArchivedAt { get; set; }
        public string? ArchivedBy { get; set; }
        public string? ArchiveReason { get; set; }

        // Navigation
        public ICollection<BudgetApproval> Approvals { get; set; } = new List<BudgetApproval>();
        public ICollection<BudgetRequest> BudgetRequests { get; set; } = new List<BudgetRequest>();
        public ICollection<ActualTransaction> ActualTransactions { get; set; } = new List<ActualTransaction>();
    }
}
