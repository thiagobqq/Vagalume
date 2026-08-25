using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vagalume.Domain.Entities;

namespace Vagalume.Infra.Data.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Token)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.ExpiraEm).IsRequired();
        builder.Property(x => x.CriadoEm).IsRequired();

        builder.Property(x => x.SubstituidoPorToken)
            .HasMaxLength(500);

        builder.HasOne(x => x.Usuario)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.Token).IsUnique();
        builder.HasIndex(x => x.UsuarioId);
    }
}
