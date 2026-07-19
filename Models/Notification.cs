using BudgetMasterFinal.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace BudgetMasterFinal.Models
{
    public class Notification : IArchivable
    {
        public int Id { get; set; }

        public int? TenantId { get; set; }
        public Tenant? Tenant { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Message { get; set; }

        [MaxLength(50)]
        public string Type { get; set; } = "Info"; // Info, Success, Warning, Error

        [MaxLength(200)]
        public string? ActionUrl { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }

        // Archive functionality
        public bool IsArchived { get; set; } = false;
        public DateTime? ArchivedAt { get; set; }
        public string? ArchivedBy { get; set; }
        public string? ArchiveReason { get; set; }
    }
}
