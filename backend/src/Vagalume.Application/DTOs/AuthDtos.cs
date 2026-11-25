using System.ComponentModel.DataAnnotations;

namespace Vagalume.Application.DTOs;

public class RegisterRequest
{
    [Required(ErrorMessage = "Nome é obrigatório")]
    [MaxLength(150)]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-mail é obrigatório")]
    [EmailAddress(ErrorMessage = "E-mail inválido")]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Senha é obrigatória")]
    [MinLength(6, ErrorMessage = "Senha deve ter no mínimo 6 caracteres")]
    [MaxLength(100)]
    public string Senha { get; set; } = string.Empty;
}

public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Senha { get; set; } = string.Empty;
}

public class RefreshRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

public class LogoutRequest
{
    public string? RefreshToken { get; set; }
    public bool TodasSessoes { get; set; }
}

public class AtualizarPerfilUsuarioRequest
{
    [Required(ErrorMessage = "Nome é obrigatório")]
    [MaxLength(150)]
    public string Nome { get; set; } = string.Empty;
}

public class AlterarSenhaRequest
{
    public string? SenhaAtual { get; set; }

    [Required(ErrorMessage = "Nova senha é obrigatória")]
    [MinLength(6, ErrorMessage = "Nova senha deve ter no mínimo 6 caracteres")]
    [MaxLength(100)]
    public string NovaSenha { get; set; } = string.Empty;
}

public class SolicitarVerificacaoRequest
{
    [MaxLength(500)]
    public string? Mensagem { get; set; }
}

public class SolicitacaoVerificacaoDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Mensagem { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? ResolvidoEm { get; set; }
}

public class GoogleLoginRequest
{
    [Required(ErrorMessage = "ID token do Google é obrigatório")]
    public string IdToken { get; set; } = string.Empty;
}

public class UsuarioDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool TemSenha { get; set; }
    public List<string> Permissoes { get; set; } = new();
}

public class AuthResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public UsuarioDto Usuario { get; set; } = null!;
}

public class RefreshResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}
