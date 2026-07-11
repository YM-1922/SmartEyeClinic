using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartEyeClinic.Web.Services;
using System.Threading.Tasks;

namespace SmartEyeClinic.Web.Controllers;

[Authorize]
public class MedicineController : Controller
{
    private readonly MedicineService _medicineService;

    public MedicineController(MedicineService medicineService)
    {
        _medicineService = medicineService;
    }

    // GET: /Medicine/Index | استعراض قائمة الأدوية المسجلة في النظام
    public async Task<IActionResult> Index()
    {
        var medicines = await _medicineService.GetAllMedicinesAsync();
        return View(medicines);
    }

    // GET: /Medicine/Create | عرض صفحة إضافة دواء جديد للأطباء والمدراء
    [Authorize(Roles = "Admin,Doctor")]
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    // POST: /Medicine/Create | معالجة طلب إضافة دواء جديد وحفظه
    [Authorize(Roles = "Admin,Doctor")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, string? description, string? manufacturer)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Medicine name is required.";
            return View();
        }

        await _medicineService.AddMedicineAsync(name, description, manufacturer);
        TempData["Success"] = "Medicine added successfully!";
        return RedirectToAction(nameof(Index));
    }
}
