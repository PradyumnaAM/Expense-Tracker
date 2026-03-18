# SpendWise — Expense Tracker

A production-ready ASP.NET 8 MVC expense tracker with authentication, PDF reports, and a dark fintech UI.

## Features
- ✅ User registration & login (ASP.NET Identity)
- ✅ Add/edit/delete transactions (income + expenses)
- ✅ Category breakdown with progress bars
- ✅ 6-month spending chart on dashboard
- ✅ Monthly PDF report generation (QuestPDF)
- ✅ Budget tracking with visual progress
- ✅ SQLite database (zero config)
- ✅ CSRF protection on all forms
- ✅ Account lockout after 5 failed logins
- ✅ Serilog structured logging
- ✅ Docker + Railway deployment ready

---

## Local Development

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)

### Run
```bash
cd ExpenseTracker
dotnet restore
dotnet run
```
Open http://localhost:5000 — register an account and start tracking.

The SQLite database (`expense_tracker.db`) is created automatically on first run.

---

## Deploy to Railway (Recommended — Free tier)

1. Push this repo to GitHub
2. Go to [railway.app](https://railway.app) → New Project → Deploy from GitHub repo
3. Select your repo — Railway auto-detects the Dockerfile
4. Done. Your app is live in ~2 minutes.

**Persistent storage for SQLite on Railway:**
- Add a Volume in Railway → mount path `/app/data`
- Update your connection string in Railway environment variables:
  ```
  ConnectionStrings__DefaultConnection=Data Source=/app/data/expense_tracker.db
  ```

---

## Deploy to Azure App Service

```bash
dotnet publish -c Release -o ./publish
# Then zip and deploy via Azure portal or:
az webapp up --name your-app-name --runtime "DOTNET:8" --os-type linux
```

---

## Deploy via Docker

```bash
docker build -t spendwise .
docker run -p 8080:8080 -v $(pwd)/data:/app/data spendwise
```

---

## Environment Variables

| Variable | Default | Description |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | `Data Source=expense_tracker.db` | SQLite file path |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Set to `Development` locally |

---

## Project Structure

```
ExpenseTracker/
├── Controllers/
│   ├── HomeController.cs       # Dashboard
│   ├── ExpensesController.cs   # CRUD transactions
│   ├── ReportsController.cs    # Reports + PDF download
│   └── AccountController.cs   # Auth + settings
├── Models/
│   ├── Expense.cs              # Entity
│   ├── ApplicationUser.cs      # Identity user
│   └── ViewModels.cs           # All view models
├── Data/
│   └── AppDbContext.cs         # EF Core + Identity
├── Views/                      # Razor views
├── wwwroot/
│   ├── css/site.css            # Full design system
│   └── js/site.js
├── Migrations/                 # EF migrations
├── Dockerfile
├── railway.toml
└── Program.cs
```
