using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetManagmentApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCodigosRequisaYAlquiler : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodigoAlquiler",
                table: "TiposEquipo",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "Actividad",
                table: "Proyectos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CodigoProyecto",
                table: "Proyectos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Modelo",
                table: "Proyectos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Obra",
                table: "Proyectos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodigoAlquiler",
                table: "ProformaDetalles",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodigoAlquiler",
                table: "TiposEquipo");

            migrationBuilder.DropColumn(
                name: "Actividad",
                table: "Proyectos");

            migrationBuilder.DropColumn(
                name: "CodigoProyecto",
                table: "Proyectos");

            migrationBuilder.DropColumn(
                name: "Modelo",
                table: "Proyectos");

            migrationBuilder.DropColumn(
                name: "Obra",
                table: "Proyectos");

            migrationBuilder.DropColumn(
                name: "CodigoAlquiler",
                table: "ProformaDetalles");
        }
    }
}
