# ADR-0018 — PostgreSQL no lugar do SQL Server

**Status:** Aceito (2026-09-21)

## Contexto

O ADR-0001 escolheu SQL Server 2022 por paridade com o projeto-base. Ao planejar o deploy público, ficou claro que isso inviabiliza qualquer hospedagem gratuita: SQL Server é pesado (~2 GB de RAM), só roda em x86 e **nenhum PaaS gratuito oferece SQL Server gerenciado** (Render, Railway, Fly.io, Koyeb). As opções gratuitas de VPS (Oracle/GCP) são ARM ou têm RAM insuficiente.

Além disso, o código dependia de `System.IdentityModel.Tokens.Jwt` apenas de forma transitiva (via `Microsoft.Data.SqlClient`, trazido pelo provider SQL Server) e de um tipo específico (`HasColumnType("nvarchar(max)")`).

## Decisão

- **Banco:** PostgreSQL 16, via `Npgsql.EntityFrameworkCore.PostgreSQL`. O provider SQL Server foi removido.
- **Local e produção usam o mesmo banco** (sem dois conjuntos de migrations).
- **Migrations regeradas** para Npgsql (as de SQL Server foram descartadas); `AppDbContextFactory` adicionado para gerar migrations em design-time sem subir a app.
- **Dependência explícita** de `System.IdentityModel.Tokens.Jwt` no `Vagalume.Infra` (não mais transitiva).
- **Docker Compose** passa a subir `postgres:16-alpine` com healthcheck `pg_isready`.

Isto **supersede** a escolha de banco do ADR-0001; o restante da stack (.NET 10, Docker Compose, EF Core, Identity) permanece.

## Consequências

- Habilita hospedagem gratuita/sustentável (ex.: Neon/Supabase para o banco, Render/Koyeb para a API, Vercel para o frontend).
- Tipos mapeados pelo provider: `uuid`, `text`, `timestamp with time zone`, `character varying(n)`, `boolean`, `integer`.
- O aviso de licenciamento do SQL Server em produção (ADR-0001) deixa de existir.
- `DateTime` agora mapeia para `timestamptz`; valores devem continuar em UTC (`DateTimeProvider`).
- Uploads seguem em volume local (`/app/uploads`) — em host com disco efêmero, migrar para storage de objetos (ver ADR-0008).
