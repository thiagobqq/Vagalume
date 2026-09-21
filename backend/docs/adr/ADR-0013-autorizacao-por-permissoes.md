# ADR-0013 — Autorização por permissões (sem roles), preparada para paywall

**Status:** Aceito (2026-09-21)

## Contexto

O módulo de busca automática de vagas entra no produto com acesso restrito: a princípio só usuários autorizados, e mais adiante apenas quem pagar. O ADR-0009 decidiu deliberadamente **sem roles** — todo usuário autenticado é candidato — e as tabelas de role do Identity (`AspNetRoles`, `AspNetUserRoles`) existem no schema, mas nunca foram usadas.

Autorização de "quem pode acessar" não é a mesma coisa que "quem pagou". Tratar o plano pago como um role (ex.: `Premium`) acoplaria a cobrança ao Identity: cada compra/cancelamento viraria manipulação de role, e o claim só refletiria no próximo refresh do JWT. Role também não tem noção de expiração — inadequado como fonte de verdade de assinatura.

## Decisão

- **Autorização por permissões (entitlements) por usuário, sem roles.** Uma permissão é uma chave string (`Permissoes` em `Vagalume.Domain/Authorization/Permissoes.cs`), ex.: `admin`, `busca_vagas.acesso`.
- **Persistência** na entidade `UsuarioPermissao` (`Chave`, `ConcedidoEm`, `ConcedidoPorUsuarioId`, `ExpiraEm`, `Origem`), com índice único `(UsuarioId, Chave)`.
- **`Origem` (`Manual`/`Assinatura`) e `ExpiraEm` desde já** — o seam para o paywall. Hoje a concessão é manual via tela de admin; na fatia de pagamentos, o webhook do provedor materializa/remove linhas com `Origem = Assinatura` e `ExpiraEm`, **sem alterar controllers**.
- **Admin é uma permissão (`admin`), não um role.** A tela de gestão de permissões (`/app/admin`, API `/api/admin`) é protegida por ela.
- **Policies do ASP.NET Core** geradas para cada chave em `Program.cs`; um `PermissaoAuthorizationHandler` (scoped) consulta `IEntitlementService.HasAsync`. Controllers usam `[Authorize(Policy = Permissoes.X)]` e não sabem a origem da permissão.
- **Permissões não vão no JWT.** São consultadas a cada requisição (`IEntitlementService`), pois mudam com frequência (concessão/revogação/expiração) e o access token é de longa duração.
- **Bootstrap:** em Development um usuário admin é semeado de forma idempotente (`Seed:Admin:Email`/`Senha`) e recebe `admin`. Em produção, a concessão inicial é manual no banco.
- **Semântica HTTP:** 403 (envelope) para acesso negado. O 402 Payment Required fica reservado para o upsell na fatia de pagamentos.

## Consequências

- O módulo novo é liberado concedendo `busca_vagas.acesso`; nada de role/Identity é tocado.
- O paywall entra como nova implementação de `IEntitlementService` + entidade de assinatura, sem reescrever a proteção das rotas.
- `AspNetRoles`/`AspNetUserRoles` permanecem no schema (herança do Identity) e seguem sem uso — custo de schema já existente, sem custo de manutenção.
- Exclusão de usuário remove as permissões em cascata (FK).
- Sem cache: cada verificação faz uma query. Se virar hot path, entra cache com invalidação em ADR futuro.
- Frontend (`useAuth().hasPermissao`, `ProtectedRoute permissao=`, nav condicional) é UX apenas; a segurança real está no backend.
