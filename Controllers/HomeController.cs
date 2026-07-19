using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BudgetMasterFinal.Data;
using BudgetMasterFinal.Models;

namespace BudgetMasterFinal.Controllers
{
    public class HomeController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public HomeController(
            SignInManager<ApplicationUser> signInManager, 
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // In development mode, if there are auth issues, clear and show landing page
            if (HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
            {
                // Check if user is authenticated but has issues
                if (User.Identity?.IsAuthenticated == true)
                {
                    try
                    {
                        var user = await _userManager.GetUserAsync(User);
                        // If user doesn't exist or has role issues, force logout
                        if (user == null || string.IsNullOrEmpty(user.Role))
                        {
                            await _signInManager.SignOutAsync();
                            // Clear all cookies
                            foreach (var cookie in Request.Cookies.Keys)
                            {
                                Response.Cookies.Delete(cookie);
                            }
                            return RedirectToAction("Index");
                        }
                        // If user is valid, redirect to dashboard
                        return RedirectToAction("Index", "Dashboard");
                    }
                    catch
                    {
                        // If any error occurs, force logout and show landing page
                        await _signInManager.SignOutAsync();
                        foreach (var cookie in Request.Cookies.Keys)
                        {
                            Response.Cookies.Delete(cookie);
                        }
                        return RedirectToAction("Index");
                    }
                }
            }
            else
            {
                // In production, normal behavior
                if (User.Identity?.IsAuthenticated == true)
                    return RedirectToAction("Index", "Dashboard");
            }
            
            // Fetch active subscription plans ordered by SortOrder
            var plans = await _context.SubscriptionPlans
                .Where(p => p.Status == "Active" && p.IsVisible)
                .OrderBy(p => p.SortOrder)
                .ToListAsync();
            
            return View(plans);
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View();

        // Development only - force clear all auth and go to landing page
        public async Task<IActionResult> Clear()
        {
            if (!HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
                return NotFound();
                
            await _signInManager.SignOutAsync();
            
            // Clear all cookies
            foreach (var cookie in Request.Cookies.Keys)
            {
                Response.Cookies.Delete(cookie);
            }
            
            return RedirectToAction("Index");
        }
    }
}
