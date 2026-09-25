using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Vagalume.Domain.Abstractions;
using Vagalume.Domain.Analise;

namespace Vagalume.Infra.Services.Analise;

public class AnalisadorAtsIa : IAnalisadorAts
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private readonly ClienteChatIa _cliente;
    private readonly IaOptions _options;

    public AnalisadorAtsIa(ClienteChatIa cliente, IOptions<IaOptions> options)
    {
        _cliente = cliente;
        _options = options.Value;
    }

    public async Task<ResultadoAnaliseAts> AnalisarAsync(
        string textoCurriculo,
        ProvedorIaConfig provedor,
        CancellationToken ct = default)
    {
        var curriculo = ClienteChatIa.Truncar(textoCurriculo, _options.MaxCharsCurriculo);

        var user = new StringBuilder()
            .AppendLine("CURRÍCULO")
            .AppendLine(curriculo)
            .ToString();

        var content = await _cliente.EnviarAsync(provedor, SystemPrompt, user, jsonMode: true, ct, _options.MaxTokensAts);
        return Interpretar(content);
    }

    internal static ResultadoAnaliseAts Interpretar(string content)
    {
        RespostaAnalise? resposta;
        try
        {
            resposta = JsonSerializer.Deserialize<RespostaAnalise>(ClienteChatIa.ExtrairJson(content), JsonOpts);
        }
        catch (JsonException)
        {
            throw new AnalisadorIaException("A IA não retornou um JSON de análise válido.");
        }

        if (resposta is null)
            throw new AnalisadorIaException("A IA não retornou uma análise válida.");

        return new ResultadoAnaliseAts
        {
            Score = Math.Clamp(resposta.Score, 0, 100),
            Resumo = resposta.Resumo?.Trim() ?? string.Empty,
            Categorias = (resposta.Categorias ?? new())
                .Where(c => !string.IsNullOrWhiteSpace(c.Categoria))
                .Select(c => new CategoriaAts
                {
                    Categoria = c.Categoria!.Trim(),
                    Nota = Math.Clamp(c.Nota, 0, 100),
                    Diagnostico = c.Diagnostico?.Trim() ?? string.Empty,
                    Recomendacoes = (c.Recomendacoes ?? new())
                        .Where(r => !string.IsNullOrWhiteSpace(r))
                        .Select(r => r.Trim())
                        .ToList()
                })
                .ToList(),
            Alertas = (resposta.Alertas ?? new())
                .Where(a => !string.IsNullOrWhiteSpace(a.Problema))
                .Select(a => new AlertaAts
                {
                    Severidade = NormalizarSeveridade(a.Severidade),
                    Problema = a.Problema!.Trim(),
                    ComoCorrigir = a.ComoCorrigir?.Trim() ?? string.Empty
                })
                .ToList(),
            PontosFortes = (resposta.PontosFortes ?? new())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim())
                .ToList(),
            Sugestoes = (resposta.Sugestoes ?? new())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .ToList()
        };
    }

    private static string NormalizarSeveridade(string? severidade)
    {
        var s = severidade?.Trim().ToLowerInvariant();
        return s is "alta" or "media" or "baixa" ? s : "media";
    }

    private const string SystemPrompt = """
Você é um especialista sênior em recrutamento e seleção e em sistemas ATS (Applicant Tracking Systems) usados no mercado brasileiro (Gupy, Kenoby, Solides, Vagas.com, Catho, entre outros). Analise APENAS o currículo fornecido e avalie o quão bem ele é lido e ranqueado por um ATS.
Responda EXCLUSIVAMENTE com um objeto JSON válido, sem texto fora do JSON, no formato:
{"score": 0, "resumo": "", "categorias": [{"categoria": "", "nota": 0, "diagnostico": "", "recomendacoes": [""]}], "alertas": [{"severidade": "alta", "problema": "", "comoCorrigir": ""}], "pontosFortes": [""], "sugestoes": [""]}
Regras:
- "score": inteiro de 0 a 100 representando a prontidão geral do currículo para um ATS brasileiro.
- "resumo": 1 a 3 frases objetivas sobre o resultado.
- "categorias": avalie exatamente estas 6 categorias, uma por item:
  1. "Palavras-chave e competências" — aderência a termos que recrutadores e ATS buscam (cargos, tecnologias, ferramentas, certificações).
  2. "Formatação e leitura por ATS" — uso de colunas, tabelas, imagens, cabeçalhos, fontes e ordem de leitura.
  3. "Dados de contato" — presença de nome, e-mail, telefone, LinkedIn e localização.
  4. "Experiência e resultados" — cargos, período, responsabilidades e resultados quantificados.
  5. "Formação e cursos" — formação acadêmica, cursos e certificações relevantes.
  6. "Idiomas e habilidades" — idiomas com nível e habilidades técnicas/comportamentais.
  Cada categoria tem "nota" inteira de 0 a 100, "diagnostico" curto e "recomendacoes" (1 a 3 ações práticas).
- "alertas": problemas que podem eliminar o currículo automaticamente ou prejudicar muito a leitura, com "severidade" em "alta", "media" ou "baixa", "problema" e "comoCorrigir".
- "pontosFortes": 2 a 4 pontos positivos concretos do currículo.
- "sugestoes": ajustes pontuais e específicos; não reescreva o currículo inteiro.
- Escreva tudo em português do Brasil.
- Baseie-se apenas no conteúdo fornecido; nunca invente experiências, formações ou resultados. Se uma informação não estiver no currículo, trate como ausente.
""";

    private sealed class RespostaAnalise
    {
        public int Score { get; set; }
        public string? Resumo { get; set; }
        public List<CategoriaItem>? Categorias { get; set; }
        public List<AlertaItem>? Alertas { get; set; }
        public List<string>? PontosFortes { get; set; }
        public List<string>? Sugestoes { get; set; }
    }

    private sealed class CategoriaItem
    {
        public string? Categoria { get; set; }
        public int Nota { get; set; }
        public string? Diagnostico { get; set; }
        public List<string>? Recomendacoes { get; set; }
    }

    private sealed class AlertaItem
    {
        public string? Severidade { get; set; }
        public string? Problema { get; set; }
        public string? ComoCorrigir { get; set; }
    }
}
