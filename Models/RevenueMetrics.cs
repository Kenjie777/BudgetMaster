namespace BudgetMasterFinal.Models
{
    public class RevenueMetrics
    {
        public decimal MonthlyRecurringRevenue { get; set; }
        public decimal AnnualRecurringRevenue { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageRevenuePerUser { get; set; }
        public decimal ChurnRate { get; set; }
        public decimal GrowthRate { get; set; }
        public int ActiveSubscriptions { get; set; }
        public int NewSubscriptions { get; set; }
        public int CancelledSubscriptions { get; set; }
        public int TrialSubscriptions { get; set; }
        public Dictionary<string, int> PlanDistribution { get; set; } = new();
        public Dictionary<string, decimal> RevenueByPlan { get; set; } = new();
        public List<MonthlyRevenue> MonthlyTrends { get; set; } = new();
    }

    public class MonthlyRevenue
    {
        public string Month { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int NewCustomers { get; set; }
        public int ChurnedCustomers { get; set; }
    }
}