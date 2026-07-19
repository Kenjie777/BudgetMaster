using BudgetMasterFinal.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace BudgetMasterFinal.Models
{
    public class BudgetRequest : IArchivable
    {
        public int Id { get; set; }

        public int TenantId { get; set; }
        public Tenant Tenant { get; set; } = null!;

        public int? BudgetId { get; set; }
        public Budget? Budget { get; set; }

        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }

        [Required]
        public string RequestedByUserId { get; set; } = string.Empty;
        public ApplicationUser? RequestedByUser { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Justification { get; set; }

        [Required]
        public decimal RequestedAmount { get; set; }

        [MaxLength(50)]
        public string Category { get; set; } = "General"; // General, Capital, Operational

        [MaxLength(30)]
        public string Status { get; set; } = "Pending"; // Pending, 1, Approved, Rejected, FinanciallyRejected

        [MaxLength(500)]
        public string? ReviewerNotes { get; set; }

        public string? ReviewedByUserId { get; set; }
        public ApplicationUser? ReviewedByUser { get; set; }

        public DateTime? ReviewedAt { get; set; }

        // Two-stage approval fields
        // Stage 1: Operational Approval (Department Head)
        [MaxLength(500)]
        public string? OperationalApprovalNotes { get; set; }
        
        public string? OperationallyApprovedByUserId { get; set; }
        public ApplicationUser? OperationallyApprovedByUser { get; set; }
        
        public DateTime? OperationalApprovalDate { get; set; }

        // Stage 2: Financial Validation (Finance Manager)
        [MaxLength(500)]
        public string? FinancialValidationNotes { get; set; }
        
        public string? FinanciallyApprovedByUserId { get; set; }
        public ApplicationUser? FinanciallyApprovedByUser { get; set; }
        
        public DateTime? FinancialApprovalDate { get; set; }
        
        public string? FinanciallyRejectedByUserId { get; set; }
        public ApplicationUser? FinanciallyRejectedByUser { get; set; }
        
        public DateTime? FinancialRejectionDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Archive functionality
        public bool IsArchived { get; set; } = false;
        public DateTime? ArchivedAt { get; set; }
        public string? ArchivedBy { get; set; }
        public string? ArchiveReason { get; set; }

        // Navigation
        public ICollection<BudgetApproval> Approvals { get; set; } = new List<BudgetApproval>();
    }
}
