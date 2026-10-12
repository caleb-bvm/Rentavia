using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rentavia.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReferenciaAuditoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RegistroClave",
                table: "Auditorias",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RegistroClave",
                table: "Auditorias");
        }
    }
}
