-- Prueba con datos ficticios. Revierte los registros al terminar.
-- Ejecutar únicamente en la base de desarrollo Rentavia.
SET NOCOUNT ON;
SET XACT_ABORT OFF;

BEGIN TRANSACTION;
BEGIN TRY
    INSERT INTO Categorias (Nombre) VALUES (N'PRUEBA_BASE_CATEGORIA');
    DECLARE @CategoriaId int = SCOPE_IDENTITY();
    INSERT INTO Sucursales (Nombre, Direccion, Activa)
        VALUES (N'PRUEBA_BASE_SUCURSAL', N'Dirección ficticia', 1);
    DECLARE @SucursalId int = SCOPE_IDENTITY();

    INSERT INTO Vehiculos (CategoriaId, SucursalId, Placa, Marca, Modelo, Anio,
        CapacidadPasajeros, CapacidadMaletas, Kilometraje, Transmision, Estado)
        VALUES (@CategoriaId, @SucursalId, N'TEST-BASE', N'Ficticia', N'Prueba', 2026,
            5, 2, 100, N'Manual', N'Activo');
    DECLARE @VehiculoId int = SCOPE_IDENTITY();
    DECLARE @VersionInicial binary(8) = (SELECT Version FROM Vehiculos WHERE Id = @VehiculoId);
    UPDATE Vehiculos SET Kilometraje = 101 WHERE Id = @VehiculoId;
    IF @VersionInicial = (SELECT Version FROM Vehiculos WHERE Id = @VehiculoId)
        THROW 51000, 'rowversion no cambió al actualizar.', 1;
    PRINT 'OK: rowversion cambia al editar';

    BEGIN TRY
        INSERT INTO Vehiculos (CategoriaId, SucursalId, Placa, Marca, Modelo, Anio,
            CapacidadPasajeros, CapacidadMaletas, Kilometraje, Transmision, Estado)
            VALUES (@CategoriaId, @SucursalId, N'TEST-BASE', N'Ficticia', N'Prueba', 2026,
                5, 2, 100, N'Manual', N'Activo');
        THROW 51001, 'La placa duplicada fue aceptada.', 1;
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() NOT IN (2601, 2627) THROW;
        PRINT 'OK: placa duplicada rechazada';
    END CATCH;

    BEGIN TRY
        UPDATE Vehiculos SET CapacidadPasajeros = 0 WHERE Id = @VehiculoId;
        THROW 51002, 'Se aceptó capacidad cero.', 1;
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() <> 547 THROW;
        PRINT 'OK: capacidad inválida rechazada';
    END CATCH;

    BEGIN TRY
        DELETE FROM Categorias WHERE Id = @CategoriaId;
        THROW 51003, 'Se borró una categoría con vehículo.', 1;
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() <> 547 THROW;
        PRINT 'OK: categoría referenciada protegida';
    END CATCH;

    INSERT INTO Clientes (Nombre, Apellido, TipoDocumento, NumeroDocumento, Licencia,
        VencimientoLicencia, Telefono, FechaRegistroUtc)
        VALUES (N'Persona', N'Ficticia', N'Prueba', N'TEST-BASE-DOC', N'TEST-LIC',
            '20300101', N'00000000', TODATETIMEOFFSET(SYSUTCDATETIME(), '+00:00'));
    BEGIN TRY
        INSERT INTO Clientes (Nombre, Apellido, TipoDocumento, NumeroDocumento, Licencia,
            VencimientoLicencia, Telefono, FechaRegistroUtc)
            VALUES (N'Otra', N'Ficticia', N'Prueba', N'TEST-BASE-DOC', N'TEST-LIC',
                '20300101', N'00000000', TODATETIMEOFFSET(SYSUTCDATETIME(), '+00:00'));
        THROW 51004, 'El documento duplicado fue aceptado.', 1;
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() NOT IN (2601, 2627) THROW;
        PRINT 'OK: documento duplicado rechazado';
    END CATCH;

    ROLLBACK TRANSACTION;
    PRINT 'OK: pruebas finalizadas; registros ficticios revertidos';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
