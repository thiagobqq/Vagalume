using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vagalume.Domain.Entities;

namespace Vagalume.Infra.Data.Configurations;

public class UsuarioPermissaoConfiguration : IEntityTypeConfiguration<UsuarioPermissao>
{
    public void Configure(EntityTypeBuilder<UsuarioPermissao> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Chave)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Origem)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.ConcedidoEm).IsRequired();

        builder.HasOne(x => x.Usuario)
            .WithMany(u => u.Permissoes)
            .HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.UsuarioId, x.Chave }).IsUnique();
    }
}
