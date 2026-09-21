using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vagalume.Domain.Entities;

namespace Vagalume.Infra.Data.Configurations;

public class FonteBuscaConfiguration : IEntityTypeConfiguration<FonteBusca>
{
    public void Configure(EntityTypeBuilder<FonteBusca> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Identificador).HasMaxLength(500);
        builder.Property(x => x.NomeEmpresa).HasMaxLength(200);
        builder.Property(x => x.Nome).HasMaxLength(200);
        builder.Property(x => x.CriadoEm).IsRequired();

        builder.HasOne(x => x.Usuario)
            .WithMany(u => u.FontesBusca)
            .HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.UsuarioId);
    }
}
