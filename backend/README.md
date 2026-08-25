# Vagalume Backend — Fatia 1

Backend da **Fatia 1** do Vagalume (rastreador de vagas) conforme ADRs em `docs/adr/`.

## Stack (ADR-0001)
- .NET 10 (`net10.0`), C# 
- 4 projetos: `Vagalume.Domain`, `Vagalume.Application`, `Vagalume.Infra`, `Vagalume.Api` (Controllers, não Minimal API)
- SQL Server 2022 + EF Core + Identity
- Docker Compose (`sqlserver` + `api`, healthcheck, volumes, migrations no startup)
- DotNetEnv + variáveis de ambiente

## Escopo Fatia 1 (ADR-0003)
- Auth: registro público, login, refresh rotativo (ADR-0009)
- Kanban candidaturas flat sem entidade Vaga (ADR-0004) + máquina estados livre + terminal único (ADR-0005) + eventos append-only (ADR-0006) + origem obrigatória (ADR-0007)
- Currículos PDF múltiplos, ativo único, volume `/app/uploads` (ADR-0008)
- Sem auditoria genérica (ADR-0010), envelope `GenericResponseData<T>` consistente (ADR-0011), contrato 11 endpoints (ADR-0012)

## Estrutura
```
backend/
├── docs/adr/          # decisões
├── src/
│   ├── Vagalume.Domain/       # entidades, enums, Abstractions
│   ├── Vagalume.Application/  # DTOs, envelope, mappings, validators
│   ├── Vagalume.Infra/        # DbContext, configs, migrations, providers
│   └── Vagalume.Api/          # Controllers, Program.cs, Swagger, JWT
├── docker-compose.yml
├── .env.example
└── global.json
```

## Contrato API (ADR-0012) — prefixo `/api`
| Método | Rota | Auth | Descrição |
|---|---|---|---|
| POST | `/api/auth/register` | não | `{nome,email,senha}` → 201 |
| POST | `/api/auth/login` | não | `{email,senha}` → `{accessToken,refreshToken,usuario}` |
| POST | `/api/auth/refresh` | não | `{refreshToken}` → novos tokens / 401 |
| POST | `/api/candidaturas` | sim | criar (nasce `Aplicado` + evento) |
| GET | `/api/candidaturas` | sim | listar ordenado por `CriadoEm` |
| GET | `/api/candidaturas/{id}` | sim | detalhe + `eventos` |
| PATCH | `/api/candidaturas/{id}` | sim | parcial `empresa/cargo/link/origem/observacoes` |
| PATCH | `/api/candidaturas/{id}/status` | sim | `{status,observacao?}` livre, reabrir permitido |
| POST | `/api/curriculos` | sim | multipart `arquivo` PDF ≤10MB → novo ativo |
| GET | `/api/curriculos` | sim | lista flag `ativo` |
| GET | `/api/curriculos/{id}/arquivo` | sim | stream PDF |
| PATCH | `/api/curriculos/{id}/ativo` | sim | marcar ativo |

- `origem`: `LinkedIn,Gupy,Catho,Indicacao,SiteEmpresa,Outro` (400 se inválido)
- `status`: `Aplicado,Triagem,Entrevista,Oferta,Encerrado` (400 se inválido, no-op se mesmo status)
- Recurso de outro usuário → `404` (não vaza existência)
- Todas respostas: `GenericResponseData<T>{success,message,data}` + HTTP coerente (ADR-0011), validação model binding retorna envelope 400, 401 envelope `Não autenticado`.

## Entidades
- `AppUser : IdentityUser<Guid>` (`Nome`) + `RefreshToken` rotativo persistido
- `Candidatura` flat: `Empresa*`, `Cargo*`, `Link?`, `Origem*`, `Observacoes?`, `Status` denormalizado, `CriadoEm/AtualizadoEm`
- `EventoTransicaoCandidatura`: `StatusAnterior?`, `StatusNovo`, `Observacao?`, `CriadoEm` — transação atômica com `Candidatura.Status`
- `Curriculo`: `NomeArquivoOriginal`, `CaminhoArmazenado` (`{usuarioId}/{guid}.pdf`), `TamanhoBytes`, `Ativo`, `CriadoEm` — upload desativa anterior mesma transação

## Rodar local (sem Docker, precisa SQL Server)
```bash
export ConnectionStrings__DefaultConnection="Server=localhost,1435;Database=Vagalume;User Id=sa;Password=Vagalume@123;TrustServerCertificate=True"
export Uploads__BasePath="/tmp/vagalume_uploads"
dotnet run --project src/Vagalume.Api
# Swagger: http://localhost:5000/swagger
```

## Rodar com Docker (recomendado)
```bash
cp .env.example .env
# se porta 1433 ocupada, exporte SQLSERVER_PORT=1435
docker compose up --build
# api: http://localhost:5000/swagger
# sqlserver: localhost:1433 (ou 1435)
```

`docker-compose.yml`: `sqlserver` com healthcheck `sqlcmd`, volume `sqlserver_data`, `api` depende `service_healthy`, volume `uploads_data:/app/uploads`, env `ConnectionStrings__DefaultConnection` aponta para `sqlserver:1433`.

Migrations são aplicadas no startup (`db.Database.Migrate()`).

## Notas ADRs
- Transições livres: qualquer status → qualquer status, inclusive `Encerrado → Oferta`.
- Evento inicial `null → Aplicado` na criação.
- Currículo: sem delete na fatia 1, validação PDF por extensão + content-type, limite 10MB (`RequestSizeLimit` + checagem manual → 413).
- Sem roles, sem confirmação e-mail, sem forgot/reset.
- Envelope + JWT `ClaimTypes.NameIdentifier` para escopo por usuário.
