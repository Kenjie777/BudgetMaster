using BudgetMasterFinal.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace BudgetMasterFinal.Models
{
    /// <summary>
    /// Represents actual operational spending/expense tracking.
    /// This is NOT a full accounting ledger system - it tracks real expenses
    /// against approved budgets for variance analysis and financial oversight.
    /// </summary>
    public class ActualTransaction : IArchivable
    {
        public int Id { get; set; }

        public int TenantId { get; set; }
        public Tenant Tenant { get; set; } = null!;

        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }

        public int? BudgetId { get; set; }
        public Budget? Budget { get; set; }

        [Required, MaxLength(200)]
        public string Description { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Category { get; set; } = string.Empty; // Operational, Capital, Administrative

        [Required, MaxLength(20)]
        public string TransactionType { get; set; } = "Expense"; // Expense, Revenue

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public DateTime TransactionDate { get; set; }

        public int FiscalYear { get; set; }

        [MaxLength(20)]
        public string? Quarter { get; set; }

        [MaxLength(100)]
        public string? ReferenceNumber { get; set; } // Invoice/Receipt number

        [MaxLength(500)]
        public string? Notes { get; set; }

        public string? CreatedByUserId { get; set; }
        public ApplicationUser? CreatedByUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Archive functionality
        public bool IsArchived { get; set; } = false;
        public DateTime? ArchivedAt { get; set; }
        public string? ArchivedBy { get; set; }
        public string? ArchiveReason { get; set; }
    }
}
