using System.Net;
using System.Net.Mail;

namespace BudgetMasterFinal.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendWelcomeEmailAsync(string toEmail, string firstName, string lastName, string role, string temporaryPassword, string organizationName, string loginUrl)
        {
            try
            {
                var subject = $"Welcome to BudgetMaster - Your Account Has Been Created";
                var body = GenerateWelcomeEmailBody(firstName, lastName, role, temporaryPassword, organizationName, loginUrl, toEmail);
                
                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send welcome email to {Email}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string firstName, string resetLink)
        {
            try
            {
                var subject = "BudgetMaster - Password Reset Request";
                var body = GeneratePasswordResetEmailBody(firstName, resetLink);
                
                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendTestEmailAsync(string toEmail)
        {
            try
            {
                var subject = "BudgetMaster - Test Email";
                var body = "<h1>Test Email</h1><p>If you received this email, your email configuration is working correctly!</p>";
                
                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send test email to {Email}", toEmail);
                return false;
            }
        }

        private async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            var smtpHost = _configuration["Email:SmtpHost"];
            var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            var smtpUsername = _configuration["Email:SmtpUsername"];
            var smtpPassword = _configuration["Email:SmtpPassword"];
            var fromEmail = _configuration["Email:FromEmail"];
            var fromName = _configuration["Email:FromName"] ?? "BudgetMaster";

            if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
            {
                _logger.LogWarning("Email configuration is incomplete. Email not sent to {Email}", toEmail);
                return false;
            }

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(smtpUsername, smtpPassword)
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail ?? smtpUsername, fromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);
            _logger.LogInformation("Email sent successfully to {Email}", toEmail);
            return true;
        }

        private string GenerateWelcomeEmailBody(string firstName, string lastName, string role, string temporaryPassword, string organizationName, string loginUrl, string email)
        {
            var roleDescription = GetRoleDescription(role);
            var roleResponsibilities = GetRoleResponsibilities(role);

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; background-color: #f4f4f4; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 20px auto; background: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px 20px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 28px; font-weight: 600; }}
        .content {{ padding: 30px 20px; }}
        .welcome-box {{ background: #f8f9fa; border-left: 4px solid #667eea; padding: 15px; margin: 20px 0; border-radius: 4px; }}
        .credentials-box {{ background: #fff3cd; border: 2px solid #ffc107; padding: 20px; margin: 20px 0; border-radius: 8px; }}
        .credentials-box h3 {{ margin-top: 0; color: #856404; }}
        .credential-item {{ margin: 10px 0; padding: 10px; background: white; border-radius: 4px; }}
        .credential-label {{ font-weight: 600; color: #666; font-size: 12px; text-transform: uppercase; }}
        .credential-value {{ font-size: 16px; color: #333; font-family: 'Courier New', monospace; font-weight: 600; }}
        .btn {{ display: inline-block; padding: 14px 30px; background: #667eea; color: white; text-decoration: none; border-radius: 6px; font-weight: 600; margin: 20px 0; }}
        .btn:hover {{ background: #5568d3; }}
        .warning {{ background: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; border-radius: 4px; }}
        .warning strong {{ color: #856404; }}
        .role-badge {{ display: inline-block; background: #667eea; color: white; padding: 6px 12px; border-radius: 20px; font-size: 14px; font-weight: 600; }}
        .responsibilities {{ background: #e7f3ff; padding: 15px; border-radius: 6px; margin: 15px 0; }}
        .responsibilities ul {{ margin: 10px 0; padding-left: 20px; }}
        .footer {{ background: #f8f9fa; padding: 20px; text-align: center; font-size: 12px; color: #666; border-top: 1px solid #dee2e6; }}
        .security-note {{ background: #d1ecf1; border-left: 4px solid #0c5460; padding: 15px; margin: 20px 0; border-radius: 4px; color: #0c5460; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎉 Welcome to BudgetMaster</h1>
            <p style='margin: 10px 0 0 0; font-size: 16px;'>Your Budget Planning & Forecasting Platform</p>
        </div>
        
        <div class='content'>
            <h2>Hello {firstName} {lastName}!</h2>
            
            <div class='welcome-box'>
                <p style='margin: 0;'><strong>Welcome to {organizationName}!</strong></p>
                <p style='margin: 10px 0 0 0;'>Your administrator has created an account for you on BudgetMaster. You've been assigned the role of <span class='role-badge'>{role}</span>.</p>
            </div>

            <p>{roleDescription}</p>

            <div class='responsibilities'>
                <strong>📋 Your Responsibilities:</strong>
                {roleResponsibilities}
            </div>

            <div class='credentials-box'>
                <h3>🔐 Your Login Credentials</h3>
                <p style='margin: 0 0 15px 0; color: #856404;'>Please keep this information secure and do not share it with anyone.</p>
                
                <div class='credential-item'>
                    <div class='credential-label'>Email Address</div>
                    <div class='credential-value'>{email}</div>
                </div>
                
                <div class='credential-item'>
                    <div class='credential-label'>Temporary Password</div>
                    <div class='credential-value'>{temporaryPassword}</div>
                </div>
            </div>

            <div class='warning'>
                <strong>⚠️ Important Security Notice:</strong>
                <p style='margin: 10px 0 0 0;'>This is a <strong>temporary password</strong>. You will be required to change it immediately after your first login. Please choose a strong, unique password that you haven't used elsewhere.</p>
            </div>

            <div style='text-align: center;'>
                <a href='{loginUrl}' class='btn'>🚀 Login to BudgetMaster</a>
            </div>

            <div class='security-note'>
                <strong>🔒 Security Best Practices:</strong>
                <ul style='margin: 10px 0; padding-left: 20px;'>
                    <li>Change your password immediately after first login</li>
                    <li>Use a strong password with at least 12 characters</li>
                    <li>Include uppercase, lowercase, numbers, and special characters</li>
                    <li>Never share your password with anyone</li>
                    <li>Enable two-factor authentication if available</li>
                </ul>
            </div>

            <p style='margin-top: 30px;'>If you have any questions or need assistance, please contact your system administrator.</p>
            
            <p style='margin-top: 20px;'>Best regards,<br><strong>The BudgetMaster Team</strong></p>
        </div>
        
        <div class='footer'>
            <p style='margin: 0;'>© {DateTime.UtcNow.Year} BudgetMaster. All rights reserved.</p>
            <p style='margin: 10px 0 0 0;'>This is an automated message. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GeneratePasswordResetEmailBody(string firstName, string resetLink)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; background-color: #f4f4f4; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 20px auto; background: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px 20px; text-align: center; }}
        .content {{ padding: 30px 20px; }}
        .btn {{ display: inline-block; padding: 14px 30px; background: #667eea; color: white; text-decoration: none; border-radius: 6px; font-weight: 600; margin: 20px 0; }}
        .warning {{ background: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; border-radius: 4px; }}
        .footer {{ background: #f8f9fa; padding: 20px; text-align: center; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔐 Password Reset Request</h1>
        </div>
        <div class='content'>
            <h2>Hello {firstName}!</h2>
            <p>We received a request to reset your password. Click the button below to create a new password:</p>
            <div style='text-align: center;'>
                <a href='{resetLink}' class='btn'>Reset Password</a>
            </div>
            <div class='warning'>
                <strong>⚠️ Security Notice:</strong>
                <p style='margin: 10px 0 0 0;'>If you didn't request this password reset, please ignore this email. Your password will remain unchanged.</p>
            </div>
            <p>This link will expire in 24 hours for security reasons.</p>
        </div>
        <div class='footer'>
            <p>© {DateTime.UtcNow.Year} BudgetMaster. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GetRoleDescription(string role)
        {
            return role switch
            {
                "FinanceManager" => "As a Finance Manager, you have comprehensive financial control authority over your organization's budget planning, forecasting, and financial analysis.",
                "Accountant" => "As an Accountant, you are responsible for recording and tracking all financial transactions, maintaining accurate financial records, and supporting budget compliance.",
                "DepartmentHead" => "As a Department Head, you have operational approval authority for your department's budget requests and oversight of departmental spending.",
                "Auditor" => "As an Auditor, you have read-only access to review all financial data, budgets, and transactions for compliance and audit purposes.",
                "Employee" => "As an Employee, you can submit budget requests for your department and track the status of your submissions.",
                _ => "Welcome to BudgetMaster! You now have access to the budget planning and forecasting platform."
            };
        }

        private string GetRoleResponsibilities(string role)
        {
            return role switch
            {
                "FinanceManager" => @"
                    <ul>
                        <li>Review and validate all budget requests</li>
                        <li>Create and manage organizational budgets</li>
                        <li>Generate financial forecasts and scenarios</li>
                        <li>Analyze variance reports and performance metrics</li>
                        <li>Oversee cross-departmental financial planning</li>
                    </ul>",
                "Accountant" => @"
                    <ul>
                        <li>Record actual transactions and expenses</li>
                        <li>Track budget utilization across departments</li>
                        <li>Maintain accurate financial records</li>
                        <li>Support variance analysis and reporting</li>
                        <li>Ensure compliance with budget allocations</li>
                    </ul>",
                "DepartmentHead" => @"
                    <ul>
                        <li>Provide operational approval for department budget requests</li>
                        <li>Monitor departmental spending and budget utilization</li>
                        <li>Submit budget requests for your department</li>
                        <li>Review team member budget submissions</li>
                        <li>Ensure departmental financial compliance</li>
                    </ul>",
                "Auditor" => @"
                    <ul>
                        <li>Review all financial transactions and budgets</li>
                        <li>Audit budget compliance and spending patterns</li>
                        <li>Generate audit reports and findings</li>
                        <li>Monitor financial controls and processes</li>
                        <li>Ensure regulatory compliance</li>
                    </ul>",
                "Employee" => @"
                    <ul>
                        <li>Submit budget requests for approval</li>
                        <li>Track the status of your budget requests</li>
                        <li>View approved budgets for your department</li>
                        <li>Access relevant financial information</li>
                    </ul>",
                _ => "<ul><li>Access the BudgetMaster platform</li><li>View relevant financial information</li></ul>"
            };
        }
    }
}
