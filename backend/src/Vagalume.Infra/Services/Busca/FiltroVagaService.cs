using Vagalume.Domain.Busca;
using Vagalume.Domain.Entities;
using Vagalume.Domain.Enums;

namespace Vagalume.Infra.Services.Busca;

public class FiltroVagaService
{
    public bool Aprovar(VagaColetada vaga, PerfilBusca perfil, bool estrito = true)
    {
        var texto = TextoVaga.Normalizar(
            $"{vaga.Titulo} {vaga.Empresa} {vaga.Localizacao} {vaga.Descricao}");

        // exclusões têm prioridade
        if (perfil.ExcluirPalavras.Any(t => TextoVaga.ContemTermo(texto, t)))
            return false;

        var positivos = perfil.PalavrasChave.Concat(perfil.Tecnologias)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();

        if (positivos.Count > 0)
        {
            var casados = positivos.Count(t => TextoVaga.ContemTermo(texto, t));
            var ok = perfil.Combina == CombinaCriterio.Todos
                ? casados == positivos.Count
                : casados > 0;
            if (!ok)
                return false;
        }

        if (estrito && perfil.Niveis.Count > 0 && vaga.Nivel != NivelVaga.NaoInformado && !perfil.Niveis.Contains(vaga.Nivel))
            return false;

        if (estrito && perfil.Modalidades.Count > 0 && !perfil.Modalidades.Contains(vaga.Modalidade))
            return false;

        if (!string.IsNullOrWhiteSpace(perfil.Localizacao))
        {
            var alvo = TextoVaga.Normalizar(vaga.Localizacao);
            if (!alvo.Contains(TextoVaga.Normalizar(perfil.Localizacao), StringComparison.Ordinal))
                return false;
        }

        return true;
    }
}
