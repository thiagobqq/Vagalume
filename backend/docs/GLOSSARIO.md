# Glossário — Vagalume

Termos de domínio do backend do Analista de Busca de Emprego. Definições normativas: quando código, docs ou conversa divergirem, vale o que está aqui.

## Entidades e conceitos

**Usuário (Candidato)** — Pessoa que cria conta no produto. Toda candidatura e todo currículo pertencem a exatamente um usuário. Não há outro papel (sem admin, sem roles) na fatia 1. [ADR-0009]

**Candidatura** — O registro central do produto: *a aplicação de um usuário a uma vaga*. Não confundir com Vaga. É flat: carrega ela mesma empresa, cargo, link, origem e observações. Nasce sempre com status `Aplicado`. [ADR-0004, ADR-0005]

**Vaga** — A oportunidade em si, agora uma **entidade própria** (`Vaga`), distinta de Candidatura. Nasce da busca automática e vive na inbox até o usuário aplicar. Tem título, empresa, localização, modalidade, nível, URL, descrição, fonte, status próprio (`Nova, Vista, Aplicada, Descartada`) e vínculo opcional com a candidatura. Usar "vaga" e "candidatura" como sinônimos em código é erro. [ADR-0004 revisto por ADR-0014]

**Status** — A coluna do kanban em que uma candidatura está: `Aplicado, Triagem, Entrevista, Oferta, Encerrado`. É o valor corrente, denormalizado na Candidatura. [ADR-0005]

**Encerrado** — O único status terminal. Significa "este processo acabou", sem distinguir motivo (recusa, desistência, vaga preenchida) — o motivo, quando informado, vai na observação do evento. Pode ser reaberto (transição livre). [ADR-0005]

**Evento de Transição** — Registro append-only e imutável de uma mudança de status: status anterior, status novo, observação opcional, timestamp. É a única fonte da trajetória da candidatura; base dos follow-ups e das métricas do funil. O status atual deve ser sempre consistente com o último evento. [ADR-0006]

**Origem (Canal)** — Por onde a candidatura aconteceu: `LinkedIn, Gupy, Catho, Indicacao, SiteEmpresa, Outro`. Obrigatória desde o primeiro dia, pois métricas comparativas ("indicação converte mais") dependem dela. [ADR-0007]

**Currículo** — Arquivo PDF enviado pelo usuário (≤10MB), com metadados no banco e arquivo em volume. Um usuário tem vários; exatamente **um é o ativo**. O upload novo sempre vira o ativo. [ADR-0008]

**Currículo Ativo** — O currículo que o sistema considera "o atual" do usuário; alvo das features futuras de comparação (gap-analysis). Invariante: um e apenas um ativo por usuário. [ADR-0008]

**Fonte de Busca** — Origem de vagas configurada pelo usuário (`FonteBusca`): tipo (`Gupy, Trampos, Programathor, InfoJobs, Catho, Rss`), identificador opcional (Gupy = slug de empresa; RSS = URL do feed), nome da empresa opcional e ativo/inativo. Gupy e Trampos usam APIs públicas; Programathor/InfoJobs/Catho usam scraping HTML; RSS é genérico. [ADR-0015, ADR-0016]

**Perfil de Busca** — Critérios e agenda do usuário para a busca automática (`PerfilBusca`): palavras-chave, tecnologias, níveis, modalidades, exclusões, localização, como combinar os termos (`Qualquer`/`Todos`), hora e dias de execução, fuso e webhook do Discord. [ADR-0015]

**Execução de Busca** — Registro de uma rodada de coleta (`ExecucaoBusca`) com as métricas: encontradas, aprovadas, duplicadas, adicionadas, além de erro/detalhe por fonte. Base do card "última busca". [ADR-0015]

**Inbox "Novas"** — A lista de vagas descobertas com status `Nova`. É a "coluna Novas" do produto; não é uma coluna do kanban de candidaturas (que nasce em `Aplicado`). [ADR-0014]

**Deduplicação de Vaga** — Impede gravar a mesma vaga duas vezes para o mesmo usuário, por URL normalizada (sem parâmetros de rastreio) e por hash de `empresa|título|localização`. Índices únicos garantem a invariante no banco. [ADR-0014]

**Busca automática** — Coleta periódica de vagas em fontes configuradas, com normalização, filtragem pelos critérios, deduplicação, persistência e notificação opcional. Fontes brasileiras (portais e tech), algumas via API e outras via scraping. Substitui a antiga "extração automática" adiada no ADR-0003. [ADR-0014, ADR-0015, ADR-0016]

**Scraping** — Leitura de HTML público de portais sem API (Programathor, InfoJobs, Catho) com AngleSharp, rate-limit conservador e User-Agent de navegador (portais com WAF bloqueiam UA de bot). Provedores isolados: falha de um site não derruba a execução. [ADR-0016]

## Escopo e processo

**Fatia 1** — Primeira entrega de ponta a ponta: auth + kanban de candidaturas + upload de currículos. Tudo fora disso (extração por URL, follow-ups, gap-analysis, prep de entrevista, métricas, pagamentos, exclusões) está explicitamente adiado. [ADR-0003]

**Extração automática** — Leitura/coleta de dados de vaga sem digitação manual. Implementada como **busca automática** (ver acima): coleta em fontes configuradas e preenche a Vaga. [ADR-0014, ADR-0015]

**Gap-analysis** — Feature futura (paga): comparar requisitos da candidatura com o currículo ativo, apontando lacunas específicas. Depende de parse do PDF. [ADR-0008]

## Técnico

**Envelope (`GenericResponseData<T>`)** — Shape único de resposta da API: `{ success, message, data }`, sempre com status HTTP coerente. Nenhum endpoint retorna corpo fora do envelope. [ADR-0011]

**Refresh rotativo** — Refresh token persistido que é substituído a cada uso; o antigo invalida. Padrão herdado do base (Intranet). [ADR-0009]

**Base (Intranet)** — Codebase de referência do autor (`Intranet-backend`), do qual este projeto replica arquitetura e padrões: 4 projetos, Controllers, Identity, JWT+refresh, EF Core, DotNetEnv, Swagger. Não confundir: é referência, não dependência.

**Volume de uploads** — Diretório `/app/uploads` montado como volume docker separado do volume de dados do SQL Server. Onde os PDFs vivem; o banco guarda só metadados. [ADR-0008]

**VPS** — Destino de produção planejado; o docker-compose local já desenha o cenário (sqlserver + api + volumes + healthcheck). [ADR-0001]
