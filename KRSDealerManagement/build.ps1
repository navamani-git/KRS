param(
    [string]$action = "build"
)

$currentDir = Get-Location
Set-Location "d:\KRS\KRSDealerManagement"

if ($action -eq "clean") {
    Write-Host "Cleaning build artifacts..."
    dotnet clean --verbosity quiet
    Get-ChildItem -Path . -Include "bin","obj" -Directory -Recurse | Remove-Item -Recurse -Force
    Write-Host "Clean complete"
}
elseif ($action -eq "rebuild") {
    Write-Host "Rebuilding solution..."
    dotnet clean --verbosity quiet 2>&1 | Out-Null
    dotnet build -c Debug 2>&1
}
else {
    Write-Host "Building solution..."
    dotnet build -c Debug 2>&1
}

Set-Location $currentDir
