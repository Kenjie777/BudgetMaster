using BudgetMasterFinal.Models;
using BudgetMasterFinal.Controllers;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BudgetMasterFinal.Services
{
    public class PdfReportService
    {
        public byte[] GeneratePlatformAnalyticsReport(PlatformAnalyticsReport report)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Header().Element(ComposeHeader);
                    page.Content().Element(content => ComposeContent(content, report));
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Generated on ");
                        text.Span(DateTime.UtcNow.ToString("MMMM dd, yyyy HH:mm:ss UTC")).SemiBold();
                        text.Span(" | Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }

        void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("Platform Analytics Report").FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
                    column.Item().Text("SaaS Platform Metrics & Insights").FontSize(12).FontColor(Colors.Grey.Darken1);
                });

                row.ConstantItem(100).Height(50).Placeholder();
            });
        }

        void ComposeContent(IContainer container, PlatformAnalyticsReport report)
        {
            container.PaddingVertical(20).Column(column =>
            {
                column.Spacing(15);

                // Summary Section
                column.Item().Element(c => ComposeSummarySection(c, report));

                // Tenant Metrics
                column.Item().Element(c => ComposeTenantMetrics(c, report));

                // User Metrics
                column.Item().Element(c => ComposeUserMetrics(c, report));

                // Subscription Metrics
                column.Item().Element(c => ComposeSubscriptionMetrics(c, report));

                // Revenue Metrics
                column.Item().Element(c => ComposeRevenueMetrics(c, report));

                // Plan Distribution
                if (report.SubscriptionsByPlan.Any())
                {
                    column.Item().Element(c => ComposePlanDistribution(c, report));
                }
            });
        }

        void ComposeSummarySection(IContainer container, PlatformAnalyticsReport report)
        {
            container.Column(column =>
            {
                column.Item().Background(Colors.Blue.Lighten4).Padding(10).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Total Tenants").FontSize(9).FontColor(Colors.Grey.Darken1);
                        col.Item().Text(report.TotalTenants.ToString()).FontSize(18).SemiBold();
                        col.Item().Text($"{report.ActiveTenants} active").FontSize(8).FontColor(Colors.Grey.Medium);
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Total Users").FontSize(9).FontColor(Colors.Grey.Darken1);
                        col.Item().Text(report.TotalUsers.ToString()).FontSize(18).SemiBold();
                        col.Item().Text($"{report.ActiveUsers} active").FontSize(8).FontColor(Colors.Grey.Medium);
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Active Subscriptions").FontSize(9).FontColor(Colors.Grey.Darken1);
                        col.Item().Text(report.ActiveSubscriptions.ToString()).FontSize(18).SemiBold();
                        col.Item().Text($"{report.ExpiringThisMonth} expiring").FontSize(8).FontColor(Colors.Grey.Medium);
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Monthly Revenue").FontSize(9).FontColor(Colors.Grey.Darken1);
                        col.Item().Text($"₱{report.MonthlyRecurringRevenue:N0}").FontSize(18).SemiBold();
                        col.Item().Text("MRR").FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });
            });
        }

        void ComposeTenantMetrics(IContainer container, PlatformAnalyticsReport report)
        {
            container.Column(column =>
            {
                column.Item().Text("Tenant Metrics").FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);
                column.Item().PaddingTop(5).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Total Tenants");
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(report.TotalTenants.ToString());

                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Active Tenants");
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(report.ActiveTenants.ToString());

                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Inactive Tenants");
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(report.InactiveTenants.ToString());

                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("New This Month");
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(report.NewTenantsThisMonth.ToString());

                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Growth Rate");
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{report.TenantGrowthRate:F2}%");
                });
            });
        }

        void ComposeUserMetrics(IContainer container, PlatformAnalyticsReport report)
        {
            container.Column(column =>
            {
                column.Item().Text("User Metrics").FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);
                column.Item().PaddingTop(5).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Total Users");
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(report.TotalUsers.ToString());

                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Active Users");
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(report.ActiveUsers.ToString());

                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("New This Month");
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(report.NewUsersThisMonth.ToString());
                });
            });
        }

        void ComposeSubscriptionMetrics(IContainer container, PlatformAnalyticsReport report)
        {
            container.Column(column =>
            {
                column.Item().Text("Subscription Metrics").FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);
                column.Item().PaddingTop(5).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Total Subscriptions");
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(report.TotalSubscriptions.ToString());

                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Active");
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(report.ActiveSubscriptions.ToString());

                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Trial");
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(report.TrialSubscriptions.ToString());

                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Expired");
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(report.ExpiredSubscriptions.ToString());

                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Expiring This Month");
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(report.ExpiringThisMonth.ToString());
                });
            });
        }

        void ComposeRevenueMetrics(IContainer container, PlatformAnalyticsReport report)
        {
            container.Column(column =>
            {
                column.Item().Text("Revenue Metrics").FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);
                column.Item().PaddingTop(5).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Monthly Recurring Revenue");
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"₱{report.MonthlyRecurringRevenue:N2}");

                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Annual Recurring Revenue");
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"₱{report.AnnualRecurringRevenue:N2}");

                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Revenue This Month");
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"₱{report.TotalRevenueThisMonth:N2}");

                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Revenue This Year");
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"₱{report.TotalRevenueThisYear:N2}");

                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Average Per Tenant");
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"₱{report.AverageRevenuePerTenant:N2}");
                });
            });
        }

        void ComposePlanDistribution(IContainer container, PlatformAnalyticsReport report)
        {
            container.Column(column =>
            {
                column.Item().Text("Subscription Plan Distribution").FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);
                column.Item().PaddingTop(5).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    // Header
                    table.Cell().Background(Colors.Grey.Lighten3).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Plan Name").SemiBold();
                    table.Cell().Background(Colors.Grey.Lighten3).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("Subscriptions").SemiBold();
                    table.Cell().Background(Colors.Grey.Lighten3).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("Revenue").SemiBold();
                    table.Cell().Background(Colors.Grey.Lighten3).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("Percentage").SemiBold();

                    // Data
                    foreach (var plan in report.SubscriptionsByPlan.OrderByDescending(p => p.Value))
                    {
                        var revenue = report.RevenueByPlan.ContainsKey(plan.Key) ? report.RevenueByPlan[plan.Key] : 0;
                        var percentage = report.TotalSubscriptions > 0 ? (decimal)plan.Value / report.TotalSubscriptions * 100 : 0;

                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(plan.Key);
                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(plan.Value.ToString());
                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"₱{revenue:N0}");
                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{percentage:F1}%");
                    }
                });
            });
        }

        // ══════════════════════════════════════════════════════════════════════════════════════
        // BUDGET CONSOLIDATION REPORT
        // ══════════════════════════════════════════════════════════════════════════════════════

        public byte[] GenerateConsolidationReport(
            int fiscalYear,
            List<ConsolidationReportRow> departmentData,
            decimal totalAllocated,
            decimal totalUsed,
            decimal totalActualSpend,
            decimal totalVariance,
            decimal totalRemaining)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(40);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Header().Element(c => ComposeConsolidationHeader(c, fiscalYear));
                    page.Content().Element(c => ComposeConsolidationContent(c, departmentData, totalAllocated, totalUsed, totalActualSpend, totalVariance, totalRemaining));
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Generated on ");
                        text.Span(DateTime.UtcNow.ToString("MMMM dd, yyyy HH:mm:ss UTC")).SemiBold();
                        text.Span(" | Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }

        void ComposeConsolidationHeader(IContainer container, int fiscalYear)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("Budget Consolidation Report").FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
                    column.Item().Text($"Fiscal Year {fiscalYear}").FontSize(14).FontColor(Colors.Grey.Darken1);
                });
            });
        }

        void ComposeConsolidationContent(
            IContainer container,
            List<ConsolidationReportRow> departmentData,
            decimal totalAllocated,
            decimal totalUsed,
            decimal totalActualSpend,
            decimal totalVariance,
            decimal totalRemaining)
        {
            container.PaddingVertical(20).Column(column =>
            {
                column.Spacing(15);

                // Summary Cards
                column.Item().Background(Colors.Blue.Lighten4).Padding(10).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Total Allocated").FontSize(9).FontColor(Colors.Grey.Darken1);
                        col.Item().Text($"₱{totalAllocated:N2}").FontSize(16).SemiBold();
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Total Used").FontSize(9).FontColor(Colors.Grey.Darken1);
                        col.Item().Text($"₱{totalUsed:N2}").FontSize(16).SemiBold();
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Actual Spend").FontSize(9).FontColor(Colors.Grey.Darken1);
                        col.Item().Text($"₱{totalActualSpend:N2}").FontSize(16).SemiBold();
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Variance").FontSize(9).FontColor(Colors.Grey.Darken1);
                        col.Item().Text($"₱{totalVariance:N2}").FontSize(16).SemiBold();
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Remaining").FontSize(9).FontColor(Colors.Grey.Darken1);
                        col.Item().Text($"₱{totalRemaining:N2}").FontSize(16).SemiBold();
                    });
                });

                // Department Table
                column.Item().Text("Department Breakdown").FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);
                column.Item().PaddingTop(5).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    // Header
                    table.Cell().Background(Colors.Grey.Lighten3).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Department").SemiBold();
                    table.Cell().Background(Colors.Grey.Lighten3).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("Allocated").SemiBold();
                    table.Cell().Background(Colors.Grey.Lighten3).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("Used").SemiBold();
                    table.Cell().Background(Colors.Grey.Lighten3).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("Actual Spend").SemiBold();
                    table.Cell().Background(Colors.Grey.Lighten3).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("Variance").SemiBold();
                    table.Cell().Background(Colors.Grey.Lighten3).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("Remaining").SemiBold();

                    // Data rows
                    foreach (var dept in departmentData)
                    {
                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(dept.Department);
                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"₱{dept.Allocated:N2}");
                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"₱{dept.Used:N2}");
                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"₱{dept.ActualSpend:N2}");
                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"₱{dept.Variance:N2}");
                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"₱{dept.Remaining:N2}");
                    }

                    // Total row
                    table.Cell().Background(Colors.Grey.Lighten3).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("TOTAL").SemiBold();
                    table.Cell().Background(Colors.Grey.Lighten3).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"₱{totalAllocated:N2}").SemiBold();
                    table.Cell().Background(Colors.Grey.Lighten3).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"₱{totalUsed:N2}").SemiBold();
                    table.Cell().Background(Colors.Grey.Lighten3).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"₱{totalActualSpend:N2}").SemiBold();
                    table.Cell().Background(Colors.Grey.Lighten3).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"₱{totalVariance:N2}").SemiBold();
                    table.Cell().Background(Colors.Grey.Lighten3).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"₱{totalRemaining:N2}").SemiBold();
                });
            });
        }
    }
}
