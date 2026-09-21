namespace Vagalume.Domain.Analise;

public class AnalisadorIaException : Exception
{
    public int? StatusProvedor { get; }

    public AnalisadorIaException(string message, int? statusProvedor = null)
        : base(message)
    {
        StatusProvedor = statusProvedor;
    }
}
