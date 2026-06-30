using SmartEyeClinic.Data;
using SmartEyeClinic.Models;

namespace SmartEyeClinic.Web.Services;

public class MedicineService
{
    private readonly AppDbContext _context;

    public MedicineService(AppDbContext context)
    {
        _context = context;
    }

    // Renamed: parameters replace Console.ReadLine() — DB logic unchanged
    public void AddMedicine(string name, string? description, string? manufacturer)
    {
        var medicine = new Medicine();

        medicine.Name        = name;
        medicine.Description = description;
        medicine.Manufacturer= manufacturer;

        _context.Medicines.Add(medicine);
        _context.SaveChanges();
    }

    // Renamed: returns list instead of Console.WriteLine() — DB logic unchanged
    public List<Medicine> GetAllMedicines()
    {
        return _context.Medicines.ToList();
    }
}
