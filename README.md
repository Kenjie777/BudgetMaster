# BudgetMaster

BudgetMaster is a **web-based budget planning and forecasting system** developed using **ASP.NET Core MVC (.NET 8)**. It helps organizations manage budgets, forecast financial performance, monitor expenditures, and streamline budget approval workflows while maintaining strong security through role-based access control and two-factor authentication.

---

# Features

- User Authentication with ASP.NET Core Identity
- Two-Factor Authentication (Google Authenticator)
- Google reCAPTCHA Integration
- Role-Based Access Control (RBAC)
- Multi-Tenant Management
- Budget Allocation
- Budget Request Management
- Financial Forecasting
- Scenario Planning
- Variance Analysis
- Budget Approval Workflow
- Reports and Data Export
- Notifications
- Audit Logging

---

# Tech Stack

## Frontend

- ASP.NET Core MVC (Razor Views)
- HTML5
- CSS3
- Bootstrap 5
- JavaScript

## Backend

- ASP.NET Core MVC (.NET 8)
- C#
- Entity Framework Core
- ASP.NET Core Identity

## Database

- Microsoft SQL Server

## Security

- ASP.NET Core Identity
- Role-Based Authorization
- Google Authenticator (2FA)
- Google reCAPTCHA
- Password Hashing
- HTTPS/TLS
- Session Management
- Audit Logging
- Input Validation

## Reporting

- QuestPDF
- ClosedXML
- CSV Export

---

# Prerequisites

Before running the project, install:

- .NET 8 SDK
- Visual Studio 2022
- Microsoft SQL Server or LocalDB
- SQL Server Management Studio (SSMS)
- Git

---

# Installation

## Clone the Repository

```bash
git clone https://github.com/your-username/BudgetMaster.git
cd BudgetMaster
```

---

## Configure the Application

Update the `appsettings.json` file:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "YOUR_CONNECTION_STRING"
  }
}
```

Configure the following settings:

- Google reCAPTCHA Keys
- SMTP Email Settings
- Google Authenticator settings (if enabled)

---

## Apply Database Migration

Run:

```bash
dotnet ef database update
```

---

## Run the Application

Using .NET CLI:

```bash
dotnet run
```

or open the project using **Visual Studio 2022** and press:

```
F5
```

Open the application:

```
https://localhost:5001
```

---

# Project Structure

```
BudgetMaster
│
├── Controllers/
├── Models/
├── Views/
├── Data/
├── Services/
├── wwwroot/
├── Migrations/
├── appsettings.json
└── Program.cs
```

---

# User Roles

| Role | Description |
|------|-------------|
| Super Administrator | Manages the entire system and tenants |
| Company Administrator | Manages company settings and users |
| Finance Manager | Handles budgets, forecasts, and financial reports |
| Department Head | Creates and approves department budget requests |
| Employee | Submits budget requests and views assigned information |

---

# Security Features

BudgetMaster implements multiple security mechanisms:

- ASP.NET Core Identity Authentication
- Role-Based Authorization
- Password Hashing
- Two-Factor Authentication (2FA)
- Google reCAPTCHA Protection
- HTTPS Enforcement
- Entity Framework Core Parameterized Queries
- Input Validation
- CSRF Protection
- Audit Logging
- Session Management

---

# Default User Accounts

## Super Administrator

```
Username:
superadmin@budgetmaster.com

Password:
Admin@123
```

## Company Administrator

```
Username:
jlouiepion@gmail.com

Password:
#Samplepassword12345
```

## Department Head

```
Username:
coclouieescorpion@gmail.com

Password:
#Samplepassword12345
```

## Finance Manager

```
Username:
coclouieescorpion1@gmail.com

Password:
#Samplepassword12345
```

## Employee

```
Username:
kensatuken@gmail.com

Password:
#Samplepassword12345
```

---

# Future Improvements

- Mobile Application
- AI-Assisted Financial Forecasting
- Interactive Dashboards
- Multi-Currency Support
- Cloud Backup Integration
- Third-Party Accounting Integration

---

# Developers

Developed as an **IT15 Capstone Project** for the:

**Bachelor of Science in Information Technology**

---

# License

This project is intended for educational purposes.
