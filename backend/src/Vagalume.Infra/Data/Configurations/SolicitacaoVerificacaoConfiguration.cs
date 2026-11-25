using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vagalume.Domain.Entities;

namespace Vagalume.Infra.Data.Configurations;

public class SolicitacaoVerificacaoConfiguration : IEntityTypeConfiguration<SolicitacaoVerificacao>
{
    public void Configure(EntityTypeBuilder<SolicitacaoVerificacao> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status).IsRequired().HasMaxLength(30);
        builder.Property(x => x.Mensagem).HasMaxLength(500);
        builder.Property(x => x.CriadoEm).IsRequired();

        builder.HasOne(x => x.Usuario)
            .WithMany()
            .HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.UsuarioId, x.Status });
    }
}
