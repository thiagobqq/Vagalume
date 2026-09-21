using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vagalume.Domain.Analise;
using Vagalume.Domain.Entities;

namespace Vagalume.Infra.Data.Configurations;

public class AnaliseCompatibilidadeConfiguration : IEntityTypeConfiguration<AnaliseCompatibilidade>
{
    public void Configure(EntityTypeBuilder<AnaliseCompatibilidade> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Provedor).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Modelo).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Resumo).IsRequired().HasMaxLength(4000);
        builder.Property(x => x.CriadoEm).IsRequired();
        builder.Property(x => x.AtualizadoEm).IsRequired();

        builder.Property(x => x.Requisitos)
            .HasConversion(JsonList.Converter<RequisitoCompatibilidade>());
        builder.Property(x => x.Requisitos)
            .Metadata.SetValueComparer(JsonList.Comparer<RequisitoCompatibilidade>());

        builder.Property(x => x.Sugestoes)
            .HasConversion(JsonList.Converter<string>());
        builder.Property(x => x.Sugestoes)
            .Metadata.SetValueComparer(JsonList.Comparer<string>());

        builder.HasOne(x => x.Usuario)
            .WithMany(u => u.AnalisesCompatibilidade)
            .HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Vaga)
            .WithMany()
            .HasForeignKey(x => x.VagaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Curriculo)
            .WithMany()
            .HasForeignKey(x => x.CurriculoId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => new { x.UsuarioId, x.VagaId }).IsUnique();
    }
}
