using BudgetMasterFinal.Interfaces;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace BudgetMasterFinal.Models
{
    public class ApplicationUser : IdentityUser, IArchivable
    {
        [Required, MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        public string FullName => $"{FirstName} {LastName}";

        public int? TenantId { get; set; }
        public Tenant? Tenant { get; set; }

        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }

        [MaxLength(50)]
        public string Role { get; set; } = "Employee"; // SuperAdmin | CompanyAdmin | FinanceManager | Accountant | DepartmentHead | Auditor | Employee

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        
        // Password management
        public bool MustChangePassword { get; set; } = false;

        // Two-Factor Authentication
        public string? AuthenticatorKey { get; set; }

        // Archive functionality
        public bool IsArchived { get; set; } = false;
        public DateTime? ArchivedAt { get; set; }
        public string? ArchivedBy { get; set; }
        public string? ArchiveReason { get; set; }

        // Navigation
        public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    }
}
