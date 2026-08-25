# ADR-0001 — Stack: .NET 10, SQL Server, Docker Compose

**Status:** Aceito (2026-08-25)

## Contexto

O produto (ver `visao-produto-rastreador-vagas.md`) precisa de um backend novo. Existe um frontend React 19 + Vite + TS + MSW já scaffoldado, cujos mocks são placeholders — não há contrato prévio a respeitar. O autor mantém um codebase de referência (Intranet-backend) com arquitetura madura em .NET: 4 projetos (Api/Application/Domain/Infra), Controllers, ASP.NET Identity, JWT com refresh rotativo persistido, EF Core, Swagger, migrations no startup.

Na primeira discussão a inclinação era Postgres; ao replicar o base, a decisão final reverteu para SQL Server por paridade total de padrões e infraestrutura (o compose do base já roda SQL Server 2022 com healthcheck).

## Decisão

- **Linguagem/runtime:** C# / .NET (`net10.0` — à frente do base, que é `net9.0`).
- **Estrutura da solução:** replicar o base — `Vagalume.Api`, `Vagalume.Application`, `Vagalume.Domain`, `Vagalume.Infra`, com Controllers (não Minimal API), DTOs e interfaces na Application, entidades na Domain, EF Core + repositórios + serviços de infraestrutura na Infra.
- **Banco de dados:** SQL Server 2022, acessado via EF Core (`Microsoft.EntityFrameworkCore.SqlServer`).
- **Ambiente:** `docker-compose` com `sqlserver` + `api`, volumes nomeados, healthcheck no banco, migrations aplicadas no startup. Deploy futuro em VPS reutilizando o mesmo compose.
- **Config:** `DotNetEnv` + variáveis de ambiente (padrão do base).

## Consequências

- Reuso direto de padrões, código e experiência do base — menos atrito e menos decisão nova.
- SQL Server no cenário B2C em VPS exige atenção a edição/licenciamento em produção (Developer é grátis só para dev; avaliar Express ou Standard quando o deploy virar real).
- `net10.0` diverge do base: pacotes Identity/EF/JwtBearer precisam de versões compatíveis com o 10 (upgrade trivial, mas validável no build).
- Postgres fica registrado como alternativa descartada; reverter exigiria ADR novo (troca de provider EF + compose).
