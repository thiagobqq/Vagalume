using System.ComponentModel.DataAnnotations;
using Vagalume.Application.Validators;
using Vagalume.Domain.Busca;
using Vagalume.Domain.Enums;

namespace Vagalume.Application.DTOs;

/// <summary>O workflow envia um lote final (inclusive vazio) ou um motivo de falha.</summary>
public class RetornoAgente : IValidatableObject
{
    public const int MaxVagas = 25;

    [MaxLength(MaxVagas)]
    public List<ItemAgente>? Vagas { get; init; }

    [MaxLength(500)]
    public string? Erro { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if ((Vagas is null ? string.IsNullOrWhiteSpace(Erro) : Erro is not null) || Vagas?.Any(v => v is null) == true)
            yield return new ValidationResult("Envie uma lista de vagas ou um erro.");
        if (Erro?.Contains('\0') == true)
            yield return new ValidationResult("Erro contém caracteres inválidos.");
    }
}

public class ItemAgente : IValidatableObject
{
    [Required, MaxLength(300)] public string Titulo { get; init; } = string.Empty;
    [Required, MaxLength(2048)] public string Url { get; init; } = string.Empty;
    [MaxLength(200)] public string? Empresa { get; init; }
    [MaxLength(200)] public string? Localizacao { get; init; }
    [MaxLength(20000)] public string? Descricao { get; init; }
    public string? Modalidade { get; init; }
    public string? Nivel { get; init; }
    public DateTimeOffset? PublicadaEm { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (!Uri.TryCreate(Url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")
            || uri.UserInfo.Length > 0 || uri.AbsoluteUri.Length > 2048)
            yield return new ValidationResult("A vaga deve ter um link HTTP(S) válido.");
        if (Nivel is not null && !EnumValidator.TryParseNivel(Nivel, out _))
            yield return new ValidationResult("Nível inválido.");
        if (Modalidade is not null && !EnumValidator.TryParseModalidade(Modalidade, out _))
            yield return new ValidationResult("Modalidade inválida.");
        if (new[] { Titulo, Url, Empresa, Localizacao, Descricao }.Any(t => t?.Contains('\0') == true))
            yield return new ValidationResult("A vaga contém caracteres inválidos.");
    }

    public VagaColetada ParaColeta() => new()
    {
        Titulo = Titulo.Trim(), Url = Url.Trim(), Empresa = Empresa, Localizacao = Localizacao,
        Descricao = Descricao, PublicadaEm = PublicadaEm?.UtcDateTime,
        Modalidade = Modalidade is null ? ModalidadeVaga.NaoInformado : Enum.Parse<ModalidadeVaga>(Modalidade),
        Nivel = Nivel is null ? NivelVaga.NaoInformado : Enum.Parse<NivelVaga>(Nivel)
    };
}
