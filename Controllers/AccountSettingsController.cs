using BudgetMasterFinal.Data;
using BudgetMasterFinal.Models;
using BudgetMasterFinal.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetMasterFinal.Controllers
{
    [Authorize]
    public class AccountSettingsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountSettingsController> _logger;

        public AccountSettingsController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            ILogger<AccountSettingsController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _logger = logger;
        }

        // GET: /AccountSettings/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var viewModel = new AccountSettingsViewModel
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role,
                LastLoginAt = user.LastLoginAt,
                CreatedAt = user.CreatedAt,
                IsActive = user.IsActive
            };

            return View(viewModel);
        }

        // POST: /AccountSettings/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string firstName, string lastName)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            // Validate input
            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            {
                return Json(new { success = false, message = "First name and last name are required." });
            }

            // Update user profile
            user.FirstName = firstName.Trim();
            user.LastName = lastName.Trim();

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                // Log the change
                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = user.Id,
                    TenantId = user.TenantId,
                    Action = "ProfileUpdated",
                    EntityType = "User",
                    NewValues = $"Updated profile: {user.FullName}",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                });
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Profile updated successfully!" });
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Json(new { success = false, message = $"Failed to update profile: {errors}" });
        }

        // POST: /AccountSettings/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            // Validate input
            if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
            {
                return Json(new { success = false, message = "All password fields are required." });
            }

            if (newPassword != confirmPassword)
            {
                return Json(new { success = false, message = "New password and confirmation do not match." });
            }

            if (newPassword.Length < 6)
            {
                return Json(new { success = false, message = "Password must be at least 6 characters long." });
            }

            // Verify current password
            var passwordCheck = await _userManager.CheckPasswordAsync(user, currentPassword);
            if (!passwordCheck)
            {
                return Json(new { success = false, message = "Current password is incorrect." });
            }

            // Change password
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (result.Succeeded)
            {
                // Clear MustChangePassword flag if set
                if (user.MustChangePassword)
                {
                    user.MustChangePassword = false;
                    await _userManager.UpdateAsync(user);
                }

                // Log the password change
                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = user.Id,
                    TenantId = user.TenantId,
                    Action = "PasswordChanged",
                    EntityType = "User",
                    NewValues = "User changed their password via Account Settings",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                });
                await _context.SaveChangesAsync();

                // Refresh sign-in to update security stamp
                await _signInManager.RefreshSignInAsync(user);

                return Json(new { success = true, message = "Password changed successfully!" });
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Json(new { success = false, message = $"Failed to change password: {errors}" });
        }

        // GET: /AccountSettings/ActivityLog
        [HttpGet]
        public async Task<IActionResult> ActivityLog()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Get user's recent activity logs
            var logs = await _context.AuditLogs
                .Where(l => l.UserId == user.Id)
                .OrderByDescending(l => l.Timestamp)
                .Take(50)
                .ToListAsync();

            return View(logs);
        }

        // POST: /AccountSettings/TerminateAllSessions
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TerminateAllSessions()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            // Update security stamp to invalidate all existing sessions
            var result = await _userManager.UpdateSecurityStampAsync(user);
            if (result.Succeeded)
            {
                // Log the action
                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = user.Id,
                    TenantId = user.TenantId,
                    Action = "SessionsTerminated",
                    EntityType = "User",
                    NewValues = "User terminated all active sessions",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                });
                await _context.SaveChangesAsync();

                // Sign out and redirect to login
                await _signInManager.SignOutAsync();
                return Json(new { success = true, message = "All sessions terminated. Please log in again.", redirect = "/Account/Login" });
            }

            return Json(new { success = false, message = "Failed to terminate sessions." });
        }

        // GET: /AccountSettings/Get2FAStatus
        [HttpGet]
        public async Task<IActionResult> Get2FAStatus()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            return Json(new { 
                success = true, 
                enabled = user.TwoFactorEnabled,
                hasAuthenticatorKey = !string.IsNullOrEmpty(user.AuthenticatorKey)
            });
        }

        // POST: /AccountSettings/Enable2FA
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enable2FA()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            // Generate a new authenticator key
            var key = GenerateAuthenticatorKey();
            user.AuthenticatorKey = key;
            
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                // Generate QR code URI
                var email = user.Email ?? user.UserName ?? "user";
                var issuer = "BudgetMaster";
                var otpauthUrl = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(email)}?secret={key}&issuer={Uri.EscapeDataString(issuer)}";
                
                // Format key for manual entry (groups of 4)
                var formattedKey = FormatKeyForDisplay(key);
                
                return Json(new { 
                    success = true, 
                    key = key,
                    formattedKey = formattedKey,
                    otpauthUrl = otpauthUrl
                });
            }

            return Json(new { success = false, message = "Failed to generate authenticator key." });
        }

        // POST: /AccountSettings/Verify2FA
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Verify2FA(string code)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            if (string.IsNullOrEmpty(user.AuthenticatorKey))
            {
                return Json(new { success = false, message = "Authenticator key not found. Please enable 2FA first." });
            }

            // Verify the code
            if (VerifyTOTP(user.AuthenticatorKey, code))
            {
                // Enable 2FA for the user
                user.TwoFactorEnabled = true;
                var result = await _userManager.UpdateAsync(user);
                
                if (result.Succeeded)
                {
                    // Log the action
                    _context.AuditLogs.Add(new AuditLog
                    {
                        UserId = user.Id,
                        TenantId = user.TenantId,
                        Action = "2FAEnabled",
                        EntityType = "User",
                        NewValues = "User enabled two-factor authentication",
                        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                    });
                    await _context.SaveChangesAsync();

                    return Json(new { success = true, message = "Two-factor authentication enabled successfully!" });
                }
            }

            return Json(new { success = false, message = "Invalid verification code. Please try again." });
        }

        // POST: /AccountSettings/Disable2FA
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Disable2FA()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            user.TwoFactorEnabled = false;
            user.AuthenticatorKey = null;
            
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                // Log the action
                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = user.Id,
                    TenantId = user.TenantId,
                    Action = "2FADisabled",
                    EntityType = "User",
                    NewValues = "User disabled two-factor authentication",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                });
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Two-factor authentication disabled successfully." });
            }

            return Json(new { success = false, message = "Failed to disable two-factor authentication." });
        }

        // Helper Methods
        private string GenerateAuthenticatorKey()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567"; // Base32 characters
            var random = new Random();
            var key = new char[32];
            for (int i = 0; i < key.Length; i++)
            {
                key[i] = chars[random.Next(chars.Length)];
            }
            return new string(key);
        }

        private string FormatKeyForDisplay(string key)
        {
            // Format as groups of 4: XXXX XXXX XXXX XXXX XXXX XXXX XXXX XXXX
            var formatted = "";
            for (int i = 0; i < key.Length; i += 4)
            {
                if (i > 0) formatted += " ";
                formatted += key.Substring(i, Math.Min(4, key.Length - i));
            }
            return formatted;
        }

        private bool VerifyTOTP(string secret, string code)
        {
            if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(code) || code.Length != 6)
                return false;

            try
            {
                var unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var timeStep = unixTimestamp / 30;

                // Check current time step and ±1 time step (90 second window)
                for (int i = -1; i <= 1; i++)
                {
                    var testCode = GenerateTOTP(secret, timeStep + i);
                    if (testCode == code)
                        return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private string GenerateTOTP(string secret, long timeStep)
        {
            var key = Base32Decode(secret);
            var timeBytes = BitConverter.GetBytes(timeStep);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(timeBytes);

            using var hmac = new System.Security.Cryptography.HMACSHA1(key);
            var hash = hmac.ComputeHash(timeBytes);
            
            var offset = hash[hash.Length - 1] & 0x0F;
            var binary = ((hash[offset] & 0x7F) << 24)
                       | ((hash[offset + 1] & 0xFF) << 16)
                       | ((hash[offset + 2] & 0xFF) << 8)
                       | (hash[offset + 3] & 0xFF);

            var otp = binary % 1000000;
            return otp.ToString("D6");
        }

        private byte[] Base32Decode(string base32)
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            base32 = base32.TrimEnd('=').ToUpper();
            
            var bits = "";
            foreach (var c in base32)
            {
                var value = alphabet.IndexOf(c);
                if (value < 0) throw new ArgumentException("Invalid Base32 character");
                bits += Convert.ToString(value, 2).PadLeft(5, '0');
            }

            var bytes = new List<byte>();
            for (int i = 0; i + 8 <= bits.Length; i += 8)
            {
                bytes.Add(Convert.ToByte(bits.Substring(i, 8), 2));
            }

            return bytes.ToArray();
        }
    }
}
