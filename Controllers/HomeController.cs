using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExpenseTracker.Data;
using ExpenseTracker.Models;

namespace ExpenseTracker.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var startOfLastMonth = startOfMonth.AddMonths(-1);

        // Load into memory first — SQLite can't SumAsync on decimal
        var thisMonthExpenses = await _db.Expenses
            .Where(e => e.UserId == user.Id && e.Date >= startOfMonth)
            .ToListAsync();

        var lastMonthExpenses = await _db.Expenses
            .Where(e => e.UserId == user.Id && e.Date >= startOfLastMonth && e.Date < startOfMonth)
            .ToListAsync();

        var recent = await _db.Expenses
            .Where(e => e.UserId == user.Id)
            .OrderByDescending(e => e.Date)
            .Take(5)
            .ToListAsync();

        // Last 6 months — load per month in memory
        var last6Months = new List<MonthlyTotal>();
        for (int i = 5; i >= 0; i--)
        {
            var monthStart = startOfMonth.AddMonths(-i);
            var monthEnd = monthStart.AddMonths(1);
            var monthData = await _db.Expenses
                .Where(e => e.UserId == user.Id && e.Type == ExpenseType.Expense
                    && e.Date >= monthStart && e.Date < monthEnd)
                .ToListAsync();
            last6Months.Add(new MonthlyTotal
            {
                MonthLabel = monthStart.ToString("MMM"),
                Total = monthData.Sum(e => e.Amount),
                IsCurrent = i == 0
            });
        }

        // All aggregations in C# (not SQL) — avoids SQLite decimal issue
        var totalSpent = thisMonthExpenses
            .Where(e => e.Type == ExpenseType.Expense)
            .Sum(e => e.Amount);

        var totalIncome = thisMonthExpenses
            .Where(e => e.Type == ExpenseType.Income)
            .Sum(e => e.Amount);

        var categoryBreakdown = thisMonthExpenses
            .Where(e => e.Type == ExpenseType.Expense)
            .GroupBy(e => e.Category)
            .Select(g => new CategoryTotal
            {
                Category = g.Key,
                Total = g.Sum(e => e.Amount),
                Percentage = totalSpent > 0 ? Math.Round((g.Sum(e => e.Amount) / totalSpent) * 100, 1) : 0
            })
            .OrderByDescending(c => c.Total)
            .ToList();

        var lastMonthTotal = lastMonthExpenses
            .Where(e => e.Type == ExpenseType.Expense)
            .Sum(e => e.Amount);

        var changeVsLast = lastMonthTotal > 0
            ? Math.Round(((totalSpent - lastMonthTotal) / lastMonthTotal) * 100, 1)
            : 0;

        var vm = new DashboardViewModel
        {
            TotalSpentThisMonth = totalSpent,
            TotalIncomeThisMonth = totalIncome,
            MonthlyBudget = user.MonthlyBudget,
            TransactionCount = thisMonthExpenses.Count,
            RecentTransactions = recent,
            Last6Months = last6Months,
            CategoryBreakdown = categoryBreakdown,
            UserDisplayName = user.DisplayName ?? user.Email ?? "User",
            ChangeVsLastMonth = changeVsLast
        };

        return View(vm);
    }

    public IActionResult Error() => View();
}