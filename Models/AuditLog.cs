using System.ComponentModel.DataAnnotations;

namespace BudgetMasterFinal.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        public int? TenantId { get; set; }

        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        [Required, MaxLength(100)]
        public string Action { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string EntityType { get; set; } = string.Empty;

        public int? EntityId { get; set; }

        [MaxLength(2000)]
        public string? OldValues { get; set; }

        [MaxLength(2000)]
        public string? NewValues { get; set; }

        [MaxLength(50)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
