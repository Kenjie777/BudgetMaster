using BudgetMasterFinal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetMasterFinal.Controllers
{
    [Authorize(Policy = "AdminPolicy")]
    public class EmailTestController : Controller
    {
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailTestController> _logger;

        public EmailTestController(
            IEmailService emailService, 
            IConfiguration configuration,
            ILogger<EmailTestController> logger)
        {
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            // Check if email is configured
            var smtpUsername = _configuration["Email:SmtpUsername"];
            var smtpPassword = _configuration["Email:SmtpPassword"];
            
            ViewBag.IsConfigured = !string.IsNullOrEmpty(smtpUsername) && 
                                   !string.IsNullOrEmpty(smtpPassword) &&
                                   smtpUsername != "your-email@gmail.com" &&
                                   smtpPassword != "your-app-specific-password";
            
            ViewBag.SmtpUsername = smtpUsername;
            ViewBag.SmtpHost = _configuration["Email:SmtpHost"];
            ViewBag.SmtpPort = _configuration["Email:SmtpPort"];
            
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendTest(string testEmail)
        {
            if (string.IsNullOrEmpty(testEmail))
            {
                TempData["Error"] = "Please provide a test email address.";
                return RedirectToAction("Index");
            }

            try
            {
                _logger.LogInformation("Attempting to send test email to {Email}", testEmail);
                
                var result = await _emailService.SendTestEmailAsync(testEmail);
                
                if (result)
                {
                    TempData["Success"] = $"Test email sent successfully to {testEmail}! Check your inbox (and spam folder).";
                    _logger.LogInformation("Test email sent successfully to {Email}", testEmail);
                }
                else
                {
                    TempData["Error"] = "Failed to send test email. Check your email configuration in appsettings.json and application logs.";
                    _logger.LogError("Failed to send test email to {Email}", testEmail);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error sending email: {ex.Message}";
                _logger.LogError(ex, "Exception while sending test email to {Email}", testEmail);
            }

            return RedirectToAction("Index");
        }
    }
}
