# ADRs — Vagalume (backend)

Decisões de arquitetura do backend do Analista de Busca de Emprego. Cada ADR é imutável: para reverter uma decisão, cria-se um novo ADR que a supersede.

| # | Título | Status |
|---|---|---|
| [0001](ADR-0001-stack-net-sqlserver-docker.md) | Stack: .NET 10, SQL Server, Docker Compose | Aceito |
| [0002](ADR-0002-backend-dita-contrato-api.md) | Backend dita o contrato de API | Aceito |
| [0003](ADR-0003-escopo-fatia-1.md) | Escopo da fatia 1: auth + kanban + currículos | Aceito |
| [0004](ADR-0004-candidatura-flat-sem-entidade-vaga.md) | Candidatura flat — sem entidade Vaga na fatia 1 | Aceito |
| [0005](ADR-0005-maquina-de-estados-kanban.md) | Máquina de estados do kanban: transições livres, terminal único | Aceito |
| [0006](ADR-0006-eventos-append-only-de-status.md) | Histórico de status como eventos append-only | Aceito |
| [0007](ADR-0007-campo-origem-desde-dia-1.md) | Campo Origem da candidatura desde o dia 1 | Aceito |
| [0008](ADR-0008-curriculos-multiplas-versoes.md) | Currículos: múltiplos por usuário, ativo único, arquivo em volume | Aceito |
| [0009](ADR-0009-auth-identity-jwt-refresh.md) | Auth: Identity + JWT + refresh rotativo, registro público, sem roles | Aceito |
| [0010](ADR-0010-sem-auditoria-generica-fatia-1.md) | Sem interceptor de auditoria genérico na fatia 1 | Aceito |
| [0011](ADR-0011-envelope-de-resposta-consistente.md) | Envelope de resposta consistente `GenericResponseData<T>` | Aceito |
| [0012](ADR-0012-contrato-api-fatia-1.md) | Contrato de API da fatia 1 | Aceito |
| [0013](ADR-0013-autorizacao-por-permissoes.md) | Autorização por permissões (sem roles), preparada para paywall | Aceito |
| [0014](ADR-0014-vaga-e-inbox-de-descoberta.md) | Vaga como entidade e inbox de descoberta (revê o ADR-0004) | Aceito |
| [0015](ADR-0015-pipeline-busca-automatica.md) | Pipeline de busca automática: fontes, filtros, agendamento e notificação | Aceito |
| [0016](ADR-0016-fontes-brasileiras-e-scraping.md) | Fontes brasileiras e scraping (complementa o ADR-0015) | Aceito |
| [0017](ADR-0017-execucao-busca-assincrona.md) | Execução de busca assíncrona (fila + 202 + polling) | Aceito |
| [0018](ADR-0018-postgresql-no-lugar-do-sqlserver.md) | PostgreSQL no lugar do SQL Server (supersede o banco do ADR-0001) | Aceito |
| [0019](ADR-0019-gap-analysis-curriculo-vaga-ia.md) | Gap-analysis currículo × vaga com IA | Aceito |
| [0020](ADR-0020-byok-provedor-de-ia-do-usuario.md) | Provedor de IA do usuário (BYOK), presets e cifragem da chave | Aceito |
| [0021](ADR-0021-linkedin-como-fonte-de-vagas.md) | LinkedIn como fonte de vagas (revisa o ADR-0016) | Aceito |
| [0022](ADR-0022-analise-ats-do-curriculo.md) | Análise ATS do currículo (somente IA, por currículo) | Aceito |
