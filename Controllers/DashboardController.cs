using BudgetMasterFinal.Data;
using BudgetMasterFinal.Models;
using BudgetMasterFinal.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetMasterFinal.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ── Smart router ──────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            return user.Role switch
            {
                "SuperAdmin"     => RedirectToAction("SuperAdmin"),
                "CompanyAdmin"   => RedirectToAction("Admin"),
                "Tenant"         => RedirectToAction("Admin"),
                "FinanceManager" => RedirectToAction("FinanceManager"),
                "Accountant"     => RedirectToAction("Accountant"),
                "DepartmentHead" => RedirectToAction("DepartmentHead"),
                "Employee"       => RedirectToAction("Employee"),
                "Auditor"        => RedirectToAction("Auditor"),
                _                => RedirectToAction("Admin")   // Default fallback
            };
        }

        // ── 1. Super Admin ────────────────────────────────────────────
        [Authorize(Policy = "SuperAdminPolicy")]
        public async Task<IActionResult> SuperAdmin()
        {
            var tenants = await _context.Tenants
                .Include(t => t.Subscription)
                .ThenInclude(s => s.SubscriptionPlan)
                .ToListAsync();
            var vm = new SuperAdminDashboardViewModel
            {
                TotalTenants       = tenants.Count,
                ActiveTenants      = tenants.Count(t => t.IsActive),
                TotalUsers         = await _context.Users.CountAsync(),
                ActiveSubscriptions= tenants.Count(t => t.Subscription?.Status == "Active"),
                TotalMonthlyRevenue= tenants
                    .Where(t => t.Subscription?.Status == "Active")
                    .Sum(t => t.Subscription!.BillingCycle == "Yearly"
                        ? t.Subscription.PriceAmount / 12
                        : t.Subscription.PriceAmount),
                RecentTenants = tenants.OrderByDescending(t => t.CreatedAt).Take(5).ToList(),
                RecentLogs    = await _context.AuditLogs
                    .Include(l => l.User)
                    .OrderByDescending(l => l.Timestamp).Take(10).ToListAsync(),
                ExpiringSubscriptions = await _context.Subscriptions
                    .Include(s => s.Tenant)
                    .Where(s => s.Status == "Active" && s.EndDate <= DateTime.UtcNow.AddDays(30))
                    .OrderBy(s => s.EndDate).ToListAsync()
            };
            return View(vm);
        }

        // ── 2. Admin (Organization Configuration + Dashboard View) ────────────────
        [Authorize(Policy = "AdminPolicy")]
        public async Task<IActionResult> Admin()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            var tid = user.TenantId;

            // Load budget data for dashboard viewing (read-only)
            var budgets = await _context.Budgets
                .Include(b => b.Department)
                .Where(b => b.TenantId == tid)
                .ToListAsync();

            var budgetRequests = await _context.BudgetRequests
                .Include(br => br.Department)
                .Include(br => br.RequestedByUser)
                .Where(br => br.TenantId == tid)
                .OrderByDescending(br => br.CreatedAt)
                .ToListAsync();

            // Calculate financial metrics for dashboard display
            var totalAllocated = budgets.Sum(b => b.AllocatedAmount);
            var totalUsed = budgets.Sum(b => b.UsedAmount);
            var varianceAmount = totalAllocated - totalUsed;

            // Chart data for Budget vs Actual (6 months)
            var chartLabels = new List<string>();
            var budgetData = new List<decimal>();
            var actualData = new List<decimal>();
            
            for (int i = 5; i >= 0; i--)
            {
                var month = DateTime.UtcNow.AddMonths(-i);
                chartLabels.Add(month.ToString("MMM yyyy"));
                
                // Monthly budget allocation
                var monthlyBudget = budgets
                    .Where(b => b.FiscalYear == month.Year)
                    .Sum(b => b.AllocatedAmount) / 12;
                budgetData.Add(monthlyBudget);
                
                // Monthly actual spending
                var monthlyActual = await _context.ActualTransactions
                    .Where(t => t.TenantId == tid && 
                               t.TransactionDate.Year == month.Year &&
                               t.TransactionDate.Month == month.Month &&
                               t.TransactionType == "Expense")
                    .SumAsync(t => t.Amount);
                actualData.Add(monthlyActual);
            }

            var vm = new DashboardViewModel
            {
                TotalDepartments  = await _context.Departments.CountAsync(d => d.TenantId == tid),
                TotalUsers        = await _context.Users.CountAsync(u => u.TenantId == tid),
                
                // Financial data for dashboard viewing
                TotalBudgets      = budgets.Count,
                TotalAllocated    = totalAllocated,
                TotalUsed         = totalUsed,
                VarianceAmount    = varianceAmount,
                PendingRequests   = budgetRequests.Count(r => r.Status == "Pending"),
                
                // Recent items for dashboard display
                RecentBudgets     = budgets.OrderByDescending(b => b.CreatedAt).Take(5).ToList(),
                RecentRequests    = budgetRequests.Take(5).ToList(),
                
                RecentNotifications = await _context.Notifications
                    .Where(n => n.UserId == user.Id && !n.IsRead)
                    .OrderByDescending(n => n.CreatedAt).Take(5).ToListAsync(),
                
                // Chart data
                ChartLabels = chartLabels,
                BudgetData  = budgetData,
                ActualData  = actualData
            };
            
            vm.VariancePercent = totalAllocated > 0 ? (double)(varianceAmount / totalAllocated * 100) : 0;
            
            return View(vm);
        }

        // ── 3. Finance Manager - Primary Financial Control Authority ────────────────────────────────────────
        [Authorize(Policy = "FinancialPolicy")]
        public async Task<IActionResult> FinanceManager()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            var tid = user.TenantId;

            // Comprehensive financial data for Finance Manager authority
            var budgets = await _context.Budgets
                .Include(b => b.Department)
                .Where(b => b.TenantId == tid)
                .ToListAsync();

            var budgetRequests = await _context.BudgetRequests
                .Include(br => br.Department)
                .Include(br => br.RequestedByUser)
                .Where(br => br.TenantId == tid)
                .ToListAsync();

            var forecasts = await _context.Forecasts
                .Where(f => f.TenantId == tid)
                .ToListAsync();

            var scenarios = await _context.ScenarioPlans
                .Where(s => s.TenantId == tid)
                .ToListAsync();

            // Financial control metrics
            var totalAllocated = budgets.Sum(b => b.AllocatedAmount);
            var totalUsed = budgets.Sum(b => b.UsedAmount);
            var varianceAmount = totalAllocated - totalUsed;
            
            // Budget request workflow metrics
            var pendingRequests = budgetRequests.Where(r => r.Status == "Pending").ToList();
            var approvedRequests = budgetRequests.Where(r => r.Status == "Approved").ToList();
            var rejectedRequests = budgetRequests.Where(r => r.Status == "Rejected").ToList();
            
            // Department-wise budget analysis
            var departmentBudgets = budgets.GroupBy(b => b.Department?.Name ?? "Unassigned")
                .Select(g => new {
                    Department = g.Key,
                    Allocated = g.Sum(b => b.AllocatedAmount),
                    Used = g.Sum(b => b.UsedAmount),
                    Variance = g.Sum(b => b.AllocatedAmount - b.UsedAmount),
                    Count = g.Count()
                }).ToList();

            // Monthly trend analysis for financial planning
            var chartLabels = new List<string>();
            var budgetData = new List<decimal>();
            var actualData = new List<decimal>();
            var forecastData = new List<decimal>();
            
            for (int i = 11; i >= 0; i--) // 12 months of data
            {
                var month = DateTime.UtcNow.AddMonths(-i);
                chartLabels.Add(month.ToString("MMM yyyy"));
                
                // Monthly budget allocation
                var monthlyBudget = budgets
                    .Where(b => b.FiscalYear == month.Year)
                    .Sum(b => b.AllocatedAmount) / 12;
                budgetData.Add(monthlyBudget);
                
                // Monthly actual spending
                var monthlyActual = await _context.ActualTransactions
                    .Where(t => t.TenantId == tid && 
                               t.TransactionDate.Year == month.Year &&
                               t.TransactionDate.Month == month.Month &&
                               t.TransactionType == "Expense")
                    .SumAsync(t => t.Amount);
                actualData.Add(monthlyActual);
                
                // Monthly forecast data
                var monthlyForecast = forecasts
                    .Where(f => f.FiscalYear == month.Year)
                    .Sum(f => f.ProjectedRevenue);
                forecastData.Add(monthlyForecast);
            }

            var vm = new FinanceManagerDashboardViewModel
            {
                // Primary financial control metrics
                TotalAllocated = totalAllocated,
                TotalUsed = totalUsed,
                VarianceAmount = varianceAmount,
                VariancePercent = totalAllocated > 0 ? (double)(varianceAmount / totalAllocated * 100) : 0,
                
                // Budget management metrics
                ActiveBudgets = budgets.Count(b => b.Status == "Approved"),
                TotalBudgets = budgets.Count,
                
                // Financial planning metrics
                TotalForecasts = forecasts.Count,
                TotalScenarios = scenarios.Count,
                ActiveForecasts = forecasts.Count(f => f.Status == "Published"),
                
                // Budget request validation workflow
                PendingRequests = pendingRequests.Count,
                PendingRequestsValue = pendingRequests.Sum(r => r.RequestedAmount),
                ApprovedRequests = approvedRequests.Count,
                RejectedRequests = rejectedRequests.Count,
                
                // Recent items for financial oversight
                RecentBudgets = budgets.OrderByDescending(b => b.CreatedAt).Take(8).ToList(),
                RecentRequests = budgetRequests.OrderByDescending(r => r.CreatedAt).Take(10).ToList(),
                RecentForecasts = forecasts.OrderByDescending(f => f.CreatedAt).Take(6).ToList(),
                
                // Chart data for financial analysis
                ChartLabels = chartLabels,
                BudgetData = budgetData,
                ActualData = actualData,
                ForecastData = forecastData,
                
                // Department analysis for cross-departmental oversight
                DepartmentSummary = departmentBudgets.Take(6).Cast<object>().ToList()
            };

            return View(vm);
        }

        // ── 4. Accountant ─────────────────────────────────────────────
        [Authorize(Policy = "AccountingPolicy")]
        public async Task<IActionResult> Accountant()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            var tid  = user.TenantId;
            var now  = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);

            var monthTx = await _context.ActualTransactions
                .Where(t => t.TenantId == tid && t.TransactionDate >= startOfMonth)
                .ToListAsync();

            var vm = new AccountantDashboardViewModel
            {
                TotalTransactionsThisMonth = monthTx.Count,
                TotalExpensesThisMonth     = monthTx.Where(t => t.TransactionType == "Expense").Sum(t => t.Amount),
                TotalIncomeThisMonth       = monthTx.Where(t => t.TransactionType == "Income").Sum(t => t.Amount),
                TotalTransactionsAllTime   = await _context.ActualTransactions.CountAsync(t => t.TenantId == tid),
                RecentTransactions         = await _context.ActualTransactions
                    .Include(t => t.Budget).Include(t => t.Department)
                    .Where(t => t.TenantId == tid)
                    .OrderByDescending(t => t.TransactionDate).Take(10).ToListAsync(),
                ActiveBudgets = await _context.Budgets
                    .Where(b => b.TenantId == tid && b.Status == "Approved")
                    .OrderByDescending(b => b.CreatedAt).Take(10).ToListAsync()
            };
            vm.NetThisMonth = vm.TotalIncomeThisMonth - vm.TotalExpensesThisMonth;
            return View(vm);
        }

        // ── 5. Department Head - Operational Approval Authority ────────────────────────────────────────
        [Authorize(Policy = "DepartmentHeadPolicy")]
        public async Task<IActionResult> DepartmentHead()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            var tid = user.TenantId;
            var deptId = user.DepartmentId;

            // Ensure Department Head can only access their own department
            if (deptId == null)
            {
                TempData["Error"] = "You are not assigned to a department. Please contact your administrator.";
                return RedirectToAction("AccessDenied", "Account");
            }

            // Department-specific data only
            var dept = await _context.Departments.FirstOrDefaultAsync(d => d.Id == deptId);
            if (dept == null)
            {
                TempData["Error"] = "Department not found. Please contact your administrator.";
                return RedirectToAction("AccessDenied", "Account");
            }

            // Departmental budget data
            var budgets = await _context.Budgets
                .Include(b => b.CreatedByUser)
                .Where(b => b.TenantId == tid && b.DepartmentId == deptId)
                .ToListAsync();

            // Departmental budget requests for operational approval
            var requests = await _context.BudgetRequests
                .Include(r => r.RequestedByUser)
                .Include(r => r.Department)
                .Where(r => r.TenantId == tid && r.DepartmentId == deptId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // Departmental transactions and spending
            var transactions = await _context.ActualTransactions
                .Where(t => t.TenantId == tid && t.DepartmentId == deptId)
                .OrderByDescending(t => t.TransactionDate)
                .Take(15)
                .ToListAsync();

            // Departmental forecasts (view only - no governance authority)
            var deptForecasts = await _context.Forecasts
                .Where(f => f.TenantId == tid)
                .OrderByDescending(f => f.CreatedAt)
                .Take(5)
                .ToListAsync();

            // Department team members
            var teamMembers = await _context.Users
                .Where(u => u.TenantId == tid && u.DepartmentId == deptId && u.IsActive)
                .ToListAsync();

            // Operational approval metrics
            var pendingRequests = requests.Where(r => r.Status == "Pending").ToList();
            var approvedRequests = requests.Where(r => r.Status == "Approved").ToList();
            var rejectedRequests = requests.Where(r => r.Status == "Rejected").ToList();
            var awaitingFinancialValidation = requests.Where(r => r.Status == "OperationallyApproved").ToList();

            // Monthly spending trend for department
            var monthlySpending = new List<decimal>();
            var monthLabels = new List<string>();
            for (int i = 5; i >= 0; i--)
            {
                var month = DateTime.UtcNow.AddMonths(-i);
                monthLabels.Add(month.ToString("MMM yyyy"));
                var monthlyAmount = await _context.ActualTransactions
                    .Where(t => t.TenantId == tid && 
                               t.DepartmentId == deptId &&
                               t.TransactionDate.Year == month.Year &&
                               t.TransactionDate.Month == month.Month &&
                               t.TransactionType == "Expense")
                    .SumAsync(t => t.Amount);
                monthlySpending.Add(monthlyAmount);
            }

            var vm = new DepartmentHeadDashboardViewModel
            {
                Department = dept,
                
                // Departmental financial metrics (view only)
                DeptAllocated = budgets.Sum(b => b.AllocatedAmount),
                DeptUsed = budgets.Sum(b => b.UsedAmount),
                DeptVariance = budgets.Sum(b => b.AllocatedAmount - b.UsedAmount),
                DeptBudgetUtilization = budgets.Sum(b => b.AllocatedAmount) > 0 
                    ? (double)(budgets.Sum(b => b.UsedAmount) / budgets.Sum(b => b.AllocatedAmount) * 100) 
                    : 0,
                
                // Operational approval workflow metrics
                PendingRequests = pendingRequests.Count,
                PendingRequestsValue = pendingRequests.Sum(r => r.RequestedAmount),
                ApprovedRequests = approvedRequests.Count,
                RejectedRequests = rejectedRequests.Count,
                AwaitingFinancialValidation = awaitingFinancialValidation.Count,
                
                // Team and operational metrics
                TeamMemberCount = teamMembers.Count,
                ActiveBudgets = budgets.Count(b => b.Status == "Approved"),
                MonthlyTransactions = transactions.Count(t => t.TransactionDate >= DateTime.UtcNow.AddDays(-30)),
                
                // Department-specific data for operational oversight
                DeptRequests = requests.Take(10).ToList(),
                DeptBudgets = budgets,
                RecentTransactions = transactions,
                DeptForecasts = deptForecasts,
                TeamMembers = teamMembers.Take(8).ToList(),
                
                // Department spending trend
                MonthlySpendingLabels = monthLabels,
                MonthlySpendingData = monthlySpending
            };

            return View(vm);
        }

        // ── 6. Employee ───────────────────────────────────────────────
        [Authorize(Policy = "EmployeePolicy")]
        public async Task<IActionResult> Employee()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            var tid = user.TenantId;

            var myRequests = await _context.BudgetRequests
                .Include(r => r.Department)
                .Where(r => r.RequestedByUserId == user.Id)
                .OrderByDescending(r => r.CreatedAt).ToListAsync();

            var dept = user.DepartmentId.HasValue
                ? await _context.Departments.FirstOrDefaultAsync(d => d.Id == user.DepartmentId)
                : null;

            var vm = new EmployeeDashboardViewModel
            {
                TotalRequests        = myRequests.Count,
                PendingRequests      = myRequests.Count(r => r.Status == "Pending"),
                ApprovedRequests     = myRequests.Count(r => r.Status == "Approved"),
                RejectedRequests     = myRequests.Count(r => r.Status == "Rejected"),
                TotalApprovedAmount  = myRequests.Where(r => r.Status == "Approved").Sum(r => r.RequestedAmount),
                MyRequests           = myRequests.Take(10).ToList(),
                Department           = dept
            };
            return View(vm);
        }

        // ── 7. Auditor ────────────────────────────────────────────────
        [Authorize(Policy = "AuditPolicy")]
        public async Task<IActionResult> Auditor()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            var tid = user.TenantId;

            var budgets = await _context.Budgets.Where(b => b.TenantId == tid).ToListAsync();
            var allTx   = await _context.ActualTransactions
                .Include(t => t.Department).Include(t => t.Budget)
                .Where(t => t.TenantId == tid)
                .OrderByDescending(t => t.TransactionDate).Take(15).ToListAsync();

            var totalBudgeted = budgets.Sum(b => b.AllocatedAmount);
            var totalUsed     = budgets.Sum(b => b.UsedAmount);

            var vm = new AuditorDashboardViewModel
            {
                TotalBudgets       = budgets.Count,
                TotalTransactions  = await _context.ActualTransactions.CountAsync(t => t.TenantId == tid),
                TotalBudgeted      = totalBudgeted,
                TotalUsed          = totalUsed,
                OverallVariance    = totalBudgeted - totalUsed,
                FlaggedItems       = await _context.BudgetRequests.CountAsync(r => r.TenantId == tid && r.Status == "Rejected"),
                RecentAuditLogs    = await _context.AuditLogs
                    .Include(l => l.User)
                    .Where(l => l.TenantId == tid)
                    .OrderByDescending(l => l.Timestamp).Take(10).ToListAsync(),
                RecentTransactions = allTx,
                Budgets            = budgets.OrderByDescending(b => b.CreatedAt).Take(8).ToList()
            };
            return View(vm);
        }
    }
}
