# ADR-0010 — Sem interceptor de auditoria genérico na fatia 1

**Status:** Aceito (2026-08-25)

## Contexto

O base tem `AuditSaveChangesInterceptor` + entidade `AuditLog` registrando mudanças genericamente. No Vagalume, o histórico relevante ao produto (trajetória da candidatura) já é capturado de forma explícita e semântica pelos eventos append-only de status (ADR-0006).

## Decisão

Não replicar o interceptor de auditoria nem a tabela `AuditLog` na fatia 1. O único histórico persistido é o de transições de status da candidatura.

## Consequências

- Menos um componente para construir e manter; menos overhead em cada `SaveChanges`.
- Não haverá trilha genérica de "quem alterou que campo quando" — se um requisito de compliance/auditoria aparecer, entra como ADR novo adicionando o interceptor (a arquitetura do base permite replicar depois).
- Eventos de transição cobrem o caso de uso do produto (funil, follow-up, detalhe da candidatura).
