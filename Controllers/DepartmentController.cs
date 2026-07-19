using BudgetMasterFinal.Data;
using BudgetMasterFinal.Models;
using BudgetMasterFinal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetMasterFinal.Controllers
{
    [Authorize(Policy = "AdminPolicy")]
    public class DepartmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IArchiveService _archiveService;

        public DepartmentController(
            ApplicationDbContext ctx, 
            UserManager<ApplicationUser> um,
            IArchiveService archiveService)
        { 
            _context = ctx; 
            _userManager = um;
            _archiveService = archiveService;
        }

        private async Task<int?> GetTenantId()
        {
            var user = await _userManager.GetUserAsync(User);
            return user?.TenantId;
        }

        public async Task<IActionResult> Index()
        {
            var tenantId = await GetTenantId();
            var depts = await _context.Departments
                .Where(d => d.TenantId == tenantId && !d.IsArchived)
                .OrderBy(d => d.Name)
                .ToListAsync();
            return View(depts);
        }

        public IActionResult Create() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Department dept)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            dept.TenantId = user.TenantId!.Value;
            dept.CreatedAt = DateTime.UtcNow;
            _context.Departments.Add(dept);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Department created.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var tenantId = await GetTenantId();
            var dept = await _context.Departments
                .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId && !d.IsArchived);
            if (dept == null) return NotFound();
            return View(dept);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Department dept)
        {
            var tenantId = await GetTenantId();
            var existing = await _context.Departments
                .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId && !d.IsArchived);
            if (existing == null) return NotFound();
            existing.Name = dept.Name;
            existing.Description = dept.Description;
            existing.HeadOfDepartment = dept.HeadOfDepartment;
            existing.BudgetAllocation = dept.BudgetAllocation;
            existing.IsActive = dept.IsActive;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Department updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Archive(int id, string? reason)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var tenantId = await GetTenantId();
            var dept = await _context.Departments
                .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId && !d.IsArchived);
            
            if (dept == null) return NotFound();

            var result = await _archiveService.ArchiveAsync<Department>(id, user.Id, reason);
            
            if (result)
            {
                TempData["Success"] = "Department archived successfully. You can restore it from the Archive page.";
            }
            else
            {
                TempData["Error"] = "Failed to archive department.";
            }
            
            return RedirectToAction(nameof(Index));
        }

        // Keep old Delete action for backward compatibility, but redirect to Archive
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            return await Archive(id, "Deleted via legacy action");
        }
    }
}
