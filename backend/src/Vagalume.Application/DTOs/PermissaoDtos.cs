using System.ComponentModel.DataAnnotations;

namespace Vagalume.Application.DTOs;

public class PermissaoDto
{
    public string Chave { get; set; } = string.Empty;
}

public class UsuarioPermissoesDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> Permissoes { get; set; } = new();
}

public class ConcederPermissaoRequest
{
    [Required(ErrorMessage = "Chave da permissão é obrigatória")]
    [MaxLength(100)]
    public string Chave { get; set; } = string.Empty;
}
