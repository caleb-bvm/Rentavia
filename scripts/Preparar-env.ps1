$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
if (Test-Path -LiteralPath '.env') { throw '.env ya existe. No se reemplazan secretos locales.' }
$sqlPassword = 'Aa1!' + [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(24))
$adminPassword = 'Aa1!' + [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(24))
@(
    "MSSQL_SA_PASSWORD=$sqlPassword"
    'ADMIN_EMAIL=admin@rentavia.test'
    "ADMIN_PASSWORD=$adminPassword"
    'OPENAI_API_KEY='
    'OPENAI_MODEL=gpt-4.1-mini'
    'WEB_PORT=8080'
    'SQL_PORT=14330'
) | Set-Content -LiteralPath '.env' -Encoding utf8
Write-Output 'Archivo .env creado con contraseñas aleatorias. Consúltalo localmente y no lo compartas.'
