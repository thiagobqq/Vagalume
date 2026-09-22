using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Vagalume.Infra.Data;

#nullable disable

namespace Vagalume.Infra.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260922120000_MergePermissoesVerifyUser")]
    public class MergePermissoesVerifyUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Unifica busca_vagas.acesso e gap_analysis.acesso em verify_user.
            // Sem backfill: as concessões antigas são removidas e os usuários
            // precisam receber verify_user manualmente.
            migrationBuilder.Sql(
                "DELETE FROM \"UsuarioPermissoes\" WHERE \"Chave\" IN ('busca_vagas.acesso', 'gap_analysis.acesso');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Sem mudança de schema; as concessões removidas não são recuperáveis.
        }
    }
}
