using BudgetMasterFinal.Data;
using BudgetMasterFinal.Models;
using BudgetMasterFinal.Models.ViewModels;
using BudgetMasterFinal.Services;
using BudgetMasterFinal.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetMasterFinal.Controllers
{
    [Authorize(Policy = "AdminPolicy")]
    public class UserManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly IArchiveService _archiveService;

        public UserManagementController(
            ApplicationDbContext ctx, 
            UserManager<ApplicationUser> um,
            IEmailService emailService,
            IConfiguration configuration,
            IArchiveService archiveService)
        { 
            _context = ctx; 
            _userManager = um;
            _emailService = emailService;
            _configuration = configuration;
            _archiveService = archiveService;
        }

        private async Task<ApplicationUser?> GetCurrentUser() => await _userManager.GetUserAsync(User);

        public async Task<IActionResult> Index()
        {
            var currentUser = await GetCurrentUser();
            if (currentUser == null) return Unauthorized();

            IQueryable<ApplicationUser> query = _context.Users.Include(u => u.Department);
            if (currentUser.Role == "SuperAdmin")
                query = query.Where(u => u.Role == "CompanyAdmin" && !u.IsArchived); // SuperAdmin can only see CompanyAdmin users
            else
                query = query.Where(u => u.TenantId == currentUser.TenantId && u.Role != "SuperAdmin" && !u.IsArchived);

            var users = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();
            return View(users);
        }

        public async Task<IActionResult> Create()
        {
            var currentUser = await GetCurrentUser();
            var vm = new AdminUserCreateViewModel
            {
                Departments = await _context.Departments
                    .Where(d => d.TenantId == currentUser!.TenantId && d.IsActive && !d.IsArchived)
                    .ToListAsync()
            };
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminUserCreateViewModel vm)
        {
            var currentUser = await GetCurrentUser();
            if (currentUser == null) return Unauthorized();

            if (!ModelState.IsValid)
            {
                vm.Departments = await _context.Departments
                    .Where(d => d.TenantId == currentUser.TenantId && d.IsActive && !d.IsArchived)
                    .ToListAsync();
                return View(vm);
            }

            var allowedRoles = new[] { "FinanceManager", "Accountant", "DepartmentHead", "Auditor", "Employee" };
            if (!allowedRoles.Contains(vm.Role)) vm.Role = "Employee";

            // Generate secure temporary password
            var temporaryPassword = PasswordGenerator.GenerateTemporaryPassword(16);

            var user = new ApplicationUser
            {
                UserName = vm.Email,
                Email = vm.Email,
                FirstName = vm.FirstName,
                LastName = vm.LastName,
                TenantId = currentUser.TenantId,
                DepartmentId = vm.DepartmentId,
                Role = vm.Role,
                IsActive = true,
                EmailConfirmed = true,
                MustChangePassword = true // Force password change on first login
            };
            
            var result = await _userManager.CreateAsync(user, temporaryPassword);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, vm.Role);
                
                // Get organization name
                var tenant = await _context.Tenants.FindAsync(currentUser.TenantId);
                var organizationName = tenant?.CompanyName ?? "Your Organization";
                
                // Get login URL
                var loginUrl = $"{Request.Scheme}://{Request.Host}/Account/Login";
                
                // Send welcome email with temporary password
                var emailSent = await _emailService.SendWelcomeEmailAsync(
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    user.Role,
                    temporaryPassword,
                    organizationName,
                    loginUrl
                );
                
                if (emailSent)
                {
                    TempData["Success"] = $"User created successfully! A welcome email with temporary password has been sent to {user.Email}.";
                }
                else
                {
                    TempData["Warning"] = $"User created successfully with role: {vm.Role}, but the welcome email could not be sent. Please check your email configuration or manually provide the temporary password to the user.";
                }
                
                return RedirectToAction(nameof(Index));
            }
            
            foreach (var err in result.Errors)
                ModelState.AddModelError(string.Empty, err.Description);
            vm.Departments = await _context.Departments
                .Where(d => d.TenantId == currentUser.TenantId && d.IsActive && !d.IsArchived)
                .ToListAsync();
            return View(vm);
        }

        public async Task<IActionResult> Edit(string id)
        {
            var user = await _context.Users.Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsArchived);
            if (user == null) return NotFound();
            var currentUser = await GetCurrentUser();
            if (currentUser!.Role != "SuperAdmin" && user.TenantId != currentUser.TenantId) return Forbid();
            ViewBag.Departments = await _context.Departments
                .Where(d => d.TenantId == user.TenantId && d.IsActive && !d.IsArchived)
                .ToListAsync();
            return View(user);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, string firstName, string lastName, int? departmentId, bool isActive)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();
            user.FirstName = firstName;
            user.LastName = lastName;
            user.DepartmentId = departmentId;
            user.IsActive = isActive;
            await _context.SaveChangesAsync();
            TempData["Success"] = "User updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Archive(string id, string? reason)
        {
            var currentUser = await GetCurrentUser();
            if (currentUser == null) return Unauthorized();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null || user.Id == currentUser.Id)
            {
                TempData["Error"] = "Cannot archive user. User not found or you cannot archive yourself.";
                return RedirectToAction(nameof(Index));
            }

            // Check if user is already archived
            if (user.IsArchived)
            {
                TempData["Error"] = "User is already archived.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _archiveService.ArchiveAsync<ApplicationUser>(id, currentUser.Id, reason);
            
            if (result)
            {
                TempData["Success"] = "User archived successfully. You can restore it from the Archive page.";
            }
            else
            {
                TempData["Error"] = "Failed to archive user.";
            }
            
            return RedirectToAction(nameof(Index));
        }

        // Keep old Delete action for backward compatibility, but redirect to Archive
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            return await Archive(id, "Deleted via legacy action");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string id, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Password reset." : string.Join(", ", result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(string id)
        {
            var currentUser = await GetCurrentUser();
            if (currentUser == null) return Unauthorized();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            // Prevent user from deactivating themselves
            if (user.Id == currentUser.Id)
            {
                TempData["Error"] = "You cannot deactivate your own account.";
                return RedirectToAction(nameof(Index));
            }

            // Check tenant access
            if (currentUser.Role != "SuperAdmin" && user.TenantId != currentUser.TenantId)
            {
                return Forbid();
            }

            // Toggle the IsActive status
            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"User {(user.IsActive ? "activated" : "deactivated")} successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
