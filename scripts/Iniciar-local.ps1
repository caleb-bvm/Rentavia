param([switch]$Preparar)
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet tool restore
if ($LASTEXITCODE -ne 0) { throw 'No se restauraron las herramientas.' }
dotnet build Rentavia.sln --nologo
if ($LASTEXITCODE -ne 0) { throw 'No se pudo compilar.' }
if ($Preparar) {
    dotnet run --project src/Rentavia.Web --no-build -- --migrate
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo preparar la base.' }
}
dotnet run --project src/Rentavia.Web --no-build
