using System.ComponentModel.DataAnnotations;
using BudgetMasterFinal.Attributes;

namespace BudgetMasterFinal.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required, MaxLength(100)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), StrongPassword]
        public string Password { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required, Display(Name = "Company Name")]
        public string CompanyName { get; set; } = string.Empty;

        public string? Industry { get; set; }

        [Display(Name = "Subscription Plan")]
        public string PlanName { get; set; } = "Starter";

        [Display(Name = "Billing Cycle")]
        public string BillingCycle { get; set; } = "Monthly";
    }

    public class AdminUserCreateViewModel
    {
        [Required, MaxLength(100)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        // Password field removed - temporary password is auto-generated and emailed

        public int? DepartmentId { get; set; }

        [Required]
        public string Role { get; set; } = "Employee";

        [Display(Name = "Department")]
        public List<Department> Departments { get; set; } = new();
    }

    public class DashboardViewModel
    {
        public string WelcomeMessage { get; set; } = string.Empty;
        public int TotalBudgets { get; set; }
        public decimal TotalAllocated { get; set; }
        public decimal TotalUsed { get; set; }
        public decimal VarianceAmount { get; set; }
        public double VariancePercent { get; set; }
        public int PendingRequests { get; set; }
        public int TotalDepartments { get; set; }
        public int TotalUsers { get; set; }
        public List<Budget> RecentBudgets { get; set; } = new();
        public List<BudgetRequest> RecentRequests { get; set; } = new();
        public List<Notification> RecentNotifications { get; set; } = new();
        // Chart data
        public List<string> ChartLabels { get; set; } = new();
        public List<decimal> BudgetData { get; set; } = new();
        public List<decimal> ActualData { get; set; } = new();
    }

    public class SuperAdminDashboardViewModel
    {
        public int TotalTenants { get; set; }
        public int ActiveTenants { get; set; }
        public int TotalUsers { get; set; }
        public int ActiveSubscriptions { get; set; }
        public decimal TotalMonthlyRevenue { get; set; }
        public List<Tenant> RecentTenants { get; set; } = new();
        public List<AuditLog> RecentLogs { get; set; } = new();
        public List<Subscription> ExpiringSubscriptions { get; set; } = new();
    }

    public class FinanceManagerDashboardViewModel
    {
        public string WelcomeMessage { get; set; } = string.Empty;
        
        // Primary financial control metrics
        public decimal TotalAllocated { get; set; }
        public decimal TotalUsed { get; set; }
        public decimal VarianceAmount { get; set; }
        public double VariancePercent { get; set; }
        
        // Budget management authority
        public int ActiveBudgets { get; set; }
        public int TotalBudgets { get; set; }
        
        // Financial planning metrics
        public int TotalForecasts { get; set; }
        public int ActiveForecasts { get; set; }
        public int TotalScenarios { get; set; }
        
        // Budget validation workflow
        public int PendingRequests { get; set; }
        public decimal PendingRequestsValue { get; set; }
        public int ApprovedRequests { get; set; }
        public int RejectedRequests { get; set; }
        
        // Financial oversight data
        public List<Budget> RecentBudgets { get; set; } = new();
        public List<BudgetRequest> RecentRequests { get; set; } = new();
        public List<Forecast> RecentForecasts { get; set; } = new();
        
        // Financial analysis charts
        public List<string> ChartLabels { get; set; } = new();
        public List<decimal> BudgetData { get; set; } = new();
        public List<decimal> ActualData { get; set; } = new();
        public List<decimal> ForecastData { get; set; } = new();
        
        // Cross-departmental financial analysis
        public List<object> DepartmentSummary { get; set; } = new();
    }

    public class AccountantDashboardViewModel
    {
        public string WelcomeMessage { get; set; } = string.Empty;
        public int TotalTransactionsThisMonth { get; set; }
        public decimal TotalExpensesThisMonth { get; set; }
        public decimal TotalIncomeThisMonth { get; set; }
        public decimal NetThisMonth { get; set; }
        public int TotalTransactionsAllTime { get; set; }
        public List<ActualTransaction> RecentTransactions { get; set; } = new();
        public List<Budget> ActiveBudgets { get; set; } = new();
    }

    public class DepartmentHeadDashboardViewModel
    {
        public string WelcomeMessage { get; set; } = string.Empty;
        public Department? Department { get; set; }
        
        // Departmental financial metrics (view only)
        public decimal DeptAllocated { get; set; }
        public decimal DeptUsed { get; set; }
        public decimal DeptVariance { get; set; }
        public double DeptBudgetUtilization { get; set; }
        
        // Operational approval workflow metrics
        public int PendingRequests { get; set; }
        public decimal PendingRequestsValue { get; set; }
        public int ApprovedRequests { get; set; }
        public int RejectedRequests { get; set; }
        public int AwaitingFinancialValidation { get; set; }
        
        // Team and operational metrics
        public int TeamMemberCount { get; set; }
        public int ActiveBudgets { get; set; }
        public int MonthlyTransactions { get; set; }
        
        // Department-specific operational data
        public List<BudgetRequest> DeptRequests { get; set; } = new();
        public List<Budget> DeptBudgets { get; set; } = new();
        public List<ActualTransaction> RecentTransactions { get; set; } = new();
        public List<Forecast> DeptForecasts { get; set; } = new();
        public List<ApplicationUser> TeamMembers { get; set; } = new();
        
        // Department spending performance
        public List<string> MonthlySpendingLabels { get; set; } = new();
        public List<decimal> MonthlySpendingData { get; set; } = new();
    }

    public class EmployeeDashboardViewModel
    {
        public string WelcomeMessage { get; set; } = string.Empty;
        public int TotalRequests { get; set; }
        public int PendingRequests { get; set; }
        public int ApprovedRequests { get; set; }
        public int RejectedRequests { get; set; }
        public decimal TotalApprovedAmount { get; set; }
        public List<BudgetRequest> MyRequests { get; set; } = new();
        public Department? Department { get; set; }
    }

    public class AuditorDashboardViewModel
    {
        public string WelcomeMessage { get; set; } = string.Empty;
        public int TotalBudgets { get; set; }
        public int TotalTransactions { get; set; }
        public decimal TotalBudgeted { get; set; }
        public decimal TotalUsed { get; set; }
        public decimal OverallVariance { get; set; }
        public int FlaggedItems { get; set; }
        public List<AuditLog> RecentAuditLogs { get; set; } = new();
        public List<ActualTransaction> RecentTransactions { get; set; } = new();
        public List<Budget> Budgets { get; set; } = new();
    }

    public class ResetPasswordViewModel
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), StrongPassword]
        [Display(Name = "New Password")]
        public string Password { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class AccountSettingsViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
