using Vagalume.Domain.Enums;

namespace Vagalume.Application.Validators;

public static class EnumValidator
{
    public static bool TryParseOrigem(string value, out OrigemCandidatura origem)
        => Enum.TryParse<OrigemCandidatura>(value, ignoreCase: false, out origem) && Enum.IsDefined(origem);

    public static bool TryParseStatus(string value, out StatusCandidatura status)
        => Enum.TryParse<StatusCandidatura>(value, ignoreCase: false, out status) && Enum.IsDefined(status);

    public static string OrigemValores => string.Join(", ", Enum.GetNames<OrigemCandidatura>());
    public static string StatusValores => string.Join(", ", Enum.GetNames<StatusCandidatura>());
}
