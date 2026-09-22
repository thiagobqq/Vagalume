# ADR-0015 — Pipeline de busca automática: fontes, filtros, agendamento e notificação

**Status:** Aceito (2026-09-21) · **Complementado por:** [ADR-0016](ADR-0016-fontes-brasileiras-e-scraping.md) (fontes brasileiras e scraping)

## Contexto

O módulo de busca (ADR-0014) precisa coletar vagas periodicamente em múltiplas fontes, extrair campos, filtrar pelos critérios do usuário, deduplicar, persistir, medir e (opcionalmente) notificar. Resta decidir de onde vêm as vagas, como agendar, como filtrar e como notificar.

Scraping de portais (LinkedIn/Gupy/Catho) é o maior risco do produto (ADR-0003): bloqueio, ToS e manutenção contínua. Boards de ATS (Greenhouse, Lever, Ashby) expõem **APIs JSON públicas por empresa**, determinísticas e sem anti-bot.

## Decisão

### Fontes (provedores plugáveis)
- Abstração `IFonteVagas` (`FonteTipo`, `BuscarAsync`) e uma `FonteVagasFactory` que resolve o provedor pelo tipo. O pipeline não conhece provedores concretos.
- Provedores da fatia: **Greenhouse, Lever, Ashby** (APIs públicas) e **RSS** (RSS 2.0/Atom genérico, parse sem dependência nova). Scraping de portais fica para fatia isolada, adicionável sem tocar no pipeline.
- Fontes são configuradas pelo usuário (`FonteBusca`): tipo, identificador (board token ou URL do feed), nome da empresa e um nome de exibição.
- HTTP via `IHttpClientFactory` (cliente nomeado `busca`, timeout e User-Agent configuráveis). Falha em uma fonte é isolada e registrada; não derruba a execução.

### Normalização e filtros
- `NormalizadorVaga` infere modalidade (remoto/híbrido/presencial) e nível (júnior/pleno/sênior) a partir de localização, título e descrição, e limpa HTML (inclusive duplamente escapado) para texto.
- `FiltroVagaService` usa normalização (sem acentos, minúsculas) que **preserva tokens técnicos** (`C#`, `.NET`, `ASP.NET`, `Node.js`) e casamento por fronteira de palavra. Critérios: palavras-chave ∪ tecnologias (OR, ou AND quando `Combina=Todos`), exclusões com prioridade, nível, modalidade e localização.
- Modalidade/nível desconhecidos: nível desconhecido passa (leniente); modalidade precisa casar quando o usuário restringe (ex.: "só remoto").
- `PerfilBusca` guarda listas (palavras-chave, tecnologias, níveis, modalidades, exclusões), localização, `Combina`, agenda e webhook do Discord. Listas são persistidas como JSON (estilo flat do ADR-0004).

### Deduplicação e persistência
- `DedupService` carrega o índice do usuário (URLs normalizadas + hashes) e o pipeline também deduplica dentro do próprio lote. Índices únicos no banco são a garantia final contra corrida.
- Nova vaga entra com `Status=Nova`. Métricas da execução (`ExecucaoBusca`): encontradas, aprovadas, duplicadas, adicionadas, além de detalhe/erro por fonte.

### Agendamento
- `BuscaVagasHostedService : BackgroundService` no próprio processo, tick configurável (`Busca:IntervaloMinutos`), concorrência limitada (`Busca:MaxConcorrencia`) e guarda anti-sobreposição por usuário.
- "Vencido" é calculado por `UltimaExecucaoEm` no fuso do usuário (`FusoHorario`, default `America/Sao_Paulo`): `DiasSemana` (formato `DayOfWeek` "0..6") + `HoraExecucao`. Execução perdida por queda é recuperada na volta (uma vez).
- Endpoint manual `POST /api/busca/executar` (botão "buscar agora") reusa o mesmo serviço.

### Notificação
- `INotificadorBusca`; a inbox é a notificação interna. `NotificadorBusca` envia resumo ao Discord quando o usuário configura um webhook (nunca logado). E-mail fica para fatia futura (sem infra de e-mail no projeto, ADR-0009).

### Acesso
- Todo o módulo atrás da policy `Permissoes.VerifyUser` (`verify_user`, ADR-0013). O paywall futuro troca a fonte do entitlement, sem tocar no módulo.

## Consequências

- Pipeline fechado de ponta a ponta sem scraping e sem dependências novas (só `IHttpClientFactory` e `System.Xml.Linq`).
- Adicionar uma fonte = implementar `IFonteVagas` e registrar no DI; filtros/dedup/persistência/notificação não mudam.
- Agendamento in-process é suficiente para VPS single-instance; múltiplas instâncias exigirão lock distribuído (pendência registrada).
- Sem cache no `IEntitlementService` e sem testes automatizados no repo (a lógica pura de normalização/filtro/dedup é testável por design).
- Portais com scraping e similaridade de conteúdo/AI entram por ADR novo (tier pago).
