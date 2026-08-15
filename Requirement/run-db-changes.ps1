# Run a SQL migration script on BOTH databases.
# Usage: .\run-db-changes.ps1 -Script "..\KRSDealerManagement\APPLY_RECENT_DB_CHANGES.sql"

param(
    [Parameter(Mandatory = $true)]
    [string]$Script
)

$resolved = Resolve-Path $Script

# 1) Production — krsenterprise.in
Write-Host "Applying to krsenterprise.in ..." -ForegroundColor Cyan
sqlcmd -S "krsenterprise.in" -d "KRSDealerManagementDB" -U "krs" -P "Fd*4xfRobB#brg15" -i $resolved -C
if ($LASTEXITCODE -ne 0) { throw "Failed on krsenterprise.in" }

# 2) Local — localhost\SQLEXPRESS
Write-Host "Applying to localhost\SQLEXPRESS ..." -ForegroundColor Cyan
sqlcmd -S "localhost\SQLEXPRESS" -d "KRSDealerManagementDB" -i $resolved -C -E
if ($LASTEXITCODE -ne 0) { throw "Failed on localhost\SQLEXPRESS" }

Write-Host "Done - applied on both databases." -ForegroundColor Green
