using BudgetMasterFinal.Data;
using BudgetMasterFinal.Models;
using BudgetMasterFinal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetMasterFinal.Controllers
{
    [Authorize] // Changed from FinancialPolicy to basic authorization
    public class BudgetController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly NotificationService _notificationService;
        private readonly IArchiveService _archiveService;

        public BudgetController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager, 
            NotificationService notificationService,
            IArchiveService archiveService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
            _archiveService = archiveService;
        }

        private async Task<int?> GetTenantId()
        {
            var user = await _userManager.GetUserAsync(User);
            return user?.TenantId;
        }

        private async Task<(int? TenantId, int? DepartmentId, string Role)> GetUserContext()
        {
            var user = await _userManager.GetUserAsync(User);
            return (user?.TenantId, user?.DepartmentId, user?.Role ?? "");
        }

        [Authorize(Policy = "FinancialPolicy")] // Only Finance Manager can see all budgets
        public async Task<IActionResult> Index()
        {
            var (tenantId, departmentId, role) = await GetUserContext();
            
            var query = _context.Budgets
                .Include(b => b.Department)
                .Include(b => b.CreatedByUser)
                .Where(b => b.TenantId == tenantId && !b.IsArchived);

            // Department Head can only see their department's budgets
            if (role == "DepartmentHead" && departmentId.HasValue)
            {
                query = query.Where(b => b.DepartmentId == departmentId.Value);
            }
            // Finance Manager can see all budgets (financial authority)
            // Admin cannot access budgets (no financial authority)

            var budgets = await query
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
                
            return View(budgets);
        }

        public async Task<IActionResult> Details(int id)
        {
            var tenantId = await GetTenantId();
            var budget = await _context.Budgets
                .Include(b => b.Department)
                .Include(b => b.CreatedByUser)
                .Include(b => b.Approvals).ThenInclude(a => a.ApprovedByUser)
                .Include(b => b.BudgetRequests).ThenInclude(r => r.RequestedByUser)
                .FirstOrDefaultAsync(b => b.Id == id && b.TenantId == tenantId && !b.IsArchived);
            if (budget == null) return NotFound();
            return View(budget);
        }

        [Authorize(Policy = "FinancialPolicy")] // Only Finance Manager can create budgets
        public async Task<IActionResult> Create()
        {
            var tenantId = await GetTenantId();
            ViewBag.Departments = await _context.Departments
                .Where(d => d.TenantId == tenantId && d.IsActive && !d.IsArchived)
                .ToListAsync();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Policy = "FinancialPolicy")] // Only Finance Manager can create budgets
        public async Task<IActionResult> Create(Budget budget)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Remove TenantId from ModelState since it's set by the controller
            ModelState.Remove("TenantId");
            ModelState.Remove("Tenant");

            budget.TenantId = user.TenantId!.Value;
            budget.CreatedByUserId = user.Id;
            budget.CreatedAt = DateTime.UtcNow;

            if (!ModelState.IsValid)
            {
                ViewBag.Departments = await _context.Departments
                    .Where(d => d.TenantId == user.TenantId && d.IsActive && !d.IsArchived)
                    .ToListAsync();
                return View(budget);
            }

            _context.Budgets.Add(budget);
            await _context.SaveChangesAsync();

            // Send role-based notifications
            await _notificationService.NotifyBudgetCreated(budget, user.Id);

            TempData["Success"] = "Budget created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "FinancialPolicy")] // Only Finance Manager can edit budgets
        public async Task<IActionResult> Edit(int id)
        {
            var tenantId = await GetTenantId();
            var budget = await _context.Budgets
                .FirstOrDefaultAsync(b => b.Id == id && b.TenantId == tenantId && !b.IsArchived);
            if (budget == null) return NotFound();
            ViewBag.Departments = await _context.Departments
                .Where(d => d.TenantId == tenantId && d.IsActive && !d.IsArchived)
                .ToListAsync();
            return View(budget);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Policy = "FinancialPolicy")] // Only Finance Manager can edit budgets
        public async Task<IActionResult> Edit(int id, Budget budget)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            
            var tenantId = await GetTenantId();
            var existing = await _context.Budgets
                .Include(b => b.Department)
                .FirstOrDefaultAsync(b => b.Id == id && b.TenantId == tenantId && !b.IsArchived);
            if (existing == null) return NotFound();

            existing.Title = budget.Title;
            existing.Description = budget.Description;
            existing.AllocatedAmount = budget.AllocatedAmount;
            existing.FiscalYear = budget.FiscalYear;
            existing.Quarter = budget.Quarter;
            existing.Status = budget.Status;
            existing.DepartmentId = budget.DepartmentId;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            
            // Send role-based notifications
            await _notificationService.NotifyBudgetUpdated(existing, user.Id);
            
            TempData["Success"] = "Budget updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Policy = "FinancialPolicy")] // Only Finance Manager can archive budgets
        public async Task<IActionResult> Archive(int id, string? reason)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var tenantId = await GetTenantId();
            var budget = await _context.Budgets
                .FirstOrDefaultAsync(b => b.Id == id && b.TenantId == tenantId && !b.IsArchived);
            
            if (budget == null) return NotFound();

            var result = await _archiveService.ArchiveAsync<Budget>(id, user.Id, reason);
            
            if (result)
            {
                TempData["Success"] = "Budget archived successfully. You can restore it from the Archive page.";
            }
            else
            {
                TempData["Error"] = "Failed to archive budget.";
            }
            
            return RedirectToAction(nameof(Index));
        }

        // Keep old Delete action for backward compatibility
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Policy = "FinancialPolicy")] // Only Finance Manager can delete budgets
        public async Task<IActionResult> Delete(int id)
        {
            return await Archive(id, "Deleted via legacy action");
        }

        // Budget Requests - Finance Manager sees ONLY operationally approved requests, Department Head sees only their department's pending requests
        [Authorize(Policy = "DepartmentOperationsPolicy")] // Finance Manager + Department Head
        public async Task<IActionResult> Requests()
        {
            var (tenantId, departmentId, role) = await GetUserContext();
            
            if (role == "DepartmentHead" && departmentId.HasValue)
            {
                // Department Head can only see their department's requests that are pending operational approval
                var deptRequests = await _context.BudgetRequests
                    .Include(r => r.Department)
                    .Include(r => r.RequestedByUser)
                    .Include(r => r.OperationallyApprovedByUser)
                    .Include(r => r.Budget)
                    .Where(r => r.TenantId == tenantId && 
                               r.DepartmentId == departmentId.Value && 
                               r.Status == "Pending")
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();
                return View(deptRequests);
            }
            else if (role == "FinanceManager")
            {
                // Finance Manager can ONLY see requests that meet ALL these criteria:
                // 1. Status is "OperationallyApproved"
                // 2. Has a valid operational approver ID
                // 3. Has an operational approval date
                // 4. The operational approver is actually a Department Head
                var financeRequests = await _context.BudgetRequests
                    .Include(r => r.Department)
                    .Include(r => r.RequestedByUser)
                    .Include(r => r.OperationallyApprovedByUser)
                    .Include(r => r.Budget)
                    .Where(r => r.TenantId == tenantId && 
                               r.Status == "OperationallyApproved" &&
                               r.OperationallyApprovedByUserId != null &&
                               r.OperationalApprovalDate != null &&
                               r.OperationallyApprovedByUser!.Role == "DepartmentHead")
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();
                return View(financeRequests);
            }
            else
            {
                // No other roles should access this endpoint
                TempData["Error"] = "Access denied. Invalid role for budget request management.";
                return RedirectToAction("Index", "Dashboard");
            }
        }

        // Finance Manager: ONLY operationally approved requests ready for financial validation
        [Authorize(Policy = "FinancialPolicy")] // Finance Manager ONLY
        public async Task<IActionResult> FinancialValidation()
        {
            var tenantId = await GetTenantId();
            
            // Ultra-strict filtering: Finance Manager can ONLY see requests that:
            // 1. Are operationally approved
            // 2. Have valid Department Head approval
            // 3. Are not already processed by Finance Manager
            var requests = await _context.BudgetRequests
                .Include(r => r.Department)
                .Include(r => r.RequestedByUser)
                .Include(r => r.OperationallyApprovedByUser)
                .Include(r => r.Budget)
                .Where(r => r.TenantId == tenantId && 
                           r.Status == "OperationallyApproved" &&
                           r.OperationallyApprovedByUserId != null &&
                           r.OperationalApprovalDate != null &&
                           r.OperationallyApprovedByUser!.Role == "DepartmentHead" &&
                           r.FinanciallyApprovedByUserId == null &&
                           r.FinanciallyRejectedByUserId == null)
                .OrderByDescending(r => r.OperationalApprovalDate)
                .ToListAsync();
                
            ViewBag.PageTitle = "Financial Validation Queue";
            ViewBag.PageSubtitle = "Requests operationally approved by Department Heads awaiting financial validation";
            
            return View("Requests", requests);
        }
        [Authorize(Policy = "FinancialPolicy")] // Finance Manager only
        public async Task<IActionResult> AllRequests()
        {
            var tenantId = await GetTenantId();
            
            // Finance Manager can see all requests but with proper workflow validation
            var requests = await _context.BudgetRequests
                .Include(r => r.Department)
                .Include(r => r.RequestedByUser)
                .Include(r => r.OperationallyApprovedByUser)
                .Include(r => r.FinanciallyApprovedByUser)
                .Include(r => r.FinanciallyRejectedByUser)
                .Include(r => r.Budget)
                .Where(r => r.TenantId == tenantId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
                
            // Add a note to the ViewBag to explain what Finance Manager can see
            ViewBag.WorkflowNote = "As Finance Manager, you can see all requests for reporting purposes, but can only take action on operationally approved requests.";
            
            return View(requests);
        }

        // Debug method to check request statuses - remove in production
        [Authorize(Policy = "FinancialPolicy")]
        public async Task<IActionResult> DebugRequests()
        {
            var tenantId = await GetTenantId();
            
            var allRequests = await _context.BudgetRequests
                .Include(r => r.RequestedByUser)
                .Include(r => r.OperationallyApprovedByUser)
                .Where(r => r.TenantId == tenantId)
                .Select(r => new {
                    r.Id,
                    r.Title,
                    r.Status,
                    RequesterName = r.RequestedByUser!.FullName,
                    RequesterRole = r.RequestedByUser.Role,
                    OperationalApprover = r.OperationallyApprovedByUser != null ? r.OperationallyApprovedByUser.FullName : "None",
                    OperationalApproverRole = r.OperationallyApprovedByUser != null ? r.OperationallyApprovedByUser.Role : "None",
                    r.OperationalApprovalDate,
                    r.CreatedAt
                })
                .ToListAsync();
                
            return Json(allRequests);
        }

        // Fix invalid request data - run this once to clean up
        [Authorize(Policy = "FinancialPolicy")]
        public async Task<IActionResult> FixRequestData()
        {
            var tenantId = await GetTenantId();
            
            // Find requests that are marked as "OperationallyApproved" but don't have proper Department Head approval
            var invalidRequests = await _context.BudgetRequests
                .Include(r => r.OperationallyApprovedByUser)
                .Where(r => r.TenantId == tenantId && 
                           r.Status == "OperationallyApproved" &&
                           (r.OperationallyApprovedByUserId == null ||
                            r.OperationalApprovalDate == null ||
                            r.OperationallyApprovedByUser!.Role != "DepartmentHead"))
                .ToListAsync();

            foreach (var request in invalidRequests)
            {
                // Reset to Pending status
                request.Status = "Pending";
                request.OperationallyApprovedByUserId = null;
                request.OperationalApprovalDate = null;
                request.OperationalApprovalNotes = null;
                request.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            
            return Json(new { 
                message = $"Fixed {invalidRequests.Count} invalid requests", 
                fixedRequests = invalidRequests.Select(r => new { r.Id, r.Title }).ToList() 
            });
        }

        // Department Head: Department-specific budget requests
        [Authorize(Policy = "DepartmentHeadPolicy")]
        public async Task<IActionResult> DepartmentRequests()
        {
            var (tenantId, departmentId, role) = await GetUserContext();
            
            if (departmentId == null)
            {
                TempData["Error"] = "You are not assigned to a department.";
                return RedirectToAction("Index", "Dashboard");
            }

            var requests = await _context.BudgetRequests
                .Include(r => r.Department)
                .Include(r => r.RequestedByUser)
                .Include(r => r.Budget)
                .Where(r => r.TenantId == tenantId && r.DepartmentId == departmentId.Value)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            
            ViewBag.Department = await _context.Departments.FirstOrDefaultAsync(d => d.Id == departmentId.Value);
            return View(requests);
        }

        // Department Head: Operational approval workflow
        [Authorize(Policy = "DepartmentHeadPolicy")]
        public async Task<IActionResult> RequestApproval()
        {
            var (tenantId, departmentId, role) = await GetUserContext();
            
            if (departmentId == null)
            {
                TempData["Error"] = "You are not assigned to a department.";
                return RedirectToAction("Index", "Dashboard");
            }

            var pendingRequests = await _context.BudgetRequests
                .Include(r => r.Department)
                .Include(r => r.RequestedByUser)
                .Include(r => r.Budget)
                .Where(r => r.TenantId == tenantId && 
                           r.DepartmentId == departmentId.Value && 
                           r.Status == "Pending")
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            
            ViewBag.Department = await _context.Departments.FirstOrDefaultAsync(d => d.Id == departmentId.Value);
            return View(pendingRequests);
        }

        // Department Head: Department-specific budgets
        [Authorize(Policy = "DepartmentHeadPolicy")]
        public async Task<IActionResult> DepartmentBudgets()
        {
            var (tenantId, departmentId, role) = await GetUserContext();
            
            if (departmentId == null)
            {
                TempData["Error"] = "You are not assigned to a department.";
                return RedirectToAction("Index", "Dashboard");
            }

            var budgets = await _context.Budgets
                .Include(b => b.Department)
                .Include(b => b.CreatedByUser)
                .Where(b => b.TenantId == tenantId && b.DepartmentId == departmentId.Value && !b.IsArchived)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
            
            ViewBag.Department = await _context.Departments.FirstOrDefaultAsync(d => d.Id == departmentId.Value);
            return View(budgets);
        }

        // Department Head: Budget proposals for their department
        [Authorize(Policy = "DepartmentHeadPolicy")]
        public async Task<IActionResult> DepartmentProposal()
        {
            var (tenantId, departmentId, role) = await GetUserContext();
            
            if (departmentId == null)
            {
                TempData["Error"] = "You are not assigned to a department.";
                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.Department = await _context.Departments.FirstOrDefaultAsync(d => d.Id == departmentId.Value);
            ViewBag.Budgets = await _context.Budgets
                .Where(b => b.TenantId == tenantId && b.DepartmentId == departmentId.Value && b.Status == "Active" && !b.IsArchived)
                .ToListAsync();
            
            return View();
        }

        // Department Head: Team activity monitoring
        [Authorize(Policy = "DepartmentHeadPolicy")]
        public async Task<IActionResult> TeamActivity()
        {
            var (tenantId, departmentId, role) = await GetUserContext();
            
            if (departmentId == null)
            {
                TempData["Error"] = "You are not assigned to a department.";
                return RedirectToAction("Index", "Dashboard");
            }

            var teamMembers = await _context.Users
                .Where(u => u.TenantId == tenantId && u.DepartmentId == departmentId.Value && u.IsActive)
                .ToListAsync();

            var teamRequests = await _context.BudgetRequests
                .Include(r => r.RequestedByUser)
                .Include(r => r.Department)
                .Where(r => r.TenantId == tenantId && r.DepartmentId == departmentId.Value)
                .OrderByDescending(r => r.CreatedAt)
                .Take(20)
                .ToListAsync();

            ViewBag.Department = await _context.Departments.FirstOrDefaultAsync(d => d.Id == departmentId.Value);
            ViewBag.TeamMembers = teamMembers;
            ViewBag.TeamRequests = teamRequests;
            
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Policy = "DepartmentOperationsPolicy")] // Finance Manager + Department Head can approve
        public async Task<IActionResult> ApproveRequest(int id, string action, string? notes)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var request = await _context.BudgetRequests
                .Include(r => r.RequestedByUser)
                .Include(r => r.Department)
                .Include(r => r.OperationallyApprovedByUser)
                .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == user.TenantId);
            if (request == null) return NotFound();

            // Enforce proper approval hierarchy
            if (user.Role == "DepartmentHead")
            {
                // Department Head: Operational approval (Stage 1)
                if (user.DepartmentId != request.DepartmentId)
                {
                    TempData["Error"] = "You can only approve requests from your own department.";
                    return RedirectToAction(nameof(DepartmentRequests));
                }

                // Department Head can only approve requests in "Pending" status
                if (request.Status != "Pending")
                {
                    TempData["Error"] = "This request is not in a state that allows operational approval.";
                    return RedirectToAction(nameof(RequestApproval));
                }

                if (action == "Approved")
                {
                    request.Status = "OperationallyApproved"; // Ready for financial validation
                    request.OperationalApprovalNotes = notes;
                    request.OperationallyApprovedByUserId = user.Id;
                    request.OperationalApprovalDate = DateTime.UtcNow;

                    // Send role-based notifications
                    await _notificationService.NotifyBudgetRequestOperationallyApproved(request, user.Id, notes);
                }
                else if (action == "Rejected")
                {
                    request.Status = "Rejected";
                    request.ReviewerNotes = notes;
                    request.ReviewedByUserId = user.Id;
                    request.ReviewedAt = DateTime.UtcNow;
                    
                    // Send role-based notifications
                    await _notificationService.NotifyBudgetRequestRejected(request, user.Id, notes, "Operational Approval");
                }
            }
            else if (user.Role == "FinanceManager")
            {
                // Finance Manager: Financial validation (Stage 2)
                // Finance Manager can only approve requests that have been operationally approved
                // OR requests submitted directly by Department Heads
                if (request.Status != "OperationallyApproved")
                {
                    TempData["Error"] = "This request must be operationally approved by the Department Head before financial validation.";
                    return RedirectToAction(nameof(Requests));
                }

                if (action == "Approved")
                {
                    request.Status = "Approved"; // Final approval
                    request.FinancialValidationNotes = notes;
                    request.FinanciallyApprovedByUserId = user.Id;
                    request.FinancialApprovalDate = DateTime.UtcNow;

                    // ── BUDGET USAGE UPDATE: Update budget allocation usage when request is fully approved ──
                    await UpdateBudgetUsageOnApproval(request);
                    
                    // Send role-based notifications
                    await _notificationService.NotifyBudgetRequestFullyApproved(request, user.Id, notes);
                }
                else if (action == "Rejected")
                {
                    request.Status = "FinanciallyRejected";
                    request.FinancialValidationNotes = notes;
                    request.FinanciallyRejectedByUserId = user.Id;
                    request.FinancialRejectionDate = DateTime.UtcNow;
                    
                    // Send role-based notifications
                    await _notificationService.NotifyBudgetRequestRejected(request, user.Id, notes, "Financial Validation");
                }
            }
            else
            {
                TempData["Error"] = "You do not have permission to approve budget requests.";
                return RedirectToAction(nameof(Requests));
            }

            request.UpdatedAt = DateTime.UtcNow;

            // Create approval record
            _context.BudgetApprovals.Add(new BudgetApproval
            {
                BudgetRequestId = request.Id,
                ApprovedByUserId = user.Id,
                Action = action,
                Comments = notes,
                ActionDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Request {action} successfully.";
            
            // Redirect based on user role
            if (user.Role == "DepartmentHead")
                return RedirectToAction(nameof(RequestApproval));
            else
                return RedirectToAction(nameof(Requests));
        }

        // ── Employee: My Requests ──────────────────────────────────
        [Authorize(Policy = "BudgetRequestPolicy")] // Employee + Department Head + Finance Manager
        public async Task<IActionResult> MyRequests()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            var requests = await _context.BudgetRequests
                .Include(r => r.Department)
                .Where(r => r.RequestedByUserId == user.Id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            return View(requests);
        }

        // ── Employee: Create Request ───────────────────────────────
        [Authorize(Policy = "BudgetRequestPolicy")] // Employee + Department Head + Finance Manager
        public async Task<IActionResult> CreateRequest()
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.Departments = await _context.Departments
                .Where(d => d.TenantId == user!.TenantId && d.IsActive && !d.IsArchived).ToListAsync();
            ViewBag.Budgets = await _context.Budgets
                .Where(b => b.TenantId == user!.TenantId && b.Status == "Active" && !b.IsArchived).ToListAsync();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Policy = "BudgetRequestPolicy")] // Employee + Department Head + Finance Manager
        public async Task<IActionResult> CreateRequest(BudgetRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            request.TenantId = user.TenantId!.Value;
            request.RequestedByUserId = user.Id;
            request.CreatedAt = DateTime.UtcNow;

            // Determine initial workflow routing based on requester role
            if (user.Role == "DepartmentHead")
            {
                // Department Head requests bypass departmental approval and go directly to Finance Manager
                request.Status = "OperationallyApproved";
                request.OperationalApprovalNotes = "Auto-approved: Request submitted by Department Head";
                request.OperationallyApprovedByUserId = user.Id;
                request.OperationalApprovalDate = DateTime.UtcNow;
                request.DepartmentId = user.DepartmentId; // Use requester's department
            }
            else
            {
                // Employee requests must go through Department Head first
                request.Status = "Pending";
                
                // Ensure request is assigned to user's department for proper routing
                if (user.DepartmentId.HasValue)
                {
                    request.DepartmentId = user.DepartmentId.Value;
                }
                else
                {
                    TempData["Error"] = "You must be assigned to a department to submit budget requests.";
                    ViewBag.Departments = await _context.Departments
                        .Where(d => d.TenantId == user.TenantId && d.IsActive && !d.IsArchived).ToListAsync();
                    ViewBag.Budgets = await _context.Budgets
                        .Where(b => b.TenantId == user.TenantId && b.Status == "Active" && !b.IsArchived).ToListAsync();
                    return View(request);
                }
            }

            _context.BudgetRequests.Add(request);
            await _context.SaveChangesAsync();

            // Load the request with user details for notifications
            request.RequestedByUser = user;
            
            // Send role-based notifications
            await _notificationService.NotifyBudgetRequestCreated(request, user.Id);

            TempData["Success"] = "Budget request submitted successfully.";
            return RedirectToAction(nameof(MyRequests));
        }

        // ── BUDGET USAGE TRACKING: Update budget allocation usage when requests are approved ──
        private async Task UpdateBudgetUsageOnApproval(BudgetRequest approvedRequest)
        {
            // Find the related budget allocation
            Budget? targetBudget = null;

            if (approvedRequest.BudgetId.HasValue)
            {
                // Request is linked to a specific budget
                targetBudget = await _context.Budgets
                    .FirstOrDefaultAsync(b => b.Id == approvedRequest.BudgetId.Value && 
                                             b.TenantId == approvedRequest.TenantId &&
                                             !b.IsArchived);
            }
            else if (approvedRequest.DepartmentId.HasValue)
            {
                // Find active budget for the department and fiscal year
                var requestYear = approvedRequest.CreatedAt.Year;
                targetBudget = await _context.Budgets
                    .FirstOrDefaultAsync(b => b.TenantId == approvedRequest.TenantId && 
                                             b.DepartmentId == approvedRequest.DepartmentId.Value && 
                                             b.FiscalYear == requestYear && 
                                             b.Status == "Active" &&
                                             !b.IsArchived);
            }

            if (targetBudget != null)
            {
                // Update budget usage: Add approved amount to UsedAmount
                targetBudget.UsedAmount += approvedRequest.RequestedAmount;
                targetBudget.UpdatedAt = DateTime.UtcNow;

                // Validate that usage doesn't exceed allocation
                if (targetBudget.UsedAmount > targetBudget.AllocatedAmount)
                {
                    // Log warning but allow over-allocation (business decision)
                    // Could add notification to Finance Manager about over-allocation
                    var overAmount = targetBudget.UsedAmount - targetBudget.AllocatedAmount;
                    
                    // Notify Finance Manager about budget over-allocation
                    var financeManagers = await _context.Users
                        .Where(u => u.TenantId == approvedRequest.TenantId && u.Role == "FinanceManager" && u.IsActive)
                        .ToListAsync();

                    foreach (var fm in financeManagers)
                    {
                        _context.Notifications.Add(new Notification
                        {
                            UserId = fm.Id,
                            TenantId = approvedRequest.TenantId,
                            Title = "Budget Over-Allocation Warning",
                            Message = $"Budget '{targetBudget.Title}' is now over-allocated by ₱{overAmount:N2} due to approved request '{approvedRequest.Title}'.",
                            Type = "Warning",
                            ActionUrl = $"/Budget/Details/{targetBudget.Id}"
                        });
                    }
                }

                // Link the request to the budget if not already linked
                if (!approvedRequest.BudgetId.HasValue)
                {
                    approvedRequest.BudgetId = targetBudget.Id;
                }
            }
        }

        // ══════════════════════════════════════════════════════════════════════════════════════
        // FINANCIAL REPORTS
        // ══════════════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Financial Reports Dashboard - Comprehensive financial reporting
        /// </summary>
        [Authorize(Policy = "FinancialPolicy")]
        public async Task<IActionResult> Reports(int? year)
        {
            var tenantId = await GetTenantId();
            year ??= DateTime.UtcNow.Year;

            // Aggregate all financial data
            var budgets = await _context.Budgets
                .Include(b => b.Department)
                .Where(b => b.TenantId == tenantId && b.FiscalYear == year && !b.IsArchived)
                .ToListAsync();

            var requests = await _context.BudgetRequests
                .Where(r => r.TenantId == tenantId && r.CreatedAt.Year == year)
                .ToListAsync();

            var transactions = await _context.ActualTransactions
                .Where(t => t.TenantId == tenantId && t.FiscalYear == year)
                .ToListAsync();

            var forecasts = await _context.Forecasts
                .Where(f => f.TenantId == tenantId && f.FiscalYear == year)
                .ToListAsync();

            // Calculate metrics
            ViewBag.Year = year;
            ViewBag.TotalAllocated = budgets.Sum(b => b.AllocatedAmount);
            ViewBag.TotalUsed = budgets.Sum(b => b.UsedAmount);
            ViewBag.TotalActualSpend = budgets.Sum(b => b.ActualSpend);
            ViewBag.TotalVariance = budgets.Sum(b => b.VarianceAmount);
            ViewBag.TotalRevenue = transactions.Where(t => t.TransactionType == "Revenue").Sum(t => t.Amount);
            ViewBag.TotalExpenses = transactions.Where(t => t.TransactionType == "Expense").Sum(t => t.Amount);
            ViewBag.NetIncome = ViewBag.TotalRevenue - ViewBag.TotalExpenses;
            ViewBag.ProjectedRevenue = forecasts.Sum(f => f.ProjectedRevenue);
            ViewBag.ProjectedExpenses = forecasts.Sum(f => f.ProjectedExpenses);
            ViewBag.TotalRequests = requests.Count;
            ViewBag.ApprovedRequests = requests.Count(r => r.Status == "Approved");
            ViewBag.PendingRequests = requests.Count(r => r.Status == "Pending" || r.Status == "OperationallyApproved");
            ViewBag.RejectedRequests = requests.Count(r => r.Status == "Rejected" || r.Status == "FinanciallyRejected");

            return View(budgets);
        }
    }
}

