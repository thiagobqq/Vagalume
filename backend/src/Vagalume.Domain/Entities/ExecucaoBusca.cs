using Vagalume.Domain.Enums;

using Vagalume.Domain.Busca;

namespace Vagalume.Domain.Entities;

public class ExecucaoBusca
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public DateTime IniciadaEm { get; set; }
    public DateTime? FinalizadaEm { get; set; }
    public bool? Sucesso { get; set; }
    public string? Erro { get; set; }

    public int Encontradas { get; set; }
    public int Aprovadas { get; set; }
    public int Duplicadas { get; set; }
    public int Adicionadas { get; set; }

    public List<ResultadoFonte> DetalhesPorFonte { get; set; } = new();

    public AppUser Usuario { get; set; } = null!;
}
