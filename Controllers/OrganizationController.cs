using BudgetMasterFinal.Data;
using BudgetMasterFinal.Models;
using BudgetMasterFinal.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetMasterFinal.Controllers
{
    [Authorize(Policy = "AdminPolicy")]
    public class OrganizationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<OrganizationController> _logger;
        private readonly IWebHostEnvironment _environment;

        public OrganizationController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager, 
            ILogger<OrganizationController> logger,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _environment = environment;
        }

        // GET: Organization/Profile
        public async Task<IActionResult> Profile()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.TenantId == null)
                return RedirectToAction("AccessDenied", "Account");

            var tenant = await _context.Tenants
                .Include(t => t.Subscription)
                .ThenInclude(s => s.SubscriptionPlan)
                .FirstOrDefaultAsync(t => t.Id == currentUser.TenantId);

            if (tenant == null)
                return NotFound();

            var model = new OrganizationProfileViewModel
            {
                TenantId = tenant.Id,
                CompanyName = tenant.CompanyName,
                Industry = tenant.Industry,
                Phone = tenant.Phone,
                Address = tenant.Address,
                Website = tenant.Website,
                CurrencyCode = tenant.CurrencyCode,
                IsActive = tenant.IsActive,
                CreatedAt = tenant.CreatedAt,
                SubscriptionPlan = tenant.Subscription?.SubscriptionPlan?.Name,
                UserCount = await _context.Users.CountAsync(u => u.TenantId == tenant.Id && u.IsActive),
                DepartmentCount = await _context.Departments.CountAsync(d => d.TenantId == tenant.Id),
                BudgetCount = await _context.Budgets.CountAsync(b => b.TenantId == tenant.Id)
            };

            return View(model);
        }

        // GET: Organization/EditProfile
        public async Task<IActionResult> EditProfile()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.TenantId == null)
                return RedirectToAction("AccessDenied", "Account");

            var tenant = await _context.Tenants.FindAsync(currentUser.TenantId);
            if (tenant == null)
                return NotFound();

            var model = new EditOrganizationProfileViewModel
            {
                CompanyName = tenant.CompanyName,
                Industry = tenant.Industry,
                Phone = tenant.Phone,
                Address = tenant.Address,
                Website = tenant.Website,
                CurrencyCode = tenant.CurrencyCode
            };

            ViewBag.Industries = GetIndustryOptions();
            ViewBag.Currencies = GetCurrencyOptions();

            return View(model);
        }

        // POST: Organization/EditProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditOrganizationProfileViewModel model, IFormFile? logoFile)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Industries = GetIndustryOptions();
                ViewBag.Currencies = GetCurrencyOptions();
                return View(model);
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.TenantId == null)
                return RedirectToAction("AccessDenied", "Account");

            var tenant = await _context.Tenants.FindAsync(currentUser.TenantId);
            if (tenant == null)
                return NotFound();

            // Update tenant information
            tenant.CompanyName = model.CompanyName;
            tenant.Industry = model.Industry;
            tenant.Phone = model.Phone;
            tenant.Address = model.Address;
            tenant.Website = model.Website;
            tenant.CurrencyCode = model.CurrencyCode;

            // Handle logo upload
            if (logoFile != null && logoFile.Length > 0)
            {
                var logoPath = await SaveLogoAsync(logoFile, tenant.Id);
                if (!string.IsNullOrEmpty(logoPath))
                {
                    // Store logo path in system settings
                    await UpdateOrCreateSetting(tenant.Id, "CompanyLogo", logoPath, "Company logo file path");
                }
            }

            await _context.SaveChangesAsync();
            await LogActivity("UpdateProfile", "Organization", tenant.Id, $"Updated organization profile for {tenant.CompanyName}");

            TempData["Success"] = "Organization profile updated successfully.";
            return RedirectToAction(nameof(Profile));
        }

        // GET: Organization/Settings
        public async Task<IActionResult> Settings()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.TenantId == null)
                return RedirectToAction("AccessDenied", "Account");

            var settings = await _context.SystemSettings
                .Where(s => s.TenantId == currentUser.TenantId)
                .OrderBy(s => s.Group)
                .ThenBy(s => s.Key)
                .ToListAsync();

            var model = new OrganizationSettingsViewModel
            {
                TenantId = currentUser.TenantId.Value,
                Settings = settings.GroupBy(s => s.Group).ToDictionary(g => g.Key, g => g.ToList())
            };

            // Ensure default settings exist
            await EnsureDefaultSettings(currentUser.TenantId.Value);

            return View(model);
        }

        // POST: Organization/UpdateSetting
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSetting(int tenantId, string key, string value, string group = "General")
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.TenantId != tenantId)
                return Forbid();

            await UpdateOrCreateSetting(tenantId, key, value, null, group);
            await LogActivity("UpdateSetting", "SystemSetting", tenantId, $"Updated setting {key} = {value}");

            return Json(new { success = true, message = "Setting updated successfully." });
        }

        // GET: Organization/FiscalYear
        public async Task<IActionResult> FiscalYear()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.TenantId == null)
                return RedirectToAction("AccessDenied", "Account");

            var fiscalYearStart = await GetSettingValue(currentUser.TenantId.Value, "FiscalYearStart") ?? "January";
            var budgetCycle = await GetSettingValue(currentUser.TenantId.Value, "BudgetCycle") ?? "Annual";
            var approvalWorkflow = await GetSettingValue(currentUser.TenantId.Value, "ApprovalWorkflow") ?? "Standard";

            var model = new FiscalYearSettingsViewModel
            {
                FiscalYearStart = fiscalYearStart,
                BudgetCycle = budgetCycle,
                ApprovalWorkflow = approvalWorkflow,
                CurrentFiscalYear = GetCurrentFiscalYear(fiscalYearStart)
            };

            return View(model);
        }

        // POST: Organization/UpdateFiscalYear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateFiscalYear(FiscalYearSettingsViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.TenantId == null)
                return RedirectToAction("AccessDenied", "Account");

            var tenantId = currentUser.TenantId.Value;

            await UpdateOrCreateSetting(tenantId, "FiscalYearStart", model.FiscalYearStart, "Fiscal year start month", "Finance");
            await UpdateOrCreateSetting(tenantId, "BudgetCycle", model.BudgetCycle, "Budget planning cycle", "Finance");
            await UpdateOrCreateSetting(tenantId, "ApprovalWorkflow", model.ApprovalWorkflow, "Budget approval workflow type", "Finance");

            await LogActivity("UpdateFiscalYear", "Organization", tenantId, "Updated fiscal year and budget settings");

            TempData["Success"] = "Fiscal year settings updated successfully.";
            return RedirectToAction(nameof(FiscalYear));
        }

        // GET: Organization/Policies
        public async Task<IActionResult> Policies()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.TenantId == null)
                return RedirectToAction("AccessDenied", "Account");

            var tenantId = currentUser.TenantId.Value;

            var model = new OrganizationPoliciesViewModel
            {
                AutoApprovalLimit = decimal.Parse(await GetSettingValue(tenantId, "AutoApprovalLimit") ?? "0"),
                RequireJustification = bool.Parse(await GetSettingValue(tenantId, "RequireJustification") ?? "true"),
                AllowBudgetOverrun = bool.Parse(await GetSettingValue(tenantId, "AllowBudgetOverrun") ?? "false"),
                NotifyOnVariance = bool.Parse(await GetSettingValue(tenantId, "NotifyOnVariance") ?? "true"),
                VarianceThreshold = decimal.Parse(await GetSettingValue(tenantId, "VarianceThreshold") ?? "10"),
                DataRetentionMonths = int.Parse(await GetSettingValue(tenantId, "DataRetentionMonths") ?? "60"),
                BackupFrequency = await GetSettingValue(tenantId, "BackupFrequency") ?? "Daily"
            };

            return View(model);
        }

        // POST: Organization/UpdatePolicies
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePolicies(OrganizationPoliciesViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.TenantId == null)
                return RedirectToAction("AccessDenied", "Account");

            var tenantId = currentUser.TenantId.Value;

            await UpdateOrCreateSetting(tenantId, "AutoApprovalLimit", model.AutoApprovalLimit.ToString(), "Auto-approval limit for budget requests", "Policy");
            await UpdateOrCreateSetting(tenantId, "RequireJustification", model.RequireJustification.ToString(), "Require justification for budget requests", "Policy");
            await UpdateOrCreateSetting(tenantId, "AllowBudgetOverrun", model.AllowBudgetOverrun.ToString(), "Allow budget overrun", "Policy");
            await UpdateOrCreateSetting(tenantId, "NotifyOnVariance", model.NotifyOnVariance.ToString(), "Send notifications on budget variance", "Policy");
            await UpdateOrCreateSetting(tenantId, "VarianceThreshold", model.VarianceThreshold.ToString(), "Variance notification threshold percentage", "Policy");
            await UpdateOrCreateSetting(tenantId, "DataRetentionMonths", model.DataRetentionMonths.ToString(), "Data retention period in months", "Policy");
            await UpdateOrCreateSetting(tenantId, "BackupFrequency", model.BackupFrequency, "Backup frequency", "Policy");

            await LogActivity("UpdatePolicies", "Organization", tenantId, "Updated organization policies");

            TempData["Success"] = "Organization policies updated successfully.";
            return RedirectToAction(nameof(Policies));
        }

        // GET: Organization/AuditLog
        public async Task<IActionResult> AuditLog(int page = 1, string? action = null, string? entity = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.TenantId == null)
                return RedirectToAction("AccessDenied", "Account");

            const int pageSize = 50;
            var query = _context.AuditLogs
                .Include(a => a.User)
                .Where(a => a.User.TenantId == currentUser.TenantId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(action))
                query = query.Where(a => a.Action.Contains(action));

            if (!string.IsNullOrEmpty(entity))
                query = query.Where(a => a.EntityType.Contains(entity));

            var totalCount = await query.CountAsync();
            var logs = await query
                .OrderByDescending(a => a.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var model = new OrganizationAuditLogViewModel
            {
                Logs = logs,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                TotalCount = totalCount,
                ActionFilter = action,
                EntityFilter = entity
            };

            return View(model);
        }

        // Helper Methods
        private async Task<string?> SaveLogoAsync(IFormFile logoFile, int tenantId)
        {
            try
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "logos");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"tenant_{tenantId}_{Guid.NewGuid()}{Path.GetExtension(logoFile.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await logoFile.CopyToAsync(stream);
                }

                return $"/uploads/logos/{fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving logo file for tenant {TenantId}", tenantId);
                return null;
            }
        }

        private async Task UpdateOrCreateSetting(int tenantId, string key, string value, string? description = null, string group = "General")
        {
            var setting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Key == key);

            if (setting == null)
            {
                setting = new SystemSetting
                {
                    TenantId = tenantId,
                    Key = key,
                    Value = value,
                    Description = description,
                    Group = group,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.SystemSettings.Add(setting);
            }
            else
            {
                setting.Value = value;
                setting.UpdatedAt = DateTime.UtcNow;
                if (!string.IsNullOrEmpty(description))
                    setting.Description = description;
            }

            await _context.SaveChangesAsync();
        }

        private async Task<string?> GetSettingValue(int tenantId, string key)
        {
            var setting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Key == key);
            return setting?.Value;
        }

        private async Task EnsureDefaultSettings(int tenantId)
        {
            var defaultSettings = new Dictionary<string, (string Value, string Description, string Group)>
            {
                { "FiscalYearStart", ("January", "Fiscal year start month", "Finance") },
                { "BudgetCycle", ("Annual", "Budget planning cycle", "Finance") },
                { "ApprovalWorkflow", ("Standard", "Budget approval workflow type", "Finance") },
                { "AutoApprovalLimit", ("0", "Auto-approval limit for budget requests", "Policy") },
                { "RequireJustification", ("true", "Require justification for budget requests", "Policy") },
                { "AllowBudgetOverrun", ("false", "Allow budget overrun", "Policy") },
                { "NotifyOnVariance", ("true", "Send notifications on budget variance", "Policy") },
                { "VarianceThreshold", ("10", "Variance notification threshold percentage", "Policy") },
                { "DataRetentionMonths", ("60", "Data retention period in months", "Policy") },
                { "BackupFrequency", ("Daily", "Backup frequency", "Policy") }
            };

            foreach (var (key, (value, description, group)) in defaultSettings)
            {
                var exists = await _context.SystemSettings
                    .AnyAsync(s => s.TenantId == tenantId && s.Key == key);

                if (!exists)
                {
                    await UpdateOrCreateSetting(tenantId, key, value, description, group);
                }
            }
        }

        private string GetCurrentFiscalYear(string fiscalYearStart)
        {
            var now = DateTime.Now;
            var startMonth = DateTime.ParseExact(fiscalYearStart, "MMMM", null).Month;
            
            if (now.Month >= startMonth)
                return $"{now.Year}-{now.Year + 1}";
            else
                return $"{now.Year - 1}-{now.Year}";
        }

        private async Task LogActivity(string action, string entityType, int entityId, string details)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = currentUser?.Id,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                NewValues = details,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                Timestamp = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        private List<string> GetIndustryOptions()
        {
            return new List<string>
            {
                "Technology", "Healthcare", "Finance", "Manufacturing", "Retail",
                "Education", "Government", "Non-Profit", "Real Estate", "Construction",
                "Transportation", "Energy", "Media", "Hospitality", "Agriculture", "Other"
            };
        }

        private List<(string Code, string Name)> GetCurrencyOptions()
        {
            return new List<(string, string)>
            {
                ("PHP", "Philippine Peso (₱)"),
                ("USD", "US Dollar ($)"),
                ("EUR", "Euro (€)"),
                ("GBP", "British Pound (£)"),
                ("JPY", "Japanese Yen (¥)"),
                ("AUD", "Australian Dollar (A$)"),
                ("CAD", "Canadian Dollar (C$)"),
                ("SGD", "Singapore Dollar (S$)")
            };
        }
    }
}