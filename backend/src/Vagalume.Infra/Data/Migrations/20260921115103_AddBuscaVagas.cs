using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vagalume.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBuscaVagas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExecucoesBusca",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IniciadaEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinalizadaEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Sucesso = table.Column<bool>(type: "bit", nullable: true),
                    Erro = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Encontradas = table.Column<int>(type: "int", nullable: false),
                    Aprovadas = table.Column<int>(type: "int", nullable: false),
                    Duplicadas = table.Column<int>(type: "int", nullable: false),
                    Adicionadas = table.Column<int>(type: "int", nullable: false),
                    DetalhesPorFonte = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecucoesBusca", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExecucoesBusca_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FontesBusca",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Identificador = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NomeEmpresa = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FontesBusca", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FontesBusca_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PerfisBusca",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    PalavrasChave = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tecnologias = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Niveis = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Modalidades = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExcluirPalavras = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Localizacao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Combina = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HoraExecucao = table.Column<TimeOnly>(type: "time", nullable: false),
                    DiasSemana = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    FusoHorario = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DiscordWebhookUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UltimaExecucaoEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerfisBusca", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerfisBusca_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Vagas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Empresa = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Localizacao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Modalidade = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Nivel = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    UrlNormalizada = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PublicadaEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FonteTipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FonteNome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HashDedup = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CandidaturaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vagas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vagas_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Vagas_Candidaturas_CandidaturaId",
                        column: x => x.CandidaturaId,
                        principalTable: "Candidaturas",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExecucoesBusca_UsuarioId_IniciadaEm",
                table: "ExecucoesBusca",
                columns: new[] { "UsuarioId", "IniciadaEm" });

            migrationBuilder.CreateIndex(
                name: "IX_FontesBusca_UsuarioId",
                table: "FontesBusca",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_PerfisBusca_UsuarioId",
                table: "PerfisBusca",
                column: "UsuarioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vagas_CandidaturaId",
                table: "Vagas",
                column: "CandidaturaId");

            migrationBuilder.CreateIndex(
                name: "IX_Vagas_UsuarioId_CriadoEm",
                table: "Vagas",
                columns: new[] { "UsuarioId", "CriadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_Vagas_UsuarioId_HashDedup",
                table: "Vagas",
                columns: new[] { "UsuarioId", "HashDedup" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vagas_UsuarioId_Status",
                table: "Vagas",
                columns: new[] { "UsuarioId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Vagas_UsuarioId_UrlNormalizada",
                table: "Vagas",
                columns: new[] { "UsuarioId", "UrlNormalizada" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExecucoesBusca");

            migrationBuilder.DropTable(
                name: "FontesBusca");

            migrationBuilder.DropTable(
                name: "PerfisBusca");

            migrationBuilder.DropTable(
                name: "Vagas");
        }
    }
}
