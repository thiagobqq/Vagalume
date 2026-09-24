using Vagalume.Domain.Busca;
using Vagalume.Domain.Entities;
using Vagalume.Domain.Enums;
using Vagalume.Infra.Services.Busca;
using Xunit;

namespace Vagalume.UnitTests;

public class FiltroVagaServiceTests
{
    private readonly FiltroVagaService _filtro = new();

    private static VagaColetada Vaga(
        string titulo = "Desenvolvedor Backend",
        string? empresa = "Acme",
        string? localizacao = "São Paulo - SP",
        string? descricao = null,
        NivelVaga nivel = NivelVaga.NaoInformado,
        ModalidadeVaga modalidade = ModalidadeVaga.NaoInformado) => new()
    {
        Titulo = titulo,
        Url = "https://exemplo.com/vaga/1",
        Empresa = empresa,
        Localizacao = localizacao,
        Descricao = descricao,
        Nivel = nivel,
        Modalidade = modalidade
    };

    private static PerfilBusca Perfil(
        List<string>? palavrasChave = null,
        List<string>? tecnologias = null,
        List<string>? excluir = null,
        List<NivelVaga>? niveis = null,
        List<ModalidadeVaga>? modalidades = null,
        string? localizacao = null,
        CombinaCriterio combina = CombinaCriterio.Qualquer) => new()
    {
        PalavrasChave = palavrasChave ?? new(),
        Tecnologias = tecnologias ?? new(),
        ExcluirPalavras = excluir ?? new(),
        Niveis = niveis ?? new(),
        Modalidades = modalidades ?? new(),
        Localizacao = localizacao,
        Combina = combina
    };

    [Fact]
    public void Aprovar_SemCriteriosPositivos_RetornaTrue()
    {
        Assert.True(_filtro.Aprovar(Vaga(), Perfil()));
    }

    [Fact]
    public void Aprovar_PalavraChavePresente_RetornaTrue()
    {
        var perfil = Perfil(palavrasChave: new List<string> { "backend" });

        Assert.True(_filtro.Aprovar(Vaga(), perfil));
    }

    [Fact]
    public void Aprovar_PalavraChaveAusente_RetornaFalse()
    {
        var perfil = Perfil(palavrasChave: new List<string> { "frontend" });

        Assert.False(_filtro.Aprovar(Vaga(), perfil));
    }

    [Fact]
    public void Aprovar_ExclusaoTemPrioridade()
    {
        var perfil = Perfil(
            palavrasChave: new List<string> { "backend" },
            excluir: new List<string> { "senior" });

        Assert.False(_filtro.Aprovar(Vaga(descricao: "Vaga backend senior"), perfil));
    }

    [Fact]
    public void Aprovar_CombinaTodos_ExigeTodosOsTermos()
    {
        var perfil = Perfil(
            palavrasChave: new List<string> { "backend", "csharp" },
            combina: CombinaCriterio.Todos);

        Assert.False(_filtro.Aprovar(Vaga(descricao: "Somente backend"), perfil));
        Assert.True(_filtro.Aprovar(Vaga(descricao: "backend com csharp"), perfil));
    }

    [Fact]
    public void Aprovar_CombinaQualquer_BastaUmTermo()
    {
        var perfil = Perfil(
            palavrasChave: new List<string> { "backend", "csharp" },
            combina: CombinaCriterio.Qualquer);

        Assert.True(_filtro.Aprovar(Vaga(descricao: "Somente backend"), perfil));
    }

    [Fact]
    public void Aprovar_NivelForaDoPerfil_Estrito_RetornaFalse()
    {
        var perfil = Perfil(niveis: new List<NivelVaga> { NivelVaga.Junior });

        Assert.False(_filtro.Aprovar(Vaga(nivel: NivelVaga.Senior), perfil));
    }

    [Fact]
    public void Aprovar_NivelForaDoPerfil_NaoEstrito_RetornaTrue()
    {
        var perfil = Perfil(niveis: new List<NivelVaga> { NivelVaga.Junior });

        Assert.True(_filtro.Aprovar(Vaga(nivel: NivelVaga.Senior), perfil, estrito: false));
    }

    [Fact]
    public void Aprovar_NivelNaoInformado_NaoEhFiltrado()
    {
        var perfil = Perfil(niveis: new List<NivelVaga> { NivelVaga.Junior });

        Assert.True(_filtro.Aprovar(Vaga(nivel: NivelVaga.NaoInformado), perfil));
    }

    [Fact]
    public void Aprovar_ModalidadeForaDoPerfil_Estrito_RetornaFalse()
    {
        var perfil = Perfil(modalidades: new List<ModalidadeVaga> { ModalidadeVaga.Remoto });

        Assert.False(_filtro.Aprovar(Vaga(modalidade: ModalidadeVaga.Presencial), perfil));
    }

    [Fact]
    public void Aprovar_ModalidadeForaDoPerfil_NaoEstrito_RetornaTrue()
    {
        var perfil = Perfil(modalidades: new List<ModalidadeVaga> { ModalidadeVaga.Remoto });

        Assert.True(_filtro.Aprovar(Vaga(modalidade: ModalidadeVaga.Presencial), perfil, estrito: false));
    }

    [Fact]
    public void Aprovar_LocalizacaoCompativel_IgnoraAcentos()
    {
        var perfil = Perfil(localizacao: "sao paulo");

        Assert.True(_filtro.Aprovar(Vaga(localizacao: "São Paulo - SP"), perfil));
    }

    [Fact]
    public void Aprovar_LocalizacaoIncompativel_RetornaFalse()
    {
        var perfil = Perfil(localizacao: "sao paulo");

        Assert.False(_filtro.Aprovar(Vaga(localizacao: "Rio de Janeiro"), perfil));
    }
}
