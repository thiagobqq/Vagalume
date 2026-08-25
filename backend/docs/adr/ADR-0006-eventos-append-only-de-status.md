# ADR-0006 — Histórico de status como eventos append-only

**Status:** Aceito (2026-08-25)

## Contexto

Dois requisitos do produto dependem de saber **quando** cada transição de status aconteceu:

- Fluxo 2 (follow-up): "já se passaram X dias desde que você aplicou" exige timestamp da candidatura e da última movimentação.
- Fluxo 5 (métricas): "tempo médio de resposta", "onde parou na triagem", taxas de conversão entre etapas — todas derivadas de durações entre transições.

Alternativa considerada: guardar só o status atual + `UpdatedAt`. Barato, mas destrói a informação temporal para sempre — e não há como reconstruir depois.

## Decisão

Toda mudança de status gera um **evento append-only** (`EventoTransicaoCandidatura`): `CandidaturaId`, `StatusAnterior`, `StatusNovo`, `Observacao` (opcional — ex.: motivo do encerramento), `CriadoEm` (via `DateTimeProvider` do base, clock abstrato).

- A candidatura nasce com um evento inicial (`null → Aplicado`) registrado na criação.
- O status atual é mantido denormalizado na `Candidatura` (para listagem do kanban sem agregação) e **deve** ser consistente com o último evento — a escrita de ambos acontece na mesma transação.
- Eventos nunca são editados nem removidos. Não há exclusão de candidatura na fatia 1 (ADR-0003), então não há cascata a definir ainda.

## Consequências

- Custo imediato mínimo (uma tabela e um insert a mais por transição); retorno: follow-ups e todo o painel de métricas ficam viáveis sem migração de dados retroativa.
- O detalhe da candidatura expõe o histórico de eventos (ADR-0012).
- Qualquer bug na escrita dupla (status atual × evento) gera divergência — o serviço de mudança de status é o único ponto de escrita, e a consistência na mesma transação é obrigatória.
