using Microsoft.AspNetCore.Mvc;
using SmartEyeClinic.Web.Services;

namespace SmartEyeClinic.Web.Controllers;

public class PatientController : Controller
{
    private readonly PatientService _patientService;

    public PatientController(PatientService patientService)
    {
        _patientService = patientService;
    }

    // List
    public IActionResult Index()
    {
        var patients = _patientService.GetAllPatients();
        return View(patients);
    }

    // Add Patient
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(
        string fullName,
        string email,
        string password,
        string phoneNumber,
        string nationalId,
        string gender,
        string address)
    {
        try
        {
            _patientService.AddPatient(
                fullName,
                email,
                password,
                phoneNumber,
                nationalId,
                gender,
                address);

            TempData["Success"] = "Patient added successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return View();
        }
    }

    // Edit Patient
    [HttpGet]
    public IActionResult Edit(int id)
    {
        var patient = _patientService.GetPatientById(id);

        if (patient == null)
            return NotFound();

        return View(patient);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(
        int id,
        string fullName,
        string email,
        string password,
        string phoneNumber,
        string nationalId,
        string gender,
        string address)
    {
        try
        {
            _patientService.UpdatePatient(
                id,
                fullName,
                email,
                password,
                phoneNumber,
                nationalId,
                gender,
                address);

            TempData["Success"] = "Patient updated successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Edit), new { id });
        }
    }

    // Delete Patient - GET (Confirmation Page)
    [HttpGet]
    public IActionResult Delete(int id)
    {
        var patient = _patientService.GetPatientById(id);
        if (patient == null)
        {
            TempData["Error"] = "Patient not found.";
            return RedirectToAction(nameof(Index));
        }
        return View(patient);
    }

    // Delete Patient - POST
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        try
        {
            _patientService.DeletePatient(id);
            TempData["Success"] = "Patient deleted successfully!";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    // Patient 360 Details
    [HttpGet]
    public IActionResult Details(int id)
    {
        var patient = _patientService.GetPatientById(id);
        if (patient == null)
        {
            TempData["Error"] = "Patient not found.";
            return RedirectToAction(nameof(Index));
        }
        return View(patient);
    }
}