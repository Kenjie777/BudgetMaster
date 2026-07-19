using BudgetMasterFinal.Data;
using BudgetMasterFinal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BudgetMasterFinal.Controllers
{
    [Authorize(Policy = "SuperAdminPolicy")]
    public class BillingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<BillingController> _logger;

        public BillingController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<BillingController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // Revenue Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var metrics = await CalculateRevenueMetrics();
            return View(metrics);
        }

        // Subscription Plans Management
        public async Task<IActionResult> Plans()
        {
            var plans = await _context.SubscriptionPlans
                .Include(p => p.Features)
                .Include(p => p.Subscriptions)
                .OrderBy(p => p.SortOrder)
                .ToListAsync();
            return View(plans);
        }

        public IActionResult CreatePlan()
        {
            return View(new SubscriptionPlan());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePlan(SubscriptionPlan plan)
        {
            if (!ModelState.IsValid)
                return View(plan);

            // Auto-calculate yearly price based on monthly price and discount percentage
            var baseYearlyPrice = plan.MonthlyPrice * 12;
            var discountAmount = baseYearlyPrice * (plan.YearlyDiscountPercentage / 100);
            plan.YearlyPrice = baseYearlyPrice - discountAmount;

            plan.CreatedAt = DateTime.UtcNow;
            _context.SubscriptionPlans.Add(plan);
            await _context.SaveChangesAsync();

            await LogBillingAction("CreatePlan", "SubscriptionPlan", plan.Id, $"Created plan: {plan.Name}");
            TempData["Success"] = "Subscription plan created successfully.";
            return RedirectToAction(nameof(Plans));
        }

        public async Task<IActionResult> EditPlan(int id)
        {
            var plan = await _context.SubscriptionPlans
                .Include(p => p.Features)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (plan == null) return NotFound();
            return View(plan);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPlan(int id, SubscriptionPlan plan)
        {
            if (id != plan.Id) return NotFound();

            if (!ModelState.IsValid)
                return View(plan);

            var existing = await _context.SubscriptionPlans.FindAsync(id);
            if (existing == null) return NotFound();

            existing.Name = plan.Name;
            existing.Description = plan.Description;
            existing.MonthlyPrice = plan.MonthlyPrice;
            existing.YearlyDiscountPercentage = plan.YearlyDiscountPercentage;
            
            // Auto-calculate yearly price based on monthly price and discount percentage
            var baseYearlyPrice = plan.MonthlyPrice * 12;
            var discountAmount = baseYearlyPrice * (plan.YearlyDiscountPercentage / 100);
            existing.YearlyPrice = baseYearlyPrice - discountAmount;
            
            existing.UsageBasedPrice = plan.UsageBasedPrice;
            existing.BillingModel = plan.BillingModel;
            existing.MaxUsers = plan.MaxUsers;
            existing.MaxDepartments = plan.MaxDepartments;
            existing.IncludesForecasting = plan.IncludesForecasting;
            existing.IncludesScenarioPlanning = plan.IncludesScenarioPlanning;
            existing.IncludesAdvancedReports = plan.IncludesAdvancedReports;
            existing.IncludesApiAccess = plan.IncludesApiAccess;
            existing.IncludesCustomBranding = plan.IncludesCustomBranding;
            existing.IncludesPrioritySupport = plan.IncludesPrioritySupport;
            existing.Status = plan.Status;
            existing.IsVisible = plan.IsVisible;
            existing.SortOrder = plan.SortOrder;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await LogBillingAction("UpdatePlan", "SubscriptionPlan", id, $"Updated plan: {existing.Name}");
            TempData["Success"] = "Subscription plan updated successfully.";
            return RedirectToAction(nameof(Plans));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivatePlan(int id)
        {
            var plan = await _context.SubscriptionPlans.FindAsync(id);
            if (plan == null) return NotFound();

            plan.Status = "Active";
            plan.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await LogBillingAction("ActivatePlan", "SubscriptionPlan", id, $"Activated plan: {plan.Name}");
            TempData["Success"] = "Subscription plan activated successfully.";
            return RedirectToAction(nameof(Plans));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivatePlan(int id)
        {
            var plan = await _context.SubscriptionPlans.FindAsync(id);
            if (plan == null) return NotFound();

            // Check if there are active subscriptions using this plan
            var activeSubscriptions = await _context.Subscriptions
                .CountAsync(s => s.SubscriptionPlanId == id && s.Status == "Active");

            if (activeSubscriptions > 0)
            {
                TempData["Error"] = $"Cannot deactivate plan '{plan.Name}' because it has {activeSubscriptions} active subscription(s).";
                return RedirectToAction(nameof(Plans));
            }

            plan.Status = "Inactive";
            plan.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await LogBillingAction("DeactivatePlan", "SubscriptionPlan", id, $"Deactivated plan: {plan.Name}");
            TempData["Success"] = "Subscription plan deactivated successfully.";
            return RedirectToAction(nameof(Plans));
        }

        // Tenant Subscriptions Management
        public async Task<IActionResult> Subscriptions(string? status = null, int? planId = null)
        {
            var query = _context.Subscriptions
                .Include(s => s.Tenant)
                .Include(s => s.SubscriptionPlan)
                .Include(s => s.BillingDiscount)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(s => s.Status == status);

            if (planId.HasValue)
                query = query.Where(s => s.SubscriptionPlanId == planId);

            var subscriptions = await query
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            ViewBag.Plans = await _context.SubscriptionPlans.Where(p => p.Status == "Active").ToListAsync();
            ViewBag.SelectedStatus = status;
            ViewBag.SelectedPlanId = planId;

            return View(subscriptions);
        }

        public async Task<IActionResult> SubscriptionDetails(int id)
        {
            var subscription = await _context.Subscriptions
                .Include(s => s.Tenant)
                .Include(s => s.SubscriptionPlan)
                .Include(s => s.BillingDiscount)
                .Include(s => s.BillingTransactions)
                .Include(s => s.BillingCredits)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subscription == null) return NotFound();
            return View(subscription);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePlan(int subscriptionId, int newPlanId, string billingCycle)
        {
            var subscription = await _context.Subscriptions
                .Include(s => s.Tenant)
                .Include(s => s.SubscriptionPlan)
                .FirstOrDefaultAsync(s => s.Id == subscriptionId);

            if (subscription == null) return NotFound();

            var newPlan = await _context.SubscriptionPlans.FindAsync(newPlanId);
            if (newPlan == null) return NotFound();

            var oldPlanName = subscription.SubscriptionPlan.Name;
            var newPrice = billingCycle == "Yearly" ? newPlan.YearlyPrice : newPlan.MonthlyPrice;

            subscription.SubscriptionPlanId = newPlanId;
            subscription.BillingCycle = billingCycle;
            subscription.PriceAmount = newPrice;
            subscription.UpdatedAt = DateTime.UtcNow;

            // Calculate prorated amount if needed
            var proratedAmount = CalculateProratedAmount(subscription, newPrice);
            if (proratedAmount != 0)
            {
                var transaction = new BillingTransaction
                {
                    TenantId = subscription.TenantId,
                    SubscriptionId = subscription.Id,
                    TransactionType = proratedAmount > 0 ? "Payment" : "Credit",
                    Amount = Math.Abs(proratedAmount),
                    NetAmount = Math.Abs(proratedAmount),
                    Status = "Completed",
                    TransactionDate = DateTime.UtcNow,
                    ProcessedAt = DateTime.UtcNow,
                    ProcessedByUserId = _userManager.GetUserId(User),
                    Notes = $"Plan change from {oldPlanName} to {newPlan.Name}"
                };
                _context.BillingTransactions.Add(transaction);
            }

            await _context.SaveChangesAsync();
            await LogBillingAction("ChangePlan", "Subscription", subscriptionId, 
                $"Changed plan from {oldPlanName} to {newPlan.Name} for tenant {subscription.Tenant.CompanyName}");

            TempData["Success"] = "Subscription plan changed successfully.";
            return RedirectToAction(nameof(SubscriptionDetails), new { id = subscriptionId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SuspendSubscription(int id, string reason)
        {
            var subscription = await _context.Subscriptions
                .Include(s => s.Tenant)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subscription == null) return NotFound();

            subscription.Status = "Suspended";
            subscription.SuspendedAt = DateTime.UtcNow;
            subscription.SuspensionReason = reason;
            subscription.SuspendedByUserId = _userManager.GetUserId(User);
            subscription.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await LogBillingAction("SuspendSubscription", "Subscription", id, 
                $"Suspended subscription for tenant {subscription.Tenant.CompanyName}. Reason: {reason}");

            TempData["Success"] = "Subscription suspended successfully.";
            return RedirectToAction(nameof(SubscriptionDetails), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreSubscription(int id)
        {
            var subscription = await _context.Subscriptions
                .Include(s => s.Tenant)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subscription == null) return NotFound();

            subscription.Status = "Active";
            subscription.SuspendedAt = null;
            subscription.SuspensionReason = null;
            subscription.SuspendedByUserId = null;
            subscription.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await LogBillingAction("RestoreSubscription", "Subscription", id, 
                $"Restored subscription for tenant {subscription.Tenant.CompanyName}");

            TempData["Success"] = "Subscription restored successfully.";
            return RedirectToAction(nameof(SubscriptionDetails), new { id });
        }

        // Credits Management
        public async Task<IActionResult> Credits()
        {
            var credits = await _context.BillingCredits
                .Include(c => c.Tenant)
                .Include(c => c.IssuedByUser)
                .Include(c => c.CreditUsages)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return View(credits);
        }

        public async Task<IActionResult> IssueCredit()
        {
            ViewBag.Tenants = await _context.Tenants.Where(t => t.IsActive).ToListAsync();
            return View(new BillingCredit());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> IssueCredit(BillingCredit credit)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Tenants = await _context.Tenants.Where(t => t.IsActive).ToListAsync();
                return View(credit);
            }

            credit.IssuedByUserId = _userManager.GetUserId(User)!;
            credit.CreatedAt = DateTime.UtcNow;
            credit.Status = "Active";

            _context.BillingCredits.Add(credit);

            // Update tenant's subscription credit balance
            var subscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.TenantId == credit.TenantId && s.Status == "Active");
            if (subscription != null)
            {
                subscription.CreditBalance = (subscription.CreditBalance ?? 0) + credit.Amount;
                subscription.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            await LogBillingAction("IssueCredit", "BillingCredit", credit.Id, 
                $"Issued credit of {credit.Amount:C} to tenant ID {credit.TenantId}. Reason: {credit.Reason}");

            TempData["Success"] = "Credit issued successfully.";
            return RedirectToAction(nameof(Credits));
        }

        // Discounts & Promotions
        public async Task<IActionResult> Discounts()
        {
            var discounts = await _context.BillingDiscounts
                .Include(d => d.CreatedByUser)
                .Include(d => d.DiscountUsages)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            return View(discounts);
        }

        public async Task<IActionResult> CreateDiscount()
        {
            ViewBag.Plans = await _context.SubscriptionPlans.Where(p => p.Status == "Active").ToListAsync();
            return View(new BillingDiscount());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDiscount(BillingDiscount discount, int[] selectedPlanIds)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Plans = await _context.SubscriptionPlans.Where(p => p.Status == "Active").ToListAsync();
                return View(discount);
            }

            discount.CreatedByUserId = _userManager.GetUserId(User)!;
            discount.CreatedAt = DateTime.UtcNow;
            
            if (selectedPlanIds?.Length > 0)
            {
                discount.ApplicableTo = "SpecificPlans";
                discount.ApplicablePlanIds = JsonSerializer.Serialize(selectedPlanIds);
            }

            _context.BillingDiscounts.Add(discount);
            await _context.SaveChangesAsync();

            await LogBillingAction("CreateDiscount", "BillingDiscount", discount.Id, 
                $"Created discount: {discount.Name} ({discount.Code})");

            TempData["Success"] = "Discount created successfully.";
            return RedirectToAction(nameof(Discounts));
        }

        public async Task<IActionResult> EditDiscount(int id)
        {
            var discount = await _context.BillingDiscounts.FindAsync(id);
            if (discount == null) return NotFound();

            ViewBag.Plans = await _context.SubscriptionPlans.Where(p => p.Status == "Active").ToListAsync();
            return View(discount);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDiscount(int id, BillingDiscount discount, int[] selectedPlanIds)
        {
            if (id != discount.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.Plans = await _context.SubscriptionPlans.Where(p => p.Status == "Active").ToListAsync();
                return View(discount);
            }

            var existing = await _context.BillingDiscounts.FindAsync(id);
            if (existing == null) return NotFound();

            existing.Name = discount.Name;
            existing.Code = discount.Code;
            existing.Description = discount.Description;
            existing.DiscountType = discount.DiscountType;
            existing.DiscountValue = discount.DiscountValue;
            existing.ValidFrom = discount.ValidFrom;
            existing.ValidUntil = discount.ValidUntil;
            existing.MaxUsageCount = discount.MaxUsageCount;
            existing.MaxUsagePerTenant = discount.MaxUsagePerTenant;
            existing.MinimumOrderAmount = discount.MinimumOrderAmount;
            existing.ApplicableTo = discount.ApplicableTo;
            existing.Status = discount.Status;
            existing.UpdatedAt = DateTime.UtcNow;

            if (selectedPlanIds?.Length > 0)
            {
                existing.ApplicableTo = "SpecificPlans";
                existing.ApplicablePlanIds = JsonSerializer.Serialize(selectedPlanIds);
            }
            else if (discount.ApplicableTo == "AllPlans")
            {
                existing.ApplicablePlanIds = null;
            }

            await _context.SaveChangesAsync();
            await LogBillingAction("UpdateDiscount", "BillingDiscount", id, $"Updated discount: {existing.Name}");

            TempData["Success"] = "Discount updated successfully.";
            return RedirectToAction(nameof(Discounts));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateDiscount(int id)
        {
            var discount = await _context.BillingDiscounts.FindAsync(id);
            if (discount == null) return NotFound();

            discount.Status = "Active";
            discount.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await LogBillingAction("ActivateDiscount", "BillingDiscount", id, $"Activated discount: {discount.Name}");
            TempData["Success"] = "Discount activated successfully.";
            return RedirectToAction(nameof(Discounts));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateDiscount(int id)
        {
            var discount = await _context.BillingDiscounts.FindAsync(id);
            if (discount == null) return NotFound();

            discount.Status = "Inactive";
            discount.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await LogBillingAction("DeactivateDiscount", "BillingDiscount", id, $"Deactivated discount: {discount.Name}");
            TempData["Success"] = "Discount deactivated successfully.";
            return RedirectToAction(nameof(Discounts));
        }

        // Billing Transactions
        public async Task<IActionResult> Transactions(int? tenantId = null, string? status = null)
        {
            var query = _context.BillingTransactions
                .Include(t => t.Tenant)
                .Include(t => t.Subscription)
                .Include(t => t.ProcessedByUser)
                .AsQueryable();

            if (tenantId.HasValue)
                query = query.Where(t => t.TenantId == tenantId);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(t => t.Status == status);

            var transactions = await query
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            ViewBag.Tenants = await _context.Tenants.ToListAsync();
            ViewBag.SelectedTenantId = tenantId;
            ViewBag.SelectedStatus = status;

            return View(transactions);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(int subscriptionId, decimal amount, string paymentMethod, string? reference)
        {
            var subscription = await _context.Subscriptions
                .Include(s => s.Tenant)
                .FirstOrDefaultAsync(s => s.Id == subscriptionId);

            if (subscription == null) return NotFound();

            var transaction = new BillingTransaction
            {
                TenantId = subscription.TenantId,
                SubscriptionId = subscriptionId,
                TransactionType = "Payment",
                Amount = amount,
                NetAmount = amount,
                Status = "Completed",
                PaymentMethod = paymentMethod,
                PaymentReference = reference,
                TransactionDate = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow,
                ProcessedByUserId = _userManager.GetUserId(User),
                Notes = "Manual payment processed by Super Admin"
            };

            _context.BillingTransactions.Add(transaction);

            // Update subscription dates if this is a renewal
            if (subscription.Status == "PastDue" || subscription.EndDate <= DateTime.UtcNow)
            {
                subscription.Status = "Active";
                subscription.EndDate = subscription.BillingCycle == "Yearly" 
                    ? DateTime.UtcNow.AddYears(1) 
                    : DateTime.UtcNow.AddMonths(1);
                subscription.NextBillingDate = subscription.EndDate;
                subscription.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            await LogBillingAction("ProcessPayment", "BillingTransaction", transaction.Id, 
                $"Processed payment of {amount:C} for tenant {subscription.Tenant.CompanyName}");

            TempData["Success"] = "Payment processed successfully.";
            return RedirectToAction(nameof(SubscriptionDetails), new { id = subscriptionId });
        }

        // Helper Methods
        private async Task<RevenueMetrics> CalculateRevenueMetrics()
        {
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var startOfYear = new DateTime(now.Year, 1, 1);

            var activeSubscriptions = await _context.Subscriptions
                .Include(s => s.SubscriptionPlan)
                .Where(s => s.Status == "Active")
                .ToListAsync();

            var mrr = activeSubscriptions
                .Where(s => s.BillingCycle == "Monthly")
                .Sum(s => s.PriceAmount) +
                activeSubscriptions
                .Where(s => s.BillingCycle == "Yearly")
                .Sum(s => s.PriceAmount / 12);

            var arr = mrr * 12;

            var totalRevenue = await _context.BillingTransactions
                .Where(t => t.Status == "Completed" && t.TransactionType == "Payment")
                .SumAsync(t => t.NetAmount);

            var monthlyTransactions = await _context.BillingTransactions
                .Where(t => t.Status == "Completed" && t.TransactionDate >= startOfMonth)
                .ToListAsync();

            var newSubscriptionsThisMonth = await _context.Subscriptions
                .CountAsync(s => s.CreatedAt >= startOfMonth);

            var cancelledSubscriptionsThisMonth = await _context.Subscriptions
                .CountAsync(s => s.Status == "Cancelled" && s.UpdatedAt >= startOfMonth);

            var trialSubscriptions = await _context.Subscriptions
                .CountAsync(s => s.Status == "Trial");

            var planDistribution = activeSubscriptions
                .GroupBy(s => s.SubscriptionPlan.Name)
                .ToDictionary(g => g.Key, g => g.Count());

            var revenueByPlan = activeSubscriptions
                .GroupBy(s => s.SubscriptionPlan.Name)
                .ToDictionary(g => g.Key, g => g.Sum(s => s.PriceAmount));

            // Calculate monthly trends for the last 12 months
            var monthlyTrends = new List<MonthlyRevenue>();
            for (int i = 11; i >= 0; i--)
            {
                var monthStart = startOfMonth.AddMonths(-i);
                var monthEnd = monthStart.AddMonths(1);

                var monthRevenue = await _context.BillingTransactions
                    .Where(t => t.Status == "Completed" && 
                               t.TransactionType == "Payment" &&
                               t.TransactionDate >= monthStart && 
                               t.TransactionDate < monthEnd)
                    .SumAsync(t => t.NetAmount);

                var newCustomers = await _context.Subscriptions
                    .CountAsync(s => s.CreatedAt >= monthStart && s.CreatedAt < monthEnd);

                var churnedCustomers = await _context.Subscriptions
                    .CountAsync(s => s.Status == "Cancelled" && 
                               s.UpdatedAt >= monthStart && s.UpdatedAt < monthEnd);

                monthlyTrends.Add(new MonthlyRevenue
                {
                    Month = monthStart.ToString("MMM yyyy"),
                    Revenue = monthRevenue,
                    NewCustomers = newCustomers,
                    ChurnedCustomers = churnedCustomers
                });
            }

            var churnRate = activeSubscriptions.Count > 0 
                ? (decimal)cancelledSubscriptionsThisMonth / activeSubscriptions.Count * 100 
                : 0;

            return new RevenueMetrics
            {
                MonthlyRecurringRevenue = mrr,
                AnnualRecurringRevenue = arr,
                TotalRevenue = totalRevenue,
                AverageRevenuePerUser = activeSubscriptions.Count > 0 ? mrr / activeSubscriptions.Count : 0,
                ChurnRate = churnRate,
                ActiveSubscriptions = activeSubscriptions.Count,
                NewSubscriptions = newSubscriptionsThisMonth,
                CancelledSubscriptions = cancelledSubscriptionsThisMonth,
                TrialSubscriptions = trialSubscriptions,
                PlanDistribution = planDistribution,
                RevenueByPlan = revenueByPlan,
                MonthlyTrends = monthlyTrends
            };
        }

        private decimal CalculateProratedAmount(Subscription subscription, decimal newPrice)
        {
            var daysRemaining = (subscription.EndDate - DateTime.UtcNow).Days;
            var totalDays = subscription.BillingCycle == "Yearly" ? 365 : 30;
            
            if (daysRemaining <= 0) return 0;

            var currentProratedAmount = (subscription.PriceAmount / totalDays) * daysRemaining;
            var newProratedAmount = (newPrice / totalDays) * daysRemaining;

            return newProratedAmount - currentProratedAmount;
        }

        private async Task LogBillingAction(string action, string entityType, int entityId, string details)
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
    }
}