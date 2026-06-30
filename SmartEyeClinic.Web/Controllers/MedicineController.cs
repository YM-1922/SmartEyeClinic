using Microsoft.AspNetCore.Mvc;
using SmartEyeClinic.Web.Services;

namespace SmartEyeClinic.Web.Controllers;

public class MedicineController : Controller
{
    private readonly MedicineService _medicineService;

    public MedicineController(MedicineService medicineService)
    {
        _medicineService = medicineService;
    }

    public IActionResult Index()
    {
        var medicines = _medicineService.GetAllMedicines();
        return View(medicines);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(string name, string? description, string? manufacturer)
    {
        _medicineService.AddMedicine(name, description, manufacturer);
        TempData["Success"] = "Medicine added successfully!";
        return RedirectToAction(nameof(Index));
    }
}
