using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vagalume.Domain.Entities;

namespace Vagalume.Infra.Data.Configurations;

public class EventoTransicaoConfiguration : IEntityTypeConfiguration<EventoTransicaoCandidatura>
{
    public void Configure(EntityTypeBuilder<EventoTransicaoCandidatura> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.StatusAnterior)
            .HasConversion<string?>()
            .HasMaxLength(30);

        builder.Property(x => x.StatusNovo)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.Observacao)
            .HasMaxLength(2000);

        builder.Property(x => x.CriadoEm).IsRequired();

        builder.HasOne(x => x.Candidatura)
            .WithMany(c => c.Eventos)
            .HasForeignKey(x => x.CandidaturaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.CandidaturaId);
        builder.HasIndex(x => x.CriadoEm);
    }
}
