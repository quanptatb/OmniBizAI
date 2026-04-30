using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Models.Entities;
using OmniBizAI.Services.Auth;

namespace OmniBizAI.Controllers;

public class DepartmentController(ApplicationDbContext context, ITenantContext tenantContext) : Controller
{
    public async Task<IActionResult> Index()
    {
        var tenantId = tenantContext.CurrentTenantId;
        if (tenantId == null) return Unauthorized();

        var departments = await context.Departments
            .Include(d => d.Company)
            .Include(d => d.ParentDepartment)
            .Where(d => d.TenantId == tenantId)
            .ToListAsync();
            
        return View(departments);
    }
    
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PrepareDropdownsAsync();
        return View(new Department());
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Department model)
    {
        var tenantId = tenantContext.CurrentTenantId;
        if (tenantId == null) return Unauthorized();

        if (ModelState.IsValid)
        {
            model.TenantId = tenantId.Value;
            context.Departments.Add(model);
            await context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        await PrepareDropdownsAsync(model.CompanyId, model.ParentDepartmentId);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var tenantId = tenantContext.CurrentTenantId;
        var dept = await context.Departments.FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId);
        if (dept == null) return NotFound();
        
        await PrepareDropdownsAsync(dept.CompanyId, dept.ParentDepartmentId);
        return View(dept);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Department model)
    {
        var tenantId = tenantContext.CurrentTenantId;
        if (id != model.Id) return BadRequest();

        if (ModelState.IsValid)
        {
            var existing = await context.Departments.FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId);
            if (existing == null) return NotFound();

            existing.Code = model.Code;
            existing.Name = model.Name;
            existing.CompanyId = model.CompanyId;
            existing.ParentDepartmentId = model.ParentDepartmentId;
            existing.IsActive = model.IsActive;

            await context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        await PrepareDropdownsAsync(model.CompanyId, model.ParentDepartmentId);
        return View(model);
    }

    private async Task PrepareDropdownsAsync(int? selectedCompanyId = null, int? selectedParentId = null)
    {
        var tenantId = tenantContext.CurrentTenantId;
        var companies = await context.Companies.Where(c => c.TenantId == tenantId && c.IsActive).ToListAsync();
        ViewBag.CompanyId = new SelectList(companies, "Id", "Name", selectedCompanyId);

        var departments = await context.Departments.Where(d => d.TenantId == tenantId && d.IsActive).ToListAsync();
        ViewBag.ParentDepartmentId = new SelectList(departments, "Id", "Name", selectedParentId);
    }
}
