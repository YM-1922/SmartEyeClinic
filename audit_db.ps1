$cs = 'Server=DESKTOP-B24H7CB;Database=SmartEyeClinic;Trusted_Connection=True;TrustServerCertificate=True;'
try {
    $conn = New-Object System.Data.SqlClient.SqlConnection($cs)
    $conn.Open()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT TABLE_NAME, COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME IN ('Surgeries','Invoices','MedicalFiles','PatientHistory','PatientInsurance','DoctorReviews','Appointments','Patients','Users') ORDER BY TABLE_NAME, COLUMN_NAME"
    $reader = $cmd.ExecuteReader()
    $out = @()
    while ($reader.Read()) {
        $out += "$($reader['TABLE_NAME']).$($reader['COLUMN_NAME'])"
    }
    $reader.Close()
    $conn.Close()
    $out | Out-File -FilePath "e:\مبادره\SmartEyeClinic\db_columns.txt" -Encoding UTF8
    "Success" | Out-File -FilePath "e:\مبادره\SmartEyeClinic\db_status.txt"
} catch {
    $_ | Out-File -FilePath "e:\مبادره\SmartEyeClinic\db_columns_error.txt" -Encoding UTF8
}
