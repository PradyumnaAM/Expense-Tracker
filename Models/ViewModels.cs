using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Models;

public class DashboardViewModel
{
    public decimal TotalSpentThisMonth { get; set; }
    public decimal TotalIncomeThisMonth { get; set; }
    public decimal MonthlyBudget { get; set; }
    public decimal BudgetRemaining => MonthlyBudget - TotalSpentThisMonth;
    public int TransactionCount { get; set; }
    public decimal PercentBudgetUsed => MonthlyBudget > 0 ? Math.Min(100, (TotalSpentThisMonth / MonthlyBudget) * 100) : 0;
    public List<Expense> RecentTransactions { get; set; } = new();
    public List<MonthlyTotal> Last6Months { get; set; } = new();
    public List<CategoryTotal> CategoryBreakdown { get; set; } = new();
    public string UserDisplayName { get; set; } = string.Empty;
    public decimal ChangeVsLastMonth { get; set; }
}

public class MonthlyTotal
{
    public string MonthLabel { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public bool IsCurrent { get; set; }
}

public class CategoryTotal
{
    public ExpenseCategory Category { get; set; }
    public decimal Total { get; set; }
    public decimal Percentage { get; set; }
}

public class ExpenseCreateViewModel
{
    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Range(0.01, 999999, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }

    [Required]
    public ExpenseCategory Category { get; set; }

    [Required]
    public ExpenseType Type { get; set; } = ExpenseType.Expense;

    [Required]
    public DateTime Date { get; set; } = DateTime.Today;

    [MaxLength(500)]
    public string? Notes { get; set; }

    public bool IsRecurring { get; set; }
}

public class ExpenseFilterViewModel
{
    public ExpenseCategory? Category { get; set; }
    public ExpenseType? Type { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public List<Expense> Expenses { get; set; } = new();
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class ReportViewModel
{
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal NetSavings => TotalIncome - TotalSpent;
    public List<CategoryTotal> ByCategory { get; set; } = new();
    public List<Expense> AllExpenses { get; set; } = new();
    public decimal PreviousMonthSpent { get; set; }
    public decimal ChangePercent => PreviousMonthSpent > 0 ? ((TotalSpent - PreviousMonthSpent) / PreviousMonthSpent) * 100 : 0;
    public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM yyyy");
}

public class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}

public class RegisterViewModel
{
    [Required, MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class SettingsViewModel
{
    [Required, MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [Range(1, 9999999)]
    public decimal MonthlyBudget { get; set; }
}
