using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExpenseTracker.Models;

public class Expense
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [Range(0.01, 999999)]
    public decimal Amount { get; set; }

    [Required]
    public ExpenseCategory Category { get; set; }

    [Required]
    public ExpenseType Type { get; set; } = ExpenseType.Expense;

    public DateTime Date { get; set; } = DateTime.UtcNow;

    [MaxLength(500)]
    public string? Notes { get; set; }

    public bool IsRecurring { get; set; }

    public ApplicationUser? User { get; set; }
}

public enum ExpenseCategory
{
    Housing,
    Food,
    Transport,
    Healthcare,
    Entertainment,
    Shopping,
    Utilities,
    Education,
    Savings,
    Income,
    Other
}

public enum ExpenseType
{
    Expense,
    Income
}
