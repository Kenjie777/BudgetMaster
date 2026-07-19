using BudgetMasterFinal.Data;
using BudgetMasterFinal.Models;
using BudgetMasterFinal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetMasterFinal.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class SuperAdminReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PdfReportService _pdfService;

        public SuperAdminReportsController(ApplicationDbContext context, PdfReportService pdfService)
        {
            _context = context;
            _pdfService = pdfService;
        }

        // Main Reports Dashboard
        public async Task<IActionResult> Index()
        {
            var report = await GeneratePlatformAnalytics(null, null, null, null);
            return View(report);
        }

        // Tenant Growth Report
        public async Task<IActionResult> TenantGrowth(DateTime? startDate, DateTime? endDate)
        {
            startDate ??= DateTime.UtcNow.AddMonths(-12);
            endDate ??= DateTime.UtcNow;

            var metrics = await _context.Tenants
                .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate)
                .GroupBy(t => new { t.CreatedAt.Year, t.CreatedAt.Month })
                .Select(g => new TenantGrowthMetric
                {
                    Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                    NewTenants = g.Count()
                })
                .OrderBy(m => m.Period)
                .ToListAsync();

            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            return View(metrics);
        }

        // Revenue Report
        public async Task<IActionResult> Revenue(DateTime? startDate, DateTime? endDate, string? plan)
        {
            startDate ??= DateTime.UtcNow.AddMonths(-12);
            endDate ??= DateTime.UtcNow;

            var query = _context.Subscriptions
                .Include(s => s.SubscriptionPlan)
                .Where(s => s.StartDate >= startDate && s.StartDate <= endDate);

            if (!string.IsNullOrEmpty(plan))
            {
                query = query.Where(s => s.SubscriptionPlan.Name == plan);
            }

            var metrics = await query
                .GroupBy(s => new { s.StartDate.Year, s.StartDate.Month })
                .Select(g => new RevenueMetric
                {
                    Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                    Revenue = g.Sum(s => s.PriceAmount),
                    Subscriptions = g.Count(),
                    AveragePerSubscription = g.Average(s => s.PriceAmount)
                })
                .OrderBy(m => m.Period)
                .ToListAsync();

            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            ViewBag.Plan = plan;
            ViewBag.Plans = await _context.SubscriptionPlans.Select(p => p.Name).ToListAsync();
            return View(metrics);
        }

        // System Usage Report
        public async Task<IActionResult> SystemUsage(DateTime? startDate, DateTime? endDate)
        {
            startDate ??= DateTime.UtcNow.AddDays(-30);
            endDate ??= DateTime.UtcNow;

            // Note: This would require a login tracking table in production
            // For now, we'll show user activity based on last login
            var metrics = new List<SystemUsageMetric>();

            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            return View(metrics);
        }

        // Audit Logs Report
        public async Task<IActionResult> AuditLogs(DateTime? startDate, DateTime? endDate, string? action)
        {
            startDate ??= DateTime.UtcNow.AddDays(-30);
            endDate ??= DateTime.UtcNow;

            var query = _context.AuditLogs
                .Where(a => a.Timestamp >= startDate && a.Timestamp <= endDate);

            if (!string.IsNullOrEmpty(action))
            {
                query = query.Where(a => a.Action == action);
            }

            var logs = await query
                .OrderByDescending(a => a.Timestamp)
                .Take(1000)
                .ToListAsync();

            var summary = logs
                .GroupBy(a => a.Action)
                .Select(g => new AuditLogSummary
                {
                    Action = g.Key,
                    Count = g.Count(),
                    LastOccurrence = g.Max(a => a.Timestamp)
                })
                .OrderByDescending(s => s.Count)
                .ToList();

            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            ViewBag.Action = action;
            ViewBag.Summary = summary;
            return View(logs);
        }

        // Subscription Status Report
        public async Task<IActionResult> Subscriptions(string? status, string? plan)
        {
            var query = _context.Subscriptions
                .Include(s => s.SubscriptionPlan)
                .Include(s => s.Tenant)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(s => s.Status == status);
            }

            if (!string.IsNullOrEmpty(plan))
            {
                query = query.Where(s => s.SubscriptionPlan.Name == plan);
            }

            var subscriptions = await query
                .OrderByDescending(s => s.StartDate)
                .ToListAsync();

            ViewBag.Status = status;
            ViewBag.Plan = plan;
            ViewBag.Plans = await _context.SubscriptionPlans.Select(p => p.Name).ToListAsync();
            return View(subscriptions);
        }

        // Export to PDF
        [HttpPost]
        public async Task<IActionResult> ExportPDF(string reportType, DateTime? startDate, DateTime? endDate, string? filter)
        {
            var report = await GeneratePlatformAnalytics(startDate, endDate, filter, null);
            var pdfBytes = _pdfService.GeneratePlatformAnalyticsReport(report);
            
            return File(pdfBytes, "application/pdf", $"Platform_Analytics_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf");
        }

        // Helper method to generate platform analytics
        private async Task<PlatformAnalyticsReport> GeneratePlatformAnalytics(
            DateTime? startDate, 
            DateTime? endDate, 
            string? plan, 
            string? status)
        {
            var now = DateTime.UtcNow;
            var firstDayOfMonth = new DateTime(now.Year, now.Month, 1);
            var firstDayOfYear = new DateTime(now.Year, 1, 1);

            var report = new PlatformAnalyticsReport
            {
                GeneratedAt = now,
                StartDate = startDate,
                EndDate = endDate,
                FilteredByPlan = plan,
                FilteredByStatus = status
            };

            // Tenant Metrics
            report.TotalTenants = await _context.Tenants.CountAsync();
            report.ActiveTenants = await _context.Tenants.CountAsync(t => t.IsActive);
            report.InactiveTenants = report.TotalTenants - report.ActiveTenants;
            report.NewTenantsThisMonth = await _context.Tenants.CountAsync(t => t.CreatedAt >= firstDayOfMonth);

            var lastMonthTenants = await _context.Tenants.CountAsync(t => t.CreatedAt < firstDayOfMonth);
            report.TenantGrowthRate = lastMonthTenants > 0 
                ? (decimal)report.NewTenantsThisMonth / lastMonthTenants * 100 
                : 0;

            // User Metrics
            report.TotalUsers = await _context.Users.CountAsync();
            report.ActiveUsers = await _context.Users.CountAsync(u => u.IsActive);
            report.NewUsersThisMonth = await _context.Users.CountAsync(u => u.CreatedAt >= firstDayOfMonth);

            // Subscription Metrics
            report.TotalSubscriptions = await _context.Subscriptions.CountAsync();
            report.ActiveSubscriptions = await _context.Subscriptions.CountAsync(s => s.Status == "Active");
            report.TrialSubscriptions = await _context.Subscriptions.CountAsync(s => s.Status == "Trial");
            report.ExpiredSubscriptions = await _context.Subscriptions.CountAsync(s => s.Status == "Expired");
            report.ExpiringThisMonth = await _context.Subscriptions
                .CountAsync(s => s.EndDate >= now && s.EndDate < now.AddMonths(1));

            // Revenue Metrics
            var activeSubscriptions = await _context.Subscriptions
                .Include(s => s.SubscriptionPlan)
                .Where(s => s.Status == "Active")
                .ToListAsync();

            report.MonthlyRecurringRevenue = activeSubscriptions
                .Where(s => s.BillingCycle == "Monthly")
                .Sum(s => s.PriceAmount);

            report.AnnualRecurringRevenue = activeSubscriptions
                .Where(s => s.BillingCycle == "Yearly")
                .Sum(s => s.PriceAmount);

            report.TotalRevenueThisMonth = await _context.Subscriptions
                .Include(s => s.SubscriptionPlan)
                .Where(s => s.StartDate >= firstDayOfMonth)
                .SumAsync(s => s.PriceAmount);

            report.TotalRevenueThisYear = await _context.Subscriptions
                .Include(s => s.SubscriptionPlan)
                .Where(s => s.StartDate >= firstDayOfYear)
                .SumAsync(s => s.PriceAmount);

            report.AverageRevenuePerTenant = report.ActiveTenants > 0
                ? (report.MonthlyRecurringRevenue + report.AnnualRecurringRevenue / 12) / report.ActiveTenants
                : 0;

            // Plan Distribution
            report.SubscriptionsByPlan = await _context.Subscriptions
                .Include(s => s.SubscriptionPlan)
                .GroupBy(s => s.SubscriptionPlan.Name)
                .Select(g => new { Plan = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Plan, x => x.Count);

            report.RevenueByPlan = await _context.Subscriptions
                .Include(s => s.SubscriptionPlan)
                .Where(s => s.Status == "Active")
                .GroupBy(s => s.SubscriptionPlan.Name)
                .Select(g => new { Plan = g.Key, Revenue = g.Sum(s => s.PriceAmount) })
                .ToDictionaryAsync(x => x.Plan, x => x.Revenue);

            // Audit Metrics
            report.TotalAuditLogs = await _context.AuditLogs.CountAsync();
            report.AuditLogsThisMonth = await _context.AuditLogs.CountAsync(a => a.Timestamp >= firstDayOfMonth);

            report.AuditLogsByAction = await _context.AuditLogs
                .Where(a => a.Timestamp >= firstDayOfMonth)
                .GroupBy(a => a.Action)
                .Select(g => new { Action = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Action, x => x.Count);

            return report;
        }
    }
}
