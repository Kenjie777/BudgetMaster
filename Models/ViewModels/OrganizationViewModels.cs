using System.ComponentModel.DataAnnotations;

namespace BudgetMasterFinal.Models.ViewModels
{
    public class OrganizationProfileViewModel
    {
        public int TenantId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string? Industry { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Website { get; set; }
        public string CurrencyCode { get; set; } = "PHP";
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? SubscriptionPlan { get; set; }
        public int UserCount { get; set; }
        public int DepartmentCount { get; set; }
        public int BudgetCount { get; set; }
    }

    public class EditOrganizationProfileViewModel
    {
        [Required, MaxLength(200)]
        [Display(Name = "Company Name")]
        public string CompanyName { get; set; } = string.Empty;

        [MaxLength(100)]
        [Display(Name = "Industry")]
        public string? Industry { get; set; }

        [MaxLength(20)]
        [Display(Name = "Phone Number")]
        public string? Phone { get; set; }

        [MaxLength(200)]
        [Display(Name = "Address")]
        public string? Address { get; set; }

        [MaxLength(100)]
        [Display(Name = "Website")]
        public string? Website { get; set; }

        [Required, MaxLength(10)]
        [Display(Name = "Currency")]
        public string CurrencyCode { get; set; } = "PHP";
    }

    public class OrganizationSettingsViewModel
    {
        public int TenantId { get; set; }
        public Dictionary<string, List<SystemSetting>> Settings { get; set; } = new();
    }

    public class FiscalYearSettingsViewModel
    {
        [Required]
        [Display(Name = "Fiscal Year Start Month")]
        public string FiscalYearStart { get; set; } = "January";

        [Required]
        [Display(Name = "Budget Planning Cycle")]
        public string BudgetCycle { get; set; } = "Annual";

        [Required]
        [Display(Name = "Approval Workflow")]
        public string ApprovalWorkflow { get; set; } = "Standard";

        public string CurrentFiscalYear { get; set; } = string.Empty;
    }

    public class OrganizationPoliciesViewModel
    {
        [Display(Name = "Auto-Approval Limit")]
        [Range(0, double.MaxValue, ErrorMessage = "Auto-approval limit must be a positive number")]
        public decimal AutoApprovalLimit { get; set; }

        [Display(Name = "Require Justification for Budget Requests")]
        public bool RequireJustification { get; set; } = true;

        [Display(Name = "Allow Budget Overrun")]
        public bool AllowBudgetOverrun { get; set; }

        [Display(Name = "Notify on Budget Variance")]
        public bool NotifyOnVariance { get; set; } = true;

        [Display(Name = "Variance Notification Threshold (%)")]
        [Range(0, 100, ErrorMessage = "Variance threshold must be between 0 and 100")]
        public decimal VarianceThreshold { get; set; } = 10;

        [Display(Name = "Data Retention Period (Months)")]
        [Range(1, 120, ErrorMessage = "Data retention period must be between 1 and 120 months")]
        public int DataRetentionMonths { get; set; } = 60;

        [Display(Name = "Backup Frequency")]
        public string BackupFrequency { get; set; } = "Daily";
    }

    public class OrganizationAuditLogViewModel
    {
        public List<AuditLog> Logs { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public string? ActionFilter { get; set; }
        public string? EntityFilter { get; set; }
    }
}