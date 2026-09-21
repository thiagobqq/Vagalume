using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vagalume.Domain.Entities;
using Vagalume.Domain.Enums;

namespace Vagalume.Infra.Data.Configurations;

public class PerfilBuscaConfiguration : IEntityTypeConfiguration<PerfilBusca>
{
    public void Configure(EntityTypeBuilder<PerfilBusca> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Localizacao).HasMaxLength(200);
        builder.Property(x => x.Combina).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.DiasSemana).IsRequired().HasMaxLength(7);
        builder.Property(x => x.FusoHorario).IsRequired().HasMaxLength(100);
        builder.Property(x => x.DiscordWebhookUrl).HasMaxLength(500);
        builder.Property(x => x.CriadoEm).IsRequired();
        builder.Property(x => x.AtualizadoEm).IsRequired();

        builder.Property(x => x.PalavrasChave)
            .HasConversion(JsonList.Converter<string>());
        builder.Property(x => x.PalavrasChave)
            .Metadata.SetValueComparer(JsonList.Comparer<string>());

        builder.Property(x => x.Tecnologias)
            .HasConversion(JsonList.Converter<string>());
        builder.Property(x => x.Tecnologias)
            .Metadata.SetValueComparer(JsonList.Comparer<string>());

        builder.Property(x => x.ExcluirPalavras)
            .HasConversion(JsonList.Converter<string>());
        builder.Property(x => x.ExcluirPalavras)
            .Metadata.SetValueComparer(JsonList.Comparer<string>());

        builder.Property(x => x.Niveis)
            .HasConversion(JsonList.Converter<NivelVaga>());
        builder.Property(x => x.Niveis)
            .Metadata.SetValueComparer(JsonList.Comparer<NivelVaga>());

        builder.Property(x => x.Modalidades)
            .HasConversion(JsonList.Converter<ModalidadeVaga>());
        builder.Property(x => x.Modalidades)
            .Metadata.SetValueComparer(JsonList.Comparer<ModalidadeVaga>());

        builder.HasOne(x => x.Usuario)
            .WithOne(u => u.PerfilBusca)
            .HasForeignKey<PerfilBusca>(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.UsuarioId).IsUnique();
    }
}
