using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ExpenseTracker.Data;
using ExpenseTracker.Models;

namespace ExpenseTracker.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReportsController(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(int? month, int? year)
    {
        var now = DateTime.UtcNow;
        var m = month ?? now.Month;
        var y = year ?? now.Year;
        var vm = await BuildReport(m, y);
        return View(vm);
    }

    public async Task<IActionResult> Download(int month, int year)
    {
        var vm = await BuildReport(month, year);
        var user = await _userManager.GetUserAsync(User);
        var pdf = GeneratePdf(vm, user?.DisplayName ?? "User");
        return File(pdf, "application/pdf", $"ExpenseReport_{vm.MonthName.Replace(" ", "_")}.pdf");
    }

    private async Task<ReportViewModel> BuildReport(int month, int year)
    {
        var userId = _userManager.GetUserId(User)!;
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);
        var prevStart = start.AddMonths(-1);

        // Load into memory — SQLite can't aggregate decimal in SQL
        var expenses = await _db.Expenses
            .Where(e => e.UserId == userId && e.Date >= start && e.Date < end)
            .OrderByDescending(e => e.Date)
            .ToListAsync();

        var prevExpenses = await _db.Expenses
            .Where(e => e.UserId == userId && e.Date >= prevStart && e.Date < start
                && e.Type == ExpenseType.Expense)
            .ToListAsync();

        var totalSpent = expenses
            .Where(e => e.Type == ExpenseType.Expense)
            .Sum(e => e.Amount);

        var byCategory = expenses
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

        return new ReportViewModel
        {
            Month = month,
            Year = year,
            TotalSpent = totalSpent,
            TotalIncome = expenses.Where(e => e.Type == ExpenseType.Income).Sum(e => e.Amount),
            ByCategory = byCategory,
            AllExpenses = expenses,
            PreviousMonthSpent = prevExpenses.Sum(e => e.Amount)
        };
    }

    private byte[] GeneratePdf(ReportViewModel vm, string userName)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11).FontColor("#1a1a2e"));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Expense Report — {vm.MonthName}")
                            .FontSize(22).Bold().FontColor("#7c6aff");
                        row.ConstantItem(120).AlignRight()
                            .Text($"Generated {DateTime.Now:MMM d, yyyy}").FontSize(9).FontColor("#888");
                    });
                    col.Item().PaddingTop(4).Text($"Prepared for {userName}").FontSize(11).FontColor("#555");
                    col.Item().PaddingTop(8).LineHorizontal(1).LineColor("#e0e0f0");
                });

                page.Content().PaddingTop(20).Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Border(1).BorderColor("#e0e0f0").Padding(12).Column(c =>
                        {
                            c.Item().Text("Total Spent").FontSize(9).FontColor("#888");
                            c.Item().Text($"${vm.TotalSpent:N2}").FontSize(20).Bold().FontColor("#f97066");
                        });
                        row.ConstantItem(12);
                        row.RelativeItem().Border(1).BorderColor("#e0e0f0").Padding(12).Column(c =>
                        {
                            c.Item().Text("Total Income").FontSize(9).FontColor("#888");
                            c.Item().Text($"${vm.TotalIncome:N2}").FontSize(20).Bold().FontColor("#4fd1a8");
                        });
                        row.ConstantItem(12);
                        row.RelativeItem().Border(1).BorderColor("#e0e0f0").Padding(12).Column(c =>
                        {
                            c.Item().Text("Net Savings").FontSize(9).FontColor("#888");
                            var color = vm.NetSavings >= 0 ? "#4fd1a8" : "#f97066";
                            c.Item().Text($"${vm.NetSavings:N2}").FontSize(20).Bold().FontColor(color);
                        });
                        row.ConstantItem(12);
                        row.RelativeItem().Border(1).BorderColor("#e0e0f0").Padding(12).Column(c =>
                        {
                            c.Item().Text("vs Last Month").FontSize(9).FontColor("#888");
                            var prefix = vm.ChangePercent >= 0 ? "+" : "";
                            var color = vm.ChangePercent <= 0 ? "#4fd1a8" : "#f97066";
                            c.Item().Text($"{prefix}{vm.ChangePercent:N1}%").FontSize(20).Bold().FontColor(color);
                        });
                    });

                    col.Item().PaddingTop(24).Text("Spending by Category").FontSize(14).Bold();
                    col.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(3);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(1);
                        });
                        table.Header(h =>
                        {
                            h.Cell().Background("#f5f5fb").Padding(8).Text("Category").Bold().FontSize(10);
                            h.Cell().Background("#f5f5fb").Padding(8).AlignRight().Text("Amount").Bold().FontSize(10);
                            h.Cell().Background("#f5f5fb").Padding(8).AlignRight().Text("% of Total").Bold().FontSize(10);
                        });
                        foreach (var cat in vm.ByCategory)
                        {
                            table.Cell().BorderBottom(1).BorderColor("#f0f0f0").Padding(8).Text(cat.Category.ToString()).FontSize(10);
                            table.Cell().BorderBottom(1).BorderColor("#f0f0f0").Padding(8).AlignRight().Text($"${cat.Total:N2}").FontSize(10);
                            table.Cell().BorderBottom(1).BorderColor("#f0f0f0").Padding(8).AlignRight().Text($"{cat.Percentage}%").FontSize(10);
                        }
                    });

                    col.Item().PaddingTop(24).Text("All Transactions").FontSize(14).Bold();
                    col.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(70);
                            cols.RelativeColumn(3);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                        });
                        table.Header(h =>
                        {
                            foreach (var hdr in new[] { "Date", "Description", "Category", "Type", "Amount" })
                                h.Cell().Background("#f5f5fb").Padding(6).Text(hdr).Bold().FontSize(9);
                        });
                        foreach (var e in vm.AllExpenses)
                        {
                            var amtColor = e.Type == ExpenseType.Income ? "#4fd1a8" : "#1a1a2e";
                            var prefix = e.Type == ExpenseType.Income ? "+" : "-";
                            table.Cell().BorderBottom(1).BorderColor("#f5f5f5").Padding(6).Text(e.Date.ToString("MMM d")).FontSize(9);
                            table.Cell().BorderBottom(1).BorderColor("#f5f5f5").Padding(6).Text(e.Description).FontSize(9);
                            table.Cell().BorderBottom(1).BorderColor("#f5f5f5").Padding(6).Text(e.Category.ToString()).FontSize(9);
                            table.Cell().BorderBottom(1).BorderColor("#f5f5f5").Padding(6).Text(e.Type.ToString()).FontSize(9);
                            table.Cell().BorderBottom(1).BorderColor("#f5f5f5").Padding(6).AlignRight()
                                .Text($"{prefix}${e.Amount:N2}").FontSize(9).FontColor(amtColor);
                        }
                    });
                });

                page.Footer().AlignCenter()
                    .Text(t =>
                    {
                        t.Span("SpendWise Expense Tracker — Page ").FontSize(9).FontColor("#aaa");
                        t.CurrentPageNumber().FontSize(9).FontColor("#aaa");
                    });
            });
        });

        return document.GeneratePdf();
    }
}