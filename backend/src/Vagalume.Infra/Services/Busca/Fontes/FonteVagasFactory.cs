using Vagalume.Domain.Abstractions;
using Vagalume.Domain.Enums;

namespace Vagalume.Infra.Services.Busca.Fontes;

public class FonteVagasFactory
{
    private readonly Dictionary<FonteTipo, IFonteVagas> _porTipo;

    public FonteVagasFactory(IEnumerable<IFonteVagas> fontes)
        => _porTipo = fontes.ToDictionary(f => f.Tipo);

    public IFonteVagas Obter(FonteTipo tipo)
        => _porTipo.TryGetValue(tipo, out var fonte)
            ? fonte
            : throw new InvalidOperationException($"Nenhum provedor registrado para a fonte {tipo}.");
}
