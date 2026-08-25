# ADR-0009 — Auth: Identity + JWT + refresh rotativo, registro público, sem roles

**Status:** Aceito (2026-08-25)

## Contexto

O base (Intranet) usa ASP.NET Identity + JWT bearer com `TokenService` que emite access token e refresh token com **rotação persistida em banco** — padrão dominado pelo autor. O produto é B2C: qualquer pessoa cria conta. Foi discutido cookie httpOnly; decidido manter o padrão do base (JWT no corpo da resposta) por paridade e previsibilidade.

Diferenças obrigatórias em relação ao base: registro é **público** (no base é admin-only), **não há roles** (todo usuário é candidato — o produto não tem admin na fatia 1), e **não há LDAP/Synology**.

## Decisão

- **ASP.NET Identity** (`AppUser` sem roles; sem tabela de roles; policies de autorização só "usuário autenticado").
- **Registro público:** `POST /api/auth/register` com nome/e-mail/senha. Sem confirmação de e-mail na fatia 1.
- **Login:** `POST /api/auth/login` → `AccessToken` + `RefreshToken` no corpo da resposta (sem cookies).
- **Refresh com rotação:** `POST /api/auth/refresh` → valida e rotaciona o refresh token persistido (replicar `TokenService`/repositório de tokens do base).
- **Forgot/reset de senha: fora da fatia 1.** Reset em dev é manual no banco.
- Regras de senha e validação de e-mail único herdadas da configuração do base (`RequireUniqueEmail`, política de senha simples).
- JWT com `ClaimTypes.NameIdentifier` (usado por currículos e candidaturas para escopo por usuário).

## Consequências

- Menos uma peça nova: o fluxo de token do base é cópia quase direta (menos roles/LDAP/mail).
- Sem confirmação de e-mail, conta com e-mail inválido é criável — aceitável na fatia 1; lembretes por e-mail (Fluxo 2) exigirão resolver isso naquela fatia.
- Sem forgot/reset por API: dor operacional conhecida e assumida em dev; entra como ADR novo quando o produto ganhar usuários reais.
- Tokens no localStorage do frontend: risco XSS aceito em troca da simplicidade do padrão existente.
