$output = @()
$cs = 'Server=DESKTOP-B24H7CB;Database=SmartEyeClinic;Trusted_Connection=True;TrustServerCertificate=True;'
$conn = New-Object System.Data.SqlClient.SqlConnection($cs)
$conn.Open()

$output += "=== COLUMNS ==="
$sql1 = "SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME IN ('Surgeries','Invoices','MedicalFiles','PatientHistory','PatientInsurance','DoctorReviews','Appointments','Patients','Users') ORDER BY TABLE_NAME, ORDINAL_POSITION;"
$cmd = $conn.CreateCommand()
$cmd.CommandText = $sql1
$r = $cmd.ExecuteReader()
while ($r.Read()) {
    $output += ("{0,-22} {1,-28} {2,-15} {3}" -f $r[0], $r[1], $r[2], $r[3])
}
$r.Close()

$output += ""
$output += "=== FOREIGN KEYS ==="
$sql2 = "SELECT fk.name AS FK_Name, tp.name AS Parent_Table, cp.name AS Parent_Column, tr.name AS Referenced_Table, cr.name AS Referenced_Column, fk.delete_referential_action_desc AS Delete_Action FROM sys.foreign_keys AS fk INNER JOIN sys.foreign_key_columns AS fkc ON fk.object_id = fkc.constraint_object_id INNER JOIN sys.tables AS tp ON fk.parent_object_id = tp.object_id INNER JOIN sys.columns AS cp ON fkc.parent_object_id = cp.object_id AND fkc.parent_column_id = cp.column_id INNER JOIN sys.tables AS tr ON fk.referenced_object_id = tr.object_id INNER JOIN sys.columns AS cr ON fkc.referenced_object_id = cr.object_id AND fkc.referenced_column_id = cr.column_id WHERE tp.name IN ('Surgeries','Invoices','MedicalFiles','PatientHistory','PatientInsurance','DoctorReviews','Appointments','Patients') ORDER BY tp.name, fk.name;"
$cmd2 = $conn.CreateCommand()
$cmd2.CommandText = $sql2
$r2 = $cmd2.ExecuteReader()
while ($r2.Read()) {
    $output += ("{0,-42} {1,-22} {2,-22} {3,-22} {4,-18} {5}" -f $r2[0], $r2[1], $r2[2], $r2[3], $r2[4], $r2[5])
}
$r2.Close()

$conn.Close()
$output += ""
$output += "Done."
$output | Out-File -FilePath "e:\مبادره\SmartEyeClinic\audit_output.txt" -Encoding UTF8
