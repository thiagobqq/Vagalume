using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vagalume.Domain.Entities;

namespace Vagalume.Infra.Data.Configurations;

public class ConfiguracaoIaConfiguration : IEntityTypeConfiguration<ConfiguracaoIa>
{
    public void Configure(EntityTypeBuilder<ConfiguracaoIa> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Provedor).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Modelo).IsRequired().HasMaxLength(200);
        builder.Property(x => x.ApiKeyCriptografada).HasMaxLength(2000);
        builder.Property(x => x.CriadoEm).IsRequired();
        builder.Property(x => x.AtualizadoEm).IsRequired();

        builder.HasOne(x => x.Usuario)
            .WithOne(u => u.ConfiguracaoIa)
            .HasForeignKey<ConfiguracaoIa>(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.UsuarioId).IsUnique();
    }
}
