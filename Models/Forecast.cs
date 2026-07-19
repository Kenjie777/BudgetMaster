using BudgetMasterFinal.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BudgetMasterFinal.Models
{
    public class Forecast : IArchivable
    {
        public int Id { get; set; }

        public int TenantId { get; set; }
        public Tenant Tenant { get; set; } = null!;

        // ── MANUAL INPUT FIELDS ──────────────────────────────────────
        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        public int FiscalYear { get; set; }

        [MaxLength(20)]
        public string Period { get; set; } = "Annual"; // Monthly, Quarterly, Annual

        [MaxLength(20)]
        public string ForecastMethod { get; set; } = "Linear"; // Linear, Exponential, MovingAverage

        public decimal ProjectedRevenue { get; set; }

        public decimal ConfidencePercent { get; set; } = 80;

        [MaxLength(20)]
        public string Status { get; set; } = "Draft"; // Draft, Published

        // ── AUTOMATICALLY CALCULATED FIELDS ──────────────────────────
        // Calculated from approved budgets, allocations, and committed requests
        public decimal ProjectedExpenses { get; set; }

        // Calculated by comparing against previous forecast periods
        public decimal GrowthRatePercent { get; set; }

        // Calculated field
        [NotMapped]
        public decimal ProjectedNetIncome => ProjectedRevenue - ProjectedExpenses;

        // Metadata
        public string? CreatedByUserId { get; set; }
        public ApplicationUser? CreatedByUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        // Calculation metadata for transparency
        [MaxLength(500)]
        public string? ExpenseCalculationNotes { get; set; }
        
        [MaxLength(500)]
        public string? GrowthRateCalculationNotes { get; set; }

        // Archive functionality
        public bool IsArchived { get; set; } = false;
        public DateTime? ArchivedAt { get; set; }
        public string? ArchivedBy { get; set; }
        public string? ArchiveReason { get; set; }
    }
}
