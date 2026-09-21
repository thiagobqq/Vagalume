namespace Vagalume.Domain.Abstractions;

public interface ICriptografiaService
{
    string Proteger(string texto);
    string? Revelar(string? texto);
}
