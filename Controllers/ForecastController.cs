using BudgetMasterFinal.Data;
using BudgetMasterFinal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetMasterFinal.Controllers
{
    [Authorize(Policy = "FinancialPolicy")]
    public class ForecastController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ForecastController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<int?> GetTenantId()
        {
            var user = await _userManager.GetUserAsync(User);
            return user?.TenantId;
        }

        public async Task<IActionResult> Index()
        {
            var tenantId = await GetTenantId();
            var forecasts = await _context.Forecasts
                .Include(f => f.CreatedByUser)
                .Where(f => f.TenantId == tenantId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
            return View(forecasts);
        }

        public async Task<IActionResult> Details(int id)
        {
            var tenantId = await GetTenantId();
            var forecast = await _context.Forecasts
                .Include(f => f.CreatedByUser)
                .FirstOrDefaultAsync(f => f.Id == id && f.TenantId == tenantId);
            if (forecast == null) return NotFound();
            return View(forecast);
        }

        public IActionResult Create() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Forecast forecast)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            forecast.TenantId = user.TenantId!.Value;
            forecast.CreatedByUserId = user.Id;
            forecast.CreatedAt = DateTime.UtcNow;

            // ── AUTOMATIC CALCULATIONS ──────────────────────────────────
            // Calculate Projected Expenses from approved budgets and allocations
            await CalculateProjectedExpenses(forecast);

            // Calculate Growth Rate from historical forecast data
            await CalculateGrowthRate(forecast);

            _context.Forecasts.Add(forecast);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Forecast created successfully with calculated projections.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var tenantId = await GetTenantId();
            var forecast = await _context.Forecasts.FirstOrDefaultAsync(f => f.Id == id && f.TenantId == tenantId);
            if (forecast == null) return NotFound();
            return View(forecast);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Forecast forecast)
        {
            var tenantId = await GetTenantId();
            var existing = await _context.Forecasts.FirstOrDefaultAsync(f => f.Id == id && f.TenantId == tenantId);
            if (existing == null) return NotFound();

            // Update manual input fields only
            existing.Title = forecast.Title;
            existing.Description = forecast.Description;
            existing.FiscalYear = forecast.FiscalYear;
            existing.Period = forecast.Period;
            existing.ProjectedRevenue = forecast.ProjectedRevenue;
            existing.ConfidencePercent = forecast.ConfidencePercent;
            existing.Status = forecast.Status;
            existing.ForecastMethod = forecast.ForecastMethod;
            existing.UpdatedAt = DateTime.UtcNow;

            // ── AUTOMATIC RECALCULATIONS ──────────────────────────────────
            // Recalculate Projected Expenses from current budget data
            await CalculateProjectedExpenses(existing);

            // Recalculate Growth Rate from historical data
            await CalculateGrowthRate(existing);

            await _context.SaveChangesAsync();
            TempData["Success"] = "Forecast updated with recalculated projections.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var tenantId = await GetTenantId();
            var forecast = await _context.Forecasts.FirstOrDefaultAsync(f => f.Id == id && f.TenantId == tenantId);
            if (forecast == null) return NotFound();
            _context.Forecasts.Remove(forecast);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Forecast deleted.";
            return RedirectToAction(nameof(Index));
        }

        // ══════════════════════════════════════════════════════════════════════════════════════
        // AUTOMATIC CALCULATION METHODS - Connected Financial Planning Engine
        // ══════════════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Calculates Projected Expenses based on approved budget allocations and committed requests
        /// This connects the forecasting module to the budgeting lifecycle
        /// </summary>
        private async Task CalculateProjectedExpenses(Forecast forecast)
        {
            var tenantId = forecast.TenantId;
            var fiscalYear = forecast.FiscalYear;

            // 1. Get all approved and active budget allocations for the fiscal year
            var approvedBudgets = await _context.Budgets
                .Where(b => b.TenantId == tenantId && 
                           b.FiscalYear == fiscalYear && 
                           (b.Status == "Active" || b.Status == "Approved"))
                .ToListAsync();

            // 2. Calculate total allocated budget (committed funds)
            var totalAllocatedBudget = approvedBudgets.Sum(b => b.AllocatedAmount);

            // 3. Get all approved budget requests for the fiscal year (additional commitments)
            var approvedRequests = await _context.BudgetRequests
                .Where(r => r.TenantId == tenantId && 
                           r.Status == "Approved" && 
                           r.CreatedAt.Year == fiscalYear)
                .ToListAsync();

            var totalApprovedRequests = approvedRequests.Sum(r => r.RequestedAmount);

            // 4. Get current budget usage (already committed/used funds)
            var totalUsedAmount = approvedBudgets.Sum(b => b.UsedAmount);

            // 5. Calculate projected expenses based on forecast method
            decimal projectedExpenses = 0;
            string calculationNotes = "";

            switch (forecast.ForecastMethod)
            {
                case "Linear":
                    // Linear: Use allocated budgets + approved requests as baseline
                    projectedExpenses = totalAllocatedBudget + totalApprovedRequests;
                    calculationNotes = $"Linear projection: ₱{totalAllocatedBudget:N2} (allocated) + ₱{totalApprovedRequests:N2} (approved requests)";
                    break;

                case "Exponential":
                    // Exponential: Apply growth factor based on usage trends
                    var usageRate = totalAllocatedBudget > 0 ? (double)(totalUsedAmount / totalAllocatedBudget) : 0;
                    var growthFactor = 1 + (usageRate * 0.2); // 20% growth factor based on usage
                    projectedExpenses = (decimal)((double)totalAllocatedBudget * growthFactor) + totalApprovedRequests;
                    calculationNotes = $"Exponential projection: ₱{totalAllocatedBudget:N2} × {growthFactor:F2} + ₱{totalApprovedRequests:N2} (requests)";
                    break;

                case "MovingAverage":
                    // Moving Average: Average of allocated, used, and requested amounts
                    var avgExpense = (totalAllocatedBudget + totalUsedAmount + totalApprovedRequests) / 3;
                    projectedExpenses = avgExpense;
                    calculationNotes = $"Moving average: (₱{totalAllocatedBudget:N2} + ₱{totalUsedAmount:N2} + ₱{totalApprovedRequests:N2}) ÷ 3";
                    break;

                default:
                    projectedExpenses = totalAllocatedBudget;
                    calculationNotes = $"Default: Total allocated budget ₱{totalAllocatedBudget:N2}";
                    break;
            }

            // 6. Apply period adjustment if not annual
            if (forecast.Period == "Quarterly")
            {
                projectedExpenses = projectedExpenses / 4;
                calculationNotes += " (quarterly adjustment: ÷ 4)";
            }
            else if (forecast.Period == "Monthly")
            {
                projectedExpenses = projectedExpenses / 12;
                calculationNotes += " (monthly adjustment: ÷ 12)";
            }

            // 7. Update forecast with calculated values
            forecast.ProjectedExpenses = projectedExpenses;
            forecast.ExpenseCalculationNotes = calculationNotes;
        }

        /// <summary>
        /// Calculates Growth Rate by comparing current forecast against previous periods
        /// Uses historical forecast data to determine growth trends
        /// </summary>
        private async Task CalculateGrowthRate(Forecast forecast)
        {
            var tenantId = forecast.TenantId;
            var currentYear = forecast.FiscalYear;
            var previousYear = currentYear - 1;

            // Find previous year's forecast for comparison
            var previousForecast = await _context.Forecasts
                .Where(f => f.TenantId == tenantId && 
                           f.FiscalYear == previousYear && 
                           f.Period == forecast.Period)
                .OrderByDescending(f => f.CreatedAt)
                .FirstOrDefaultAsync();

            if (previousForecast != null)
            {
                // Calculate growth rate: ((Current - Previous) / Previous) × 100
                var currentTotal = forecast.ProjectedRevenue - forecast.ProjectedExpenses;
                var previousTotal = previousForecast.ProjectedRevenue - previousForecast.ProjectedExpenses;

                if (previousTotal != 0)
                {
                    var growthRate = ((currentTotal - previousTotal) / previousTotal) * 100;
                    forecast.GrowthRatePercent = growthRate;
                    forecast.GrowthRateCalculationNotes = $"Compared to FY{previousYear} {forecast.Period} forecast: " +
                        $"(₱{currentTotal:N2} - ₱{previousTotal:N2}) / ₱{previousTotal:N2} × 100 = {growthRate:F2}%";
                }
                else
                {
                    forecast.GrowthRatePercent = 0;
                    forecast.GrowthRateCalculationNotes = $"Previous year net income was zero, growth rate set to 0%";
                }
            }
            else
            {
                // No previous forecast found - check if there are any budgets from previous year
                var previousYearBudgets = await _context.Budgets
                    .Where(b => b.TenantId == tenantId && b.FiscalYear == previousYear)
                    .ToListAsync();

                if (previousYearBudgets.Any())
                {
                    var previousYearTotal = previousYearBudgets.Sum(b => b.AllocatedAmount);
                    var currentTotal = forecast.ProjectedExpenses;

                    if (previousYearTotal != 0)
                    {
                        var growthRate = ((currentTotal - previousYearTotal) / previousYearTotal) * 100;
                        forecast.GrowthRatePercent = growthRate;
                        forecast.GrowthRateCalculationNotes = $"Compared to FY{previousYear} budget allocations: " +
                            $"(₱{currentTotal:N2} - ₱{previousYearTotal:N2}) / ₱{previousYearTotal:N2} × 100 = {growthRate:F2}%";
                    }
                    else
                    {
                        forecast.GrowthRatePercent = 0;
                        forecast.GrowthRateCalculationNotes = "Previous year budget was zero, growth rate set to 0%";
                    }
                }
                else
                {
                    forecast.GrowthRatePercent = 0;
                    forecast.GrowthRateCalculationNotes = $"No historical data found for FY{previousYear}, growth rate set to 0%";
                }
            }
        }

        // ── API ENDPOINT: Recalculate Forecast ──────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> RecalculateForecast(int id)
        {
            var tenantId = await GetTenantId();
            var forecast = await _context.Forecasts.FirstOrDefaultAsync(f => f.Id == id && f.TenantId == tenantId);
            
            if (forecast == null) return NotFound();

            // Recalculate both expenses and growth rate
            await CalculateProjectedExpenses(forecast);
            await CalculateGrowthRate(forecast);
            forecast.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                projectedExpenses = forecast.ProjectedExpenses,
                growthRate = forecast.GrowthRatePercent,
                expenseNotes = forecast.ExpenseCalculationNotes,
                growthNotes = forecast.GrowthRateCalculationNotes
            });
        }
    }
}
