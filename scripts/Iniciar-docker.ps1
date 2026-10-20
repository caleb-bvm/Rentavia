param([switch]$CrearAdministrador, [switch]$DatosDemo)
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
if (-not (Test-Path -LiteralPath '.env')) { throw 'Copia .env.example a .env y configura las contraseñas.' }
docker info --format '{{.ServerVersion}}'
if ($LASTEXITCODE -ne 0) { throw 'Docker Desktop debe estar ejecutándose con contenedores Linux.' }
docker compose --env-file .env -f docker/docker-compose.yml config --quiet
if ($LASTEXITCODE -ne 0) { throw 'Revisa .env y la configuración Compose.' }
docker compose --env-file .env -f docker/docker-compose.yml build web
if ($LASTEXITCODE -ne 0) { throw 'No se pudo construir la imagen.' }
docker compose --env-file .env -f docker/docker-compose.yml up -d database
if ($LASTEXITCODE -ne 0) { throw 'No se pudo iniciar SQL Server.' }
docker compose --env-file .env -f docker/docker-compose.yml run --rm setup --migrate
if ($LASTEXITCODE -ne 0) { throw 'No se aplicaron las migraciones.' }
if ($CrearAdministrador) {
    docker compose --env-file .env -f docker/docker-compose.yml run --rm setup --create-admin
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo crear el administrador. La cuenta existente no se sobrescribe.' }
}
if ($DatosDemo) {
    docker compose --env-file .env -f docker/docker-compose.yml run --rm setup --seed-demo
    if ($LASTEXITCODE -ne 0) { throw 'No se cargaron los datos demo; solo se permite en una base vacía.' }
}
docker compose --env-file .env -f docker/docker-compose.yml up -d web
if ($LASTEXITCODE -ne 0) { throw 'No se pudo iniciar la web.' }
Write-Output 'Abre la web en el puerto WEB_PORT de .env (8080 por defecto).'
