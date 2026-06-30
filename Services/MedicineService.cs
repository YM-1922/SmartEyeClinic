using System;
using SmartEyeClinic.Data;
using SmartEyeClinic.Models;

namespace SmartEyeClinic.Services
{
    public class MedicineService
    {
        private readonly AppDbContext _context;

        public MedicineService(AppDbContext context)
        {
            _context = context;
        }

        public void AddMedicine()
        {
            var medicine = new Medicine();

            Console.Write("Medicine Name: ");
            medicine.Name = Console.ReadLine()!;

            Console.Write("Description: ");
            medicine.Description = Console.ReadLine();

            Console.Write("Manufacturer: ");
            medicine.Manufacturer = Console.ReadLine();

            _context.Medicines.Add(medicine);
            _context.SaveChanges();

            Console.WriteLine("Medicine Added Successfully!");
        }

        public void ShowMedicines()
        {
            var medicines = _context.Medicines.ToList();

            Console.WriteLine("\n===== Medicines =====");

            foreach (var m in medicines)
            {
                Console.WriteLine(
                    $"ID: {m.Id} | Name: {m.Name} | Manufacturer: {m.Manufacturer}"
                );
            }
        }
    }
}
