using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vagalume.Domain.Busca;
using Vagalume.Domain.Entities;

namespace Vagalume.Infra.Data.Configurations;

public class ExecucaoBuscaConfiguration : IEntityTypeConfiguration<ExecucaoBusca>
{
    public void Configure(EntityTypeBuilder<ExecucaoBusca> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Erro).HasMaxLength(2000);
        builder.Property(x => x.IniciadaEm).IsRequired();

        builder.Property(x => x.DetalhesPorFonte)
            .HasConversion(JsonList.Converter<ResultadoFonte>());
        builder.Property(x => x.DetalhesPorFonte)
            .Metadata.SetValueComparer(JsonList.Comparer<ResultadoFonte>());

        builder.HasOne(x => x.Usuario)
            .WithMany(u => u.ExecucoesBusca)
            .HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.UsuarioId, x.IniciadaEm });
    }
}
