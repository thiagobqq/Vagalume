using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vagalume.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class CabecalhosWebhook : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CabecalhosCifrados",
                table: "FontesBusca",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CabecalhosCifrados",
                table: "FontesBusca");
        }
    }
}
