using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rentavia.Infrastructure.Data.Migrations
{
    /// <summary>
    /// El cliente se identifica solo con DUI o pasaporte; se quitan la licencia y su vencimiento.
    /// Los documentos de tipo libre que ya existían pasan a PASAPORTE.
    /// </summary>
    public partial class DocumentoClienteSinLicencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [Clientes] SET [TipoDocumento] = N'PASAPORTE' WHERE [TipoDocumento] <> N'DUI';");

            migrationBuilder.DropColumn(
                name: "Licencia",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "VencimientoLicencia",
                table: "Clientes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Licencia",
                table: "Clientes",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateOnly>(
                name: "VencimientoLicencia",
                table: "Clientes",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));
        }
    }
}
