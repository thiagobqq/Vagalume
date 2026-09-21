# ADR-0017 — Execução de busca assíncrona (fila + 202 + polling)

**Status:** Aceito (2026-09-21)

## Contexto

O endpoint `POST /api/busca/executar` executava todo o pipeline de forma síncrona. Com 5 fontes, delays e buscas de detalhe, a execução leva dezenas de segundos (medido: ~75–90s, podendo passar disso com mais termos/fontes). O nginx do frontend corta em `proxy_read_timeout 90s` e devolve **504**; além disso, o cancelamento da requisição abortava a execução no meio.

O agendador (`BuscaVagasHostedService`) já rodava em background, mas o disparo manual e o agendado não eram coordenados.

## Decisão

- **Disparo assíncrono:** `POST /api/busca/executar` cria a `ExecucaoBusca` (`FinalizadaEm = null`) e a enfileira, respondendo **`202 Accepted`** imediatamente com a execução. Não há mais trabalho pesado no request.
- **Fila + worker:** `IBuscaQueue` sobre `System.Threading.Channels` (sem dependência nova) e `BuscaQueueHostedService` consumindo com concorrência limitada (`Busca:MaxConcorrencia`). `Busca:TimeoutExecucaoSegundos` limita a execução inteira.
- **Agendador unificado:** `BuscaVagasHostedService` deixa de executar e passa a **enfileirar** os usuários vencidos — mesmo caminho do botão "buscar agora".
- **Coalescência:** `BuscaEnfileirador` (usado pelos dois gatilhos) verifica, sob trava por usuário (`BuscaExecucaoCoordinator`), se já existe execução com `FinalizadaEm == null`; se houver, devolve a existente (idempotente, evita clique duplo). Sem perfil/fonte ativos → `400`. Fila cheia → `503`.
- **Status sem migration:** "em andamento" é inferido de `FinalizadaEm == null` (a linha é criada no enfileiramento), então o polling já enxerga a execução.
- **Polling:** `GET /api/busca/execucoes/{id}` devolve o estado; o frontend consulta a cada ~3s e atualiza lista/métricas ao concluir. Ao recarregar, o frontend retoma o acompanhamento se houver execução em andamento.
- **Órfãs:** no startup, execuções com `FinalizadaEm == null` (interrompidas por restart/crash) são marcadas como falha, para a UI não ficar "buscando" para sempre.
- **Timeout por fonte:** cada provedor roda com um `CancellationTokenSource` de `Busca:TimeoutFonteSegundos`, para uma fonte lenta não consumir o orçamento inteiro.
- **Caps menores** em `Busca:Scraping` (`MaxItens` 25, `MaxPaginas` 2, `MaxDetalhes` 10, `DelayMs` 500) para execuções mais curtas.

`POST /api/busca/executar` → `202` (ou `200`-equivalente quando já em andamento), `GET /api/busca/execucoes/{id}` para o polling, `GET /api/busca/execucoes` para o histórico.

## Consequências

- Fim do 504 e da execução abortada por desconexão do cliente; o resultado é persistido independentemente da conexão.
- O `ColetaVagasService` passou a receber o `execucaoId` (a linha é criada pelo enfileirador), permitindo o polling imediato.
- Coordenação é in-process (`Channel` + trava por usuário): suficiente para uma instância; múltiplas instâncias exigirão lock distribuído/fila externa (mesma pendência anotada no ADR-0015).
- Fila em memória é perdida em restart — mitigado pela limpeza de órfãs; execuções enfileiradas e não iniciadas simplesmente somem (o usuário dispara de novo ou o agendador reenfileira).
