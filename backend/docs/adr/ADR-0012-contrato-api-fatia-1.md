# ADR-0012 — Contrato de API da fatia 1

**Status:** Aceito (2026-08-25)

## Contexto

Fechamento do desenho de endpoints da fatia 1 (escopo no ADR-0003). O backend dita o contrato (ADR-0002); este ADR o registra para o frontend implementar contra (atualizar mocks MSW).

## Decisão

Prefixo: `/api`. Autenticação: `Authorization: Bearer {accessToken}` em tudo, exceto `register`/`login`/`refresh`. Todas as respostas usam o envelope do ADR-0011.

### Auth

| Método | Rota | Corpo | Retorno |
|---|---|---|---|
| POST | `/api/auth/register` | `{ nome, email, senha }` | `201` |
| POST | `/api/auth/login` | `{ email, senha }` | `200` → `{ accessToken, refreshToken, usuario }` |
| POST | `/api/auth/refresh` | `{ refreshToken }` | `200` → `{ accessToken, refreshToken }` / `401` |

Sem roles, sem confirmação de e-mail, sem forgot/reset (ADR-0009).

### Candidaturas

| Método | Rota | Corpo | Retorno |
|---|---|---|---|
| POST | `/api/candidaturas` | `{ empresa*, cargo*, link?, origem*, observacoes? }` | `201` → candidatura (status `Aplicado`, evento inicial criado) |
| GET | `/api/candidaturas` | — | `200` → lista do usuário (ordenada por data de candidatura) |
| GET | `/api/candidaturas/{id}` | — | `200` → candidatura **+ eventos de status** |
| PATCH | `/api/candidaturas/{id}` | `{ empresa?, cargo?, link?, origem?, observacoes? }` | `200` → candidatura atualizada |
| PATCH | `/api/candidaturas/{id}/status` | `{ status, observacao? }` | `200` → candidatura atualizada + evento registrado |

- `origem`: enum string — `LinkedIn, Gupy, Catho, Indicacao, SiteEmpresa, Outro` (ADR-0007).
- `status`: enum string — `Aplicado, Triagem, Entrevista, Oferta, Encerrado` (ADR-0005).
- Mudança para o mesmo status atual: permitida (no-op que não gera evento) — ou `400` se evento duplicado; definir na implementação e refletir no Swagger.
- Recurso de outro usuário: `404`. Sem delete.

### Currículos

| Método | Rota | Corpo | Retorno |
|---|---|---|---|
| POST | `/api/curriculos` | multipart `arquivo` (PDF, ≤10MB) | `201` → currículo (novo vira ativo) |
| GET | `/api/curriculos` | — | `200` → lista com flag `ativo` |
| GET | `/api/curriculos/{id}/arquivo` | — | `200` → stream do PDF |
| PATCH | `/api/curriculos/{id}/ativo` | — | `200` → currículo ativo atualizado |

- Sem delete (ADRs 0003, 0008).
- Recurso de outro usuário: `404`.

## Consequências

- Contrato pequeno (11 endpoints) e suficiente para todo o fluxo da fatia 1.
- PATCH parcial sem replace total (PUT) — edição de campos individuais.
- O detalhe da candidatura carregar eventos evita endpoint separado de histórico na fatia 1.
- Frontend atualiza mocks/rotas MSW conforme esta tabela; divergência = bug.
