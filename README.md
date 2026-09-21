# Vagalume — Rastreador de vagas (caderno)

Monorepo com **backend .NET 10 + PostgreSQL** e **frontend React 19 + Vite + Tailwind** estilizado como caderno.

## Stack
- **Backend** `backend/`: .NET 10, EF Core, Identity + JWT + Refresh rotativo, PostgreSQL 16, Docker, Swagger, envelope `GenericResponseData<T>`, busca automática de vagas, gap-analysis com IA (BYOK)
- **Frontend** `frontend/`: React 19, React Router 7, Vite 8, Tailwind 4, shadcn/new-york, autenticação real, kanban 5 colunas, currículos PDF, busca de vagas e análise de compatibilidade
- **Infra**: Docker Compose com 3 serviços (postgres + api + frontend via nginx proxy)

## Rodar tudo com Docker (recomendado — compose geral)

Na **raiz** `Vagalume/`:

```bash
cp .env.example .env
# ajuste portas se precisar (FRONTEND_PORT, API_PORT, POSTGRES_PORT)
docker compose up --build
```

- Frontend: http://localhost:3000 (nginx serve `dist` + proxy `/api` → `api:8080`)
- API: http://localhost:5000/swagger (Development)
- PostgreSQL: localhost:5432 (vagalume / Vagalume@123)

Também funciona a partir de `backend/`:
```bash
cd backend
cp .env.example .env
docker compose up --build
# mesmo stack, frontend em http://localhost:3000
```

### Variáveis (.env na raiz ou backend/.env)
```
POSTGRES_USER=vagalume
POSTGRES_PASSWORD=Vagalume@123
POSTGRES_DB=vagalume
POSTGRES_PORT=5432
API_PORT=5000
FRONTEND_PORT=3000
JWT_KEY=chave-super-secreta-de-32-caracteres!!-vagalume-dev
VITE_API_URL=   # vazio = usa proxy /api (recomendado no docker)
SEGURANCA_CHAVE_CRIPTO=troque-este-segredo-em-producao  # cifra a API key de IA (BYOK)
IA_PRESETS_OLLAMA_BASE_URL=   # opcional: habilita provedor Ollama local
```

## Rodar em dev sem Docker

**Backend** (precisa PostgreSQL externo):
```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=vagalume;Username=vagalume;Password=Vagalume@123"
dotnet run --project backend/src/Vagalume.Api
```

**Frontend**:
```bash
cd frontend
npm install
npm run dev  # http://localhost:5173 — vite proxy /api → http://localhost:5000
```

`VITE_API_URL` vazio usa proxy (`vite.config.ts` dev + `nginx.conf` prod). Para apontar direto: `VITE_API_URL=http://localhost:5000`.

## Estrutura
```
Vagalume/
├── docker-compose.yml      # compose geral (postgres + api + frontend)
├── .env.example
├── backend/                # .NET API (ver backend/README.md)
│   ├── src/Vagalume.Api/
│   ├── src/Vagalume.Infra/
│   └── docker-compose.yml  # também inclui frontend (context ../frontend)
└── frontend/               # React SPA
    ├── src/lib/api.ts      # client envelope + refresh
    ├── src/features/auth/  # login/register/context
    ├── src/features/candidaturas/ # tipos e API (status Origem do backend)
    ├── src/features/vagas/ # busca de vagas + análise de compatibilidade
    ├── src/features/ia/    # configuração do provedor de IA (BYOK)
    ├── Dockerfile          # node build → nginx
    └── nginx.conf          # SPA fallback + proxy /api → api:8080
```
