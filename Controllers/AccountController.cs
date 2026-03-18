using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ExpenseTracker.Models;

namespace ExpenseTracker.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountController(UserManager<ApplicationUser> um, SignInManager<ApplicationUser> sm)
    {
        _userManager = um;
        _signInManager = sm;
    }

    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel vm, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(vm);
        var result = await _signInManager.PasswordSignInAsync(vm.Email, vm.Password, vm.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
            return LocalRedirect(returnUrl ?? Url.Action("Index", "Home")!);
        if (result.IsLockedOut)
            ModelState.AddModelError("", "Account locked. Try again in 15 minutes.");
        else
            ModelState.AddModelError("", "Invalid email or password.");
        return View(vm);
    }

    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var user = new ApplicationUser
        {
            UserName = vm.Email,
            Email = vm.Email,
            DisplayName = vm.DisplayName,
            MonthlyBudget = 5000m
        };
        var result = await _userManager.CreateAsync(user, vm.Password);
        if (result.Succeeded)
        {
            await _signInManager.SignInAsync(user, false);
            return RedirectToAction("Index", "Home");
        }
        foreach (var e in result.Errors)
            ModelState.AddModelError("", e.Description);
        return View(vm);
    }

    [Authorize]
    public async Task<IActionResult> Settings()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction(nameof(Login));
        return View(new SettingsViewModel
        {
            DisplayName = user.DisplayName ?? "",
            MonthlyBudget = user.MonthlyBudget
        });
    }

    [Authorize, HttpPost]
    public async Task<IActionResult> Settings(SettingsViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction(nameof(Login));
        user.DisplayName = vm.DisplayName;
        user.MonthlyBudget = vm.MonthlyBudget;
        await _userManager.UpdateAsync(user);
        TempData["Success"] = "Settings saved.";
        return RedirectToAction(nameof(Settings));
    }

    [HttpPost, Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }
}
