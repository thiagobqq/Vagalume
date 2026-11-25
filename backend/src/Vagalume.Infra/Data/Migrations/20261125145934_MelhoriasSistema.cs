using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vagalume.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class MelhoriasSistema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AnalisesAts_Curriculos_CurriculoId",
                table: "AnalisesAts");

            migrationBuilder.DropForeignKey(
                name: "FK_AnalisesCompatibilidade_Curriculos_CurriculoId",
                table: "AnalisesCompatibilidade");

            migrationBuilder.DropForeignKey(
                name: "FK_Vagas_Candidaturas_CandidaturaId",
                table: "Vagas");

            migrationBuilder.DropIndex(
                name: "IX_Curriculos_UsuarioId",
                table: "Curriculos");

            migrationBuilder.AddColumn<string>(
                name: "TokenHash",
                table: "RefreshTokens",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Rotulo",
                table: "Curriculos",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TextoExtraido",
                table: "Curriculos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TextoLegivel",
                table: "Curriculos",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CurriculoId",
                table: "Candidaturas",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescricaoVaga",
                table: "Candidaturas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FaixaSalarial",
                table: "Candidaturas",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Modalidade",
                table: "Candidaturas",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoEncerramento",
                table: "Candidaturas",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrepEntrevistaJson",
                table: "Candidaturas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProximaEtapaEm",
                table: "Candidaturas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SolicitacoesVerificacao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Mensagem = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvidoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvidoPorUsuarioId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitacoesVerificacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitacoesVerificacao_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ExpiraEm",
                table: "RefreshTokens",
                column: "ExpiraEm");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            // Garante no máximo um currículo ativo por usuário antes de criar o índice único parcial
            migrationBuilder.Sql("""
                WITH ranked AS (
                    SELECT "Id", ROW_NUMBER() OVER (PARTITION BY "UsuarioId" ORDER BY "CriadoEm" DESC, "Id" DESC) AS rn
                    FROM "Curriculos"
                    WHERE "Ativo" = true
                )
                UPDATE "Curriculos" c
                SET "Ativo" = false
                FROM ranked r
                WHERE c."Id" = r."Id" AND r.rn > 1;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Curriculos_UsuarioId_Ativo",
                table: "Curriculos",
                column: "UsuarioId",
                unique: true,
                filter: "\"Ativo\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_Candidaturas_CurriculoId",
                table: "Candidaturas",
                column: "CurriculoId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacoesVerificacao_UsuarioId_Status",
                table: "SolicitacoesVerificacao",
                columns: new[] { "UsuarioId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_AnalisesAts_Curriculos_CurriculoId",
                table: "AnalisesAts",
                column: "CurriculoId",
                principalTable: "Curriculos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AnalisesCompatibilidade_Curriculos_CurriculoId",
                table: "AnalisesCompatibilidade",
                column: "CurriculoId",
                principalTable: "Curriculos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Candidaturas_Curriculos_CurriculoId",
                table: "Candidaturas",
                column: "CurriculoId",
                principalTable: "Curriculos",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Vagas_Candidaturas_CandidaturaId",
                table: "Vagas",
                column: "CandidaturaId",
                principalTable: "Candidaturas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AnalisesAts_Curriculos_CurriculoId",
                table: "AnalisesAts");

            migrationBuilder.DropForeignKey(
                name: "FK_AnalisesCompatibilidade_Curriculos_CurriculoId",
                table: "AnalisesCompatibilidade");

            migrationBuilder.DropForeignKey(
                name: "FK_Candidaturas_Curriculos_CurriculoId",
                table: "Candidaturas");

            migrationBuilder.DropForeignKey(
                name: "FK_Vagas_Candidaturas_CandidaturaId",
                table: "Vagas");

            migrationBuilder.DropTable(
                name: "SolicitacoesVerificacao");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_ExpiraEm",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_Curriculos_UsuarioId_Ativo",
                table: "Curriculos");

            migrationBuilder.DropIndex(
                name: "IX_Candidaturas_CurriculoId",
                table: "Candidaturas");

            migrationBuilder.DropColumn(
                name: "TokenHash",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "Rotulo",
                table: "Curriculos");

            migrationBuilder.DropColumn(
                name: "TextoExtraido",
                table: "Curriculos");

            migrationBuilder.DropColumn(
                name: "TextoLegivel",
                table: "Curriculos");

            migrationBuilder.DropColumn(
                name: "CurriculoId",
                table: "Candidaturas");

            migrationBuilder.DropColumn(
                name: "DescricaoVaga",
                table: "Candidaturas");

            migrationBuilder.DropColumn(
                name: "FaixaSalarial",
                table: "Candidaturas");

            migrationBuilder.DropColumn(
                name: "Modalidade",
                table: "Candidaturas");

            migrationBuilder.DropColumn(
                name: "MotivoEncerramento",
                table: "Candidaturas");

            migrationBuilder.DropColumn(
                name: "PrepEntrevistaJson",
                table: "Candidaturas");

            migrationBuilder.DropColumn(
                name: "ProximaEtapaEm",
                table: "Candidaturas");

            migrationBuilder.CreateIndex(
                name: "IX_Curriculos_UsuarioId",
                table: "Curriculos",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_AnalisesAts_Curriculos_CurriculoId",
                table: "AnalisesAts",
                column: "CurriculoId",
                principalTable: "Curriculos",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AnalisesCompatibilidade_Curriculos_CurriculoId",
                table: "AnalisesCompatibilidade",
                column: "CurriculoId",
                principalTable: "Curriculos",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Vagas_Candidaturas_CandidaturaId",
                table: "Vagas",
                column: "CandidaturaId",
                principalTable: "Candidaturas",
                principalColumn: "Id");
        }
    }
}
