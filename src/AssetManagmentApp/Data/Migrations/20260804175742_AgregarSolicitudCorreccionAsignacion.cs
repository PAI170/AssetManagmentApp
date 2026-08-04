using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetManagmentApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarSolicitudCorreccionAsignacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SolicitudesCorreccion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AsignacionActivoProyectoId = table.Column<int>(type: "int", nullable: false),
                    ProyectoOriginalId = table.Column<int>(type: "int", nullable: false),
                    ProyectoNuevoId = table.Column<int>(type: "int", nullable: false),
                    Motivo = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Estado = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UsuarioSolicitaId = table.Column<int>(type: "int", nullable: false),
                    FechaSolicitud = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UsuarioResuelveId = table.Column<int>(type: "int", nullable: true),
                    FechaResolucion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ComentarioResolucion = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesCorreccion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitudesCorreccion_AsignacionesActivoProyecto_AsignacionA~",
                        column: x => x.AsignacionActivoProyectoId,
                        principalTable: "AsignacionesActivoProyecto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudesCorreccion_AspNetUsers_UsuarioResuelveId",
                        column: x => x.UsuarioResuelveId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudesCorreccion_AspNetUsers_UsuarioSolicitaId",
                        column: x => x.UsuarioSolicitaId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudesCorreccion_Proyectos_ProyectoNuevoId",
                        column: x => x.ProyectoNuevoId,
                        principalTable: "Proyectos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudesCorreccion_Proyectos_ProyectoOriginalId",
                        column: x => x.ProyectoOriginalId,
                        principalTable: "Proyectos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesCorreccion_AsignacionActivoProyectoId",
                table: "SolicitudesCorreccion",
                column: "AsignacionActivoProyectoId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesCorreccion_ProyectoNuevoId",
                table: "SolicitudesCorreccion",
                column: "ProyectoNuevoId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesCorreccion_ProyectoOriginalId",
                table: "SolicitudesCorreccion",
                column: "ProyectoOriginalId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesCorreccion_UsuarioResuelveId",
                table: "SolicitudesCorreccion",
                column: "UsuarioResuelveId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesCorreccion_UsuarioSolicitaId",
                table: "SolicitudesCorreccion",
                column: "UsuarioSolicitaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SolicitudesCorreccion");
        }
    }
}
