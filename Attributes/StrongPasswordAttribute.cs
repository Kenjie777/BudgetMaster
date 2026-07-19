using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace BudgetMasterFinal.Attributes
{
    /// <summary>
    /// Custom validation attribute for strong password requirements
    /// - Minimum 12 characters
    /// - At least one uppercase letter
    /// - At least one lowercase letter
    /// - At least one number
    /// - At least one special character
    /// </summary>
    public class StrongPasswordAttribute : ValidationAttribute
    {
        public StrongPasswordAttribute()
        {
            ErrorMessage = "Password must be at least 12 characters and contain at least one uppercase letter, one lowercase letter, one number, and one special character.";
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return new ValidationResult("Password is required.");
            }

            string password = value.ToString()!;

            // Check minimum length
            if (password.Length < 12)
            {
                return new ValidationResult("Password must be at least 12 characters long.");
            }

            // Check for uppercase letter
            if (!Regex.IsMatch(password, @"[A-Z]"))
            {
                return new ValidationResult("Password must contain at least one uppercase letter.");
            }

            // Check for lowercase letter
            if (!Regex.IsMatch(password, @"[a-z]"))
            {
                return new ValidationResult("Password must contain at least one lowercase letter.");
            }

            // Check for number
            if (!Regex.IsMatch(password, @"[0-9]"))
            {
                return new ValidationResult("Password must contain at least one number.");
            }

            // Check for special character
            if (!Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]"))
            {
                return new ValidationResult("Password must contain at least one special character (!@#$%^&*()_+-=[]{}; ':\"\\|,.<>/?)."); 
            }

            return ValidationResult.Success;
        }
    }
}
