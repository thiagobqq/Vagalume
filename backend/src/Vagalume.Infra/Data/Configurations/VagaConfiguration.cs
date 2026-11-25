using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vagalume.Domain.Entities;

namespace Vagalume.Infra.Data.Configurations;

public class VagaConfiguration : IEntityTypeConfiguration<Vaga>
{
    public void Configure(EntityTypeBuilder<Vaga> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Titulo).IsRequired().HasMaxLength(300);
        builder.Property(x => x.Empresa).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Localizacao).HasMaxLength(200);
        builder.Property(x => x.Url).IsRequired().HasMaxLength(2048);
        builder.Property(x => x.UrlNormalizada).IsRequired().HasMaxLength(2048);
        builder.Property(x => x.Descricao);
        builder.Property(x => x.FonteNome).IsRequired().HasMaxLength(200);
        builder.Property(x => x.HashDedup).IsRequired().HasMaxLength(64);

        builder.Property(x => x.Modalidade).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Nivel).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.FonteTipo).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);

        builder.Property(x => x.CriadoEm).IsRequired();

        builder.HasOne(x => x.Usuario)
            .WithMany(u => u.Vagas)
            .HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Candidatura)
            .WithMany()
            .HasForeignKey(x => x.CandidaturaId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.UsuarioId, x.UrlNormalizada }).IsUnique();
        builder.HasIndex(x => new { x.UsuarioId, x.HashDedup }).IsUnique();
        builder.HasIndex(x => new { x.UsuarioId, x.Status });
        builder.HasIndex(x => new { x.UsuarioId, x.CriadoEm });
    }
}
