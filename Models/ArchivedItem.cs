using System.ComponentModel.DataAnnotations;

namespace BudgetMasterFinal.Models
{
    /// <summary>
    /// Metadata for archived items across all entity types
    /// </summary>
    public class ArchivedItem
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string EntityType { get; set; } = string.Empty;

        [Required, MaxLength(450)]
        public string EntityId { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? EntityName { get; set; }

        [Required]
        public DateTime ArchivedAt { get; set; } = DateTime.UtcNow;

        [Required, MaxLength(450)]
        public string ArchivedBy { get; set; } = string.Empty;

        public string? ArchiveReason { get; set; }

        public int? TenantId { get; set; }

        /// <summary>
        /// JSON snapshot of the entity at the time of archiving
        /// </summary>
        public string? OriginalData { get; set; }

        /// <summary>
        /// Indicates whether the item can be restored
        /// </summary>
        public bool CanRestore { get; set; } = true;

        public DateTime? RestoredAt { get; set; }

        [MaxLength(450)]
        public string? RestoredBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Tenant? Tenant { get; set; }
    }
}
