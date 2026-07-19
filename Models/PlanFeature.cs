using System.ComponentModel.DataAnnotations;

namespace BudgetMasterFinal.Models
{
    public class PlanFeature
    {
        public int Id { get; set; }

        public int SubscriptionPlanId { get; set; }
        public SubscriptionPlan SubscriptionPlan { get; set; } = null!;

        [Required, MaxLength(100)]
        public string FeatureName { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Description { get; set; }

        [MaxLength(20)]
        public string FeatureType { get; set; } = "Boolean"; // Boolean, Numeric, Text

        public string? FeatureValue { get; set; } // JSON or simple value

        public bool IsEnabled { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}