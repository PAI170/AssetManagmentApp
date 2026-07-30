using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetManagmentApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenamePrecioMensualToPrecioPorDia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PrecioMensual",
                table: "TiposEquipo",
                newName: "PrecioPorDia");

            migrationBuilder.RenameColumn(
                name: "PrecioMensualUsado",
                table: "ProformaDetalles",
                newName: "PrecioPorDiaUsado");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PrecioPorDia",
                table: "TiposEquipo",
                newName: "PrecioMensual");

            migrationBuilder.RenameColumn(
                name: "PrecioPorDiaUsado",
                table: "ProformaDetalles",
                newName: "PrecioMensualUsado");
        }
    }
}
