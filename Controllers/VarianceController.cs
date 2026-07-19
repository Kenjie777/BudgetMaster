using BudgetMasterFinal.Data;
using BudgetMasterFinal.Models;
using BudgetMasterFinal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetMasterFinal.Controllers
{
    [Authorize(Policy = "AccountingPolicy")]
    public class VarianceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly NotificationService _notificationService;

        public VarianceController(ApplicationDbContext ctx, UserManager<ApplicationUser> um, NotificationService notificationService)
        { 
            _context = ctx; 
            _userManager = um;
            _notificationService = notificationService;
        }

        private async Task<int?> GetTenantId()
        {
            var user = await _userManager.GetUserAsync(User);
            return user?.TenantId;
        }

        /// <summary>
        /// Variance Analysis Dashboard - Compares Budgeted vs Actual Spending
        /// Shows how closely departments follow planned budgets
        /// </summary>
        public async Task<IActionResult> Index(int? year)
        {
            var tenantId = await GetTenantId();
            year ??= DateTime.UtcNow.Year;
            
            var budgets = await _context.Budgets
                .Include(b => b.Department)
                .Where(b => b.TenantId == tenantId && b.FiscalYear == year)
                .ToListAsync();
                
            var actuals = await _context.ActualTransactions
                .Include(t => t.Department)
                .Where(t => t.TenantId == tenantId && t.FiscalYear == year)
                .ToListAsync();
            
            ViewBag.Year = year;
            ViewBag.Budgets = budgets;
            ViewBag.Actuals = actuals;
            
            // Budget Planning Data
            ViewBag.TotalAllocated = budgets.Sum(b => b.AllocatedAmount);
            
            // Committed Budget Usage (from approved requests)
            ViewBag.TotalUsed = budgets.Sum(b => b.UsedAmount);
            
            // Actual Operational Spending (from expense tracking)
            ViewBag.TotalActualSpend = budgets.Sum(b => b.ActualSpend);
            
            // Variance: Budgeted vs Actual
            ViewBag.TotalVariance = ViewBag.TotalAllocated - ViewBag.TotalActualSpend;
            
            return View();
        }

        /// <summary>
        /// Expense Tracking - View all recorded actual spending transactions
        /// </summary>
        public async Task<IActionResult> Transactions()
        {
            var tenantId = await GetTenantId();
            var txns = await _context.ActualTransactions
                .Include(t => t.Department)
                .Include(t => t.Budget)
                .Include(t => t.CreatedByUser)
                .Where(t => t.TenantId == tenantId)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
            return View(txns);
        }

        /// <summary>
        /// Add Transaction Form - Record actual operational spending
        /// </summary>
        public async Task<IActionResult> AddTransaction()
        {
            var tenantId = await GetTenantId();
            ViewBag.Departments = await _context.Departments
                .Where(d => d.TenantId == tenantId && d.IsActive)
                .ToListAsync();
            ViewBag.Budgets = await _context.Budgets
                .Where(b => b.TenantId == tenantId && b.Status == "Active")
                .ToListAsync();
            return View();
        }

        /// <summary>
        /// Record Actual Spending Transaction
        /// Updates budget ActualSpend and recalculates variance
        /// </summary>
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTransaction(ActualTransaction txn)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            
            txn.TenantId = user.TenantId!.Value;
            txn.CreatedByUserId = user.Id;
            txn.FiscalYear = txn.TransactionDate.Year;
            txn.Quarter = $"Q{(txn.TransactionDate.Month - 1) / 3 + 1}";
            txn.CreatedAt = DateTime.UtcNow;
            
            _context.ActualTransactions.Add(txn);

            // ── UPDATE ACTUAL SPEND FOR VARIANCE ANALYSIS ──────────────────
            await UpdateBudgetActualSpend(txn);

            await _context.SaveChangesAsync();
            
            // Send role-based notifications
            await _notificationService.NotifyTransactionRecorded(txn, user.Id);
            
            TempData["Success"] = "Actual spending recorded and variance updated.";
            return RedirectToAction(nameof(Transactions));
        }

        /// <summary>
        /// Delete Transaction and recalculate budget ActualSpend
        /// </summary>
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTransaction(int id)
        {
            var tenantId = await GetTenantId();
            var txn = await _context.ActualTransactions
                .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId);
            
            if (txn == null) return NotFound();

            // Store budget info before deletion for recalculation
            var budgetId = txn.BudgetId;
            var departmentId = txn.DepartmentId;
            var fiscalYear = txn.FiscalYear;
            var amount = txn.Amount;
            var transactionType = txn.TransactionType;

            _context.ActualTransactions.Remove(txn);

            // ── RECALCULATE ACTUAL SPEND AFTER DELETION ────────────────────
            if (transactionType == "Expense")
            {
                if (budgetId.HasValue)
                {
                    var budget = await _context.Budgets.FindAsync(budgetId.Value);
                    if (budget != null)
                    {
                        budget.ActualSpend -= amount;
                        budget.UpdatedAt = DateTime.UtcNow;
                    }
                }
                else if (departmentId.HasValue)
                {
                    // Recalculate for department budgets
                    var deptBudgets = await _context.Budgets
                        .Where(b => b.TenantId == tenantId && 
                                   b.DepartmentId == departmentId.Value && 
                                   b.FiscalYear == fiscalYear)
                        .ToListAsync();

                    foreach (var budget in deptBudgets)
                    {
                        await RecalculateBudgetActualSpend(budget.Id);
                    }
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Transaction deleted and variance recalculated.";
            return RedirectToAction(nameof(Transactions));
        }

        /// <summary>
        /// Quick-entry form for Accountant dashboard
        /// </summary>
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTransaction(string Description, decimal Amount,
            string TransactionType, int? BudgetId, DateTime TransactionDate)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            int? deptId = null;
            if (BudgetId.HasValue)
            {
                var bud = await _context.Budgets.FindAsync(BudgetId.Value);
                deptId = bud?.DepartmentId;
            }

            var txn = new ActualTransaction
            {
                TenantId        = user.TenantId!.Value,
                CreatedByUserId = user.Id,
                Description     = Description,
                Amount          = Amount,
                TransactionType = TransactionType,
                BudgetId        = BudgetId,
                DepartmentId    = deptId,
                TransactionDate = TransactionDate,
                FiscalYear      = TransactionDate.Year,
                Quarter         = $"Q{(TransactionDate.Month - 1) / 3 + 1}",
                Category        = "Operational",
                CreatedAt       = DateTime.UtcNow
            };
            _context.ActualTransactions.Add(txn);

            // ── UPDATE ACTUAL SPEND FOR VARIANCE ANALYSIS ──────────────────
            await UpdateBudgetActualSpend(txn);

            await _context.SaveChangesAsync();
            
            // Send role-based notifications
            await _notificationService.NotifyTransactionRecorded(txn, user.Id);
            
            TempData["Success"] = $"{TransactionType} of ₱{Amount:N2} recorded.";
            return RedirectToAction("Accountant", "Dashboard");
        }

        // ══════════════════════════════════════════════════════════════════════════════════════
        // ACTUAL SPEND TRACKING & VARIANCE CALCULATION
        // ══════════════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Updates budget ActualSpend when a transaction is recorded
        /// Maintains separation between committed usage and actual spending
        /// </summary>
        private async Task UpdateBudgetActualSpend(ActualTransaction transaction)
        {
            // Only update for expense transactions
            if (transaction.TransactionType != "Expense") return;

            Budget? targetBudget = null;

            // 1. If transaction is linked to a specific budget, update that budget
            if (transaction.BudgetId.HasValue)
            {
                targetBudget = await _context.Budgets
                    .FirstOrDefaultAsync(b => b.Id == transaction.BudgetId.Value && 
                                             b.TenantId == transaction.TenantId);
            }
            // 2. Otherwise, find active budget for the department and fiscal year
            else if (transaction.DepartmentId.HasValue)
            {
                targetBudget = await _context.Budgets
                    .FirstOrDefaultAsync(b => b.TenantId == transaction.TenantId && 
                                             b.DepartmentId == transaction.DepartmentId.Value && 
                                             b.FiscalYear == transaction.FiscalYear && 
                                             b.Status == "Active");

                // Link transaction to found budget
                if (targetBudget != null)
                {
                    transaction.BudgetId = targetBudget.Id;
                }
            }

            // 3. Update budget ActualSpend
            if (targetBudget != null)
            {
                targetBudget.ActualSpend += transaction.Amount;
                targetBudget.UpdatedAt = DateTime.UtcNow;

                // Check for over-spending and notify
                if (targetBudget.ActualSpend > targetBudget.AllocatedAmount)
                {
                    var overAmount = targetBudget.ActualSpend - targetBudget.AllocatedAmount;
                    
                    // Send role-based overspending notifications
                    await _notificationService.NotifyBudgetOverspending(targetBudget, overAmount);
                }
            }
        }

        /// <summary>
        /// Recalculates budget ActualSpend from all related transactions
        /// Used when transactions are deleted or modified
        /// </summary>
        private async Task RecalculateBudgetActualSpend(int budgetId)
        {
            var budget = await _context.Budgets.FindAsync(budgetId);
            if (budget == null) return;

            // Sum all expense transactions for this budget
            var totalActualSpend = await _context.ActualTransactions
                .Where(t => t.BudgetId == budgetId && t.TransactionType == "Expense")
                .SumAsync(t => t.Amount);

            budget.ActualSpend = totalActualSpend;
            budget.UpdatedAt = DateTime.UtcNow;
        }

        // ══════════════════════════════════════════════════════════════════════════════════════
        // PERFORMANCE ANALYTICS
        // ══════════════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Performance Analytics Dashboard - Advanced financial performance metrics
        /// </summary>
        public async Task<IActionResult> Analytics(int? year)
        {
            var tenantId = await GetTenantId();
            year ??= DateTime.UtcNow.Year;

            // Get all data for analytics
            var budgets = await _context.Budgets
                .Include(b => b.Department)
                .Where(b => b.TenantId == tenantId && b.FiscalYear == year)
                .ToListAsync();

            var transactions = await _context.ActualTransactions
                .Include(t => t.Department)
                .Where(t => t.TenantId == tenantId && t.FiscalYear == year)
                .ToListAsync();

            var departments = await _context.Departments
                .Where(d => d.TenantId == tenantId && d.IsActive)
                .ToListAsync();

            // Calculate performance metrics
            ViewBag.Year = year;
            ViewBag.Budgets = budgets;
            ViewBag.Transactions = transactions;
            ViewBag.Departments = departments;
            
            // Overall metrics
            ViewBag.TotalAllocated = budgets.Sum(b => b.AllocatedAmount);
            ViewBag.TotalActualSpend = budgets.Sum(b => b.ActualSpend);
            ViewBag.TotalVariance = budgets.Sum(b => b.VarianceAmount);
            ViewBag.OverallEfficiency = ViewBag.TotalAllocated > 0 
                ? (double)((ViewBag.TotalAllocated - ViewBag.TotalActualSpend) / ViewBag.TotalAllocated * 100)
                : 0;

            // Department performance
            var deptPerformance = departments.Select(d => new
            {
                Department = d,
                Allocated = budgets.Where(b => b.DepartmentId == d.Id).Sum(b => b.AllocatedAmount),
                ActualSpend = budgets.Where(b => b.DepartmentId == d.Id).Sum(b => b.ActualSpend),
                Variance = budgets.Where(b => b.DepartmentId == d.Id).Sum(b => b.VarianceAmount),
                Efficiency = budgets.Where(b => b.DepartmentId == d.Id).Sum(b => b.AllocatedAmount) > 0
                    ? (double)(budgets.Where(b => b.DepartmentId == d.Id).Sum(b => b.VarianceAmount) / 
                               budgets.Where(b => b.DepartmentId == d.Id).Sum(b => b.AllocatedAmount) * 100)
                    : 0
            }).OrderByDescending(d => d.Efficiency).ToList();

            ViewBag.DepartmentPerformance = deptPerformance;
            ViewBag.BestPerformer = deptPerformance.FirstOrDefault();
            ViewBag.WorstPerformer = deptPerformance.LastOrDefault();

            return View();
        }
    }
}

