using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vagalume.Domain.Entities;

namespace Vagalume.Infra.Data.Configurations;

public class CandidaturaConfiguration : IEntityTypeConfiguration<Candidatura>
{
    public void Configure(EntityTypeBuilder<Candidatura> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Empresa)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Cargo)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Link)
            .HasMaxLength(2048);

        builder.Property(x => x.Observacoes)
            .HasMaxLength(2000);

        builder.Property(x => x.Origem)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.CriadoEm).IsRequired();
        builder.Property(x => x.AtualizadoEm).IsRequired();

        builder.HasOne(x => x.Usuario)
            .WithMany(u => u.Candidaturas)
            .HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.UsuarioId);
        builder.HasIndex(x => new { x.UsuarioId, x.CriadoEm });
    }
}
