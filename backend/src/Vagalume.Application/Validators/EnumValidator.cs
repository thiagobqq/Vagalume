using Vagalume.Domain.Enums;

namespace Vagalume.Application.Validators;

public static class EnumValidator
{
    public static bool TryParseOrigem(string value, out OrigemCandidatura origem)
        => Enum.TryParse<OrigemCandidatura>(value, ignoreCase: false, out origem) && Enum.IsDefined(origem);

    public static bool TryParseStatus(string value, out StatusCandidatura status)
        => Enum.TryParse<StatusCandidatura>(value, ignoreCase: false, out status) && Enum.IsDefined(status);

    public static bool TryParseFonteTipo(string value, out FonteTipo tipo)
        => Enum.TryParse<FonteTipo>(value, ignoreCase: false, out tipo) && Enum.IsDefined(tipo);

    public static bool TryParseStatusVaga(string value, out StatusVaga status)
        => Enum.TryParse<StatusVaga>(value, ignoreCase: false, out status) && Enum.IsDefined(status);

    public static bool TryParseNivel(string value, out NivelVaga nivel)
        => Enum.TryParse<NivelVaga>(value, ignoreCase: false, out nivel) && Enum.IsDefined(nivel);

    public static bool TryParseModalidade(string value, out ModalidadeVaga modalidade)
        => Enum.TryParse<ModalidadeVaga>(value, ignoreCase: false, out modalidade) && Enum.IsDefined(modalidade);

    public static bool TryParseCombina(string value, out CombinaCriterio combina)
        => Enum.TryParse<CombinaCriterio>(value, ignoreCase: false, out combina) && Enum.IsDefined(combina);

    public static string OrigemValores => string.Join(", ", Enum.GetNames<OrigemCandidatura>());
    public static string StatusValores => string.Join(", ", Enum.GetNames<StatusCandidatura>());
    public static string FonteTipoValores => string.Join(", ", Enum.GetNames<FonteTipo>());
    public static string StatusVagaValores => string.Join(", ", Enum.GetNames<StatusVaga>());
    public static string NivelValores => string.Join(", ", Enum.GetNames<NivelVaga>());
    public static string ModalidadeValores => string.Join(", ", Enum.GetNames<ModalidadeVaga>());
    public static string CombinaValores => string.Join(", ", Enum.GetNames<CombinaCriterio>());
}
