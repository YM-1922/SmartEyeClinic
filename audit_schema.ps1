$cs = 'Server=DESKTOP-B24H7CB;Database=SmartEyeClinic;Trusted_Connection=True;TrustServerCertificate=True;'
$conn = New-Object System.Data.SqlClient.SqlConnection($cs)
$conn.Open()

Write-Host "=== COLUMNS ==="
$sql1 = @"
SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME IN ('Surgeries','Invoices','MedicalFiles','PatientHistory','PatientInsurance','DoctorReviews','Appointments','Patients','Users')
ORDER BY TABLE_NAME, ORDINAL_POSITION;
"@
$cmd = $conn.CreateCommand()
$cmd.CommandText = $sql1
$r = $cmd.ExecuteReader()
while ($r.Read()) {
    Write-Host ("{0,-22} {1,-25} {2,-15} {3}" -f $r[0], $r[1], $r[2], $r[3])
}
$r.Close()

Write-Host ""
Write-Host "=== FOREIGN KEYS ==="
$sql2 = @"
SELECT
    fk.name AS FK_Name,
    tp.name AS Parent_Table,
    cp.name AS Parent_Column,
    tr.name AS Referenced_Table,
    cr.name AS Referenced_Column,
    fk.delete_referential_action_desc AS Delete_Action
FROM sys.foreign_keys AS fk
INNER JOIN sys.foreign_key_columns AS fkc ON fk.object_id = fkc.constraint_object_id
INNER JOIN sys.tables AS tp ON fk.parent_object_id = tp.object_id
INNER JOIN sys.columns AS cp ON fkc.parent_object_id = cp.object_id AND fkc.parent_column_id = cp.column_id
INNER JOIN sys.tables AS tr ON fk.referenced_object_id = tr.object_id
INNER JOIN sys.columns AS cr ON fkc.referenced_object_id = cr.object_id AND fkc.referenced_column_id = cr.column_id
WHERE tp.name IN ('Surgeries','Invoices','MedicalFiles','PatientHistory','PatientInsurance','DoctorReviews','Appointments','Patients')
ORDER BY tp.name, fk.name;
"@
$cmd2 = $conn.CreateCommand()
$cmd2.CommandText = $sql2
$r2 = $cmd2.ExecuteReader()
while ($r2.Read()) {
    Write-Host ("{0,-40} {1,-20} {2,-20} {3,-20} {4,-20} {5}" -f $r2[0], $r2[1], $r2[2], $r2[3], $r2[4], $r2[5])
}
$r2.Close()

Write-Host ""
Write-Host "=== INDEXES ==="
$sql3 = @"
SELECT
    t.name AS Table_Name,
    i.name AS Index_Name,
    i.is_unique,
    i.is_primary_key,
    STRING_AGG(c.name, ', ') AS Columns
FROM sys.indexes i
JOIN sys.tables t ON i.object_id = t.object_id
JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE t.name IN ('Surgeries','Invoices','MedicalFiles','PatientHistory','PatientInsurance','DoctorReviews','Appointments','Patients')
GROUP BY t.name, i.name, i.is_unique, i.is_primary_key
ORDER BY t.name, i.name;
"@
$cmd3 = $conn.CreateCommand()
$cmd3.CommandText = $sql3
$r3 = $cmd3.ExecuteReader()
while ($r3.Read()) {
    Write-Host ("{0,-25} {1,-45} unique={2} pk={3} cols=[{4}]" -f $r3[0], $r3[1], $r3[2], $r3[3], $r3[4])
}
$r3.Close()

$conn.Close()
Write-Host ""
Write-Host "Done."
