using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OmniBizAI.Controllers;

[Authorize]
public class FinanceController : Controller
{
    [Authorize(Roles = "Admin,Director,Manager,Accountant")]
    public IActionResult Budgets() => View();

    public IActionResult PaymentRequests() => View();

    [Authorize(Roles = "Admin,Director,Manager,Accountant")]
    public IActionResult Transactions() => View();

    [Authorize(Roles = "Admin,Director,Manager,Accountant")]
    public IActionResult Vendors() => View();

    [Authorize(Roles = "Admin,Director,Manager,Accountant")]
    public IActionResult Categories() => View();

    [Authorize(Roles = "Admin,Director,Manager,Accountant")]
    public IActionResult Wallets() => View();
}
