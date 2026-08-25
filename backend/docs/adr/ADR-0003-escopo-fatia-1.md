# ADR-0003 — Escopo da fatia 1: auth + kanban + currículos

**Status:** Aceito (2026-08-25)

## Contexto

O produto tem cinco fluxos (cadastro/kanban, follow-ups, gap currículo×vaga, prep de entrevista, métricas do funil) e camada grátis/paga. A parte de maior risco técnico é a extração automática de vagas por URL (scraping de LinkedIn/Gupy/Catho — bloqueios, ToS, manutenção contínua). Começar por ela travaria a entrega.

## Decisão

A fatia 1 entrega, de ponta a ponta:

1. **Autenticação** — registro público, login, refresh (detalhes no ADR-0009).
2. **Kanban de candidaturas** — CRUD de candidaturas + mudança de status (ADRs 0004–0007).
3. **Currículos** — upload/armazenamento de PDF com múltiplas versões (ADR-0008).

**Fora da fatia 1 (explícito):** extração automática por URL, pagamentos/entitlements, lembretes de follow-up (exige scheduler/job), gap-analysis, geração de follow-up por IA, prep de entrevista, painel de métricas do funil, exclusão de candidaturas e de currículos.

## Consequências

- Caminho de menor risco primeiro: tudo da fatia 1 é CRUD bem conhecido, sem scraping nem integração de pagamento.
- O modelo de dados já nasce preparado para o que vem (eventos de status, origem, currículo ativo), evitando migrations caras depois.
- O paywall é adiado, mas nenhuma decisão atual presume "tudo grátis para sempre" — campos e endpoints premium entram por ADR novo na fatia de pagamentos.
