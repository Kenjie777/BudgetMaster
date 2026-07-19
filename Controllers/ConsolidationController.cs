using BudgetMasterFinal.Data;
using BudgetMasterFinal.Models;
using BudgetMasterFinal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetMasterFinal.Controllers
{
    /// <summary>
    /// Budget Consolidation Module - Organization-wide Financial Aggregation
    /// Combines departmental budgets, allocations, usage, spending, and forecasts
    /// into a unified company-level financial overview for Finance Managers
    /// </summary>
    [Authorize(Policy = "FinancialPolicy")] // Finance Manager only
    public class ConsolidationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly PdfReportService _pdfService;

        public ConsolidationController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, PdfReportService pdfService)
        {
            _context = context;
            _userManager = userManager;
            _pdfService = pdfService;
        }

        private async Task<int?> GetTenantId()
        {
            var user = await _userManager.GetUserAsync(User);
            return user?.TenantId;
        }

        /// <summary>
        /// Main Consolidation Dashboard - Organization-wide Budget Overview
        /// Aggregates all departmental financial data into a single view
        /// </summary>
        public async Task<IActionResult> Index(int? year)
        {
            var tenantId = await GetTenantId();
            year ??= DateTime.UtcNow.Year;

            // ── AGGREGATE DEPARTMENTAL BUDGET DATA ──────────────────────────────────
            var departments = await _context.Departments
                .Where(d => d.TenantId == tenantId && d.IsActive)
                .OrderBy(d => d.Name)
                .ToListAsync();

            var budgets = await _context.Budgets
                .Include(b => b.Department)
                .Where(b => b.TenantId == tenantId && b.FiscalYear == year)
                .ToListAsync();

            var budgetRequests = await _context.BudgetRequests
                .Include(r => r.Department)
                .Where(r => r.TenantId == tenantId && r.CreatedAt.Year == year)
                .ToListAsync();

            var transactions = await _context.ActualTransactions
                .Include(t => t.Department)
                .Where(t => t.TenantId == tenantId && t.FiscalYear == year)
                .ToListAsync();

            var forecasts = await _context.Forecasts
                .Where(f => f.TenantId == tenantId && f.FiscalYear == year)
                .ToListAsync();

            // ── BUILD DEPARTMENT CONSOLIDATION DATA ──────────────────────────────────
            var consolidationData = departments.Select(dept => new DepartmentConsolidation
            {
                Department = dept,
                Budgets = budgets.Where(b => b.DepartmentId == dept.Id).ToList(),
                TotalAllocated = budgets.Where(b => b.DepartmentId == dept.Id).Sum(b => b.AllocatedAmount),
                TotalUsed = budgets.Where(b => b.DepartmentId == dept.Id).Sum(b => b.UsedAmount),
                TotalActualSpend = budgets.Where(b => b.DepartmentId == dept.Id).Sum(b => b.ActualSpend),
                TotalVariance = budgets.Where(b => b.DepartmentId == dept.Id).Sum(b => b.VarianceAmount),
                PendingRequests = budgetRequests.Count(r => r.DepartmentId == dept.Id && r.Status == "Pending"),
                ApprovedRequests = budgetRequests.Count(r => r.DepartmentId == dept.Id && r.Status == "Approved"),
                RejectedRequests = budgetRequests.Count(r => r.DepartmentId == dept.Id && (r.Status == "Rejected" || r.Status == "FinanciallyRejected")),
                TransactionCount = transactions.Count(t => t.DepartmentId == dept.Id),
                RevenueTransactions = transactions.Where(t => t.DepartmentId == dept.Id && t.TransactionType == "Revenue").Sum(t => t.Amount),
                ExpenseTransactions = transactions.Where(t => t.DepartmentId == dept.Id && t.TransactionType == "Expense").Sum(t => t.Amount)
            }).ToList();

            // ── ORGANIZATION-WIDE TOTALS ──────────────────────────────────────────────
            ViewBag.Year = year;
            ViewBag.TotalAllocated = consolidationData.Sum(d => d.TotalAllocated);
            ViewBag.TotalUsed = consolidationData.Sum(d => d.TotalUsed);
            ViewBag.TotalActualSpend = consolidationData.Sum(d => d.TotalActualSpend);
            ViewBag.TotalVariance = consolidationData.Sum(d => d.TotalVariance);
            ViewBag.TotalRemaining = ViewBag.TotalAllocated - ViewBag.TotalUsed;
            ViewBag.TotalPendingRequests = consolidationData.Sum(d => d.PendingRequests);
            ViewBag.TotalApprovedRequests = consolidationData.Sum(d => d.ApprovedRequests);
            ViewBag.TotalRevenue = consolidationData.Sum(d => d.RevenueTransactions);
            ViewBag.TotalExpenses = consolidationData.Sum(d => d.ExpenseTransactions);
            ViewBag.NetIncome = ViewBag.TotalRevenue - ViewBag.TotalExpenses;
            
            // Forecast data
            ViewBag.TotalProjectedRevenue = forecasts.Sum(f => f.ProjectedRevenue);
            ViewBag.TotalProjectedExpenses = forecasts.Sum(f => f.ProjectedExpenses);
            ViewBag.ProjectedNetIncome = ViewBag.TotalProjectedRevenue - ViewBag.TotalProjectedExpenses;
            ViewBag.AverageGrowthRate = forecasts.Any() ? forecasts.Average(f => f.GrowthRatePercent) : 0;

            // Department count
            ViewBag.ActiveDepartments = departments.Count;
            ViewBag.DepartmentsOverBudget = consolidationData.Count(d => d.TotalActualSpend > d.TotalAllocated);
            ViewBag.DepartmentsOnTrack = consolidationData.Count(d => d.TotalActualSpend <= d.TotalAllocated * 0.8m);
            ViewBag.DepartmentsAtRisk = consolidationData.Count(d => d.TotalActualSpend > d.TotalAllocated * 0.8m && d.TotalActualSpend <= d.TotalAllocated);

            return View(consolidationData);
        }

        /// <summary>
        /// Department Comparison View - Side-by-side departmental performance
        /// </summary>
        public async Task<IActionResult> DepartmentComparison(int? year)
        {
            var tenantId = await GetTenantId();
            year ??= DateTime.UtcNow.Year;

            var departments = await _context.Departments
                .Where(d => d.TenantId == tenantId && d.IsActive)
                .OrderBy(d => d.Name)
                .ToListAsync();

            var budgets = await _context.Budgets
                .Include(b => b.Department)
                .Where(b => b.TenantId == tenantId && b.FiscalYear == year)
                .ToListAsync();

            var consolidationData = departments.Select(dept => new DepartmentConsolidation
            {
                Department = dept,
                Budgets = budgets.Where(b => b.DepartmentId == dept.Id).ToList(),
                TotalAllocated = budgets.Where(b => b.DepartmentId == dept.Id).Sum(b => b.AllocatedAmount),
                TotalUsed = budgets.Where(b => b.DepartmentId == dept.Id).Sum(b => b.UsedAmount),
                TotalActualSpend = budgets.Where(b => b.DepartmentId == dept.Id).Sum(b => b.ActualSpend),
                TotalVariance = budgets.Where(b => b.DepartmentId == dept.Id).Sum(b => b.VarianceAmount)
            }).OrderByDescending(d => d.TotalAllocated).ToList();

            ViewBag.Year = year;
            return View(consolidationData);
        }

        /// <summary>
        /// Quarterly Breakdown - Organization budget performance by quarter
        /// </summary>
        public async Task<IActionResult> QuarterlyBreakdown(int? year)
        {
            var tenantId = await GetTenantId();
            year ??= DateTime.UtcNow.Year;

            var budgets = await _context.Budgets
                .Include(b => b.Department)
                .Where(b => b.TenantId == tenantId && b.FiscalYear == year)
                .ToListAsync();

            var transactions = await _context.ActualTransactions
                .Include(t => t.Department)
                .Where(t => t.TenantId == tenantId && t.FiscalYear == year)
                .ToListAsync();

            // Group by quarter
            var quarters = new[] { "Q1", "Q2", "Q3", "Q4" };
            var quarterlyData = quarters.Select(q => new QuarterlyConsolidation
            {
                Quarter = q,
                Allocated = budgets.Where(b => b.Quarter == q).Sum(b => b.AllocatedAmount),
                Used = budgets.Where(b => b.Quarter == q).Sum(b => b.UsedAmount),
                ActualSpend = budgets.Where(b => b.Quarter == q).Sum(b => b.ActualSpend),
                Variance = budgets.Where(b => b.Quarter == q).Sum(b => b.VarianceAmount),
                Revenue = transactions.Where(t => t.Quarter == q && t.TransactionType == "Revenue").Sum(t => t.Amount),
                Expenses = transactions.Where(t => t.Quarter == q && t.TransactionType == "Expense").Sum(t => t.Amount)
            }).ToList();

            ViewBag.Year = year;
            return View(quarterlyData);
        }

        /// <summary>
        /// Forecast Integration - Consolidation with forecasting data
        /// </summary>
        public async Task<IActionResult> ForecastIntegration(int? year)
        {
            var tenantId = await GetTenantId();
            year ??= DateTime.UtcNow.Year;

            var budgets = await _context.Budgets
                .Where(b => b.TenantId == tenantId && b.FiscalYear == year)
                .ToListAsync();

            var forecasts = await _context.Forecasts
                .Include(f => f.CreatedByUser)
                .Where(f => f.TenantId == tenantId && f.FiscalYear == year)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            // Calculate actual vs forecast variance
            var totalActualRevenue = await _context.ActualTransactions
                .Where(t => t.TenantId == tenantId && t.FiscalYear == year && t.TransactionType == "Revenue")
                .SumAsync(t => t.Amount);

            var totalActualExpenses = await _context.ActualTransactions
                .Where(t => t.TenantId == tenantId && t.FiscalYear == year && t.TransactionType == "Expense")
                .SumAsync(t => t.Amount);

            ViewBag.Year = year;
            ViewBag.TotalAllocated = budgets.Sum(b => b.AllocatedAmount);
            ViewBag.TotalActualSpend = budgets.Sum(b => b.ActualSpend);
            ViewBag.TotalActualRevenue = totalActualRevenue;
            ViewBag.TotalActualExpenses = totalActualExpenses;
            ViewBag.TotalProjectedRevenue = forecasts.Sum(f => f.ProjectedRevenue);
            ViewBag.TotalProjectedExpenses = forecasts.Sum(f => f.ProjectedExpenses);
            ViewBag.RevenueVariance = totalActualRevenue - forecasts.Sum(f => f.ProjectedRevenue);
            ViewBag.ExpenseVariance = totalActualExpenses - forecasts.Sum(f => f.ProjectedExpenses);

            return View(forecasts);
        }

        /// <summary>
        /// Trend Analysis - Multi-year budget trends
        /// </summary>
        public async Task<IActionResult> TrendAnalysis()
        {
            var tenantId = await GetTenantId();
            var currentYear = DateTime.UtcNow.Year;
            var years = Enumerable.Range(currentYear - 3, 4).ToList(); // Last 3 years + current

            var yearlyData = new List<YearlyConsolidation>();

            foreach (var year in years)
            {
                var budgets = await _context.Budgets
                    .Where(b => b.TenantId == tenantId && b.FiscalYear == year)
                    .ToListAsync();

                var transactions = await _context.ActualTransactions
                    .Where(t => t.TenantId == tenantId && t.FiscalYear == year)
                    .ToListAsync();

                var forecasts = await _context.Forecasts
                    .Where(f => f.TenantId == tenantId && f.FiscalYear == year)
                    .ToListAsync();

                yearlyData.Add(new YearlyConsolidation
                {
                    Year = year,
                    TotalAllocated = budgets.Sum(b => b.AllocatedAmount),
                    TotalUsed = budgets.Sum(b => b.UsedAmount),
                    TotalActualSpend = budgets.Sum(b => b.ActualSpend),
                    TotalVariance = budgets.Sum(b => b.VarianceAmount),
                    TotalRevenue = transactions.Where(t => t.TransactionType == "Revenue").Sum(t => t.Amount),
                    TotalExpenses = transactions.Where(t => t.TransactionType == "Expense").Sum(t => t.Amount),
                    ProjectedRevenue = forecasts.Sum(f => f.ProjectedRevenue),
                    ProjectedExpenses = forecasts.Sum(f => f.ProjectedExpenses),
                    AverageGrowthRate = forecasts.Any() ? forecasts.Average(f => f.GrowthRatePercent) : 0
                });
            }

            return View(yearlyData);
        }

        /// <summary>
        /// Export Consolidation Report - Generate downloadable PDF report
        /// </summary>
        public async Task<IActionResult> ExportReport(int? year)
        {
            var tenantId = await GetTenantId();
            year ??= DateTime.UtcNow.Year;

            var departments = await _context.Departments
                .Where(d => d.TenantId == tenantId && d.IsActive)
                .ToListAsync();

            var budgets = await _context.Budgets
                .Include(b => b.Department)
                .Where(b => b.TenantId == tenantId && b.FiscalYear == year)
                .ToListAsync();

            var consolidationData = departments.Select(dept => new ConsolidationReportRow
            {
                Department = dept.Name,
                Allocated = budgets.Where(b => b.DepartmentId == dept.Id).Sum(b => b.AllocatedAmount),
                Used = budgets.Where(b => b.DepartmentId == dept.Id).Sum(b => b.UsedAmount),
                ActualSpend = budgets.Where(b => b.DepartmentId == dept.Id).Sum(b => b.ActualSpend),
                Variance = budgets.Where(b => b.DepartmentId == dept.Id).Sum(b => b.VarianceAmount),
                Remaining = budgets.Where(b => b.DepartmentId == dept.Id).Sum(b => b.RemainingAmount)
            }).ToList();

            var totalAllocated = consolidationData.Sum(d => d.Allocated);
            var totalUsed = consolidationData.Sum(d => d.Used);
            var totalActualSpend = consolidationData.Sum(d => d.ActualSpend);
            var totalVariance = consolidationData.Sum(d => d.Variance);
            var totalRemaining = consolidationData.Sum(d => d.Remaining);

            // Generate PDF using PdfReportService
            var pdfBytes = _pdfService.GenerateConsolidationReport(
                year.Value,
                consolidationData,
                totalAllocated,
                totalUsed,
                totalActualSpend,
                totalVariance,
                totalRemaining
            );

            return File(pdfBytes, "application/pdf", $"Budget_Consolidation_FY{year}_{DateTime.UtcNow:yyyyMMdd}.pdf");
        }
    }

    // ══════════════════════════════════════════════════════════════════════════════════════
    // VIEW MODELS FOR CONSOLIDATION
    // ══════════════════════════════════════════════════════════════════════════════════════

    public class DepartmentConsolidation
    {
        public Department Department { get; set; } = null!;
        public List<Budget> Budgets { get; set; } = new();
        public decimal TotalAllocated { get; set; }
        public decimal TotalUsed { get; set; }
        public decimal TotalActualSpend { get; set; }
        public decimal TotalVariance { get; set; }
        public decimal TotalRemaining => TotalAllocated - TotalUsed;
        public decimal UsagePercentage => TotalAllocated > 0 ? (TotalUsed / TotalAllocated) * 100 : 0;
        public decimal SpendPercentage => TotalAllocated > 0 ? (TotalActualSpend / TotalAllocated) * 100 : 0;
        public int PendingRequests { get; set; }
        public int ApprovedRequests { get; set; }
        public int RejectedRequests { get; set; }
        public int TransactionCount { get; set; }
        public decimal RevenueTransactions { get; set; }
        public decimal ExpenseTransactions { get; set; }
        public decimal NetIncome => RevenueTransactions - ExpenseTransactions;
    }

    public class QuarterlyConsolidation
    {
        public string Quarter { get; set; } = string.Empty;
        public decimal Allocated { get; set; }
        public decimal Used { get; set; }
        public decimal ActualSpend { get; set; }
        public decimal Variance { get; set; }
        public decimal Revenue { get; set; }
        public decimal Expenses { get; set; }
        public decimal NetIncome => Revenue - Expenses;
    }

    public class YearlyConsolidation
    {
        public int Year { get; set; }
        public decimal TotalAllocated { get; set; }
        public decimal TotalUsed { get; set; }
        public decimal TotalActualSpend { get; set; }
        public decimal TotalVariance { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetIncome => TotalRevenue - TotalExpenses;
        public decimal ProjectedRevenue { get; set; }
        public decimal ProjectedExpenses { get; set; }
        public decimal ProjectedNetIncome => ProjectedRevenue - ProjectedExpenses;
        public decimal AverageGrowthRate { get; set; }
    }

    public class ConsolidationReportRow
    {
        public string Department { get; set; } = string.Empty;
        public decimal Allocated { get; set; }
        public decimal Used { get; set; }
        public decimal ActualSpend { get; set; }
        public decimal Variance { get; set; }
        public decimal Remaining { get; set; }
    }
}
