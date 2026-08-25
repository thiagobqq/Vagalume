# Glossário — Vagalume

Termos de domínio do backend do Analista de Busca de Emprego. Definições normativas: quando código, docs ou conversa divergirem, vale o que está aqui.

## Entidades e conceitos

**Usuário (Candidato)** — Pessoa que cria conta no produto. Toda candidatura e todo currículo pertencem a exatamente um usuário. Não há outro papel (sem admin, sem roles) na fatia 1. [ADR-0009]

**Candidatura** — O registro central do produto: *a aplicação de um usuário a uma vaga*. Não confundir com Vaga. É flat: carrega ela mesma empresa, cargo, link, origem e observações. Nasce sempre com status `Aplicado`. [ADR-0004, ADR-0005]

**Vaga** — Conceito do produto (a oportunidade em si), **mas não é entidade de banco na fatia 1**. Os dados da vaga vivem como campos de texto dentro da Candidatura. Usar "vaga" e "candidatura" como sinônimos em código é erro. [ADR-0004]

**Status** — A coluna do kanban em que uma candidatura está: `Aplicado, Triagem, Entrevista, Oferta, Encerrado`. É o valor corrente, denormalizado na Candidatura. [ADR-0005]

**Encerrado** — O único status terminal. Significa "este processo acabou", sem distinguir motivo (recusa, desistência, vaga preenchida) — o motivo, quando informado, vai na observação do evento. Pode ser reaberto (transição livre). [ADR-0005]

**Evento de Transição** — Registro append-only e imutável de uma mudança de status: status anterior, status novo, observação opcional, timestamp. É a única fonte da trajetória da candidatura; base dos follow-ups e das métricas do funil. O status atual deve ser sempre consistente com o último evento. [ADR-0006]

**Origem (Canal)** — Por onde a candidatura aconteceu: `LinkedIn, Gupy, Catho, Indicacao, SiteEmpresa, Outro`. Obrigatória desde o primeiro dia, pois métricas comparativas ("indicação converte mais") dependem dela. [ADR-0007]

**Currículo** — Arquivo PDF enviado pelo usuário (≤10MB), com metadados no banco e arquivo em volume. Um usuário tem vários; exatamente **um é o ativo**. O upload novo sempre vira o ativo. [ADR-0008]

**Currículo Ativo** — O currículo que o sistema considera "o atual" do usuário; alvo das features futuras de comparação (gap-analysis). Invariante: um e apenas um ativo por usuário. [ADR-0008]

## Escopo e processo

**Fatia 1** — Primeira entrega de ponta a ponta: auth + kanban de candidaturas + upload de currículos. Tudo fora disso (extração por URL, follow-ups, gap-analysis, prep de entrevista, métricas, pagamentos, exclusões) está explicitamente adiado. [ADR-0003]

**Extração automática** — Feature futura: ler a página de uma vaga pelo link e preencher os campos da candidatura. Não existe na fatia 1; maior risco técnico do produto. [ADR-0003, ADR-0004]

**Gap-analysis** — Feature futura (paga): comparar requisitos da candidatura com o currículo ativo, apontando lacunas específicas. Depende de parse do PDF. [ADR-0008]

## Técnico

**Envelope (`GenericResponseData<T>`)** — Shape único de resposta da API: `{ success, message, data }`, sempre com status HTTP coerente. Nenhum endpoint retorna corpo fora do envelope. [ADR-0011]

**Refresh rotativo** — Refresh token persistido que é substituído a cada uso; o antigo invalida. Padrão herdado do base (Intranet). [ADR-0009]

**Base (Intranet)** — Codebase de referência do autor (`Intranet-backend`), do qual este projeto replica arquitetura e padrões: 4 projetos, Controllers, Identity, JWT+refresh, EF Core, DotNetEnv, Swagger. Não confundir: é referência, não dependência.

**Volume de uploads** — Diretório `/app/uploads` montado como volume docker separado do volume de dados do SQL Server. Onde os PDFs vivem; o banco guarda só metadados. [ADR-0008]

**VPS** — Destino de produção planejado; o docker-compose local já desenha o cenário (sqlserver + api + volumes + healthcheck). [ADR-0001]
