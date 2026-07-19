using BudgetMasterFinal.Data;
using BudgetMasterFinal.Models;
using BudgetMasterFinal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetMasterFinal.Controllers
{
    [Authorize(Roles = "SuperAdmin,CompanyAdmin,Tenant")]
    public class ArchiveController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IArchiveService _archiveService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ArchiveController> _logger;

        public ArchiveController(
            ApplicationDbContext context,
            IArchiveService archiveService,
            UserManager<ApplicationUser> userManager,
            ILogger<ArchiveController> logger)
        {
            _context = context;
            _archiveService = archiveService;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: /Archive/Index
        public async Task<IActionResult> Index(string? entityType = null, string? search = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Get tenant ID (null for SuperAdmin)
            int? tenantId = User.IsInRole("SuperAdmin") ? null : currentUser.TenantId;

            // Get archived items
            var archivedItems = await _archiveService.GetArchivedItemsAsync(tenantId, entityType);

            // Load user information for ArchivedBy
            var userIds = archivedItems.Select(a => a.ArchivedBy).Distinct().ToList();
            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.FullName);
            
            ViewBag.Users = users;

            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                archivedItems = archivedItems
                    .Where(a => (a.EntityName != null && a.EntityName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                               (a.ArchiveReason != null && a.ArchiveReason.Contains(search, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            // Apply date range filter
            if (fromDate.HasValue)
            {
                archivedItems = archivedItems.Where(a => a.ArchivedAt >= fromDate.Value).ToList();
            }

            if (toDate.HasValue)
            {
                archivedItems = archivedItems.Where(a => a.ArchivedAt <= toDate.Value.AddDays(1)).ToList();
            }

            // Get entity type counts for tabs
            var entityCounts = archivedItems
                .GroupBy(a => a.EntityType)
                .ToDictionary(g => g.Key, g => g.Count());

            ViewBag.EntityCounts = entityCounts;
            ViewBag.CurrentEntityType = entityType;
            ViewBag.Search = search;
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;
            ViewBag.TotalCount = archivedItems.Count;

            return View(archivedItems);
        }

        // POST: /Archive/Restore
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(string entityType, string entityId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            try
            {
                bool result = false;

                // Route to appropriate restore method based on entity type
                switch (entityType)
                {
                    case "ApplicationUser":
                        result = await _archiveService.RestoreAsync<ApplicationUser>(entityId, currentUser.Id);
                        break;
                    case "Department":
                        result = await _archiveService.RestoreAsync<Department>(int.Parse(entityId), currentUser.Id);
                        break;
                    case "Budget":
                        result = await _archiveService.RestoreAsync<Budget>(int.Parse(entityId), currentUser.Id);
                        break;
                    case "BudgetRequest":
                        result = await _archiveService.RestoreAsync<BudgetRequest>(int.Parse(entityId), currentUser.Id);
                        break;
                    case "Forecast":
                        result = await _archiveService.RestoreAsync<Forecast>(int.Parse(entityId), currentUser.Id);
                        break;
                    case "ScenarioPlan":
                        result = await _archiveService.RestoreAsync<ScenarioPlan>(int.Parse(entityId), currentUser.Id);
                        break;
                    case "Notification":
                        result = await _archiveService.RestoreAsync<Notification>(int.Parse(entityId), currentUser.Id);
                        break;
                    case "ActualTransaction":
                        result = await _archiveService.RestoreAsync<ActualTransaction>(int.Parse(entityId), currentUser.Id);
                        break;
                    case "Subscription":
                        result = await _archiveService.RestoreAsync<Subscription>(int.Parse(entityId), currentUser.Id);
                        break;
                    case "SubscriptionPlan":
                        result = await _archiveService.RestoreAsync<SubscriptionPlan>(int.Parse(entityId), currentUser.Id);
                        break;
                    case "BillingTransaction":
                        result = await _archiveService.RestoreAsync<BillingTransaction>(int.Parse(entityId), currentUser.Id);
                        break;
                    case "BillingDiscount":
                        result = await _archiveService.RestoreAsync<BillingDiscount>(int.Parse(entityId), currentUser.Id);
                        break;
                    case "Tenant":
                        if (!User.IsInRole("SuperAdmin"))
                        {
                            return Json(new { success = false, message = "Only SuperAdmin can restore tenants" });
                        }
                        result = await _archiveService.RestoreAsync<Tenant>(int.Parse(entityId), currentUser.Id);
                        break;
                    default:
                        return Json(new { success = false, message = "Unknown entity type" });
                }

                if (result)
                {
                    return Json(new { success = true, message = $"{entityType} restored successfully" });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to restore item" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error restoring {entityType} with ID {entityId}");
                return Json(new { success = false, message = "An error occurred while restoring the item" });
            }
        }

        // POST: /Archive/PermanentDelete
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "SuperAdminPolicy")]
        public async Task<IActionResult> PermanentDelete(string entityType, string entityId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            try
            {
                bool result = false;

                // Route to appropriate delete method based on entity type
                switch (entityType)
                {
                    case "Department":
                        result = await _archiveService.PermanentDeleteAsync<Department>(int.Parse(entityId));
                        break;
                    case "Budget":
                        result = await _archiveService.PermanentDeleteAsync<Budget>(int.Parse(entityId));
                        break;
                    case "BudgetRequest":
                        result = await _archiveService.PermanentDeleteAsync<BudgetRequest>(int.Parse(entityId));
                        break;
                    case "Forecast":
                        result = await _archiveService.PermanentDeleteAsync<Forecast>(int.Parse(entityId));
                        break;
                    case "ScenarioPlan":
                        result = await _archiveService.PermanentDeleteAsync<ScenarioPlan>(int.Parse(entityId));
                        break;
                    case "Notification":
                        result = await _archiveService.PermanentDeleteAsync<Notification>(int.Parse(entityId));
                        break;
                    case "ActualTransaction":
                        result = await _archiveService.PermanentDeleteAsync<ActualTransaction>(int.Parse(entityId));
                        break;
                    case "Subscription":
                        result = await _archiveService.PermanentDeleteAsync<Subscription>(int.Parse(entityId));
                        break;
                    case "SubscriptionPlan":
                        result = await _archiveService.PermanentDeleteAsync<SubscriptionPlan>(int.Parse(entityId));
                        break;
                    case "BillingTransaction":
                        result = await _archiveService.PermanentDeleteAsync<BillingTransaction>(int.Parse(entityId));
                        break;
                    case "BillingDiscount":
                        result = await _archiveService.PermanentDeleteAsync<BillingDiscount>(int.Parse(entityId));
                        break;
                    case "Tenant":
                        result = await _archiveService.PermanentDeleteAsync<Tenant>(int.Parse(entityId));
                        break;
                    default:
                        return Json(new { success = false, message = "Unknown entity type" });
                }

                if (result)
                {
                    _logger.LogWarning($"SuperAdmin {currentUser.Email} permanently deleted {entityType} with ID {entityId}");
                    return Json(new { success = true, message = $"{entityType} permanently deleted" });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to delete item" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error permanently deleting {entityType} with ID {entityId}");
                return Json(new { success = false, message = "An error occurred while deleting the item" });
            }
        }

        // GET: /Archive/Details
        public async Task<IActionResult> Details(int id)
        {
            var archivedItem = await _context.ArchivedItems
                .Include(a => a.Tenant)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (archivedItem == null)
            {
                return NotFound();
            }

            // Check tenant access
            var currentUser = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("SuperAdmin") && archivedItem.TenantId != currentUser?.TenantId)
            {
                return Forbid();
            }

            return View(archivedItem);
        }

        // GET: /Archive/Stats
        public async Task<IActionResult> Stats()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int? tenantId = User.IsInRole("SuperAdmin") ? null : currentUser.TenantId;

            var archivedItems = await _archiveService.GetArchivedItemsAsync(tenantId);

            var stats = new
            {
                TotalArchived = archivedItems.Count,
                ByEntityType = archivedItems.GroupBy(a => a.EntityType)
                    .Select(g => new { EntityType = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToList(),
                RecentArchives = archivedItems.OrderByDescending(a => a.ArchivedAt).Take(10).ToList(),
                ByMonth = archivedItems.GroupBy(a => new { a.ArchivedAt.Year, a.ArchivedAt.Month })
                    .Select(g => new { 
                        Month = $"{g.Key.Year}-{g.Key.Month:D2}", 
                        Count = g.Count() 
                    })
                    .OrderByDescending(x => x.Month)
                    .Take(12)
                    .ToList()
            };

            return View(stats);
        }
    }
}
