# Base de datos de Rentavía

Motor: SQL Server. El esquema se crea con las migraciones de Entity Framework Core, que están en `src/Rentavia.Infrastructure/Data/Migrations/`.

## Archivos

| Archivo | Uso |
| --- | --- |
| `scripts/Esquema-completo.sql` | SQL generado a partir de las cinco migraciones, para revisar el esquema completo |
| `scripts/Verificar-base.sql` | Prueba de restricciones: placa y documento únicos, capacidad, borrado restringido y rowversion. Revierte sus cambios al terminar |

El diagrama entidad-relación y el diccionario de datos están en el [modelo de datos](../docs/tecnico/Modelo-completo.md).

## Aplicar el esquema

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project src/Rentavia.Web -- --migrate
```

También funciona `dotnet ef database update --project src/Rentavia.Infrastructure --startup-project src/Rentavia.Web`. En Docker lo hace el servicio `setup`. `Esquema-completo.sql` no debe ejecutarse sobre una base que ya tiene las migraciones aplicadas.

## Verificar restricciones

```powershell
sqlcmd -S '.\SQLEXPRESS' -d Rentavia -E -C -b -f 65001 -i database/scripts/Verificar-base.sql
```

Solo en desarrollo: el script revierte sus filas, pero los valores IDENTITY consumidos pueden dejar saltos.
