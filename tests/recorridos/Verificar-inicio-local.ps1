#requires -Version 5.1
# Prueba el lanzador contra una base temporal propia y restaura la configuracion local.
[CmdletBinding()]
param([string]$ServidorSql = '.\SQLEXPRESS')
$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$lanzador = Join-Path $raiz 'scripts\Iniciar-Rentavia.ps1'
$archivoConfig = Join-Path $raiz '.env.local.json'
$configAnterior = $null
if (Test-Path -LiteralPath $archivoConfig) { $configAnterior = [IO.File]::ReadAllBytes($archivoConfig) }
$base = 'Rentavia_Inicio_Pruebas_' + [Guid]::NewGuid().ToString('N')
$salida = Join-Path $raiz ('tmp\inicio-local-' + $base)
New-Item -ItemType Directory -Path $salida -Force | Out-Null
$conexion = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
$conexion['Data Source'] = $ServidorSql
$conexion['Initial Catalog'] = $base
$conexion['Integrated Security'] = $true
$conexion['Encrypt'] = $ServidorSql -notmatch '^\(localdb\)\\'
$conexion['TrustServerCertificate'] = $true
$proceso = $null

function Consultar-Prueba {
    param([string]$Consulta)
    $sql = New-Object System.Data.SqlClient.SqlConnection $conexion.ConnectionString
    try {
        $sql.Open()
        $comando = $sql.CreateCommand()
        $comando.CommandText = $Consulta
        return $comando.ExecuteScalar()
    } finally { $sql.Dispose() }
}
function Comprobar {
    param([bool]$Condicion, [string]$Mensaje)
    if (-not $Condicion) { throw $Mensaje }
    Write-Host "OK: $Mensaje"
}

try {
    # Las credenciales de esta prueba son aleatorias y nunca se muestran.
    @{
        ServidorSql = $ServidorSql
        BaseDatos = $base
        ADMIN_EMAIL = 'prueba.inicio@rentavia.test'
        ADMIN_PASSWORD = 'Aa1!' + [Guid]::NewGuid().ToString('N')
    } | ConvertTo-Json | Set-Content -LiteralPath $archivoConfig -Encoding UTF8
    $carpetaAnterior = (Get-Location).Path
    $entornoAnterior = $env:ConnectionStrings__Rentavia
    & $lanzador -SoloPreparar -SinDatosIniciales
    Comprobar ((Get-Location).Path -eq $carpetaAnterior) 'Restauro la carpeta de trabajo'
    Comprobar ($env:ConnectionStrings__Rentavia -eq $entornoAnterior) 'Restauro la conexion del proceso'
    Comprobar ((Consultar-Prueba 'SELECT COUNT(*) FROM __EFMigrationsHistory') -eq 5) 'Aplico las cinco migraciones actuales'
    Comprobar ((Consultar-Prueba 'SELECT COUNT(*) FROM AspNetUsers') -eq 1) 'Creo una unica cuenta inicial'
    Comprobar ((Consultar-Prueba 'SELECT COUNT(*) FROM Vehiculos') -eq 0) 'Respeto -SinDatosIniciales'
    $hash = Consultar-Prueba 'SELECT TOP (1) PasswordHash FROM AspNetUsers'
    & $lanzador -SoloPreparar
    & $lanzador -SoloPreparar
    Comprobar ((Consultar-Prueba 'SELECT COUNT(*) FROM AspNetUsers') -eq 1) 'Las siguientes ejecuciones conservaron la cuenta'
    Comprobar ((Consultar-Prueba 'SELECT TOP (1) PasswordHash FROM AspNetUsers') -eq $hash) 'Conservo la contrasena inicial'
    Comprobar ((Consultar-Prueba 'SELECT COUNT(*) FROM Vehiculos') -eq 3) 'Cargo los vehiculos iniciales una sola vez'
    Comprobar ((Consultar-Prueba 'SELECT COUNT(*) FROM Clientes') -eq 1) 'Conservo el cliente inicial sin duplicarlo'

    $listener = New-Object Net.Sockets.TcpListener ([Net.IPAddress]::Loopback), 0
    $listener.Start()
    $puerto = $listener.LocalEndpoint.Port
    $listener.Stop()
    $url = "http://127.0.0.1:$puerto"
    $proceso = Start-Process -FilePath (Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe') `
        -WindowStyle Hidden -PassThru -ArgumentList (
        '-NoProfile -ExecutionPolicy Bypass -File "{0}" -SinAbrirNavegador -Puerto {1}' -f $lanzador, $puerto) `
        -RedirectStandardOutput (Join-Path $salida 'web.log') -RedirectStandardError (Join-Path $salida 'errores.log')
    $disponible = $false
    for ($intento = 0; $intento -lt 90; $intento++) {
        if ($proceso.HasExited) { throw "El lanzador termino antes de iniciar la web. Revisa $salida" }
        try {
            $salud = Invoke-WebRequest -UseBasicParsing -Uri "$url/health" -TimeoutSec 2
            if ($salud.StatusCode -eq 200) { $disponible = $true; break }
        } catch { }
        Start-Sleep -Seconds 1
    }
    Comprobar $disponible 'La web respondio y confirmo su conexion SQL'
    $ingreso = Invoke-WebRequest -UseBasicParsing -Uri "$url/Cuenta/Ingresar" -SessionVariable sesion
    $token = [regex]::Match($ingreso.Content, 'name="__RequestVerificationToken"[^>]*value="([^"]+)"').Groups[1].Value
    Comprobar (-not [string]::IsNullOrWhiteSpace($token)) 'El formulario de ingreso entrego un token antifalsificacion'
    $credenciales = Get-Content -LiteralPath $archivoConfig -Raw | ConvertFrom-Json
    $respuesta = Invoke-WebRequest -UseBasicParsing -Uri "$url/Cuenta/Ingresar" -Method Post -WebSession $sesion -Body @{
        Correo = $credenciales.ADMIN_EMAIL
        Clave = $credenciales.ADMIN_PASSWORD
        __RequestVerificationToken = $token
    }
    Comprobar ($respuesta.BaseResponse.ResponseUri.AbsolutePath -ne '/Cuenta/Ingresar') 'La cuenta generada pudo iniciar sesion'
    Write-Host 'Verificacion del lanzador completada.'
} finally {
    try {
        if ($proceso -and -not $proceso.HasExited) {
            & taskkill.exe /PID $proceso.Id /T /F | Out-Null
        }
        [System.Data.SqlClient.SqlConnection]::ClearAllPools()
        # El unico DROP permitido es el nombre temporal generado en esta prueba.
        if ($base -notmatch '^Rentavia_Inicio_Pruebas_[a-f0-9]{32}$') { throw 'Nombre temporal inesperado.' }
        $conexion['Initial Catalog'] = 'master'
        Consultar-Prueba "IF DB_ID('$base') IS NOT NULL BEGIN ALTER DATABASE [$base] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$base]; END" | Out-Null
    } finally {
        if ($null -ne $configAnterior) { [IO.File]::WriteAllBytes($archivoConfig, $configAnterior) }
        elseif (Test-Path -LiteralPath $archivoConfig) { Remove-Item -LiteralPath $archivoConfig }
    }
}
