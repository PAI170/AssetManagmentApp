using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetManagmentApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarEsBodegaAProyecto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsBodega",
                table: "Proyectos",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EsBodega",
                table: "Proyectos");
        }
    }
}
