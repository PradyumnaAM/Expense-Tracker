using Microsoft.AspNetCore.Identity;

namespace ExpenseTracker.Models;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
    public decimal MonthlyBudget { get; set; } = 5000m;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
