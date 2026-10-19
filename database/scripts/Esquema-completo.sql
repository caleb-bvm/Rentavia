IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [AspNetRoles] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);

CREATE TABLE [AspNetUsers] (
    [Id] nvarchar(450) NOT NULL,
    [UserName] nvarchar(256) NULL,
    [NormalizedUserName] nvarchar(256) NULL,
    [Email] nvarchar(256) NULL,
    [NormalizedEmail] nvarchar(256) NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(max) NULL,
    [SecurityStamp] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset NULL,
    [LockoutEnabled] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
);

CREATE TABLE [Categorias] (
    [Id] int NOT NULL IDENTITY,
    [Nombre] nvarchar(50) NOT NULL,
    [Descripcion] nvarchar(150) NULL,
    CONSTRAINT [PK_Categorias] PRIMARY KEY ([Id])
);

CREATE TABLE [Clientes] (
    [Id] int NOT NULL IDENTITY,
    [Nombre] nvarchar(50) NOT NULL,
    [Apellido] nvarchar(50) NOT NULL,
    [TipoDocumento] nvarchar(20) NOT NULL,
    [NumeroDocumento] nvarchar(30) NOT NULL,
    [Licencia] nvarchar(30) NOT NULL,
    [VencimientoLicencia] date NOT NULL,
    [Telefono] nvarchar(20) NOT NULL,
    [Correo] nvarchar(100) NULL,
    [FechaRegistroUtc] datetimeoffset(0) NOT NULL,
    [Version] rowversion NOT NULL,
    CONSTRAINT [PK_Clientes] PRIMARY KEY ([Id])
);

CREATE TABLE [Sucursales] (
    [Id] int NOT NULL IDENTITY,
    [Nombre] nvarchar(100) NOT NULL,
    [Direccion] nvarchar(200) NOT NULL,
    [Telefono] nvarchar(20) NULL,
    [Activa] bit NOT NULL,
    CONSTRAINT [PK_Sucursales] PRIMARY KEY ([Id])
);

CREATE TABLE [AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] nvarchar(450) NOT NULL,
    [ProviderKey] nvarchar(450) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserRoles] (
    [UserId] nvarchar(450) NOT NULL,
    [RoleId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserTokens] (
    [UserId] nvarchar(450) NOT NULL,
    [LoginProvider] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Vehiculos] (
    [Id] int NOT NULL IDENTITY,
    [CategoriaId] int NOT NULL,
    [SucursalId] int NOT NULL,
    [Placa] nvarchar(15) NOT NULL,
    [Marca] nvarchar(50) NOT NULL,
    [Modelo] nvarchar(50) NOT NULL,
    [Anio] int NOT NULL,
    [CapacidadPasajeros] int NOT NULL,
    [CapacidadMaletas] int NOT NULL,
    [Kilometraje] int NOT NULL,
    [Transmision] nvarchar(20) NOT NULL,
    [Estado] nvarchar(20) NOT NULL,
    [Version] rowversion NOT NULL,
    CONSTRAINT [PK_Vehiculos] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Vehiculos_Estado] CHECK ([Estado] IN ('Activo', 'FueraDeServicio', 'Retirado')),
    CONSTRAINT [CK_Vehiculos_Kilometraje] CHECK ([Kilometraje] >= 0),
    CONSTRAINT [CK_Vehiculos_Maletas] CHECK ([CapacidadMaletas] >= 0),
    CONSTRAINT [CK_Vehiculos_Pasajeros] CHECK ([CapacidadPasajeros] > 0),
    CONSTRAINT [CK_Vehiculos_Transmision] CHECK ([Transmision] IN ('Manual', 'Automatica')),
    CONSTRAINT [FK_Vehiculos_Categorias_CategoriaId] FOREIGN KEY ([CategoriaId]) REFERENCES [Categorias] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Vehiculos_Sucursales_SucursalId] FOREIGN KEY ([SucursalId]) REFERENCES [Sucursales] ([Id]) ON DELETE NO ACTION
);

CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);

CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;

CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);

CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);

CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);

CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);

CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;

CREATE UNIQUE INDEX [IX_Categorias_Nombre] ON [Categorias] ([Nombre]);

CREATE UNIQUE INDEX [IX_Clientes_TipoDocumento_NumeroDocumento] ON [Clientes] ([TipoDocumento], [NumeroDocumento]);

CREATE INDEX [IX_Vehiculos_CategoriaId] ON [Vehiculos] ([CategoriaId]);

CREATE UNIQUE INDEX [IX_Vehiculos_Placa] ON [Vehiculos] ([Placa]);

CREATE INDEX [IX_Vehiculos_SucursalId] ON [Vehiculos] ([SucursalId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261009033143_BaseInicialSqlServer', N'10.0.11');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Vehiculos] ADD [FotoRuta] nvarchar(200) NULL;

ALTER TABLE [Vehiculos] ADD [TarifaDiaria] decimal(12,2) NOT NULL DEFAULT 0.0;

ALTER TABLE [Clientes] ADD [Activo] bit NOT NULL DEFAULT CAST(1 AS bit);

CREATE TABLE [Auditorias] (
    [Id] int NOT NULL IDENTITY,
    [FechaUtc] datetimeoffset NOT NULL,
    [UsuarioId] nvarchar(450) NOT NULL,
    [Accion] nvarchar(50) NOT NULL,
    [Entidad] nvarchar(50) NOT NULL,
    [RegistroId] int NOT NULL,
    CONSTRAINT [PK_Auditorias] PRIMARY KEY ([Id])
);

CREATE TABLE [Mantenimientos] (
    [Id] int NOT NULL IDENTITY,
    [VehiculoId] int NOT NULL,
    [InicioUtc] datetimeoffset NOT NULL,
    [FinUtc] datetimeoffset NOT NULL,
    [Tipo] nvarchar(30) NOT NULL,
    [Descripcion] nvarchar(500) NOT NULL,
    [Costo] decimal(12,2) NOT NULL,
    [Finalizado] bit NOT NULL,
    CONSTRAINT [PK_Mantenimientos] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Mantenimientos_Datos] CHECK ([FinUtc] > [InicioUtc] AND [Costo] >= 0),
    CONSTRAINT [FK_Mantenimientos_Vehiculos_VehiculoId] FOREIGN KEY ([VehiculoId]) REFERENCES [Vehiculos] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Recomendaciones] (
    [Id] int NOT NULL IDENTITY,
    [FechaUtc] datetimeoffset NOT NULL,
    [UsuarioId] nvarchar(450) NOT NULL,
    [EntradaJson] nvarchar(max) NOT NULL,
    [CandidatosJson] nvarchar(max) NOT NULL,
    [ResultadoJson] nvarchar(max) NOT NULL,
    [Origen] nvarchar(30) NOT NULL,
    [Modelo] nvarchar(100) NOT NULL,
    [VersionPrompt] nvarchar(30) NOT NULL,
    CONSTRAINT [PK_Recomendaciones] PRIMARY KEY ([Id])
);

CREATE TABLE [Reservas] (
    [Id] int NOT NULL IDENTITY,
    [ClienteId] int NOT NULL,
    [VehiculoId] int NOT NULL,
    [InicioUtc] datetimeoffset NOT NULL,
    [FinUtc] datetimeoffset NOT NULL,
    [CreadaUtc] datetimeoffset NOT NULL,
    [TarifaDiaria] decimal(12,2) NOT NULL,
    [Dias] int NOT NULL,
    [TotalEstimado] decimal(12,2) NOT NULL,
    [Estado] nvarchar(20) NOT NULL,
    [Version] rowversion NOT NULL,
    CONSTRAINT [PK_Reservas] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Reservas_Fechas] CHECK ([FinUtc] > [InicioUtc]),
    CONSTRAINT [CK_Reservas_Total] CHECK ([TarifaDiaria] > 0 AND [TotalEstimado] > 0 AND [Dias] > 0),
    CONSTRAINT [FK_Reservas_Clientes_ClienteId] FOREIGN KEY ([ClienteId]) REFERENCES [Clientes] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Reservas_Vehiculos_VehiculoId] FOREIGN KEY ([VehiculoId]) REFERENCES [Vehiculos] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Alquileres] (
    [Id] int NOT NULL IDENTITY,
    [ReservaId] int NOT NULL,
    [VehiculoId] int NOT NULL,
    [EntregaUtc] datetimeoffset NOT NULL,
    [DevolucionUtc] datetimeoffset NULL,
    [KilometrajeInicial] int NOT NULL,
    [KilometrajeFinal] int NULL,
    [CombustibleInicial] int NOT NULL,
    [CombustibleFinal] int NULL,
    [ObservacionesEntrega] nvarchar(500) NOT NULL,
    [ObservacionesDevolucion] nvarchar(500) NULL,
    [CargoAdicional] decimal(12,2) NOT NULL,
    [ConceptoCargo] nvarchar(150) NULL,
    [MontoTotal] decimal(12,2) NOT NULL,
    [Estado] nvarchar(20) NOT NULL,
    [Version] rowversion NOT NULL,
    CONSTRAINT [PK_Alquileres] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Alquileres_Combustible] CHECK ([CombustibleInicial] BETWEEN 0 AND 100 AND ([CombustibleFinal] IS NULL OR [CombustibleFinal] BETWEEN 0 AND 100)),
    CONSTRAINT [CK_Alquileres_Importes] CHECK ([CargoAdicional] >= 0 AND [MontoTotal] >= 0),
    CONSTRAINT [CK_Alquileres_Km] CHECK ([KilometrajeInicial] >= 0 AND ([KilometrajeFinal] IS NULL OR [KilometrajeFinal] >= [KilometrajeInicial])),
    CONSTRAINT [FK_Alquileres_Reservas_ReservaId] FOREIGN KEY ([ReservaId]) REFERENCES [Reservas] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Alquileres_Vehiculos_VehiculoId] FOREIGN KEY ([VehiculoId]) REFERENCES [Vehiculos] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Ocupaciones] (
    [Id] int NOT NULL IDENTITY,
    [VehiculoId] int NOT NULL,
    [ReservaId] int NULL,
    [MantenimientoId] int NULL,
    [InicioUtc] datetimeoffset NOT NULL,
    [FinUtc] datetimeoffset NOT NULL,
    [Activa] bit NOT NULL,
    CONSTRAINT [PK_Ocupaciones] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Ocupaciones_Fechas] CHECK ([FinUtc] > [InicioUtc]),
    CONSTRAINT [CK_Ocupaciones_Origen] CHECK (([ReservaId] IS NOT NULL AND [MantenimientoId] IS NULL) OR ([ReservaId] IS NULL AND [MantenimientoId] IS NOT NULL)),
    CONSTRAINT [FK_Ocupaciones_Mantenimientos_MantenimientoId] FOREIGN KEY ([MantenimientoId]) REFERENCES [Mantenimientos] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Ocupaciones_Reservas_ReservaId] FOREIGN KEY ([ReservaId]) REFERENCES [Reservas] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Ocupaciones_Vehiculos_VehiculoId] FOREIGN KEY ([VehiculoId]) REFERENCES [Vehiculos] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Pagos] (
    [Id] int NOT NULL IDENTITY,
    [AlquilerId] int NOT NULL,
    [FechaUtc] datetimeoffset NOT NULL,
    [Monto] decimal(12,2) NOT NULL,
    [Metodo] nvarchar(30) NOT NULL,
    [Referencia] nvarchar(100) NULL,
    CONSTRAINT [PK_Pagos] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Pagos_Monto] CHECK ([Monto] > 0),
    CONSTRAINT [FK_Pagos_Alquileres_AlquilerId] FOREIGN KEY ([AlquilerId]) REFERENCES [Alquileres] ([Id]) ON DELETE NO ACTION
);

ALTER TABLE [Vehiculos] ADD CONSTRAINT [CK_Vehiculos_Tarifa] CHECK ([TarifaDiaria] >= 0);

CREATE UNIQUE INDEX [IX_Alquileres_ReservaId] ON [Alquileres] ([ReservaId]);

CREATE UNIQUE INDEX [IX_Alquileres_VehiculoId] ON [Alquileres] ([VehiculoId]) WHERE [Estado] = 'Abierto';

CREATE INDEX [IX_Mantenimientos_VehiculoId] ON [Mantenimientos] ([VehiculoId]);

CREATE UNIQUE INDEX [IX_Ocupaciones_MantenimientoId] ON [Ocupaciones] ([MantenimientoId]) WHERE [MantenimientoId] IS NOT NULL;

CREATE UNIQUE INDEX [IX_Ocupaciones_ReservaId] ON [Ocupaciones] ([ReservaId]) WHERE [ReservaId] IS NOT NULL;

CREATE INDEX [IX_Ocupaciones_VehiculoId_Activa_InicioUtc_FinUtc] ON [Ocupaciones] ([VehiculoId], [Activa], [InicioUtc], [FinUtc]);

CREATE INDEX [IX_Pagos_AlquilerId] ON [Pagos] ([AlquilerId]);

CREATE INDEX [IX_Reservas_ClienteId] ON [Reservas] ([ClienteId]);

CREATE INDEX [IX_Reservas_VehiculoId_InicioUtc_FinUtc] ON [Reservas] ([VehiculoId], [InicioUtc], [FinUtc]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261012173252_OperacionDeRentas', N'10.0.11');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Auditorias] ADD [RegistroClave] nvarchar(450) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261012182424_ReferenciaAuditoria', N'10.0.11');

COMMIT;
GO

