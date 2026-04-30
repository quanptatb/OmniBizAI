using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Models.Entities;
using OmniBizAI.Services.Auth;

namespace OmniBizAI.Controllers;

public class CompanyController(ApplicationDbContext context, ITenantContext tenantContext) : Controller
{
    public async Task<IActionResult> Index()
    {
        var tenantId = tenantContext.CurrentTenantId;
        if (tenantId == null) return Unauthorized();

        var companies = await context.Companies
            .Where(c => c.TenantId == tenantId)
            .ToListAsync();
            
        return View(companies);
    }
    
    [HttpGet]
    public IActionResult Create()
    {
        return View(new Company());
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Company model)
    {
        var tenantId = tenantContext.CurrentTenantId;
        if (tenantId == null) return Unauthorized();

        if (ModelState.IsValid)
        {
            model.TenantId = tenantId.Value;
            context.Companies.Add(model);
            await context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var tenantId = tenantContext.CurrentTenantId;
        var company = await context.Companies.FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId);
        if (company == null) return NotFound();
        return View(company);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Company model)
    {
        var tenantId = tenantContext.CurrentTenantId;
        if (id != model.Id) return BadRequest();

        if (ModelState.IsValid)
        {
            var existing = await context.Companies.FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId);
            if (existing == null) return NotFound();

            existing.Code = model.Code;
            existing.Name = model.Name;
            existing.Address = model.Address;
            existing.TaxCode = model.TaxCode;
            existing.IsActive = model.IsActive;

            await context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(model);
    }
}
