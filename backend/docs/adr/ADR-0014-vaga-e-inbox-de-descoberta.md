# ADR-0014 — Vaga como entidade e inbox de descoberta (revê o ADR-0004)

**Status:** Aceito (2026-09-21)

## Contexto

O ADR-0004 decidiu não criar a entidade `Vaga` na fatia 1, porque a `Candidatura` era flat e não havia recurso que comparasse vagas entre usuários. O ADR-0003 adiou a extração automática por ser o maior risco técnico do produto.

Agora entra o módulo de busca automática: o sistema descobre vagas sozinho, a partir de fontes configuradas pelo usuário, sem que ele tenha aplicado. Uma vaga descoberta **não é uma candidatura** — o usuário ainda não aplicou. Colocá-la no kanban (que nasce em `Aplicado`, ADR-0005) misturaria oportunidade não-aplicada com o funil e contaminaria as métricas de conversão.

## Decisão

- **Nova entidade `Vaga`**, separada de `Candidatura`, que representa uma oportunidade descoberta pela busca. Campos: título, empresa, localização, modalidade, nível, URL (e URL normalizada), descrição, data de publicação, tipo/nome da fonte, hash de deduplicação, status e vínculo opcional com a candidatura.
- **Status próprio** (`StatusVaga`): `Nova`, `Vista`, `Aplicada`, `Descartada`. A "coluna Novas" do requisito é a inbox de vagas com status `Nova`, não uma coluna do kanban.
- **Deduplicação por usuário**: `UrlNormalizada` (URL sem parâmetros de rastreio) e `HashDedup` (`empresa|título|localização` normalizados, SHA-256) são únicos por usuário. Cada vaga válida é gravada uma única vez; execuções seguintes contam duplicadas e não recriam.
- **Aplicar** cria a `Candidatura` correspondente (empresa, cargo, link, `Origem` mapeada da fonte), nasce em `Aplicado` com evento inicial (ADR-0006) e marca a vaga como `Aplicada`, tudo na mesma transação. A vaga guarda o `CandidaturaId`.
- `Candidatura` continua flat e não ganha campos de vaga; o vínculo é unilateral (`Vaga.CandidaturaId`).

## Consequências

- Revê explicitamente o ADR-0004 ("sem entidade Vaga"); o gatilho que ele mesmo previa — "quando a extração automática por URL chegar, aí se avalia normalizar" — chegou.
- O kanban e suas métricas permanecem íntegros: só entra no funil o que o usuário decidiu aplicar.
- A FK `Vaga.CandidaturaId → Candidaturas` é `NO ACTION` (sem cascade): o caminho `Usuário → Candidaturas` e `Usuário → Vagas` já cascateia, e `SET NULL` gerava múltiplos caminhos de cascade no SQL Server.
- Deduplicação é por usuário (mesma vaga pode existir para usuários diferentes), coerente com filtros e inbox individuais. Catálogo global compartilhado fica para quando houver recurso cross-usuário (mesma lógica do ADR-0004).
- Similaridade de conteúdo (além de URL e empresa+título+local) fica para fatia futura.
