using BudgetMasterFinal.Data;
using BudgetMasterFinal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetMasterFinal.Controllers
{
    [Authorize(Policy = "FinancialPolicy")]
    public class ScenarioController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ScenarioController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
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
            var scenarios = await _context.ScenarioPlans
                .Include(s => s.CreatedByUser)
                .Where(s => s.TenantId == tenantId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
            return View(scenarios);
        }

        public IActionResult Create() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ScenarioPlan scenario)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            scenario.TenantId = user.TenantId!.Value;
            scenario.CreatedByUserId = user.Id;
            scenario.CreatedAt = DateTime.UtcNow;
            _context.ScenarioPlans.Add(scenario);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Scenario plan created.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var tenantId = await GetTenantId();
            var scenario = await _context.ScenarioPlans.FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId);
            if (scenario == null) return NotFound();
            return View(scenario);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ScenarioPlan scenario)
        {
            var tenantId = await GetTenantId();
            var existing = await _context.ScenarioPlans.FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId);
            if (existing == null) return NotFound();
            existing.Name = scenario.Name;
            existing.Description = scenario.Description;
            existing.ScenarioType = scenario.ScenarioType;
            existing.FiscalYear = scenario.FiscalYear;
            existing.BaseRevenue = scenario.BaseRevenue;
            existing.BaseExpenses = scenario.BaseExpenses;
            existing.RevenueAdjustmentPercent = scenario.RevenueAdjustmentPercent;
            existing.ExpenseAdjustmentPercent = scenario.ExpenseAdjustmentPercent;
            existing.Status = scenario.Status;
            existing.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Scenario updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var tenantId = await GetTenantId();
            var s = await _context.ScenarioPlans.FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId);
            if (s == null) return NotFound();
            _context.ScenarioPlans.Remove(s);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Scenario deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
