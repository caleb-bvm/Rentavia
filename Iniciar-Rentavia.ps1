#requires -Version 5.1
<#
.SYNOPSIS
Verifica e instala dependencias, prepara SQL Server y ejecuta Rentavia localmente.
.EXAMPLE
powershell -NoProfile -ExecutionPolicy Bypass -File .\Iniciar-Rentavia.ps1
.EXAMPLE
.\Iniciar-Rentavia.ps1 -ServidorSql '(localdb)\Rentavia' -BaseDatos Rentavia_Demo -SoloPreparar
#>
[CmdletBinding()]
param(
    [string]$ServidorSql,
    [ValidatePattern('^[A-Za-z0-9_]{1,128}$')][string]$BaseDatos,
    [ValidateRange(1024, 65535)][int]$Puerto = 5295,
    [switch]$SinDatosDemo,
    [switch]$SoloPreparar,
    [switch]$SinAbrirNavegador
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0
$raiz = $PSScriptRoot
$cache = Join-Path $raiz '.dotnet-local\bootstrap'
$configuracionLocal = Join-Path $raiz '.env.local.json'
$variables = @('PATH', 'DOTNET_ROOT', 'ASPNETCORE_ENVIRONMENT', 'ASPNETCORE_URLS',
    'ConnectionStrings__Rentavia', 'ADMIN_EMAIL', 'ADMIN_PASSWORD', 'OPENAI_API_KEY', 'OpenAI__Modelo')
$entornoAnterior = @{}
foreach ($nombre in $variables) { $entornoAnterior[$nombre] = [Environment]::GetEnvironmentVariable($nombre, 'Process') }
$progresoAnterior = $ProgressPreference
$tlsAnterior = [Net.ServicePointManager]::SecurityProtocol
$ubicacionCambiada = $false

function Invocar-Programa {
    param([string]$Ruta, [string[]]$Argumentos, [string]$MensajeError)
    & $Ruta @Argumentos | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "$MensajeError (codigo $LASTEXITCODE)." }
}

function Descargar-Archivo {
    param([string]$Url, [string]$Destino)
    # Un archivo incompleto nunca se reutiliza como instalador.
    Invoke-WebRequest -UseBasicParsing -Uri $Url -OutFile "$Destino.partial"
    Move-Item -LiteralPath "$Destino.partial" -Destination $Destino -Force
}

function Comprobar-FirmaMicrosoft {
    param([string]$Ruta)
    $firma = Get-AuthenticodeSignature -LiteralPath $Ruta
    if ($firma.Status -ne 'Valid' -or $firma.SignerCertificate.Subject -notmatch 'O=Microsoft Corporation') {
        throw "No se pudo verificar la firma de Microsoft: $Ruta"
    }
}

function Obtener-Dotnet {
    $sdk = (Get-Content -LiteralPath (Join-Path $raiz 'global.json') -Raw | ConvertFrom-Json).sdk.version
    $instalacion = Join-Path $raiz '.dotnet-local\dotnet'
    $dotnetLocal = Join-Path $instalacion 'dotnet.exe'
    $comando = Get-Command dotnet.exe -ErrorAction SilentlyContinue
    $candidatos = @($dotnetLocal)
    if ($comando) { $candidatos += $comando.Source }
    foreach ($candidato in $candidatos) {
        if (-not (Test-Path -LiteralPath $candidato -PathType Leaf)) { continue }
        try {
            # --version respeta global.json y su politica de rollForward.
            $version = & $candidato --version 2>$null
            if ($LASTEXITCODE -ne 0) { continue }
            $runtimes = & $candidato --list-runtimes
            if ($LASTEXITCODE -eq 0 -and ($runtimes -match '^Microsoft.AspNetCore.App 10\.0\.')) {
                Write-Host "SDK .NET disponible: $version"
                return $candidato
            }
        } catch { continue }
    }
    Write-Host "Descargando .NET SDK $sdk desde Microsoft..."
    $instalador = Join-Path $cache 'dotnet-install.ps1'
    Descargar-Archivo 'https://dot.net/v1/dotnet-install.ps1' $instalador
    # El instalador oficial puede llamar a exit: se ejecuta en otro proceso.
    Invocar-Programa (Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe') @(
        '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $instalador,
        '-Version', $sdk, '-InstallDir', $instalacion, '-NoPath'
    ) 'No se pudo instalar el SDK .NET'
    Invocar-Programa $dotnetLocal @('--version') 'El SDK instalado no satisface global.json'
    return $dotnetLocal
}

function Buscar-LocalDb {
    $comando = Get-Command SqlLocalDB.exe -ErrorAction SilentlyContinue
    if ($comando) { return $comando.Source }
    $carpetaSql = Join-Path $env:ProgramFiles 'Microsoft SQL Server'
    if (Test-Path -LiteralPath $carpetaSql) {
        $herramienta = Get-ChildItem -Path (Join-Path $carpetaSql '*\Tools\Binn\SqlLocalDB.exe') -ErrorAction SilentlyContinue |
            Sort-Object FullName -Descending | Select-Object -First 1
        if ($herramienta) { return $herramienta.FullName }
    }
}

function Instalar-LocalDb {
    if ($env:PROCESSOR_ARCHITECTURE -eq 'ARM64' -or $env:PROCESSOR_ARCHITEW6432 -eq 'ARM64') {
        throw 'La instalacion automatica de SQL Server LocalDB requiere Windows x64. Usa una instancia SQL local compatible con -ServidorSql.'
    }
    Write-Host 'Descargando SQL Server 2022 Express LocalDB desde Microsoft...'
    $descargas = Join-Path $cache 'sql-localdb'
    New-Item -ItemType Directory -Path $descargas -Force | Out-Null
    $bootstrapper = Join-Path $descargas 'SQL2022-SSEI-Expr.exe'
    Descargar-Archivo 'https://go.microsoft.com/fwlink/?linkid=2215160' $bootstrapper
    Comprobar-FirmaMicrosoft $bootstrapper
    $descarga = Start-Process -FilePath $bootstrapper -WindowStyle Hidden -Wait -PassThru -ArgumentList (
        '/QUIET /ACTION=Download /MEDIATYPE=LocalDB /ENU /MEDIAPATH="{0}"' -f $descargas)
    if ($descarga.ExitCode -ne 0) { throw "No se pudo descargar LocalDB (codigo $($descarga.ExitCode))." }
    $msi = Get-ChildItem -LiteralPath $descargas -Recurse -Filter SqlLocalDB.msi | Select-Object -First 1
    if (-not $msi) { throw 'La descarga de Microsoft no contiene SqlLocalDB.msi.' }
    Comprobar-FirmaMicrosoft $msi.FullName
    $log = Join-Path $descargas 'instalacion.log'
    Write-Host 'Instalando LocalDB. Windows puede solicitar permisos de administrador.'
    $instalacion = Start-Process -FilePath (Join-Path $env:SystemRoot 'System32\msiexec.exe') -Verb RunAs `
        -WindowStyle Hidden -Wait -PassThru -ArgumentList (
        '/i "{0}" /qn /norestart IACCEPTSQLLOCALDBLICENSETERMS=YES /L*v "{1}"' -f $msi.FullName, $log)
    if ($instalacion.ExitCode -eq 3010) { throw 'LocalDB requiere reiniciar Windows. Reinicia y vuelve a ejecutar este archivo.' }
    if ($instalacion.ExitCode -ne 0) { throw "No se pudo instalar LocalDB. Revisa $log (codigo $($instalacion.ExitCode))." }
    $herramienta = Buscar-LocalDb
    if (-not $herramienta) { throw 'No se encontro SqlLocalDB.exe despues de instalarlo.' }
    return $herramienta
}

function Preparar-ServidorSql {
    param([string]$Servidor)
    if ($Servidor -match '^\(localdb\)\\([A-Za-z0-9_-]+)$') {
        $instancia = $Matches[1]
        $localDb = Buscar-LocalDb
        if (-not $localDb) { $localDb = Instalar-LocalDb }
        $instancias = @(& $localDb info)
        if ($LASTEXITCODE -ne 0) { throw 'No se pudieron consultar las instancias de LocalDB.' }
        if ($instancias -notcontains $instancia) {
            Invocar-Programa $localDb @('create', $instancia) 'No se pudo crear la instancia LocalDB'
        }
        Invocar-Programa $localDb @('start', $instancia) 'No se pudo iniciar LocalDB'
    } elseif ($Servidor -match '^(?:\.|localhost|127\.0\.0\.1)(?:\\([A-Za-z0-9_]+))?(?:,\d+)?$') {
        $nombreServicio = 'MSSQLSERVER'
        if ($Matches[1]) { $nombreServicio = 'MSSQL$' + $Matches[1] }
        $servicio = Get-Service -Name $nombreServicio -ErrorAction SilentlyContinue
        if ($servicio -and $servicio.Status -ne 'Running') {
            Write-Host "Iniciando $nombreServicio. Windows puede solicitar permisos de administrador."
            $orden = "`$ErrorActionPreference = 'Stop'; Start-Service -Name '$nombreServicio'"
            $codificada = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($orden))
            $inicio = Start-Process -FilePath (Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe') `
                -Verb RunAs -WindowStyle Hidden -Wait -PassThru -ArgumentList "-NoProfile -EncodedCommand $codificada"
            if ($inicio.ExitCode -ne 0) { throw "No se pudo iniciar el servicio $nombreServicio." }
            $servicio.WaitForStatus('Running', [TimeSpan]::FromSeconds(30))
        }
    } else {
        throw 'ServidorSql debe ser local: .\SQLEXPRESS, localhost, 127.0.0.1 o (localdb)\Rentavia.'
    }
}

function Consultar-Sql {
    param([string]$Conexion, [string]$Consulta)
    $sql = New-Object System.Data.SqlClient.SqlConnection $Conexion
    try {
        $sql.Open()
        $comando = $sql.CreateCommand()
        $comando.CommandText = $Consulta
        return $comando.ExecuteScalar()
    } finally { $sql.Dispose() }
}

function Leer-EntornoDemo {
    $valores = @{}
    $archivo = Join-Path $raiz '.env'
    if (Test-Path -LiteralPath $archivo) {
        foreach ($linea in Get-Content -LiteralPath $archivo) {
            if ($linea -match '^\s*(ADMIN_EMAIL|ADMIN_PASSWORD|OPENAI_API_KEY|OPENAI_MODEL)\s*=(.*)$') {
                $valores[$Matches[1]] = $Matches[2].Trim().Trim('"').Trim("'")
            }
        }
    }
    return $valores
}

try {
    if ($env:OS -ne 'Windows_NT' -or -not [Environment]::Is64BitOperatingSystem) {
        throw 'Este lanzador requiere Windows de 64 bits y PowerShell 5.1 o posterior.'
    }
    Push-Location -LiteralPath $raiz
    $ubicacionCambiada = $true
    $ProgressPreference = 'SilentlyContinue'
    [Net.ServicePointManager]::SecurityProtocol = $tlsAnterior -bor [Net.SecurityProtocolType]::Tls12
    New-Item -ItemType Directory -Path $cache -Force | Out-Null
    if (-not $SoloPreparar) {
        $listener = New-Object Net.Sockets.TcpListener ([Net.IPAddress]::Loopback), $Puerto
        try { $listener.Start() }
        catch { throw "El puerto $Puerto esta ocupado. Deten la otra instancia o usa -Puerto 5296." }
        finally { $listener.Stop() }
    }
    Write-Host '[1/5] Verificando .NET y dependencias...'
    $dotnet = Obtener-Dotnet
    $env:DOTNET_ROOT = Split-Path -Parent $dotnet
    $env:PATH = "$env:DOTNET_ROOT;$env:PATH"
    Invocar-Programa $dotnet @('tool', 'restore') 'No se restauraron las herramientas .NET'
    Invocar-Programa $dotnet @('restore', 'Rentavia.sln', '--nologo') 'No se restauraron los paquetes NuGet'
    Invocar-Programa $dotnet @('build', 'Rentavia.sln', '--no-restore', '--nologo') 'No se pudo compilar el proyecto'

    Write-Host '[2/5] Preparando SQL Server local...'
    $config = @{}
    if (Test-Path -LiteralPath $configuracionLocal) {
        $archivoConfig = Get-Content -LiteralPath $configuracionLocal -Raw | ConvertFrom-Json
        foreach ($propiedad in $archivoConfig.PSObject.Properties) { $config[$propiedad.Name] = $propiedad.Value }
    }
    if (-not $ServidorSql) {
        if ($config['ServidorSql']) { $ServidorSql = $config['ServidorSql'] }
        elseif (Get-Service -Name 'MSSQL$SQLEXPRESS' -ErrorAction SilentlyContinue) { $ServidorSql = '.\SQLEXPRESS' }
        else { $ServidorSql = '(localdb)\Rentavia' }
    }
    if (-not $BaseDatos) {
        $BaseDatos = 'Rentavia'
        if ($config['BaseDatos']) { $BaseDatos = $config['BaseDatos'] }
    }
    # Tambien validar valores leidos del archivo, que no pasan por ValidatePattern.
    if ($BaseDatos -notmatch '^[A-Za-z0-9_]{1,128}$') { throw 'Nombre de base de datos local invalido.' }
    Preparar-ServidorSql $ServidorSql
    $conexion = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
    $conexion['Data Source'] = $ServidorSql
    $conexion['Initial Catalog'] = 'master'
    $conexion['Integrated Security'] = $true
    # LocalDB usa una canalizacion local y no admite TLS con System.Data.SqlClient.
    $conexion['Encrypt'] = $ServidorSql -notmatch '^\(localdb\)\\'
    $conexion['TrustServerCertificate'] = $true
    $conexion['Connect Timeout'] = 30
    Consultar-Sql $conexion.ConnectionString 'SELECT 1' | Out-Null
    $conexion['Initial Catalog'] = $BaseDatos
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:ConnectionStrings__Rentavia = $conexion.ConnectionString
    $env:ASPNETCORE_URLS = "http://127.0.0.1:$Puerto"
    Write-Host "Servidor: $ServidorSql / Base: $BaseDatos"

    Write-Host '[3/5] Importando el esquema con las migraciones del proyecto...'
    $proyecto = 'src/Rentavia.Web/Rentavia.Web.csproj'
    $ejecucion = @('run', '--project', $proyecto, '--no-build', '--no-launch-profile', '--')
    Invocar-Programa $dotnet ($ejecucion + '--migrate') 'No se pudo crear o actualizar la base de datos'

    Write-Host '[4/5] Verificando la cuenta inicial y los datos demo...'
    $demo = Leer-EntornoDemo
    $usuarios = Consultar-Sql $conexion.ConnectionString 'SELECT COUNT(*) FROM AspNetUsers'
    $config['ServidorSql'] = $ServidorSql
    $config['BaseDatos'] = $BaseDatos
    if ($usuarios -eq 0) {
        if (-not $config['ADMIN_EMAIL']) {
            $config['ADMIN_EMAIL'] = 'admin.demo@rentavia.test'
            if ($demo['ADMIN_EMAIL']) { $config['ADMIN_EMAIL'] = $demo['ADMIN_EMAIL'] }
        }
        if (-not $config['ADMIN_PASSWORD']) {
            if ($demo['ADMIN_PASSWORD']) { $config['ADMIN_PASSWORD'] = $demo['ADMIN_PASSWORD'] }
            else {
                $bytes = New-Object byte[] 24
                $generador = [Security.Cryptography.RandomNumberGenerator]::Create()
                try { $generador.GetBytes($bytes) } finally { $generador.Dispose() }
                $config['ADMIN_PASSWORD'] = 'Aa1!' + [Convert]::ToBase64String($bytes)
            }
        }
        # Guardar antes de crear la cuenta permite retomar una preparacion interrumpida.
        $config | ConvertTo-Json | Set-Content -LiteralPath $configuracionLocal -Encoding UTF8
        $env:ADMIN_EMAIL = $config['ADMIN_EMAIL']
        $env:ADMIN_PASSWORD = $config['ADMIN_PASSWORD']
        Invocar-Programa $dotnet ($ejecucion + '--create-admin') 'No se pudo crear el administrador'
        Write-Host "Cuenta creada. Correo y clave disponibles en $configuracionLocal (solo en este equipo)."
    } else { Write-Host 'La base ya tiene usuarios. Usa tus credenciales habituales.' }
    $config | ConvertTo-Json | Set-Content -LiteralPath $configuracionLocal -Encoding UTF8
    [Environment]::SetEnvironmentVariable('ADMIN_EMAIL', $entornoAnterior['ADMIN_EMAIL'], 'Process')
    [Environment]::SetEnvironmentVariable('ADMIN_PASSWORD', $entornoAnterior['ADMIN_PASSWORD'], 'Process')
    if (-not $SinDatosDemo) {
        $registros = Consultar-Sql $conexion.ConnectionString @'
SELECT (SELECT COUNT(*) FROM Categorias) + (SELECT COUNT(*) FROM Sucursales)
     + (SELECT COUNT(*) FROM Clientes) + (SELECT COUNT(*) FROM Vehiculos)
'@
        if ($registros -eq 0) {
            Invocar-Programa $dotnet ($ejecucion + '--seed-demo') 'No se pudieron cargar los datos de demostracion'
        } else { Write-Host 'Los catalogos ya tienen datos; se conserva su contenido.' }
    }
    # Las variables ya configuradas en la sesion tienen prioridad sobre .env.
    if (-not $env:OPENAI_API_KEY -and $demo['OPENAI_API_KEY']) { $env:OPENAI_API_KEY = $demo['OPENAI_API_KEY'] }
    if (-not $env:OpenAI__Modelo -and $demo['OPENAI_MODEL']) { $env:OpenAI__Modelo = $demo['OPENAI_MODEL'] }
    if ($SoloPreparar) {
        Write-Host '[5/5] Preparacion completada. Ejecuta este archivo sin -SoloPreparar para iniciar la web.'
    } else {
        $url = "http://127.0.0.1:$Puerto"
        Write-Host "[5/5] Iniciando Rentavia en $url. Detener con Ctrl+C."
        if (-not $SinAbrirNavegador) {
            # El navegador se abre cuando /health confirma web y conexion SQL.
            $espera = @'
param($Url)
for ($intento = 0; $intento -lt 60; $intento++) {
    try {
        $respuesta = Invoke-WebRequest -UseBasicParsing -Uri "$Url/health" -TimeoutSec 2
        if ($respuesta.StatusCode -eq 200) { Start-Process $Url; break }
    } catch { }
    Start-Sleep -Seconds 1
}
'@
            $navegador = Start-Job -ScriptBlock ([scriptblock]::Create($espera)) -ArgumentList $url
        }
        try { Invocar-Programa $dotnet $ejecucion 'La aplicacion termino con un error' }
        finally {
            if (-not $SinAbrirNavegador) { Stop-Job $navegador; Remove-Job $navegador -Force }
        }
    }
} finally {
    foreach ($nombre in $variables) { [Environment]::SetEnvironmentVariable($nombre, $entornoAnterior[$nombre], 'Process') }
    $ProgressPreference = $progresoAnterior
    [Net.ServicePointManager]::SecurityProtocol = $tlsAnterior
    if ($ubicacionCambiada) { Pop-Location }
}
