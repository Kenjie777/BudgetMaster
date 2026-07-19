using BudgetMasterFinal.Data;
using BudgetMasterFinal.Models;
using BudgetMasterFinal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetMasterFinal.Controllers
{
    [Authorize(Policy = "SuperAdminPolicy")]
    public class SuperAdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IArchiveService _archiveService;

        public SuperAdminController(
            ApplicationDbContext ctx, 
            UserManager<ApplicationUser> um,
            IArchiveService archiveService)
        { 
            _context = ctx; 
            _userManager = um;
            _archiveService = archiveService;
        }

        public async Task<IActionResult> Tenants()
        {
            var tenants = await _context.Tenants
                .Include(t => t.Subscription)
                .ThenInclude(s => s.SubscriptionPlan)
                .Include(t => t.Users)
                .Where(t => !t.IsArchived)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
            return View(tenants);
        }

        [HttpPost, ValidateAntiForgeryToken]    
        public async Task<IActionResult> ToggleTenant(int id)
        {
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsArchived);
            if (tenant == null) return NotFound();
            tenant.IsActive = !tenant.IsActive;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Tenant {(tenant.IsActive ? "activated" : "deactivated")}.";
            return RedirectToAction(nameof(Tenants));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveTenant(int id, string? reason)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsArchived);
            
            if (tenant == null) return NotFound();

            var result = await _archiveService.ArchiveAsync<Tenant>(id, user.Id, reason);
            
            if (result)
            {
                TempData["Success"] = "Tenant archived successfully. You can restore it from the Archive page.";
            }
            else
            {
                TempData["Error"] = "Failed to archive tenant.";
            }
            
            return RedirectToAction(nameof(Tenants));
        }

        // Keep old DeleteTenant action for backward compatibility
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTenant(int id)
        {
            return await ArchiveTenant(id, "Deleted via legacy action");
        }

        public async Task<IActionResult> Subscriptions()
        {
            var subs = await _context.Subscriptions
                .Include(s => s.Tenant)
                .Include(s => s.SubscriptionPlan)
                .Where(s => !s.IsArchived)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
            return View(subs);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSubscription(int id, string status, DateTime endDate)
        {
            var sub = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsArchived);
            if (sub == null) return NotFound();
            sub.Status = status;
            sub.EndDate = endDate;
            sub.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Subscription updated.";
            return RedirectToAction(nameof(Subscriptions));
        }

        public async Task<IActionResult> AuditLogs(int page = 1)
        {
            int pageSize = 50;
            var logs = await _context.AuditLogs
                .Include(l => l.User)
                .OrderByDescending(l => l.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            ViewBag.Page = page;
            ViewBag.TotalCount = await _context.AuditLogs.CountAsync();
            return View(logs);
        }

        public async Task<IActionResult> Settings()
        {
            var settings = await _context.SystemSettings
                .Where(s => s.TenantId == null)
                .ToListAsync();
            return View(settings);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSetting(int id, string value)
        {
            var setting = await _context.SystemSettings.FindAsync(id);
            if (setting == null) return NotFound();
            setting.Value = value;
            setting.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Setting updated.";
            return RedirectToAction(nameof(Settings));
        }
    }
}
