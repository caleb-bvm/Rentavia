# Genera database/datos/datos.sql con todas las filas de la base local.
# Uso: ./scripts/Exportar-datos.ps1 [-Servidor '.\SQLEXPRESS'] [-BaseDatos Rentavia]
param(
    [string]$Servidor = '.\SQLEXPRESS',
    [string]$BaseDatos = 'Rentavia',
    [string]$Salida = (Join-Path $PSScriptRoot '..\database\datos\datos.sql')
)
$ErrorActionPreference = 'Stop'
$inv = [Globalization.CultureInfo]::InvariantCulture

$cn = New-Object System.Data.SqlClient.SqlConnection "Server=$Servidor;Database=$BaseDatos;Integrated Security=True;TrustServerCertificate=True"
$cn.Open()

function Consultar($sql) {
    $da = New-Object System.Data.SqlClient.SqlDataAdapter $sql, $cn
    $t = New-Object System.Data.DataTable
    [void]$da.Fill($t)
    return ,$t
}

function Literal($v) {
    if ($v -is [DBNull] -or $null -eq $v) { return 'NULL' }
    switch ($v.GetType().Name) {
        'String'         { return "N'" + $v.Replace("'", "''") + "'" }
        'Boolean'        { return [int]$v }
        'DateTime'       { return "'" + $v.ToString('yyyy-MM-ddTHH:mm:ss.fffffff', $inv) + "'" }
        'DateTimeOffset' { return "'" + $v.ToString('yyyy-MM-ddTHH:mm:ss.fffffffzzz', $inv) + "'" }
        'TimeSpan'       { return "'" + $v.ToString() + "'" }
        'Guid'           { return "'" + $v + "'" }
        'Byte[]'         { return '0x' + [BitConverter]::ToString($v).Replace('-', '') }
        default          { return ([IFormattable]$v).ToString($null, $inv) }
    }
}

$tablas = (Consultar "SELECT s.name esquema, t.name tabla, OBJECTPROPERTY(t.object_id,'TableHasIdentity') ident
    FROM sys.tables t JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE t.name <> '__EFMigrationsHistory' ORDER BY t.name").Rows

$sb = New-Object Text.StringBuilder
[void]$sb.AppendLine("-- Datos de $BaseDatos exportados el $(Get-Date -Format 'yyyy-MM-dd HH:mm').")
[void]$sb.AppendLine('-- Ejecutar sobre una base con las migraciones aplicadas. Reemplaza todas las filas existentes.')
[void]$sb.AppendLine('SET NOCOUNT ON;')
# Los índices filtrados exigen estas opciones; sqlcmd no las activa por defecto.
[void]$sb.AppendLine('SET QUOTED_IDENTIFIER ON;')
[void]$sb.AppendLine('SET ANSI_NULLS ON;')
[void]$sb.AppendLine('SET XACT_ABORT ON;')
[void]$sb.AppendLine('BEGIN TRANSACTION;')
foreach ($t in $tablas) { [void]$sb.AppendLine("ALTER TABLE [$($t.esquema)].[$($t.tabla)] NOCHECK CONSTRAINT ALL;") }
foreach ($t in $tablas) { [void]$sb.AppendLine("DELETE FROM [$($t.esquema)].[$($t.tabla)];") }

foreach ($t in $tablas) {
    $nombre = "[$($t.esquema)].[$($t.tabla)]"
    # Se omiten columnas calculadas y rowversion porque SQL Server las genera.
    $cols = (Consultar "SELECT c.name FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id
        WHERE c.object_id = OBJECT_ID('$nombre') AND c.is_computed = 0 AND ty.name <> 'timestamp' ORDER BY c.column_id").Rows | ForEach-Object { $_.name }
    $lista = ($cols | ForEach-Object { "[$_]" }) -join ', '
    $filas = (Consultar "SELECT $lista FROM $nombre").Rows
    if ($filas.Count -eq 0) { continue }
    [void]$sb.AppendLine()
    [void]$sb.AppendLine("-- $($t.tabla): $($filas.Count) filas")
    if ($t.ident -eq 1) { [void]$sb.AppendLine("SET IDENTITY_INSERT $nombre ON;") }
    foreach ($f in $filas) {
        $valores = ($cols | ForEach-Object { Literal $f[$_] }) -join ', '
        [void]$sb.AppendLine("INSERT INTO $nombre ($lista) VALUES ($valores);")
    }
    if ($t.ident -eq 1) {
        [void]$sb.AppendLine("SET IDENTITY_INSERT $nombre OFF;")
        [void]$sb.AppendLine("DBCC CHECKIDENT ('$nombre', RESEED) WITH NO_INFOMSGS;")
    }
}

[void]$sb.AppendLine()
foreach ($t in $tablas) { [void]$sb.AppendLine("ALTER TABLE [$($t.esquema)].[$($t.tabla)] WITH CHECK CHECK CONSTRAINT ALL;") }
[void]$sb.AppendLine('COMMIT;')
$cn.Close()

New-Item -ItemType Directory -Force (Split-Path $Salida) | Out-Null
[IO.File]::WriteAllText($Salida, $sb.ToString(), (New-Object Text.UTF8Encoding $true))
Write-Host "Datos exportados en $Salida"
