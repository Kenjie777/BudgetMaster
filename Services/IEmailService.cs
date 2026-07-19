namespace BudgetMasterFinal.Services
{
    public interface IEmailService
    {
        Task<bool> SendWelcomeEmailAsync(string toEmail, string firstName, string lastName, string role, string temporaryPassword, string organizationName, string loginUrl);
        Task<bool> SendPasswordResetEmailAsync(string toEmail, string firstName, string resetLink);
        Task<bool> SendTestEmailAsync(string toEmail);
    }
}
