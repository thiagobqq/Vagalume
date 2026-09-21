using Vagalume.Domain.Enums;

namespace Vagalume.Domain.Entities;

public class UsuarioPermissao
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public string Chave { get; set; } = string.Empty;
    public DateTime ConcedidoEm { get; set; }
    public Guid? ConcedidoPorUsuarioId { get; set; }
    public DateTime? ExpiraEm { get; set; }
    public OrigemPermissao Origem { get; set; } = OrigemPermissao.Manual;

    public AppUser Usuario { get; set; } = null!;
}
