CREATE DATABASE SmartEyeClinic;
GO

USE SmartEyeClinic;
GO   



USE SmartEyeClinic;
GO

/* =========================
   ROLES
========================= */
CREATE TABLE Roles
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name VARCHAR(50) NOT NULL UNIQUE,
    Description VARCHAR(200)
);

/* =========================
   USERS
========================= */
CREATE TABLE Users
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FullName VARCHAR(100) NOT NULL,
    Email VARCHAR(150) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    PhoneNumber VARCHAR(20) UNIQUE,
    RoleId INT NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    ProfilePicture VARCHAR(300),
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME NULL,

    CONSTRAINT FK_Users_Roles
        FOREIGN KEY (RoleId) REFERENCES Roles(Id)
);

/* =========================
   BRANCHES
========================= */
CREATE TABLE Branches
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Address VARCHAR(200),
    Phone VARCHAR(20)
);

/* =========================
   SPECIALIZATIONS
========================= */
CREATE TABLE Specializations
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name VARCHAR(100) NOT NULL UNIQUE,
    Description VARCHAR(300)
);

/* =========================
   DOCTORS
========================= */
CREATE TABLE Doctors
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL UNIQUE,
    SpecializationId INT NOT NULL,
    LicenseNumber VARCHAR(50) NOT NULL UNIQUE,
    ConsultationFee DECIMAL(10,2) NOT NULL,
    Bio VARCHAR(500),

    CONSTRAINT FK_Doctors_Users
        FOREIGN KEY (UserId) REFERENCES Users(Id),

    CONSTRAINT FK_Doctors_Specializations
        FOREIGN KEY (SpecializationId) REFERENCES Specializations(Id)
);

/* =========================
   DOCTOR BRANCHES
========================= */
CREATE TABLE DoctorBranches
(
    DoctorId INT NOT NULL,
    BranchId INT NOT NULL,

    CONSTRAINT PK_DoctorBranches PRIMARY KEY (DoctorId, BranchId),

    CONSTRAINT FK_DoctorBranches_Doctors
        FOREIGN KEY (DoctorId) REFERENCES Doctors(Id),

    CONSTRAINT FK_DoctorBranches_Branches
        FOREIGN KEY (BranchId) REFERENCES Branches(Id)
);

/* =========================
   PATIENTS
========================= */
CREATE TABLE Patients
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL UNIQUE,
    NationalId VARCHAR(20) UNIQUE,
    DateOfBirth DATE,
    Gender VARCHAR(10),
    BloodType VARCHAR(5),
    Address VARCHAR(200),
    EmergencyContact VARCHAR(20),

    CONSTRAINT FK_Patients_Users
        FOREIGN KEY (UserId) REFERENCES Users(Id)
);

/* =========================
   PATIENT HISTORY
========================= */
CREATE TABLE PatientHistory
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PatientId INT NOT NULL,
    DiseaseName VARCHAR(150) NOT NULL,
    DiagnosedDate DATE,
    Notes VARCHAR(500),

    CONSTRAINT FK_PatientHistory_Patients
        FOREIGN KEY (PatientId) REFERENCES Patients(Id)
);

CREATE INDEX IX_PatientHistory_Patient
ON PatientHistory(PatientId);

/* =========================
   RECEPTIONISTS
========================= */
CREATE TABLE Receptionists
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL UNIQUE,
    BranchId INT NOT NULL,
    ShiftStart TIME,
    ShiftEnd TIME,

    CONSTRAINT FK_Receptionists_Users
        FOREIGN KEY (UserId) REFERENCES Users(Id),

    CONSTRAINT FK_Receptionists_Branches
        FOREIGN KEY (BranchId) REFERENCES Branches(Id)
);

/* =========================
   DOCTOR SCHEDULES
========================= */
CREATE TABLE DoctorSchedules
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    DoctorId INT NOT NULL,
    DayOfWeek VARCHAR(20) NOT NULL,
    StartTime TIME NOT NULL,
    EndTime TIME NOT NULL,
    IsAvailable BIT DEFAULT 1,

    CONSTRAINT FK_DoctorSchedules_Doctors
        FOREIGN KEY (DoctorId) REFERENCES Doctors(Id)
);

/* =========================
   DOCTOR REVIEWS
========================= */
CREATE TABLE DoctorReviews
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    DoctorId INT NOT NULL,
    PatientId INT NOT NULL,
    Rating INT NOT NULL,
    Comment VARCHAR(500),
    CreatedAt DATETIME DEFAULT GETDATE(),

    CONSTRAINT FK_DoctorReviews_Doctors
        FOREIGN KEY (DoctorId) REFERENCES Doctors(Id),

    CONSTRAINT FK_DoctorReviews_Patients
        FOREIGN KEY (PatientId) REFERENCES Patients(Id)
);

ALTER TABLE DoctorReviews
ADD CONSTRAINT UQ_DoctorReviews UNIQUE (DoctorId, PatientId);

ALTER TABLE DoctorReviews
ADD CONSTRAINT CHK_Rating CHECK (Rating BETWEEN 1 AND 5);

/* =========================
   APPOINTMENTS
========================= */
CREATE TABLE Appointments
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PatientId INT NOT NULL,
    DoctorId INT NOT NULL,
    ReceptionistId INT NULL,
    BranchId INT NOT NULL,

    AppointmentDateTime DATETIME NOT NULL,
    DurationMinutes INT NOT NULL DEFAULT 30,

    Type VARCHAR(20) NOT NULL,
    Status VARCHAR(20) NOT NULL,
    Notes VARCHAR(500),

    CreatedAt DATETIME DEFAULT GETDATE(),

    CONSTRAINT FK_Appointments_Patients FOREIGN KEY (PatientId) REFERENCES Patients(Id),
    CONSTRAINT FK_Appointments_Doctors FOREIGN KEY (DoctorId) REFERENCES Doctors(Id),
    CONSTRAINT FK_Appointments_Receptionists FOREIGN KEY (ReceptionistId) REFERENCES Receptionists(Id),
    CONSTRAINT FK_Appointments_Branches FOREIGN KEY (BranchId) REFERENCES Branches(Id)
);

/* INDEXES */
CREATE INDEX IX_Appointments_Doctor_Date
ON Appointments(DoctorId, AppointmentDateTime);

CREATE INDEX IX_Appointments_Patient
ON Appointments(PatientId);

/* =========================
   QUEUE
========================= */
CREATE TABLE Queue
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    AppointmentId INT NOT NULL UNIQUE,
    QueueNumber INT NOT NULL,
    Priority INT DEFAULT 0,
    Status VARCHAR(20) NOT NULL,
    CheckInTime DATETIME,
    CalledAt DATETIME,
    EstimatedTime DATETIME,

    CONSTRAINT FK_Queue_Appointments
        FOREIGN KEY (AppointmentId) REFERENCES Appointments(Id)
);

CREATE INDEX IX_Queue_Number
ON Queue(QueueNumber);

/* =========================
   EXAMINATIONS
========================= */
CREATE TABLE Examinations
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    AppointmentId INT NOT NULL UNIQUE,
    Diagnosis VARCHAR(500) NOT NULL,
    Symptoms VARCHAR(500),
    VisualAcuityLeft VARCHAR(10),
    VisualAcuityRight VARCHAR(10),
    IntraocularPressure VARCHAR(20),
    TreatmentPlan VARCHAR(1000),
    ExaminedAt DATETIME DEFAULT GETDATE(),

    CONSTRAINT FK_Examinations_Appointments
        FOREIGN KEY (AppointmentId) REFERENCES Appointments(Id)
);

/* =========================
   MEDICINES
========================= */
CREATE TABLE Medicines
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name VARCHAR(150) NOT NULL,
    Description VARCHAR(500),
    Manufacturer VARCHAR(150)
);

/* =========================
   PRESCRIPTIONS
========================= */
CREATE TABLE PrescriptionHeaders
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ExaminationId INT NOT NULL UNIQUE,
    CreatedAt DATETIME DEFAULT GETDATE(),

    CONSTRAINT FK_PrescriptionHeaders_Examinations
        FOREIGN KEY (ExaminationId) REFERENCES Examinations(Id)
);

CREATE TABLE PrescriptionItems
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PrescriptionId INT NOT NULL,
    MedicineId INT NOT NULL,
    Dosage VARCHAR(100),
    DurationDays INT,
    Instructions VARCHAR(300),

    CONSTRAINT FK_PrescriptionItems_Headers
        FOREIGN KEY (PrescriptionId) REFERENCES PrescriptionHeaders(Id),

    CONSTRAINT FK_PrescriptionItems_Medicines
        FOREIGN KEY (MedicineId) REFERENCES Medicines(Id)
);

/* =========================
   SURGERY TYPES
========================= */
CREATE TABLE SurgeryTypes
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name VARCHAR(100) NOT NULL UNIQUE,
    Description VARCHAR(300)
);

/* =========================
   SURGERIES
========================= */
CREATE TABLE Surgeries
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PatientId INT NOT NULL,
    DoctorId INT NOT NULL,
    AppointmentId INT NOT NULL UNIQUE,
    SurgeryTypeId INT NOT NULL,
    SurgeryDate DATETIME NOT NULL,
    Outcome VARCHAR(20),
    Notes VARCHAR(500),

    CONSTRAINT FK_Surgeries_Patients
        FOREIGN KEY (PatientId) REFERENCES Patients(Id),

    CONSTRAINT FK_Surgeries_Doctors
        FOREIGN KEY (DoctorId) REFERENCES Doctors(Id),

    CONSTRAINT FK_Surgeries_Appointments
        FOREIGN KEY (AppointmentId) REFERENCES Appointments(Id),

    CONSTRAINT FK_Surgeries_Types
        FOREIGN KEY (SurgeryTypeId) REFERENCES SurgeryTypes(Id)
);

/* =========================
   MEDICAL FILES
========================= */
CREATE TABLE MedicalFiles
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PatientId INT NOT NULL,
    AppointmentId INT NULL,
    UploadedBy INT NULL,
    FileType VARCHAR(30) NOT NULL,
    FilePath VARCHAR(300) NOT NULL,
    FileSize BIGINT,
    UploadedAt DATETIME DEFAULT GETDATE(),

    CONSTRAINT FK_MedicalFiles_Patients FOREIGN KEY (PatientId) REFERENCES Patients(Id),
    CONSTRAINT FK_MedicalFiles_Appointments FOREIGN KEY (AppointmentId) REFERENCES Appointments(Id),
    CONSTRAINT FK_MedicalFiles_Users FOREIGN KEY (UploadedBy) REFERENCES Users(Id)
);

/* =========================
   INVOICES
========================= */
CREATE TABLE Invoices
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    AppointmentId INT NOT NULL UNIQUE,
    PatientId INT NOT NULL,
    InvoiceNumber VARCHAR(50) UNIQUE,
    TotalAmount DECIMAL(10,2) NOT NULL,
    PaidAmount DECIMAL(10,2) DEFAULT 0,
    Tax DECIMAL(5,2) DEFAULT 0,
    Discount DECIMAL(5,2) DEFAULT 0,
    Status VARCHAR(20) NOT NULL,
    IssuedAt DATETIME DEFAULT GETDATE(),

    CONSTRAINT FK_Invoices_Appointments FOREIGN KEY (AppointmentId) REFERENCES Appointments(Id),
    CONSTRAINT FK_Invoices_Patients FOREIGN KEY (PatientId) REFERENCES Patients(Id)
);

CREATE INDEX IX_Invoices_Patient
ON Invoices(PatientId);

CREATE INDEX IX_Invoices_Appointment
ON Invoices(AppointmentId);

/* =========================
   PAYMENT METHODS
========================= */
CREATE TABLE PaymentMethods
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name VARCHAR(50) NOT NULL UNIQUE
);

/* =========================
   PAYMENTS
========================= */
CREATE TABLE Payments
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    InvoiceId INT NOT NULL,
    PaymentMethodId INT NOT NULL,
    Amount DECIMAL(10,2) NOT NULL,
    TransactionRef VARCHAR(100),
    PaidAt DATETIME DEFAULT GETDATE(),

    CONSTRAINT FK_Payments_Invoice FOREIGN KEY (InvoiceId) REFERENCES Invoices(Id),
    CONSTRAINT FK_Payments_Method FOREIGN KEY (PaymentMethodId) REFERENCES PaymentMethods(Id)
);

/* =========================
   INSURANCE
========================= */
CREATE TABLE InsuranceCompanies
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Phone VARCHAR(20),
    Address VARCHAR(200)
);

CREATE TABLE PatientInsurance
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PatientId INT NOT NULL,
    InsuranceCompanyId INT NOT NULL,
    InsuranceNumber VARCHAR(50),
    StartDate DATE,
    EndDate DATE,

    CONSTRAINT FK_PatientInsurance_Patients FOREIGN KEY (PatientId) REFERENCES Patients(Id),
    CONSTRAINT FK_PatientInsurance_Companies FOREIGN KEY (InsuranceCompanyId) REFERENCES InsuranceCompanies(Id)
);

/* =========================
   NOTIFICATIONS
========================= */
CREATE TABLE Notifications
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    Type VARCHAR(30) NOT NULL,
    Title VARCHAR(150),
    Channel VARCHAR(30),
    Message VARCHAR(500) NOT NULL,
    IsRead BIT DEFAULT 0,
    SentAt DATETIME DEFAULT GETDATE(),

    CONSTRAINT FK_Notifications_Users FOREIGN KEY (UserId) REFERENCES Users(Id)
);

/* =========================
   AUDIT LOGS
========================= */
CREATE TABLE AuditLogs
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NULL,
    Action VARCHAR(200) NOT NULL,
    TableName VARCHAR(50),
    RecordId INT,
    CreatedAt DATETIME DEFAULT GETDATE(),

    CONSTRAINT FK_AuditLogs_Users FOREIGN KEY (UserId) REFERENCES Users(Id)
);