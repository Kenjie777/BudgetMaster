using BudgetMasterFinal.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace BudgetMasterFinal.Models
{
    public class ScenarioPlan : IArchivable
    {
        public int Id { get; set; }

        public int TenantId { get; set; }
        public Tenant Tenant { get; set; } = null!;

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required, MaxLength(20)]
        public string ScenarioType { get; set; } = "Baseline"; // Baseline, BestCase, WorstCase, Custom

        public int FiscalYear { get; set; }

        public decimal BaseRevenue { get; set; }
        public decimal BaseExpenses { get; set; }

        // Adjustments as percentage
        public decimal RevenueAdjustmentPercent { get; set; }
        public decimal ExpenseAdjustmentPercent { get; set; }

        public decimal AdjustedRevenue => BaseRevenue * (1 + RevenueAdjustmentPercent / 100);
        public decimal AdjustedExpenses => BaseExpenses * (1 + ExpenseAdjustmentPercent / 100);
        public decimal NetImpact => AdjustedRevenue - AdjustedExpenses - (BaseRevenue - BaseExpenses);

        [MaxLength(20)]
        public string Status { get; set; } = "Active"; // Active, Archived

        public string? CreatedByUserId { get; set; }
        public ApplicationUser? CreatedByUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Archive functionality
        public bool IsArchived { get; set; } = false;
        public DateTime? ArchivedAt { get; set; }
        public string? ArchivedBy { get; set; }
        public string? ArchiveReason { get; set; }
    }
}
