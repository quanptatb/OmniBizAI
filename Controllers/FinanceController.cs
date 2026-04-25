using Microsoft.AspNetCore.Mvc;

namespace OmniBizAI.Controllers;

public class FinanceController : Controller
{
    public IActionResult Budgets() => View();

    public IActionResult PaymentRequests() => View();

    public IActionResult Transactions() => View();

    public IActionResult Vendors() => View();
}
