using System.ComponentModel.DataAnnotations;

namespace BudgetMasterFinal.Models
{
    public class SystemSetting
    {
        public int Id { get; set; }

        public int? TenantId { get; set; } // null = global
        public Tenant? Tenant { get; set; }

        [Required, MaxLength(100)]
        public string Key { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Value { get; set; }

        [MaxLength(200)]
        public string? Description { get; set; }

        [MaxLength(50)]
        public string Group { get; set; } = "General"; // General, Email, Finance, Security

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
