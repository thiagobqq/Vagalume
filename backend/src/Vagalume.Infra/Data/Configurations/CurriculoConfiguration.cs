using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vagalume.Domain.Entities;

namespace Vagalume.Infra.Data.Configurations;

public class CurriculoConfiguration : IEntityTypeConfiguration<Curriculo>
{
    public void Configure(EntityTypeBuilder<Curriculo> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.NomeArquivoOriginal)
            .IsRequired()
            .HasMaxLength(260);

        builder.Property(x => x.Rotulo)
            .HasMaxLength(120);

        builder.Property(x => x.CaminhoArmazenado)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.TamanhoBytes).IsRequired();
        builder.Property(x => x.Ativo).IsRequired();
        builder.Property(x => x.TextoExtraido);
        builder.Property(x => x.TextoLegivel);
        builder.Property(x => x.CriadoEm).IsRequired();

        builder.HasOne(x => x.Usuario)
            .WithMany(u => u.Curriculos)
            .HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.UsuarioId);
        builder.HasIndex(x => x.UsuarioId)
            .IsUnique()
            .HasDatabaseName("IX_Curriculos_UsuarioId_Ativo")
            .HasFilter("\"Ativo\" = true");
    }
}
