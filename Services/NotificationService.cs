using BudgetMasterFinal.Data;
using BudgetMasterFinal.Models;
using Microsoft.EntityFrameworkCore;

namespace BudgetMasterFinal.Services
{
    /// <summary>
    /// Centralized notification service for role-based notifications
    /// Ensures all users receive relevant notifications based on their roles and responsibilities
    /// </summary>
    public class NotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrencyService _currencyService;

        public NotificationService(ApplicationDbContext context, ICurrencyService currencyService)
        {
            _context = context;
            _currencyService = currencyService;
        }

        // ══════════════════════════════════════════════════════════════════════════════════════
        // BUDGET NOTIFICATIONS
        // ══════════════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Notify relevant users when a new budget is created
        /// </summary>
        public async Task NotifyBudgetCreated(Budget budget, string creatorId)
        {
            var currencySymbol = await _currencyService.GetCurrencySymbolAsync(budget.TenantId);
            var notifications = new List<Notification>();

            // 1. Notify Finance Managers (they manage all budgets)
            var financeManagers = await GetUsersByRole(budget.TenantId, "FinanceManager");
            foreach (var fm in financeManagers.Where(u => u.Id != creatorId))
            {
                notifications.Add(new Notification
                {
                    UserId = fm.Id,
                    TenantId = budget.TenantId,
                    Title = "New Budget Created",
                    Message = $"Budget '{budget.Title}' for FY {budget.FiscalYear} has been created with {currencySymbol}{budget.AllocatedAmount:N2} allocation.",
                    Type = "Info",
                    ActionUrl = $"/Budget/Details/{budget.Id}"
                });
            }

            // 2. Notify Department Head if budget is for their department
            if (budget.DepartmentId.HasValue)
            {
                var deptHead = await GetDepartmentHead(budget.TenantId, budget.DepartmentId.Value);
                if (deptHead != null && deptHead.Id != creatorId)
                {
                    notifications.Add(new Notification
                    {
                        UserId = deptHead.Id,
                        TenantId = budget.TenantId,
                        Title = "New Budget for Your Department",
                        Message = $"A new budget '{budget.Title}' of {currencySymbol}{budget.AllocatedAmount:N2} has been allocated to your department for FY {budget.FiscalYear}.",
                        Type = "Success",
                        ActionUrl = $"/Budget/DepartmentBudgets"
                    });
                }

                // 3. Notify all department employees
                var deptEmployees = await GetDepartmentUsers(budget.TenantId, budget.DepartmentId.Value);
                foreach (var emp in deptEmployees.Where(u => u.Id != creatorId && u.Role == "Employee"))
                {
                    notifications.Add(new Notification
                    {
                        UserId = emp.Id,
                        TenantId = budget.TenantId,
                        Title = "New Department Budget Available",
                        Message = $"Your department has received a new budget allocation of {currencySymbol}{budget.AllocatedAmount:N2} for FY {budget.FiscalYear}.",
                        Type = "Info",
                        ActionUrl = $"/Budget/MyRequests"
                    });
                }
            }

            // 4. Notify Accountants (they track all financial activities)
            var accountants = await GetUsersByRole(budget.TenantId, "Accountant");
            foreach (var acc in accountants.Where(u => u.Id != creatorId))
            {
                notifications.Add(new Notification
                {
                    UserId = acc.Id,
                    TenantId = budget.TenantId,
                    Title = "New Budget Created",
                    Message = $"Budget '{budget.Title}' created with {currencySymbol}{budget.AllocatedAmount:N2} allocation for FY {budget.FiscalYear}.",
                    Type = "Info",
                    ActionUrl = $"/Budget/Details/{budget.Id}"
                });
            }

            // 5. Notify Auditors (they audit all financial activities)
            var auditors = await GetUsersByRole(budget.TenantId, "Auditor");
            foreach (var aud in auditors)
            {
                notifications.Add(new Notification
                {
                    UserId = aud.Id,
                    TenantId = budget.TenantId,
                    Title = "New Budget Created",
                    Message = $"Budget '{budget.Title}' created with {currencySymbol}{budget.AllocatedAmount:N2} allocation for FY {budget.FiscalYear}.",
                    Type = "Info",
                    ActionUrl = $"/Budget/Details/{budget.Id}"
                });
            }

            await _context.Notifications.AddRangeAsync(notifications);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Notify relevant users when a budget is updated
        /// </summary>
        public async Task NotifyBudgetUpdated(Budget budget, string updaterId)
        {
            var notifications = new List<Notification>();

            // Notify Finance Managers
            var financeManagers = await GetUsersByRole(budget.TenantId, "FinanceManager");
            foreach (var fm in financeManagers.Where(u => u.Id != updaterId))
            {
                notifications.Add(new Notification
                {
                    UserId = fm.Id,
                    TenantId = budget.TenantId,
                    Title = "Budget Updated",
                    Message = $"Budget '{budget.Title}' has been updated.",
                    Type = "Info",
                    ActionUrl = $"/Budget/Details/{budget.Id}"
                });
            }

            // Notify Department Head
            if (budget.DepartmentId.HasValue)
            {
                var deptHead = await GetDepartmentHead(budget.TenantId, budget.DepartmentId.Value);
                if (deptHead != null && deptHead.Id != updaterId)
                {
                    notifications.Add(new Notification
                    {
                        UserId = deptHead.Id,
                        TenantId = budget.TenantId,
                        Title = "Department Budget Updated",
                        Message = $"Budget '{budget.Title}' for your department has been updated.",
                        Type = "Warning",
                        ActionUrl = $"/Budget/DepartmentBudgets"
                    });
                }
            }

            await _context.Notifications.AddRangeAsync(notifications);
            await _context.SaveChangesAsync();
        }

        // ══════════════════════════════════════════════════════════════════════════════════════
        // BUDGET REQUEST NOTIFICATIONS
        // ══════════════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Notify relevant users when a new budget request is created
        /// </summary>
        public async Task NotifyBudgetRequestCreated(BudgetRequest request, string requesterId)
        {
            var currencySymbol = await _currencyService.GetCurrencySymbolAsync(request.TenantId);
            var notifications = new List<Notification>();

            // 1. Notify Department Head (for operational approval)
            if (request.DepartmentId.HasValue)
            {
                var deptHead = await GetDepartmentHead(request.TenantId, request.DepartmentId.Value);
                if (deptHead != null && deptHead.Id != requesterId)
                {
                    notifications.Add(new Notification
                    {
                        UserId = deptHead.Id,
                        TenantId = request.TenantId,
                        Title = "New Budget Request Awaiting Approval",
                        Message = $"{request.RequestedByUser?.FullName ?? "A team member"} submitted a budget request '{request.Title}' for {currencySymbol}{request.RequestedAmount:N2}.",
                        Type = "Warning",
                        ActionUrl = $"/Budget/RequestApproval"
                    });
                }
            }

            // 2. Notify Finance Managers (for visibility)
            var financeManagers = await GetUsersByRole(request.TenantId, "FinanceManager");
            foreach (var fm in financeManagers.Where(u => u.Id != requesterId))
            {
                notifications.Add(new Notification
                {
                    UserId = fm.Id,
                    TenantId = request.TenantId,
                    Title = "New Budget Request Submitted",
                    Message = $"Budget request '{request.Title}' for {currencySymbol}{request.RequestedAmount:N2} has been submitted and is pending operational approval.",
                    Type = "Info",
                    ActionUrl = $"/Budget/AllRequests"
                });
            }

            // 3. Notify Accountants (for tracking)
            var accountants = await GetUsersByRole(request.TenantId, "Accountant");
            foreach (var acc in accountants)
            {
                notifications.Add(new Notification
                {
                    UserId = acc.Id,
                    TenantId = request.TenantId,
                    Title = "New Budget Request",
                    Message = $"Budget request '{request.Title}' for {currencySymbol}{request.RequestedAmount:N2} has been submitted.",
                    Type = "Info",
                    ActionUrl = $"/Budget/Requests"
                });
            }

            await _context.Notifications.AddRangeAsync(notifications);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Notify relevant users when a budget request is operationally approved by Department Head
        /// </summary>
        public async Task NotifyBudgetRequestOperationallyApproved(BudgetRequest request, string approverId, string? notes)
        {
            var currencySymbol = await _currencyService.GetCurrencySymbolAsync(request.TenantId);
            var notifications = new List<Notification>();

            // 1. Notify requester
            notifications.Add(new Notification
            {
                UserId = request.RequestedByUserId,
                TenantId = request.TenantId,
                Title = "Budget Request Operationally Approved",
                Message = $"Your budget request '{request.Title}' has been operationally approved and is now awaiting financial validation." +
                         (notes != null ? $" Notes: {notes}" : ""),
                Type = "Success",
                ActionUrl = $"/Budget/MyRequests"
            });

            // 2. Notify Finance Managers (for financial validation)
            var financeManagers = await GetUsersByRole(request.TenantId, "FinanceManager");
            foreach (var fm in financeManagers.Where(u => u.Id != approverId))
            {
                notifications.Add(new Notification
                {
                    UserId = fm.Id,
                    TenantId = request.TenantId,
                    Title = "Budget Request Ready for Financial Validation",
                    Message = $"Budget request '{request.Title}' for {currencySymbol}{request.RequestedAmount:N2} has been operationally approved and requires financial validation.",
                    Type = "Warning",
                    ActionUrl = $"/Budget/FinancialValidation"
                });
            }

            // 3. Notify Accountants
            var accountants = await GetUsersByRole(request.TenantId, "Accountant");
            foreach (var acc in accountants)
            {
                notifications.Add(new Notification
                {
                    UserId = acc.Id,
                    TenantId = request.TenantId,
                    Title = "Budget Request Operationally Approved",
                    Message = $"Budget request '{request.Title}' for {currencySymbol}{request.RequestedAmount:N2} has been operationally approved.",
                    Type = "Info",
                    ActionUrl = $"/Budget/Requests"
                });
            }

            await _context.Notifications.AddRangeAsync(notifications);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Notify relevant users when a budget request is fully approved by Finance Manager
        /// </summary>
        public async Task NotifyBudgetRequestFullyApproved(BudgetRequest request, string approverId, string? notes)
        {
            var currencySymbol = await _currencyService.GetCurrencySymbolAsync(request.TenantId);
            var notifications = new List<Notification>();

            // 1. Notify requester
            notifications.Add(new Notification
            {
                UserId = request.RequestedByUserId,
                TenantId = request.TenantId,
                Title = "Budget Request Fully Approved",
                Message = $"Your budget request '{request.Title}' for {currencySymbol}{request.RequestedAmount:N2} has been fully approved and is ready for implementation." +
                         (notes != null ? $" Notes: {notes}" : ""),
                Type = "Success",
                ActionUrl = $"/Budget/MyRequests"
            });

            // 2. Notify Department Head
            if (request.DepartmentId.HasValue)
            {
                var deptHead = await GetDepartmentHead(request.TenantId, request.DepartmentId.Value);
                if (deptHead != null && deptHead.Id != request.RequestedByUserId)
                {
                    notifications.Add(new Notification
                    {
                        UserId = deptHead.Id,
                        TenantId = request.TenantId,
                        Title = "Budget Request Approved",
                        Message = $"Budget request '{request.Title}' for {currencySymbol}{request.RequestedAmount:N2} has been fully approved by Finance Manager.",
                        Type = "Success",
                        ActionUrl = $"/Budget/DepartmentRequests"
                    });
                }
            }

            // 3. Notify Accountants (for implementation tracking)
            var accountants = await GetUsersByRole(request.TenantId, "Accountant");
            foreach (var acc in accountants)
            {
                notifications.Add(new Notification
                {
                    UserId = acc.Id,
                    TenantId = request.TenantId,
                    Title = "Budget Request Approved",
                    Message = $"Budget request '{request.Title}' for {currencySymbol}{request.RequestedAmount:N2} has been fully approved.",
                    Type = "Success",
                    ActionUrl = $"/Budget/Requests"
                });
            }

            // 4. Notify Auditors
            var auditors = await GetUsersByRole(request.TenantId, "Auditor");
            foreach (var aud in auditors)
            {
                notifications.Add(new Notification
                {
                    UserId = aud.Id,
                    TenantId = request.TenantId,
                    Title = "Budget Request Approved",
                    Message = $"Budget request '{request.Title}' for {currencySymbol}{request.RequestedAmount:N2} has been approved.",
                    Type = "Info",
                    ActionUrl = $"/Budget/Requests"
                });
            }

            await _context.Notifications.AddRangeAsync(notifications);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Notify relevant users when a budget request is rejected
        /// </summary>
        public async Task NotifyBudgetRequestRejected(BudgetRequest request, string rejecterId, string? notes, string rejectionStage)
        {
            var currencySymbol = await _currencyService.GetCurrencySymbolAsync(request.TenantId);
            var notifications = new List<Notification>();

            // 1. Notify requester
            notifications.Add(new Notification
            {
                UserId = request.RequestedByUserId,
                TenantId = request.TenantId,
                Title = $"Budget Request Rejected ({rejectionStage})",
                Message = $"Your budget request '{request.Title}' has been rejected during {rejectionStage.ToLower()}." +
                         (notes != null ? $" Reason: {notes}" : ""),
                Type = "Error",
                ActionUrl = $"/Budget/MyRequests"
            });

            // 2. Notify Department Head (if rejected by Finance Manager)
            if (rejectionStage == "Financial Validation" && request.DepartmentId.HasValue)
            {
                var deptHead = await GetDepartmentHead(request.TenantId, request.DepartmentId.Value);
                if (deptHead != null && deptHead.Id != request.RequestedByUserId)
                {
                    notifications.Add(new Notification
                    {
                        UserId = deptHead.Id,
                        TenantId = request.TenantId,
                        Title = "Budget Request Rejected by Finance",
                        Message = $"Budget request '{request.Title}' for {currencySymbol}{request.RequestedAmount:N2} was rejected during financial validation." +
                                 (notes != null ? $" Reason: {notes}" : ""),
                        Type = "Error",
                        ActionUrl = $"/Budget/DepartmentRequests"
                    });
                }
            }

            // 3. Notify Finance Managers (if rejected by Department Head)
            if (rejectionStage == "Operational Approval")
            {
                var financeManagers = await GetUsersByRole(request.TenantId, "FinanceManager");
                foreach (var fm in financeManagers)
                {
                    notifications.Add(new Notification
                    {
                        UserId = fm.Id,
                        TenantId = request.TenantId,
                        Title = "Budget Request Rejected",
                        Message = $"Budget request '{request.Title}' for {currencySymbol}{request.RequestedAmount:N2} was rejected during operational approval.",
                        Type = "Info",
                        ActionUrl = $"/Budget/AllRequests"
                    });
                }
            }

            // 4. Notify Accountants
            var accountants = await GetUsersByRole(request.TenantId, "Accountant");
            foreach (var acc in accountants)
            {
                notifications.Add(new Notification
                {
                    UserId = acc.Id,
                    TenantId = request.TenantId,
                    Title = "Budget Request Rejected",
                    Message = $"Budget request '{request.Title}' for {currencySymbol}{request.RequestedAmount:N2} was rejected.",
                    Type = "Info",
                    ActionUrl = $"/Budget/Requests"
                });
            }

            await _context.Notifications.AddRangeAsync(notifications);
            await _context.SaveChangesAsync();
        }

        // ══════════════════════════════════════════════════════════════════════════════════════
        // TRANSACTION NOTIFICATIONS
        // ══════════════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Notify relevant users when a transaction is recorded
        /// </summary>
        public async Task NotifyTransactionRecorded(ActualTransaction transaction, string creatorId)
        {
            var currencySymbol = await _currencyService.GetCurrencySymbolAsync(transaction.TenantId);
            var notifications = new List<Notification>();

            // 1. Notify Finance Managers
            var financeManagers = await GetUsersByRole(transaction.TenantId, "FinanceManager");
            foreach (var fm in financeManagers.Where(u => u.Id != creatorId))
            {
                notifications.Add(new Notification
                {
                    UserId = fm.Id,
                    TenantId = transaction.TenantId,
                    Title = $"New {transaction.TransactionType} Recorded",
                    Message = $"{transaction.TransactionType} of {currencySymbol}{transaction.Amount:N2} recorded: {transaction.Description}",
                    Type = "Info",
                    ActionUrl = $"/Variance/Transactions"
                });
            }

            // 2. Notify Department Head if transaction is for their department
            if (transaction.DepartmentId.HasValue)
            {
                var deptHead = await GetDepartmentHead(transaction.TenantId, transaction.DepartmentId.Value);
                if (deptHead != null && deptHead.Id != creatorId)
                {
                    notifications.Add(new Notification
                    {
                        UserId = deptHead.Id,
                        TenantId = transaction.TenantId,
                        Title = $"Department {transaction.TransactionType} Recorded",
                        Message = $"{transaction.TransactionType} of {currencySymbol}{transaction.Amount:N2} recorded for your department: {transaction.Description}",
                        Type = "Info",
                        ActionUrl = $"/Variance/Transactions"
                    });
                }
            }

            // 3. Notify Auditors
            var auditors = await GetUsersByRole(transaction.TenantId, "Auditor");
            foreach (var aud in auditors)
            {
                notifications.Add(new Notification
                {
                    UserId = aud.Id,
                    TenantId = transaction.TenantId,
                    Title = $"New {transaction.TransactionType} Recorded",
                    Message = $"{transaction.TransactionType} of {currencySymbol}{transaction.Amount:N2}: {transaction.Description}",
                    Type = "Info",
                    ActionUrl = $"/Variance/Transactions"
                });
            }

            await _context.Notifications.AddRangeAsync(notifications);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Notify relevant users when budget overspending is detected
        /// </summary>
        public async Task NotifyBudgetOverspending(Budget budget, decimal overAmount)
        {
            var currencySymbol = await _currencyService.GetCurrencySymbolAsync(budget.TenantId);
            var notifications = new List<Notification>();

            // 1. Notify Finance Managers (critical alert)
            var financeManagers = await GetUsersByRole(budget.TenantId, "FinanceManager");
            foreach (var fm in financeManagers)
            {
                notifications.Add(new Notification
                {
                    UserId = fm.Id,
                    TenantId = budget.TenantId,
                    Title = "Budget Overspending Alert",
                    Message = $"Budget '{budget.Title}' has exceeded allocated amount by {currencySymbol}{overAmount:N2}. " +
                             $"Actual spend: {currencySymbol}{budget.ActualSpend:N2}, Allocated: {currencySymbol}{budget.AllocatedAmount:N2}",
                    Type = "Error",
                    ActionUrl = $"/Variance/Index?year={budget.FiscalYear}"
                });
            }

            // 2. Notify Department Head
            if (budget.DepartmentId.HasValue)
            {
                var deptHead = await GetDepartmentHead(budget.TenantId, budget.DepartmentId.Value);
                if (deptHead != null)
                {
                    notifications.Add(new Notification
                    {
                        UserId = deptHead.Id,
                        TenantId = budget.TenantId,
                        Title = "Department Budget Overspending",
                        Message = $"Your department budget '{budget.Title}' has exceeded the allocated amount by {currencySymbol}{overAmount:N2}. Immediate action required.",
                        Type = "Error",
                        ActionUrl = $"/Budget/DepartmentBudgets"
                    });
                }
            }

            // 3. Notify Accountants
            var accountants = await GetUsersByRole(budget.TenantId, "Accountant");
            foreach (var acc in accountants)
            {
                notifications.Add(new Notification
                {
                    UserId = acc.Id,
                    TenantId = budget.TenantId,
                    Title = "Budget Overspending Detected",
                    Message = $"Budget '{budget.Title}' has exceeded allocated amount by {currencySymbol}{overAmount:N2}.",
                    Type = "Warning",
                    ActionUrl = $"/Variance/Index?year={budget.FiscalYear}"
                });
            }

            // 4. Notify Auditors
            var auditors = await GetUsersByRole(budget.TenantId, "Auditor");
            foreach (var aud in auditors)
            {
                notifications.Add(new Notification
                {
                    UserId = aud.Id,
                    TenantId = budget.TenantId,
                    Title = "Budget Overspending Alert",
                    Message = $"Budget '{budget.Title}' has exceeded allocated amount by {currencySymbol}{overAmount:N2}.",
                    Type = "Error",
                    ActionUrl = $"/Variance/Index?year={budget.FiscalYear}"
                });
            }

            await _context.Notifications.AddRangeAsync(notifications);
            await _context.SaveChangesAsync();
        }

        // ══════════════════════════════════════════════════════════════════════════════════════
        // HELPER METHODS
        // ══════════════════════════════════════════════════════════════════════════════════════

        private async Task<List<ApplicationUser>> GetUsersByRole(int tenantId, string role)
        {
            return await _context.Users
                .Where(u => u.TenantId == tenantId && u.Role == role && u.IsActive)
                .ToListAsync();
        }

        private async Task<ApplicationUser?> GetDepartmentHead(int tenantId, int departmentId)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.TenantId == tenantId && 
                                         u.DepartmentId == departmentId && 
                                         u.Role == "DepartmentHead" && 
                                         u.IsActive);
        }

        private async Task<List<ApplicationUser>> GetDepartmentUsers(int tenantId, int departmentId)
        {
            return await _context.Users
                .Where(u => u.TenantId == tenantId && u.DepartmentId == departmentId && u.IsActive)
                .ToListAsync();
        }

        // ══════════════════════════════════════════════════════════════════════════════════════
        // SUPER ADMIN NOTIFICATIONS
        // ══════════════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Notify Super Admin about new tenant registration
        /// </summary>
        public async Task NotifySuperAdminNewTenant(Tenant tenant)
        {
            var superAdmins = await _context.Users
                .Where(u => u.Role == "SuperAdmin" && u.IsActive)
                .ToListAsync();

            foreach (var admin in superAdmins)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = admin.Id,
                    TenantId = null, // Super Admin notifications are not tenant-specific
                    Title = "New Company Registered",
                    Message = $"Company '{tenant.CompanyName}' has registered on the platform.",
                    Type = "Info",
                    ActionUrl = $"/SuperAdmin/Tenants"
                });
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Notify Super Admin about subscription expiring soon
        /// </summary>
        public async Task NotifySuperAdminSubscriptionExpiring(Subscription subscription, int daysRemaining)
        {
            var superAdmins = await _context.Users
                .Where(u => u.Role == "SuperAdmin" && u.IsActive)
                .ToListAsync();

            var tenant = await _context.Tenants.FindAsync(subscription.TenantId);
            if (tenant == null) return;

            foreach (var admin in superAdmins)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = admin.Id,
                    TenantId = null,
                    Title = "Subscription Expiring Soon",
                    Message = $"Subscription for '{tenant.CompanyName}' expires in {daysRemaining} days.",
                    Type = "Warning",
                    ActionUrl = $"/Billing/Subscriptions"
                });
            }

            await _context.SaveChangesAsync();
        }

        // ══════════════════════════════════════════════════════════════════════════════════════
        // TENANT ADMIN NOTIFICATIONS
        // ══════════════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Notify Tenant Admin about new user registration
        /// </summary>
        public async Task NotifyAdminNewUser(ApplicationUser newUser, int tenantId)
        {
            var admins = await GetUsersByRole(tenantId, "Admin");

            foreach (var admin in admins.Where(a => a.Id != newUser.Id))
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = admin.Id,
                    TenantId = tenantId,
                    Title = "New User Registered",
                    Message = $"{newUser.FullName} ({newUser.Role}) has joined your organization.",
                    Type = "Info",
                    ActionUrl = $"/UserManagement/Index"
                });
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Notify Tenant Admin about department changes
        /// </summary>
        public async Task NotifyAdminDepartmentChange(Department department, string action)
        {
            var admins = await GetUsersByRole(department.TenantId, "Admin");

            foreach (var admin in admins)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = admin.Id,
                    TenantId = department.TenantId,
                    Title = $"Department {action}",
                    Message = $"Department '{department.Name}' has been {action.ToLower()}.",
                    Type = "Info",
                    ActionUrl = $"/Department/Index"
                });
            }

            await _context.SaveChangesAsync();
        }
    }
}
