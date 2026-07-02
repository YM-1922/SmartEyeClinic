using Microsoft.EntityFrameworkCore;
using SmartEyeClinic.Data;
using SmartEyeClinic.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartEyeClinic.Web.Services;

public class MedicineService
{
    private readonly AppDbContext _context;

    public MedicineService(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddMedicineAsync(string name, string? description, string? manufacturer)
    {
        var medicine = new Medicine();

        medicine.Name        = name;
        medicine.Description = description;
        medicine.Manufacturer= manufacturer;

        _context.Medicines.Add(medicine);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Medicine>> GetAllMedicinesAsync()
    {
        return await _context.Medicines.AsNoTracking().ToListAsync();
    }
}
