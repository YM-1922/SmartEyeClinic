using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartEyeClinic.Web.Services;
using System;
using System.Threading.Tasks;

namespace SmartEyeClinic.Web.Controllers;

[Authorize(Roles = "Admin,Doctor,Receptionist")]
public class PatientController : Controller
{
    private readonly PatientService _patientService;

    public PatientController(PatientService patientService)
    {
        _patientService = patientService;
    }

    // GET: /Patient/Index | عرض قائمة المرضى المسجلين في النظام
    public async Task<IActionResult> Index()
    {
        var patients = await _patientService.GetAllPatientsAsync();
        return View(patients);
    }

    // GET: /Patient/Create | عرض صفحة إضافة مريض جديد
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    // POST: /Patient/Create | معالجة طلب إضافة مريض جديد في النظام وإنشاء حساب له
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
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
            await _patientService.AddPatientAsync(
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

    // GET: /Patient/Edit/{id} | عرض صفحة تعديل بيانات مريض معين
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var patient = await _patientService.GetPatientByIdAsync(id);

        if (patient == null)
            return NotFound();

        return View(patient);
    }

    // POST: /Patient/Edit/{id} | معالجة طلب تعديل بيانات مريض معين وحفظ التغييرات
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
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
            await _patientService.UpdatePatientAsync(
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

    // GET: /Patient/Delete/{id} | عرض صفحة تأكيد حذف المريض (صفحة التأكيد)
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var patient = await _patientService.GetPatientByIdAsync(id);
        if (patient == null)
        {
            TempData["Error"] = "Patient not found.";
            return RedirectToAction(nameof(Index));
        }
        return View(patient);
    }

    // POST: /Patient/Delete/{id} | معالجة الحذف النهائي للمريض من قاعدة البيانات
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            await _patientService.DeletePatientAsync(id);
            TempData["Success"] = "Patient deleted successfully!";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    // GET: /Patient/Details/{id} | عرض الملف الطبي الشامل للمريض (سجل 360 درجة) للتحليل السريري
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        if (User.IsInRole("Patient"))
        {
            var patIdClaim = User.FindFirst("PatientId")?.Value;
            if (patIdClaim == null || int.Parse(patIdClaim) != id)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
        }

        var patient = await _patientService.GetPatientByIdAsync(id);
        if (patient == null)
        {
            TempData["Error"] = "Patient not found.";
            return RedirectToAction(nameof(Index));
        }
        return View(patient);
    }
}