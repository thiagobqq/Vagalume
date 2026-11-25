using Vagalume.Domain.Enums;

namespace Vagalume.Application.Validators;

public static class EnumValidator
{
    private static bool TryParseNome<T>(string? value, out T parsed) where T : struct, Enum
    {
        parsed = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var first = value[0];
        if (char.IsDigit(first) || first == '-' || first == '+')
            return false;

        return Enum.TryParse(value, ignoreCase: false, out parsed) && Enum.IsDefined(parsed);
    }

    public static bool TryParseOrigem(string value, out OrigemCandidatura origem)
        => TryParseNome(value, out origem);

    public static bool TryParseStatus(string value, out StatusCandidatura status)
        => TryParseNome(value, out status);

    public static bool TryParseFonteTipo(string value, out FonteTipo tipo)
        => TryParseNome(value, out tipo);

    public static bool TryParseStatusVaga(string value, out StatusVaga status)
        => TryParseNome(value, out status);

    public static bool TryParseNivel(string value, out NivelVaga nivel)
        => TryParseNome(value, out nivel);

    public static bool TryParseModalidade(string value, out ModalidadeVaga modalidade)
        => TryParseNome(value, out modalidade);

    public static bool TryParseCombina(string value, out CombinaCriterio combina)
        => TryParseNome(value, out combina);

    public static bool TryParseProvedorIa(string value, out ProvedorIa provedor)
        => TryParseNome(value, out provedor);

    public static string OrigemValores => string.Join(", ", Enum.GetNames<OrigemCandidatura>());
    public static string StatusValores => string.Join(", ", Enum.GetNames<StatusCandidatura>());
    public static string FonteTipoValores => string.Join(", ", Enum.GetNames<FonteTipo>());
    public static string StatusVagaValores => string.Join(", ", Enum.GetNames<StatusVaga>());
    public static string NivelValores => string.Join(", ", Enum.GetNames<NivelVaga>());
    public static string ModalidadeValores => string.Join(", ", Enum.GetNames<ModalidadeVaga>());
    public static string CombinaValores => string.Join(", ", Enum.GetNames<CombinaCriterio>());
    public static string ProvedorIaValores => string.Join(", ", Enum.GetNames<ProvedorIa>());
}
