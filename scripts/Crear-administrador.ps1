param([Parameter(Mandatory=$true)][string]$Correo)
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$passwordSeguro = Read-Host 'Contraseña inicial (10 caracteres, mayúscula, minúscula, número y símbolo)' -AsSecureString
$passwordPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($passwordSeguro)
try {
    $env:ADMIN_EMAIL = $Correo
    $env:ADMIN_PASSWORD = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordPointer)
    dotnet run --project src/Rentavia.Web -- --create-admin
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo crear la cuenta.' }
} finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordPointer)
    Remove-Item Env:ADMIN_PASSWORD -ErrorAction SilentlyContinue
    Remove-Item Env:ADMIN_EMAIL -ErrorAction SilentlyContinue
}
