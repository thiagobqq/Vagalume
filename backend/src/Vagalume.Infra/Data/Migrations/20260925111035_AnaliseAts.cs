using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vagalume.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class AnaliseAts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnalisesAts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurriculoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provedor = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Modelo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    Resumo = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Categorias = table.Column<string>(type: "text", nullable: false),
                    Alertas = table.Column<string>(type: "text", nullable: false),
                    PontosFortes = table.Column<string>(type: "text", nullable: false),
                    Sugestoes = table.Column<string>(type: "text", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalisesAts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalisesAts_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AnalisesAts_Curriculos_CurriculoId",
                        column: x => x.CurriculoId,
                        principalTable: "Curriculos",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalisesAts_CurriculoId",
                table: "AnalisesAts",
                column: "CurriculoId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalisesAts_UsuarioId_CurriculoId",
                table: "AnalisesAts",
                columns: new[] { "UsuarioId", "CurriculoId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalisesAts");
        }
    }
}
