using BudgetMasterFinal.Data;
using BudgetMasterFinal.Models;
using BudgetMasterFinal.Models.ViewModels;
using BudgetMasterFinal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetMasterFinal.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Dashboard");
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return View(model);

            // Verify reCAPTCHA
            var recaptchaResponse = Request.Form["g-recaptcha-response"];
            if (string.IsNullOrEmpty(recaptchaResponse))
            {
                ModelState.AddModelError(string.Empty, "Please complete the reCAPTCHA verification.");
                return View(model);
            }

            var isRecaptchaValid = await VerifyRecaptcha(recaptchaResponse);
            if (!isRecaptchaValid)
            {
                ModelState.AddModelError(string.Empty, "reCAPTCHA verification failed. Please try again.");
                return View(model);
            }

            var user = await _userManager.Users
                .Include(u => u.Tenant)
                .FirstOrDefaultAsync(u => u.Email == model.Email);
                
            if (user == null || !user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "Invalid credentials or account inactive.");
                return View(model);
            }

            // Check if tenant is active (for non-SuperAdmin users)
            if (user.Role != "SuperAdmin" && user.Tenant != null && !user.Tenant.IsActive)
            {
                ModelState.AddModelError(string.Empty, "Your organization account has been deactivated. Please contact support.");
                return View(model);
            }

            // Check if user has 2FA enabled BEFORE attempting sign-in
            if (user.TwoFactorEnabled && !string.IsNullOrEmpty(user.AuthenticatorKey))
            {
                // Verify password first
                var passwordCheck = await _userManager.CheckPasswordAsync(user, model.Password);
                if (!passwordCheck)
                {
                    ModelState.AddModelError(string.Empty, "Invalid email or password.");
                    return View(model);
                }
                
                // Password is correct, redirect to OTP verification
                // Store user ID in session for OTP verification
                HttpContext.Session.SetString("2FA_UserId", user.Id);
                HttpContext.Session.SetString("2FA_RememberMe", model.RememberMe.ToString());
                
                return RedirectToAction("VerifyOTP", new { returnUrl });
            }

            // No 2FA, proceed with normal sign-in
            var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);
            if (result.Succeeded)
            {

                user.LastLoginAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                // Check if user must change password (first login with temporary password)
                if (user.MustChangePassword)
                {
                    // Store user ID in TempData for password change page
                    TempData["MustChangePasswordUserId"] = user.Id;
                    TempData["Info"] = "For security reasons, you must change your temporary password before accessing the system.";
                    return RedirectToAction("ChangePassword", "Account");
                }

                // Debug: Log user information
                _logger.LogInformation($"User {user.Email} logged in with Role: {user.Role}");
                
                // Ensure user has proper role claims
                var userRoles = await _userManager.GetRolesAsync(user);
                _logger.LogInformation($"User {user.Email} has roles: {string.Join(", ", userRoles)}");
                
                if (!userRoles.Contains(user.Role))
                {
                    _logger.LogInformation($"Adding user {user.Email} to role {user.Role}");
                    // Remove old roles and add current role
                    await _userManager.RemoveFromRolesAsync(user, userRoles);
                    await _userManager.AddToRoleAsync(user, user.Role);
                }

                // Refresh sign-in to update claims
                await _signInManager.RefreshSignInAsync(user);

                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = user.Id,
                    TenantId = user.TenantId,
                    Action = "Login",
                    EntityType = "User",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                });
                await _context.SaveChangesAsync();

                return user.Role switch
                {
                    "SuperAdmin"     => RedirectToAction("SuperAdmin",     "Dashboard"),
                    "Tenant"         => RedirectToAction("Admin",          "Dashboard"),
                    "Admin"          => RedirectToAction("Admin",          "Dashboard"),
                    "FinanceManager" => RedirectToAction("FinanceManager", "Dashboard"),
                    "Accountant"     => RedirectToAction("Accountant",     "Dashboard"),
                    "DepartmentHead" => RedirectToAction("DepartmentHead", "Dashboard"),
                    "Employee"       => RedirectToAction("Employee",       "Dashboard"),
                    "Auditor"        => RedirectToAction("Auditor",        "Dashboard"),
                    _                => RedirectToAction("Admin",          "Dashboard")
                };
            }

            if (result.IsLockedOut)
                ModelState.AddModelError(string.Empty, "Account locked. Try again later.");
            else
                ModelState.AddModelError(string.Empty, "Invalid email or password.");

            return View(model);
        }

        private async Task<bool> VerifyRecaptcha(string recaptchaResponse)
        {
            try
            {
                var secretKey = "6LdUJugsAAAAAC8NqiCSMMkc7MiJzm5odhtBvY36";
                using var httpClient = new HttpClient();
                var response = await httpClient.PostAsync(
                    $"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={recaptchaResponse}",
                    null);

                var jsonResponse = await response.Content.ReadAsStringAsync();
                dynamic result = System.Text.Json.JsonSerializer.Deserialize<dynamic>(jsonResponse);
                
                return result?.GetProperty("success").GetBoolean() ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying reCAPTCHA");
                return false;
            }
        }

        [HttpGet]
        public async Task<IActionResult> Register()
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Dashboard");
            
            // Load subscription plans for pricing display
            var plans = await _context.SubscriptionPlans
                .Where(p => p.Status == "Active" && p.IsVisible)
                .OrderBy(p => p.SortOrder)
                .ToListAsync();
            
            ViewBag.SubscriptionPlans = plans;
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Create tenant
            var tenant = new Tenant
            {
                CompanyName = model.CompanyName,
                Industry = model.Industry,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();

            // Get or create subscription plan
            var planName = model.PlanName ?? "Starter";
            var subscriptionPlan = await _context.SubscriptionPlans.FirstOrDefaultAsync(p => p.Name == planName);
            if (subscriptionPlan == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid subscription plan selected.");
                return View(model);
            }

            // Calculate price based on billing cycle
            decimal price = model.BillingCycle == "Yearly" ? subscriptionPlan.YearlyPrice : subscriptionPlan.MonthlyPrice;

            var subscription = new Subscription
            {
                TenantId = tenant.Id,
                SubscriptionPlanId = subscriptionPlan.Id,
                BillingCycle = model.BillingCycle,
                PriceAmount = price,
                Status = "Active",
                StartDate = DateTime.UtcNow,
                EndDate = model.BillingCycle == "Yearly" ? DateTime.UtcNow.AddYears(1) : DateTime.UtcNow.AddMonths(1),
                NextBillingDate = model.BillingCycle == "Yearly" ? DateTime.UtcNow.AddYears(1) : DateTime.UtcNow.AddMonths(1),
                CurrentUsers = 1,
                CurrentDepartments = 3,
                CreditBalance = 0
            };
            _context.Subscriptions.Add(subscription);

            // Create admin user (first user becomes the tenant admin)
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                TenantId = tenant.Id,
                Role = "Tenant",
                IsActive = true,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                // Cleanup tenant
                _context.Tenants.Remove(tenant);
                await _context.SaveChangesAsync();
                foreach (var err in result.Errors)
                    ModelState.AddModelError(string.Empty, err.Description);
                return View(model);
            }

            await _userManager.AddToRoleAsync(user, "Tenant");
            await _context.SaveChangesAsync();

            // Seed default departments
            var defaultDepts = new[] { "Finance", "Operations", "HR" };
            foreach (var deptName in defaultDepts)
            {
                _context.Departments.Add(new Department { TenantId = tenant.Id, Name = deptName });
            }
            await _context.SaveChangesAsync();

            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Admin", "Dashboard");
        }

        [HttpGet]
        public async Task<IActionResult> ClearAuth()
        {
            await _signInManager.SignOutAsync();
            // Clear all cookies
            foreach (var cookie in Request.Cookies.Keys)
            {
                Response.Cookies.Delete(cookie);
            }
            return RedirectToAction("Index", "Home");
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize]
        public async Task<IActionResult> Logout()
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = userId,
                    TenantId = user.TenantId,
                    Action = "Logout",
                    EntityType = "User"
                });
                await _context.SaveChangesAsync();
            }
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied(string? returnUrl = null) 
        {
            // If user is not authenticated, redirect to home page
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction("Index", "Home");
            }
            
            // If user is authenticated but doesn't have access, show access denied page
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpGet, Authorize]
        public async Task<IActionResult> ChangePassword()
        {
            // Check if this is a forced password change (first login)
            var mustChangeUserId = TempData["MustChangePasswordUserId"] as string;
            if (!string.IsNullOrEmpty(mustChangeUserId))
            {
                var user = await _userManager.FindByIdAsync(mustChangeUserId);
                if (user != null && user.MustChangePassword)
                {
                    ViewBag.IsFirstLogin = true;
                    ViewBag.UserEmail = user.Email;
                    return View();
                }
            }
            
            // Regular password change
            ViewBag.IsFirstLogin = false;
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            if (string.IsNullOrEmpty(newPassword) || newPassword != confirmPassword)
            {
                ModelState.AddModelError(string.Empty, "New password and confirmation password do not match.");
                ViewBag.IsFirstLogin = user.MustChangePassword;
                ViewBag.UserEmail = user.Email;
                return View();
            }

            // Verify current password
            var passwordCheck = await _userManager.CheckPasswordAsync(user, currentPassword);
            if (!passwordCheck)
            {
                ModelState.AddModelError(string.Empty, "Current password is incorrect.");
                ViewBag.IsFirstLogin = user.MustChangePassword;
                ViewBag.UserEmail = user.Email;
                return View();
            }

            // Change password
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            
            if (result.Succeeded)
            {
                // Clear the MustChangePassword flag
                user.MustChangePassword = false;
                await _userManager.UpdateAsync(user);
                
                // Log the password change
                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = user.Id,
                    TenantId = user.TenantId,
                    Action = "PasswordChanged",
                    EntityType = "User",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                });
                await _context.SaveChangesAsync();
                
                // Refresh sign-in
                await _signInManager.RefreshSignInAsync(user);
                
                TempData["Success"] = "Your password has been changed successfully!";
                
                // Redirect to appropriate dashboard
                return user.Role switch
                {
                    "SuperAdmin"     => RedirectToAction("SuperAdmin",     "Dashboard"),
                    "Tenant"         => RedirectToAction("Admin",          "Dashboard"),
                    "Admin"          => RedirectToAction("Admin",          "Dashboard"),
                    "FinanceManager" => RedirectToAction("FinanceManager", "Dashboard"),
                    "Accountant"     => RedirectToAction("Accountant",     "Dashboard"),
                    "DepartmentHead" => RedirectToAction("DepartmentHead", "Dashboard"),
                    "Employee"       => RedirectToAction("Employee",       "Dashboard"),
                    "Auditor"        => RedirectToAction("Auditor",        "Dashboard"),
                    _                => RedirectToAction("Index",          "Dashboard")
                };
            }
            
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            
            ViewBag.IsFirstLogin = user.MustChangePassword;
            ViewBag.UserEmail = user.Email;
            return View();
        }

        // Temporary debug endpoint
        [HttpGet]
        public async Task<IActionResult> Debug()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                ViewBag.Message = "No user logged in";
                return Content("No user logged in");
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var allRoles = _context.Roles.ToList();
            var allUsers = _context.Users.ToList();

            // Check subscription data
            var tenants = await _context.Tenants
                .Include(t => t.Subscription)
                .ThenInclude(s => s.SubscriptionPlan)
                .ToListAsync();

            var debugInfo = $@"
Current User: {user.Email} (Role: {user.Role})
User Identity Roles: {string.Join(", ", userRoles)}
User Claims: {string.Join(", ", User.Claims.Select(c => $"{c.Type}:{c.Value}"))}

All Roles in System: {string.Join(", ", allRoles.Select(r => r.Name))}

All Users:
{string.Join("\n", allUsers.Select(u => $"- {u.Email}: {u.Role}"))}

Tenants and Subscriptions:
{string.Join("\n", tenants.Select(t => $"- {t.CompanyName}: Subscription={t.Subscription?.Id}, Plan={t.Subscription?.SubscriptionPlan?.Name}, Status={t.Subscription?.Status}"))}
";

            return Content(debugInfo, "text/plain");
        }

        // Temporary setup endpoint to create admin user
        [HttpGet]
        public async Task<IActionResult> Setup()
        {
            // Check if any admin users exist
            var adminExists = await _context.Users.AnyAsync(u => u.Role == "CompanyAdmin");
            if (adminExists)
            {
                return Content("Admin user already exists. Go to /Account/Login to login.");
            }

            // Get or create a tenant
            var tenant = await _context.Tenants.FirstOrDefaultAsync();
            if (tenant == null)
            {
                tenant = new Tenant
                {
                    CompanyName = "Demo Company",
                    Industry = "Technology",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Tenants.Add(tenant);
                await _context.SaveChangesAsync();
            }

            // Create admin user
            var adminEmail = "admin@demo.com";
            var existingUser = await _userManager.FindByEmailAsync(adminEmail);
            if (existingUser != null)
            {
                await _userManager.DeleteAsync(existingUser);
            }

            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "Admin",
                LastName = "User",
                TenantId = tenant.Id,
                Role = "CompanyAdmin",
                IsActive = true,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(admin, "Admin@123");
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(admin, "CompanyAdmin");
                return Content($@"
Admin user created successfully!

Email: {adminEmail}
Password: Admin@123

Go to /Account/Login to login.
");
            }
            else
            {
                return Content($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }

        // Diagnostic endpoint to check current user's role and claims
        [HttpGet, Authorize]
        public async Task<IActionResult> CheckRole()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Content("User not found");
            }

            var claims = User.Claims.Select(c => $"{c.Type}: {c.Value}").ToList();
            var roleClaims = User.Claims.Where(c => c.Type == "Role" || c.Type == System.Security.Claims.ClaimTypes.Role).ToList();

            var output = $@"
=== USER INFORMATION ===
Email: {user.Email}
Name: {user.FullName}
Role (from database): {user.Role}
TenantId: {user.TenantId}
IsActive: {user.IsActive}

=== ROLE CLAIMS ===
{string.Join("\n", roleClaims.Select(c => $"{c.Type}: {c.Value}"))}

=== ALL CLAIMS ===
{string.Join("\n", claims)}

=== AUTHORIZATION CHECK ===
Has 'Role' claim with 'Auditor': {User.HasClaim("Role", "Auditor")}
Has ClaimTypes.Role with 'Auditor': {User.HasClaim(System.Security.Claims.ClaimTypes.Role, "Auditor")}
Is in 'Auditor' role (Identity): {User.IsInRole("Auditor")}

=== QUICK FIX ===
To change your role to Auditor, visit: /Account/SetRoleToAuditor
";

            return Content(output, "text/plain");
        }

        // Quick fix endpoint to set current user's role to Auditor (for testing)
        [HttpGet, Authorize]
        public async Task<IActionResult> SetRoleToAuditor()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Content("User not found");
            }

            // Update the user's role in the database
            user.Role = "Auditor";
            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                return Content($"Failed to update user role: {string.Join(", ", updateResult.Errors.Select(e => e.Description))}");
            }

            // Remove user from all existing roles
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            }

            // Add user to Auditor role
            var addRoleResult = await _userManager.AddToRoleAsync(user, "Auditor");

            if (!addRoleResult.Succeeded)
            {
                return Content($"Failed to add user to Auditor role: {string.Join(", ", addRoleResult.Errors.Select(e => e.Description))}");
            }

            // Sign out and sign back in to refresh claims
            await _signInManager.SignOutAsync();
            await _signInManager.SignInAsync(user, isPersistent: false);

            return Content(@"
✅ SUCCESS!

Your role has been changed to Auditor.
Your session has been refreshed with new claims.

You can now access Auditor pages:
- /Dashboard/Auditor
- /Auditor/Budgets
- /Auditor/BudgetRequests
- /Auditor/Transactions
- /Auditor/AuditLogs
- etc.

Click here to go to Auditor Dashboard: <a href='/Dashboard/Auditor'>Auditor Dashboard</a>
", "text/html");
        }

        // ============================================
        // Forgot Password & Reset Password
        // ============================================

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError(string.Empty, "Please enter your email address.");
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            
            // Don't reveal whether the user exists or not for security
            // Always show success message
            TempData["Success"] = "If an account with that email exists, a password reset link has been sent.";

            if (user != null && user.IsActive)
            {
                // Generate password reset token
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                
                // Create reset link
                var resetLink = Url.Action("ResetPassword", "Account", 
                    new { token = token, email = user.Email }, 
                    Request.Scheme);

                // Send email (implement this in your email service)
                try
                {
                    var emailService = HttpContext.RequestServices.GetRequiredService<IEmailService>();
                    await emailService.SendPasswordResetEmailAsync(user.Email, user.FirstName, resetLink);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send password reset email to {Email}", email);
                }
            }

            return RedirectToAction("ForgotPasswordConfirmation");
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login");
            }

            var model = new ResetPasswordViewModel
            {
                Token = token,
                Email = email
            };

            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user doesn't exist
                return RedirectToAction("ResetPasswordConfirmation");
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            
            if (result.Succeeded)
            {
                // Clear the MustChangePassword flag if it was set
                if (user.MustChangePassword)
                {
                    user.MustChangePassword = false;
                    await _userManager.UpdateAsync(user);
                }

                return RedirectToAction("ResetPasswordConfirmation");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        // ============================================
        // Two-Factor Authentication (OTP Verification)
        // ============================================

        [HttpGet]
        public IActionResult VerifyOTP(string? returnUrl = null)
        {
            // Check if user has a pending 2FA session
            var userId = HttpContext.Session.GetString("2FA_UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOTP(string code, string? returnUrl = null)
        {
            // Retrieve user ID from session
            var userId = HttpContext.Session.GetString("2FA_UserId");
            if (string.IsNullOrEmpty(userId))
            {
                ModelState.AddModelError(string.Empty, "Session expired. Please login again.");
                return View();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "User not found or inactive.");
                return View();
            }

            // Validate code format
            if (string.IsNullOrEmpty(code) || code.Length != 6 || !code.All(char.IsDigit))
            {
                ModelState.AddModelError(string.Empty, "Invalid verification code format.");
                return View();
            }

            // Verify TOTP code
            if (!VerifyTOTP(user.AuthenticatorKey, code))
            {
                ModelState.AddModelError(string.Empty, "Invalid verification code. Please try again.");
                return View();
            }

            // Code is valid - sign in the user
            var rememberMeStr = HttpContext.Session.GetString("2FA_RememberMe");
            var rememberMe = bool.TryParse(rememberMeStr, out var remember) && remember;

            await _signInManager.SignInAsync(user, isPersistent: rememberMe);

            // Update last login time
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            // Log successful 2FA login
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = user.Id,
                TenantId = user.TenantId,
                Action = "Login2FA",
                EntityType = "User",
                NewValues = "User logged in with 2FA verification",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });
            await _context.SaveChangesAsync();

            // Clear session data
            HttpContext.Session.Remove("2FA_UserId");
            HttpContext.Session.Remove("2FA_RememberMe");

            // Redirect to appropriate dashboard
            return user.Role switch
            {
                "SuperAdmin"     => RedirectToAction("SuperAdmin",     "Dashboard"),
                "Tenant"         => RedirectToAction("Admin",          "Dashboard"),
                "Admin"          => RedirectToAction("Admin",          "Dashboard"),
                "FinanceManager" => RedirectToAction("FinanceManager", "Dashboard"),
                "Accountant"     => RedirectToAction("Accountant",     "Dashboard"),
                "DepartmentHead" => RedirectToAction("DepartmentHead", "Dashboard"),
                "Employee"       => RedirectToAction("Employee",       "Dashboard"),
                "Auditor"        => RedirectToAction("Auditor",        "Dashboard"),
                _                => RedirectToAction("Admin",          "Dashboard")
            };
        }

        // Helper method for TOTP verification
        private bool VerifyTOTP(string? secret, string code)
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
