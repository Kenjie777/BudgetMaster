using BudgetMasterFinal.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace BudgetMasterFinal.Models
{
    public class Tenant : IArchivable
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Industry { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(200)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? Website { get; set; }

        [MaxLength(10)]
        public string CurrencyCode { get; set; } = "PHP";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Archive functionality
        public bool IsArchived { get; set; } = false;
        public DateTime? ArchivedAt { get; set; }
        public string? ArchivedBy { get; set; }
        public string? ArchiveReason { get; set; }

        // Navigation
        public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
        public ICollection<Department> Departments { get; set; } = new List<Department>();
        public ICollection<Budget> Budgets { get; set; } = new List<Budget>();
        public ICollection<Forecast> Forecasts { get; set; } = new List<Forecast>();
        public ICollection<ScenarioPlan> ScenarioPlans { get; set; } = new List<ScenarioPlan>();
        public ICollection<ActualTransaction> ActualTransactions { get; set; } = new List<ActualTransaction>();
        public ICollection<BudgetRequest> BudgetRequests { get; set; } = new List<BudgetRequest>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public ICollection<SystemSetting> SystemSettings { get; set; } = new List<SystemSetting>();
        public Subscription? Subscription { get; set; }
    }
}
