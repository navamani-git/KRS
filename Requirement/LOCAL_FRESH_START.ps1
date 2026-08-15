# LOCAL DB ONLY — per Requirement/my prompt:
#   1) Truncate ALL tables
#   2) Seed only: admin, dealerships, manager + finance per dealer, subdealers (Aug-26 list)
# Does NOT touch production.

$ErrorActionPreference = 'Stop'
$root = Join-Path $PSScriptRoot '..\KRSDealerManagement' | Resolve-Path

$scripts = @(
    'LOCAL_TRUNCATE_ALL_TABLES.sql',
    'LOCAL_INSERT_ADMIN.sql',
    'LOCAL_HIERARCHY_3_DEALERS.sql',
    'LOCAL_SEED_MASTERS.sql',
    'SEED_SUBDEALERS_AUG26_UPDATE.sql'
)

Write-Host '=== LOCAL FRESH START (truncate all, minimal seed only) ===' -ForegroundColor Yellow

foreach ($name in $scripts) {
    $path = Join-Path $root $name
    if (-not (Test-Path $path)) { throw "Missing script: $path" }
    Write-Host "`n>> $name" -ForegroundColor Cyan
    sqlcmd -S "localhost\SQLEXPRESS" -d "KRSDealerManagementDB" -i $path -C -E
    if ($LASTEXITCODE -ne 0) { throw "Failed: $name" }
}

Write-Host "`n=== DONE ===" -ForegroundColor Green
Write-Host "Masters seeded: Status Master (32 statuses across 7 categories)"
Write-Host "Dealerships: KARUR, NAMAKKAL, SALEM (3 only per Excel)"
Write-Host "Staff: karur_mgr, namakkal_mgr, salem_mgr + *_finance / KARUR@123"
Write-Host "Subdealers: password Subdealers@123"
