using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vagalume.Domain.Entities;

namespace Vagalume.Infra.Data.Configurations;

public class PedidoAgenteConfiguration : IEntityTypeConfiguration<PedidoAgente>
{
    public void Configure(EntityTypeBuilder<PedidoAgente> builder)
    {
        builder.HasOne(p => p.Execucao).WithMany().HasForeignKey(p => p.ExecucaoId);
        builder.HasIndex(p => new { p.ExecucaoId, p.FonteId }).IsUnique();
        builder.HasIndex(p => p.ProximoEnvioEm).HasFilter("\"FinalizadoEm\" IS NULL");
        builder.HasIndex(p => p.ExpiraEm).HasFilter("\"FinalizadoEm\" IS NULL");
    }
}
