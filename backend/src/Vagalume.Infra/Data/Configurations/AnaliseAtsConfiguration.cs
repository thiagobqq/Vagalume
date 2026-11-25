using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vagalume.Domain.Analise;
using Vagalume.Domain.Entities;

namespace Vagalume.Infra.Data.Configurations;

public class AnaliseAtsConfiguration : IEntityTypeConfiguration<AnaliseAts>
{
    public void Configure(EntityTypeBuilder<AnaliseAts> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Provedor).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Modelo).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Resumo).IsRequired().HasMaxLength(4000);
        builder.Property(x => x.CriadoEm).IsRequired();
        builder.Property(x => x.AtualizadoEm).IsRequired();

        builder.Property(x => x.Categorias)
            .HasConversion(JsonList.Converter<CategoriaAts>());
        builder.Property(x => x.Categorias)
            .Metadata.SetValueComparer(JsonList.Comparer<CategoriaAts>());

        builder.Property(x => x.Alertas)
            .HasConversion(JsonList.Converter<AlertaAts>());
        builder.Property(x => x.Alertas)
            .Metadata.SetValueComparer(JsonList.Comparer<AlertaAts>());

        builder.Property(x => x.PontosFortes)
            .HasConversion(JsonList.Converter<string>());
        builder.Property(x => x.PontosFortes)
            .Metadata.SetValueComparer(JsonList.Comparer<string>());

        builder.Property(x => x.Sugestoes)
            .HasConversion(JsonList.Converter<string>());
        builder.Property(x => x.Sugestoes)
            .Metadata.SetValueComparer(JsonList.Comparer<string>());

        builder.HasOne(x => x.Usuario)
            .WithMany(u => u.AnalisesAts)
            .HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Curriculo)
            .WithMany()
            .HasForeignKey(x => x.CurriculoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.UsuarioId, x.CurriculoId }).IsUnique();
    }
}
