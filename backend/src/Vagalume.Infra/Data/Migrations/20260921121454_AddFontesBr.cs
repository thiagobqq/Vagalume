using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vagalume.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFontesBr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // remove dados das fontes descontinuadas (Greenhouse/Lever/Ashby/SiteEmpresa/Outro)
            migrationBuilder.Sql(
                "DELETE FROM [Vagas] WHERE [FonteTipo] NOT IN ('Gupy','Trampos','Programathor','InfoJobs','Catho','Rss');");
            migrationBuilder.Sql(
                "DELETE FROM [FontesBusca] WHERE [Tipo] NOT IN ('Gupy','Trampos','Programathor','InfoJobs','Catho','Rss');");

            migrationBuilder.AlterColumn<string>(
                name: "NomeEmpresa",
                table: "FontesBusca",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Identificador",
                table: "FontesBusca",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "NomeEmpresa",
                table: "FontesBusca",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Identificador",
                table: "FontesBusca",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);
        }
    }
}
