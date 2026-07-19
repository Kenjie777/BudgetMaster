using BudgetMasterFinal.Data;
using BudgetMasterFinal.Models;
using BudgetMasterFinal.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Add session services
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(1);
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdminPolicy", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim("Role", "SuperAdmin") ||
            context.User.HasClaim(System.Security.Claims.ClaimTypes.Role, "SuperAdmin") ||
            context.User.IsInRole("SuperAdmin")));
    
    // Organization configuration access (CompanyAdmin and Tenant ONLY - NO Finance Manager)
    options.AddPolicy("AdminPolicy", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim("Role", "CompanyAdmin") ||
            context.User.HasClaim(System.Security.Claims.ClaimTypes.Role, "CompanyAdmin") ||
            context.User.IsInRole("CompanyAdmin") ||
            context.User.HasClaim("Role", "Tenant") ||
            context.User.HasClaim(System.Security.Claims.ClaimTypes.Role, "Tenant") ||
            context.User.IsInRole("Tenant")));
    
    // Financial control and planning authority (Finance Manager ONLY)
    options.AddPolicy("FinancialPolicy", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim("Role", "FinanceManager") ||
            context.User.HasClaim(System.Security.Claims.ClaimTypes.Role, "FinanceManager") ||
            context.User.IsInRole("FinanceManager")));
    
    // Budget validation and approval (Finance Manager primary authority)
    options.AddPolicy("BudgetApprovalPolicy", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim("Role", "FinanceManager") ||
            context.User.HasClaim(System.Security.Claims.ClaimTypes.Role, "FinanceManager") ||
            context.User.IsInRole("FinanceManager")));
    
    // Financial reporting and analysis (Finance Manager + Accountant)
    options.AddPolicy("FinancialReportingPolicy", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim("Role", "FinanceManager") ||
            context.User.HasClaim(System.Security.Claims.ClaimTypes.Role, "FinanceManager") ||
            context.User.IsInRole("FinanceManager") ||
            context.User.HasClaim("Role", "Accountant") ||
            context.User.HasClaim(System.Security.Claims.ClaimTypes.Role, "Accountant") ||
            context.User.IsInRole("Accountant")));
    
    // Accounting operations (Finance Manager + Accountant)
    options.AddPolicy("AccountingPolicy", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim("Role", "FinanceManager") ||
            context.User.HasClaim(System.Security.Claims.ClaimTypes.Role, "FinanceManager") ||
            context.User.IsInRole("FinanceManager") ||
            context.User.HasClaim("Role", "Accountant") ||
            context.User.HasClaim(System.Security.Claims.ClaimTypes.Role, "Accountant") ||
            context.User.IsInRole("Accountant")));
    
    // Department operations (Department Head for operational approval, Finance Manager for financial validation)
    options.AddPolicy("DepartmentOperationsPolicy", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim("Role", "DepartmentHead") ||
            context.User.HasClaim(System.Security.Claims.ClaimTypes.Role, "DepartmentHead") ||
            context.User.IsInRole("DepartmentHead") ||
            context.User.HasClaim("Role", "FinanceManager") ||
            context.User.HasClaim(System.Security.Claims.ClaimTypes.Role, "FinanceManager") ||
            context.User.IsInRole("FinanceManager")));
    
    // Department Head operational authority (Department Head ONLY for their department)
    options.AddPolicy("DepartmentHeadPolicy", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim("Role", "DepartmentHead") ||
            context.User.HasClaim(System.Security.Claims.ClaimTypes.Role, "DepartmentHead") ||
            context.User.IsInRole("DepartmentHead")));
    
    // Departmental budget operations (Department Head + Finance Manager with different scopes)
    options.AddPolicy("DepartmentalBudgetPolicy", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim("Role", "DepartmentHead") ||
            context.User.HasClaim(System.Security.Claims.ClaimTypes.Role, "DepartmentHead") ||
            context.User.IsInRole("DepartmentHead") ||
            context.User.HasClaim("Role", "FinanceManager") ||
            context.User.HasClaim(System.Security.Claims.ClaimTypes.Role, "FinanceManager") ||
            context.User.IsInRole("FinanceManager")));
    
    // Budget request submission (Employee + Department Head)
    options.AddPolicy("BudgetRequestPolicy", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim("Role", "Employee") ||
            context.User.HasClaim(System.Security.Claims.ClaimTypes.Role, "Employee") ||
            context.User.IsInRole("Employee") ||
            context.User.HasClaim("Role", "DepartmentHead") ||
            context.User.HasClaim(System.Security.Claims.ClaimTypes.Role, "DepartmentHead") ||
            context.User.IsInRole("DepartmentHead") ||
            context.User.HasClaim("Role", "FinanceManager") ||
            context.User.HasClaim(System.Security.Claims.ClaimTypes.Role, "FinanceManager") ||
            context.User.IsInRole("FinanceManager")));
            
    // Employee operational authority
    options.AddPolicy("EmployeePolicy", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim("Role", "Employee") ||
            context.User.HasClaim(System.Security.Claims.ClaimTypes.Role, "Employee") ||
            context.User.IsInRole("Employee")));
    
    // Audit and compliance access (Auditor + Finance Manager for financial audits)
    options.AddPolicy("AuditPolicy", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim("Role", "Auditor") ||
            context.User.HasClaim(System.Security.Claims.ClaimTypes.Role, "Auditor") ||
            context.User.IsInRole("Auditor") ||
            context.User.HasClaim("Role", "FinanceManager") ||
            context.User.HasClaim(System.Security.Claims.ClaimTypes.Role, "FinanceManager") ||
            context.User.IsInRole("FinanceManager")));
});

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
    });

// Add claims transformation service
builder.Services.AddScoped<IClaimsTransformation, RoleClaimsTransformation>();

// Add notification service
builder.Services.AddScoped<NotificationService>();

// Add currency service
builder.Services.AddScoped<ICurrencyService, CurrencyService>();

// Add email service
builder.Services.AddScoped<IEmailService, EmailService>();

// Add archive service
builder.Services.AddScoped<IArchiveService, ArchiveService>();

// Add PDF report service
builder.Services.AddScoped<PdfReportService>();

var app = builder.Build();

// Seed the database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await SeedData.InitializeAsync(services);
}

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// Ensure UTF-8 encoding for all responses
app.Use(async (context, next) =>
{
    context.Response.Headers["Content-Type"] = "text/html; charset=utf-8";
    await next();
});

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

    app.Run();
