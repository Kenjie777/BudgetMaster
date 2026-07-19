using System.ComponentModel.DataAnnotations;

namespace BudgetMasterFinal.Models
{
    public class BudgetApproval
    {
        public int Id { get; set; }

        public int? BudgetId { get; set; }
        public Budget? Budget { get; set; }

        public int? BudgetRequestId { get; set; }
        public BudgetRequest? BudgetRequest { get; set; }

        [Required]
        public string ApprovedByUserId { get; set; } = string.Empty;
        public ApplicationUser? ApprovedByUser { get; set; }

        [Required, MaxLength(20)]
        public string Action { get; set; } = string.Empty; // Approved, Rejected, Revision, Submitted

        [MaxLength(500)]
        public string? Comments { get; set; }

        public DateTime ActionDate { get; set; } = DateTime.UtcNow;

        public int StepNumber { get; set; } = 1;
    }
}
