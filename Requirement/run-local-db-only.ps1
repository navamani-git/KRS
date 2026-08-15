# Run a SQL script on LOCAL database only (localhost\SQLEXPRESS).
# Usage: .\run-local-db-only.ps1 -Script "..\KRSDealerManagement\LOCAL_TRUNCATE_ALL_TABLES.sql"

param(
    [Parameter(Mandatory = $true)]
    [string]$Script
)

$resolved = Resolve-Path $Script
Write-Host "Applying to localhost\SQLEXPRESS ..." -ForegroundColor Cyan
sqlcmd -S "localhost\SQLEXPRESS" -d "KRSDealerManagementDB" -i $resolved -C -E
if ($LASTEXITCODE -ne 0) { throw "Failed on localhost\SQLEXPRESS" }
Write-Host "Done - applied on local database." -ForegroundColor Green
