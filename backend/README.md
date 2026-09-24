# Vagalume Backend — Fatia 1

Backend da **Fatia 1** do Vagalume (rastreador de vagas) conforme ADRs em `docs/adr/`.

## Stack (ADR-0001)
- .NET 10 (`net10.0`), C# 
- 4 projetos: `Vagalume.Domain`, `Vagalume.Application`, `Vagalume.Infra`, `Vagalume.Api` (Controllers, não Minimal API)
- PostgreSQL 16 + EF Core (`Npgsql`) + Identity
- Docker Compose (`postgres` + `api`, healthcheck, volumes, migrations no startup)
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
| GET | `/api/ia/provedores` | sim¹ | catálogo de provedores de IA (sem URL livre) |
| GET | `/api/ia/configuracao` | sim¹ | provedor/modelo do usuário (chave mascarada) |
| PUT | `/api/ia/configuracao` | sim¹ | salva provedor/modelo/API key (cifrada) |
| POST | `/api/ia/testar` | sim¹ | testa a conexão com o provedor |
| GET | `/api/vagas/{id}/compatibilidade` | sim¹ | análise de compatibilidade existente |
| POST | `/api/vagas/{id}/compatibilidade` | sim¹ | gera/regera gap-analysis currículo × vaga |

¹ Policy `verify_user` (ADR-0013/0019/0020).

- `origem`: `LinkedIn,Gupy,Catho,Indicacao,SiteEmpresa,Outro` (400 se inválido)
- `status`: `Aplicado,Triagem,Entrevista,Oferta,Encerrado` (400 se inválido, no-op se mesmo status)
- Recurso de outro usuário → `404` (não vaza existência)
- Todas respostas: `GenericResponseData<T>{success,message,data}` + HTTP coerente (ADR-0011), validação model binding retorna envelope 400, 401 envelope `Não autenticado`.

## Entidades
- `AppUser : IdentityUser<Guid>` (`Nome`) + `RefreshToken` rotativo persistido
- `Candidatura` flat: `Empresa*`, `Cargo*`, `Link?`, `Origem*`, `Observacoes?`, `Status` denormalizado, `CriadoEm/AtualizadoEm`
- `EventoTransicaoCandidatura`: `StatusAnterior?`, `StatusNovo`, `Observacao?`, `CriadoEm` — transação atômica com `Candidatura.Status`
- `Curriculo`: `NomeArquivoOriginal`, `CaminhoArmazenado` (`{usuarioId}/{guid}.pdf`), `TamanhoBytes`, `Ativo`, `CriadoEm` — upload desativa anterior mesma transação
- `Vaga`: oportunidade descoberta pela busca (`Titulo`, `Empresa`, `Url`, `Descricao`, `Status`, dedup) — inbox separada das candidaturas (ADRs 0014–0017)
- `ConfiguracaoIa`: provedor de IA do usuário (BYOK) — `Provedor`, `Modelo`, `ApiKeyCriptografada` (AES-256-GCM), `Ativo` (ADR-0020)
- `AnaliseCompatibilidade`: gap-analysis currículo ativo × vaga — `Score`, `Resumo`, `Requisitos`, `Sugestoes`, `CurriculoId`, único por `(UsuarioId, VagaId)` (ADR-0019)

## Rodar local (sem Docker, precisa PostgreSQL)
```bash
cp .env.example .env   # .env na raiz ou em backend/ é carregado via DotNetEnv (TraversePath)
dotnet run --project src/Vagalume.Api
# Swagger: http://localhost:5000/swagger
```
Alternativa sem `.env` — exporte no formato .NET:
```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=vagalume;Username=vagalume;Password=Vagalume@123"
export Uploads__BasePath="/tmp/vagalume_uploads"
export Seguranca__ChaveCripto="troque-este-segredo-em-producao"   # cifra a API key de IA (BYOK)
```

## Rodar com Docker (recomendado)
```bash
# da raiz do repositório (compose canônico com frontend):
cp .env.example .env
docker compose up --build
# api: http://localhost:5000/swagger
# frontend: http://localhost:3000 (nginx + proxy /api → api:8080)
# postgres: localhost:5432 (vagalume / Vagalume@123)
```

`../docker-compose.yml` (raiz) é o compose geral canônico: `postgres` com healthcheck `pg_isready`, volume `postgres_data`, `api` depende `service_healthy`, `frontend` (nginx) depende `api`, volume `uploads_data:/app/uploads`. O serviço `api` usa `env_file: .env` (sem segredos embutidos no compose) e só sobrescreve a connection string para `Host=postgres`.

Migrations são aplicadas no startup (`db.Database.Migrate()`).

## Notas ADRs
- Transições livres: qualquer status → qualquer status, inclusive `Encerrado → Oferta`.
- Evento inicial `null → Aplicado` na criação.
- Currículo: sem delete na fatia 1, validação PDF por extensão + content-type, limite 10MB (`RequestSizeLimit` + checagem manual → 413).
- Sem roles, sem confirmação e-mail, sem forgot/reset.
- Envelope + JWT `ClaimTypes.NameIdentifier` para escopo por usuário.
