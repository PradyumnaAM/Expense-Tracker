using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExpenseTracker.Data;
using ExpenseTracker.Models;

namespace ExpenseTracker.Controllers;

[Authorize]
public class ExpensesController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public ExpensesController(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(ExpenseFilterViewModel filter)
    {
        var userId = _userManager.GetUserId(User)!;
        var query = _db.Expenses.Where(e => e.UserId == userId).AsQueryable();

        if (filter.Category.HasValue)
            query = query.Where(e => e.Category == filter.Category.Value);
        if (filter.Type.HasValue)
            query = query.Where(e => e.Type == filter.Type.Value);
        if (filter.From.HasValue)
            query = query.Where(e => e.Date >= filter.From.Value.ToUniversalTime());
        if (filter.To.HasValue)
            query = query.Where(e => e.Date <= filter.To.Value.ToUniversalTime().AddDays(1));
        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(e => e.Description.Contains(filter.Search));

        filter.TotalCount = await query.CountAsync();
        filter.Expenses = await query
            .OrderByDescending(e => e.Date)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return View(filter);
    }

    public IActionResult Create() => View(new ExpenseCreateViewModel { Date = DateTime.Today });

    [HttpPost]
    public async Task<IActionResult> Create(ExpenseCreateViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var userId = _userManager.GetUserId(User)!;
        var expense = new Expense
        {
            UserId = userId,
            Description = vm.Description,
            Amount = vm.Amount,
            Category = vm.Category,
            Type = vm.Type,
            Date = DateTime.SpecifyKind(vm.Date, DateTimeKind.Utc),
            Notes = vm.Notes,
            IsRecurring = vm.IsRecurring
        };

        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Transaction added successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var expense = await _db.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (expense == null) return NotFound();

        return View(new ExpenseCreateViewModel
        {
            Description = expense.Description,
            Amount = expense.Amount,
            Category = expense.Category,
            Type = expense.Type,
            Date = expense.Date.ToLocalTime().Date,
            Notes = expense.Notes,
            IsRecurring = expense.IsRecurring
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, ExpenseCreateViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var userId = _userManager.GetUserId(User)!;
        var expense = await _db.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (expense == null) return NotFound();

        expense.Description = vm.Description;
        expense.Amount = vm.Amount;
        expense.Category = vm.Category;
        expense.Type = vm.Type;
        expense.Date = DateTime.SpecifyKind(vm.Date, DateTimeKind.Utc);
        expense.Notes = vm.Notes;
        expense.IsRecurring = vm.IsRecurring;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Transaction updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var expense = await _db.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (expense == null) return NotFound();

        _db.Expenses.Remove(expense);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Transaction deleted.";
        return RedirectToAction(nameof(Index));
    }
}
