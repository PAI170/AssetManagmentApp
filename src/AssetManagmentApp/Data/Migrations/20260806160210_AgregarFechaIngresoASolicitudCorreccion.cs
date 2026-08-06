using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetManagmentApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarFechaIngresoASolicitudCorreccion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "FechaIngresoNueva",
                table: "SolicitudesCorreccion",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "FechaIngresoOriginal",
                table: "SolicitudesCorreccion",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaIngresoNueva",
                table: "SolicitudesCorreccion");

            migrationBuilder.DropColumn(
                name: "FechaIngresoOriginal",
                table: "SolicitudesCorreccion");
        }
    }
}
