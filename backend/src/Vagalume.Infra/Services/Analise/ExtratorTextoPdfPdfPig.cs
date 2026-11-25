using System.Text;
using UglyToad.PdfPig;
using Vagalume.Domain.Abstractions;

namespace Vagalume.Infra.Services.Analise;

public class ExtratorTextoPdfPdfPig : IExtratorTextoPdf
{
    private const int MaxPaginas = 30;
    private const int MaxCaracteres = 60_000;

    public Task<string> ExtrairAsync(Stream pdf, CancellationToken ct = default)
        => Task.Run(() =>
        {
            try
            {
                var sb = new StringBuilder();
                using var documento = PdfDocument.Open(pdf);
                var paginasLidas = 0;

                foreach (var pagina in documento.GetPages())
                {
                    ct.ThrowIfCancellationRequested();
                    if (++paginasLidas > MaxPaginas || sb.Length >= MaxCaracteres)
                        break;

                    if (!string.IsNullOrWhiteSpace(pagina.Text))
                        sb.AppendLine(pagina.Text);
                }

                var texto = sb.ToString().Trim();
                return texto.Length <= MaxCaracteres ? texto : texto[..MaxCaracteres];
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidDataException("O arquivo PDF está corrompido, protegido por senha ou em formato inválido.", ex);
            }
        }, ct);
}
