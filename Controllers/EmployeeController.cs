using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Models.Entities;
using OmniBizAI.Services.Auth;

namespace OmniBizAI.Controllers;

public class EmployeeController(
    ApplicationDbContext context, 
    ITenantContext tenantContext,
    UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        var tenantId = tenantContext.CurrentTenantId;
        if (tenantId == null) return Unauthorized();

        var employees = await context.EmployeeProfiles
            .Include(e => e.User)
            .Include(e => e.Department)
            .Where(e => e.Department.TenantId == tenantId)
            .ToListAsync();
            
        return View(employees);
    }
    
    // Simplistic Create method linking an existing User to a Profile
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PrepareDropdownsAsync();
        return View(new EmployeeProfile());
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeProfile model)
    {
        var tenantId = tenantContext.CurrentTenantId;
        if (tenantId == null) return Unauthorized();

        if (ModelState.IsValid)
        {
            // Verify User belongs to same tenant
            var user = await userManager.FindByIdAsync(model.UserId);
            if (user == null || user.TenantId != tenantId)
            {
                ModelState.AddModelError("UserId", "Invalid User.");
                await PrepareDropdownsAsync(model.UserId, model.DepartmentId);
                return View(model);
            }

            context.EmployeeProfiles.Add(model);
            await context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        await PrepareDropdownsAsync(model.UserId, model.DepartmentId);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var tenantId = tenantContext.CurrentTenantId;
        var emp = await context.EmployeeProfiles
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == id && e.Department.TenantId == tenantId);
            
        if (emp == null) return NotFound();
        
        await PrepareDropdownsAsync(emp.UserId, emp.DepartmentId);
        return View(emp);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EmployeeProfile model)
    {
        var tenantId = tenantContext.CurrentTenantId;
        if (id != model.Id) return BadRequest();

        if (ModelState.IsValid)
        {
            var existing = await context.EmployeeProfiles
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Id == id && e.Department.TenantId == tenantId);
                
            if (existing == null) return NotFound();

            existing.DepartmentId = model.DepartmentId;
            existing.PositionName = model.PositionName;
            existing.EmployeeCode = model.EmployeeCode;
            existing.JoinDate = model.JoinDate;

            await context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        await PrepareDropdownsAsync(model.UserId, model.DepartmentId);
        return View(model);
    }

    private async Task PrepareDropdownsAsync(string? selectedUserId = null, int? selectedDepartmentId = null)
    {
        var tenantId = tenantContext.CurrentTenantId;
        
        var users = await userManager.Users.Where(u => u.TenantId == tenantId).ToListAsync();
        ViewBag.UserId = new SelectList(users, "Id", "FullName", selectedUserId);

        var departments = await context.Departments.Where(d => d.TenantId == tenantId && d.IsActive).ToListAsync();
        ViewBag.DepartmentId = new SelectList(departments, "Id", "Name", selectedDepartmentId);
    }
}
