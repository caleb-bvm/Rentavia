using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rentavia.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class OperacionDeRentas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FotoRuta",
                table: "Vehiculos",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TarifaDiaria",
                table: "Vehiculos",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "Clientes",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "Auditorias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FechaUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UsuarioId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Accion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Entidad = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RegistroId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Auditorias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Mantenimientos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehiculoId = table.Column<int>(type: "int", nullable: false),
                    InicioUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FinUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Costo = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Finalizado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mantenimientos", x => x.Id);
                    table.CheckConstraint("CK_Mantenimientos_Datos", "[FinUtc] > [InicioUtc] AND [Costo] >= 0");
                    table.ForeignKey(
                        name: "FK_Mantenimientos_Vehiculos_VehiculoId",
                        column: x => x.VehiculoId,
                        principalTable: "Vehiculos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Recomendaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FechaUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UsuarioId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    EntradaJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CandidatosJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResultadoJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Origen = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Modelo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VersionPrompt = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recomendaciones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Reservas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    VehiculoId = table.Column<int>(type: "int", nullable: false),
                    InicioUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FinUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadaUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TarifaDiaria = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Dias = table.Column<int>(type: "int", nullable: false),
                    TotalEstimado = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservas", x => x.Id);
                    table.CheckConstraint("CK_Reservas_Fechas", "[FinUtc] > [InicioUtc]");
                    table.CheckConstraint("CK_Reservas_Total", "[TarifaDiaria] > 0 AND [TotalEstimado] > 0 AND [Dias] > 0");
                    table.ForeignKey(
                        name: "FK_Reservas_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservas_Vehiculos_VehiculoId",
                        column: x => x.VehiculoId,
                        principalTable: "Vehiculos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Alquileres",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReservaId = table.Column<int>(type: "int", nullable: false),
                    VehiculoId = table.Column<int>(type: "int", nullable: false),
                    EntregaUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DevolucionUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    KilometrajeInicial = table.Column<int>(type: "int", nullable: false),
                    KilometrajeFinal = table.Column<int>(type: "int", nullable: true),
                    CombustibleInicial = table.Column<int>(type: "int", nullable: false),
                    CombustibleFinal = table.Column<int>(type: "int", nullable: true),
                    ObservacionesEntrega = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ObservacionesDevolucion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CargoAdicional = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    ConceptoCargo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    MontoTotal = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alquileres", x => x.Id);
                    table.CheckConstraint("CK_Alquileres_Combustible", "[CombustibleInicial] BETWEEN 0 AND 100 AND ([CombustibleFinal] IS NULL OR [CombustibleFinal] BETWEEN 0 AND 100)");
                    table.CheckConstraint("CK_Alquileres_Importes", "[CargoAdicional] >= 0 AND [MontoTotal] >= 0");
                    table.CheckConstraint("CK_Alquileres_Km", "[KilometrajeInicial] >= 0 AND ([KilometrajeFinal] IS NULL OR [KilometrajeFinal] >= [KilometrajeInicial])");
                    table.ForeignKey(
                        name: "FK_Alquileres_Reservas_ReservaId",
                        column: x => x.ReservaId,
                        principalTable: "Reservas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Alquileres_Vehiculos_VehiculoId",
                        column: x => x.VehiculoId,
                        principalTable: "Vehiculos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Ocupaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehiculoId = table.Column<int>(type: "int", nullable: false),
                    ReservaId = table.Column<int>(type: "int", nullable: true),
                    MantenimientoId = table.Column<int>(type: "int", nullable: true),
                    InicioUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FinUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Activa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ocupaciones", x => x.Id);
                    table.CheckConstraint("CK_Ocupaciones_Fechas", "[FinUtc] > [InicioUtc]");
                    table.CheckConstraint("CK_Ocupaciones_Origen", "([ReservaId] IS NOT NULL AND [MantenimientoId] IS NULL) OR ([ReservaId] IS NULL AND [MantenimientoId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Ocupaciones_Mantenimientos_MantenimientoId",
                        column: x => x.MantenimientoId,
                        principalTable: "Mantenimientos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ocupaciones_Reservas_ReservaId",
                        column: x => x.ReservaId,
                        principalTable: "Reservas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ocupaciones_Vehiculos_VehiculoId",
                        column: x => x.VehiculoId,
                        principalTable: "Vehiculos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Pagos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlquilerId = table.Column<int>(type: "int", nullable: false),
                    FechaUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Metodo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Referencia = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pagos", x => x.Id);
                    table.CheckConstraint("CK_Pagos_Monto", "[Monto] > 0");
                    table.ForeignKey(
                        name: "FK_Pagos_Alquileres_AlquilerId",
                        column: x => x.AlquilerId,
                        principalTable: "Alquileres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Vehiculos_Tarifa",
                table: "Vehiculos",
                sql: "[TarifaDiaria] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_Alquileres_ReservaId",
                table: "Alquileres",
                column: "ReservaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Alquileres_VehiculoId",
                table: "Alquileres",
                column: "VehiculoId",
                unique: true,
                filter: "[Estado] = 'Abierto'");

            migrationBuilder.CreateIndex(
                name: "IX_Mantenimientos_VehiculoId",
                table: "Mantenimientos",
                column: "VehiculoId");

            migrationBuilder.CreateIndex(
                name: "IX_Ocupaciones_MantenimientoId",
                table: "Ocupaciones",
                column: "MantenimientoId",
                unique: true,
                filter: "[MantenimientoId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Ocupaciones_ReservaId",
                table: "Ocupaciones",
                column: "ReservaId",
                unique: true,
                filter: "[ReservaId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Ocupaciones_VehiculoId_Activa_InicioUtc_FinUtc",
                table: "Ocupaciones",
                columns: new[] { "VehiculoId", "Activa", "InicioUtc", "FinUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_AlquilerId",
                table: "Pagos",
                column: "AlquilerId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_ClienteId",
                table: "Reservas",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_VehiculoId_InicioUtc_FinUtc",
                table: "Reservas",
                columns: new[] { "VehiculoId", "InicioUtc", "FinUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Auditorias");

            migrationBuilder.DropTable(
                name: "Ocupaciones");

            migrationBuilder.DropTable(
                name: "Pagos");

            migrationBuilder.DropTable(
                name: "Recomendaciones");

            migrationBuilder.DropTable(
                name: "Mantenimientos");

            migrationBuilder.DropTable(
                name: "Alquileres");

            migrationBuilder.DropTable(
                name: "Reservas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Vehiculos_Tarifa",
                table: "Vehiculos");

            migrationBuilder.DropColumn(
                name: "FotoRuta",
                table: "Vehiculos");

            migrationBuilder.DropColumn(
                name: "TarifaDiaria",
                table: "Vehiculos");

            migrationBuilder.DropColumn(
                name: "Activo",
                table: "Clientes");
        }
    }
}
