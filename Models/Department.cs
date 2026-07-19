using BudgetMasterFinal.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace BudgetMasterFinal.Models
{
    public class Department : IArchivable
    {
        public int Id { get; set; }

        public int TenantId { get; set; }
        public Tenant Tenant { get; set; } = null!;

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(100)]
        public string? HeadOfDepartment { get; set; }

        public decimal? BudgetAllocation { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Archive functionality
        public bool IsArchived { get; set; } = false;
        public DateTime? ArchivedAt { get; set; }
        public string? ArchivedBy { get; set; }
        public string? ArchiveReason { get; set; }

        // Navigation
        public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
        public ICollection<Budget> Budgets { get; set; } = new List<Budget>();
        public ICollection<BudgetRequest> BudgetRequests { get; set; } = new List<BudgetRequest>();
        public ICollection<ActualTransaction> ActualTransactions { get; set; } = new List<ActualTransaction>();
    }
}
