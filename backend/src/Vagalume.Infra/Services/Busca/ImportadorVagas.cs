using Vagalume.Domain.Abstractions;
using Vagalume.Domain.Busca;
using Vagalume.Domain.Entities;
using Vagalume.Domain.Enums;
using Vagalume.Infra.Data;

namespace Vagalume.Infra.Services.Busca;

/// <summary>Aplica o mesmo filtro e índice de duplicatas a todas as fontes.</summary>
public class ImportadorVagas(AppDbContext db, DedupService dedup, FiltroVagaService filtro, IDateTimeProvider clock)
{
    public List<Vaga> Adicionar(IReadOnlyList<VagaColetada> coletadas, FonteBusca fonte, PerfilBusca perfil,
        ExecucaoBusca execucao, ResultadoFonte detalhe, IndiceDedup indice, int limite)
    {
        var novas = new List<Vaga>();
        detalhe.Encontradas = coletadas.Count;
        execucao.Encontradas += coletadas.Count;

        foreach (var cru in coletadas)
        {
            if (detalhe.Adicionadas >= limite)
                break;

            var normalizada = NormalizadorVaga.Normalizar(cru, fonte);

            if (!filtro.Aprovar(normalizada, perfil))
                continue;

            detalhe.Aprovadas++;
            execucao.Aprovadas++;

            var url = dedup.UrlNormalizada(normalizada);
            var hash = dedup.CalcularHash(normalizada);

            if (indice.Contem(url, hash))
            {
                detalhe.Duplicadas++;
                execucao.Duplicadas++;
                continue;
            }

            indice.Adicionar(url, hash);

            var empresa = NormalizadorVaga.Trim(normalizada.Empresa ?? fonte.NomeEmpresa ?? "Empresa não informada", NormalizadorVaga.MaxEmpresa)
                ?? "Empresa não informada";
            var fonteNome = NormalizadorVaga.Trim(detalhe.FonteNome, 200) ?? fonte.Tipo.ToString();
            var urlNorm = NormalizadorVaga.Trim(url, NormalizadorVaga.MaxUrl) ?? string.Empty;

            var entidade = new Vaga
            {
                Id = Guid.NewGuid(),
                UsuarioId = execucao.UsuarioId,
                Titulo = normalizada.Titulo,
                Empresa = empresa,
                Localizacao = normalizada.Localizacao,
                Modalidade = normalizada.Modalidade,
                Nivel = normalizada.Nivel,
                Url = normalizada.Url,
                UrlNormalizada = urlNorm,
                Descricao = normalizada.Descricao,
                PublicadaEm = normalizada.PublicadaEm,
                FonteTipo = fonte.Tipo,
                FonteNome = fonteNome,
                HashDedup = hash,
                Status = StatusVaga.Nova,
                CriadoEm = clock.UtcNow
            };

            db.Vagas.Add(entidade);
            novas.Add(entidade);

            detalhe.Adicionadas++;
            execucao.Adicionadas++;
        }

        return novas;
    }
}
