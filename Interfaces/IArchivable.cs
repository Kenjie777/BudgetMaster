namespace BudgetMasterFinal.Interfaces
{
    /// <summary>
    /// Interface for entities that support archive/restore functionality
    /// </summary>
    public interface IArchivable
    {
        /// <summary>
        /// Indicates whether the entity is archived
        /// </summary>
        bool IsArchived { get; set; }

        /// <summary>
        /// Date and time when the entity was archived
        /// </summary>
        DateTime? ArchivedAt { get; set; }

        /// <summary>
        /// User ID who archived the entity
        /// </summary>
        string? ArchivedBy { get; set; }

        /// <summary>
        /// Reason for archiving the entity
        /// </summary>
        string? ArchiveReason { get; set; }
    }
}
