using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vagalume.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class FonteAgente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Instrucoes",
                table: "FontesBusca",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WebhookCifrado",
                table: "FontesBusca",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PedidosAgente",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecucaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    FonteId = table.Column<Guid>(type: "uuid", nullable: false),
                    CriteriosJson = table.Column<string>(type: "text", nullable: false),
                    Instrucoes = table.Column<string>(type: "text", nullable: true),
                    TokenHash = table.Column<string>(type: "text", nullable: false),
                    TokenCifrado = table.Column<string>(type: "text", nullable: true),
                    ExpiraEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProximoEnvioEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Tentativas = table.Column<int>(type: "integer", nullable: false),
                    FinalizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    HashRetorno = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidosAgente", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PedidosAgente_ExecucoesBusca_ExecucaoId",
                        column: x => x.ExecucaoId,
                        principalTable: "ExecucoesBusca",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PedidosAgente_ExecucaoId_FonteId",
                table: "PedidosAgente",
                columns: new[] { "ExecucaoId", "FonteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PedidosAgente_ExpiraEm",
                table: "PedidosAgente",
                column: "ExpiraEm",
                filter: "\"FinalizadoEm\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PedidosAgente_ProximoEnvioEm",
                table: "PedidosAgente",
                column: "ProximoEnvioEm",
                filter: "\"FinalizadoEm\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PedidosAgente");

            migrationBuilder.DropColumn(
                name: "Instrucoes",
                table: "FontesBusca");

            migrationBuilder.DropColumn(
                name: "WebhookCifrado",
                table: "FontesBusca");
        }
    }
}
