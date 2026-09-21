using System.Text;
using UglyToad.PdfPig;
using Vagalume.Domain.Abstractions;

namespace Vagalume.Infra.Services.Analise;

public class ExtratorTextoPdfPdfPig : IExtratorTextoPdf
{
    public Task<string> ExtrairAsync(Stream pdf, CancellationToken ct = default)
        => Task.Run(() =>
        {
            var sb = new StringBuilder();
            using var documento = PdfDocument.Open(pdf);
            foreach (var pagina in documento.GetPages())
            {
                ct.ThrowIfCancellationRequested();
                if (!string.IsNullOrWhiteSpace(pagina.Text))
                    sb.AppendLine(pagina.Text);
            }
            return sb.ToString().Trim();
        }, ct);
}
