using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SmartEyeClinic.Data;
using SmartEyeClinic.Services;

class Program
{
    static void Main(string[] args)
    {
        // Read connection string from appsettings.json
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var connectionString = config.GetConnectionString("DefaultConnection");

        // Setup DbContext
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        using var context = new AppDbContext(options);

        // Create services
        var patientService     = new PatientService(context);
        var doctorService      = new DoctorService(context);
        var appointmentService = new AppointmentService(context);
        var examinationService = new ExaminationService(context);
        var medicineService    = new MedicineService(context);
        var prescriptionService = new PrescriptionService(context);
        var invoiceService     = new InvoiceService(context);
        var paymentService     = new PaymentService(context);

        try
        {
            while (true)
            {
                Console.WriteLine("\n===== Smart Eye Clinic =====");
                Console.WriteLine("--- Patients ---");
                Console.WriteLine("1.  Add Patient");
                Console.WriteLine("2.  Show Patients");
                Console.WriteLine("--- Doctors ---");
                Console.WriteLine("3.  Add Doctor");
                Console.WriteLine("4.  Show Doctors");
                Console.WriteLine("--- Appointments ---");
                Console.WriteLine("5.  Add Appointment");
                Console.WriteLine("6.  Show Appointments");
                Console.WriteLine("--- Examinations ---");
                Console.WriteLine("7.  Add Examination");
                Console.WriteLine("8.  Show Examinations");
                Console.WriteLine("--- Medicines ---");
                Console.WriteLine("9.  Add Medicine");
                Console.WriteLine("10. Show Medicines");
                Console.WriteLine("--- Prescriptions ---");
                Console.WriteLine("11. Create Prescription");
                Console.WriteLine("12. Show Prescriptions");
                Console.WriteLine("--- Invoices ---");
                Console.WriteLine("13. Add Invoice");
                Console.WriteLine("14. Show Invoices");
                Console.WriteLine("--- Payments ---");
                Console.WriteLine("15. Add Payment Method");
                Console.WriteLine("16. Show Payment Methods");
                Console.WriteLine("17. Add Payment");
                Console.WriteLine("18. Show Payments");
                Console.WriteLine("----------------------------");
                Console.WriteLine("0.  Exit");
                Console.Write("Choose: ");

                string? choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        patientService.AddPatient();
                        break;

                    case "2":
                        patientService.ShowPatients();
                        break;

                    case "3":
                        doctorService.AddDoctor();
                        break;

                    case "4":
                        doctorService.ShowDoctors();
                        break;

                    case "5":
                        appointmentService.AddAppointment();
                        break;

                    case "6":
                        appointmentService.ShowAppointments();
                        break;

                    case "7":
                        examinationService.AddExamination();
                        break;

                    case "8":
                        examinationService.ShowExaminations();
                        break;

                    case "9":
                        medicineService.AddMedicine();
                        break;

                    case "10":
                        medicineService.ShowMedicines();
                        break;

                    case "11":
                        prescriptionService.CreatePrescription();
                        break;

                    case "12":
                        prescriptionService.ShowPrescriptions();
                        break;

                    case "13":
                        invoiceService.AddInvoice();
                        break;

                    case "14":
                        invoiceService.ShowInvoices();
                        break;

                    case "15":
                        paymentService.AddPaymentMethod();
                        break;

                    case "16":
                        paymentService.ShowPaymentMethods();
                        break;

                    case "17":
                        paymentService.AddPayment();
                        break;

                    case "18":
                        paymentService.ShowPayments();
                        break;

                    case "0":
                        return;

                    default:
                        Console.WriteLine("Invalid Choice");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error:");
            Console.WriteLine(ex.InnerException?.Message ?? ex.Message);
        }
    }
}