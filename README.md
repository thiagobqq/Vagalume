# Vagalume — Rastreador de vagas (caderno)

Monorepo com **backend .NET 10 + SQL Server** e **frontend React 19 + Vite + Tailwind** estilizado como caderno.

## Stack
- **Backend** `backend/`: .NET 10, EF Core, Identity + JWT + Refresh rotativo, SQL Server 2022, Docker, Swagger, envelope `GenericResponseData<T>`
- **Frontend** `frontend/`: React 19, React Router 7, Vite 8, Tailwind 4, shadcn/new-york, autenticação real, kanban 5 colunas, currículos PDF
- **Infra**: Docker Compose com 3 serviços (sqlserver + api + frontend via nginx proxy)

## Rodar tudo com Docker (recomendado — compose geral)

Na **raiz** `Vagalume/`:

```bash
cp .env.example .env
# ajuste portas se precisar (FRONTEND_PORT, API_PORT, SQLSERVER_PORT)
docker compose up --build
```

- Frontend: http://localhost:3000 (nginx serve `dist` + proxy `/api` → `api:8080`)
- API: http://localhost:5000/swagger (Development)
- SQL Server: localhost:1433 (sa / Vagalume@123)

Também funciona a partir de `backend/`:
```bash
cd backend
cp .env.example .env
docker compose up --build
# mesmo stack, frontend em http://localhost:3000
```

### Variáveis (.env na raiz ou backend/.env)
```
SA_PASSWORD=Vagalume@123
SQLSERVER_PORT=1433
API_PORT=5000
FRONTEND_PORT=3000
JWT_KEY=chave-super-secreta-de-32-caracteres!!-vagalume-dev
VITE_API_URL=   # vazio = usa proxy /api (recomendado no docker)
```

## Rodar em dev sem Docker

**Backend** (precisa SQL Server externo):
```bash
export ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=Vagalume;User Id=sa;Password=Vagalume@123;TrustServerCertificate=True"
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
├── docker-compose.yml      # compose geral (sqlserver + api + frontend)
├── .env.example
├── backend/                # .NET API (ver backend/README.md)
│   ├── src/Vagalume.Api/
│   ├── src/Vagalume.Infra/
│   └── docker-compose.yml  # também inclui frontend (context ../frontend)
└── frontend/               # React SPA
    ├── src/lib/api.ts      # client envelope + refresh
    ├── src/features/auth/  # login/register/context
    ├── src/features/candidaturas/ # tipos e API (status Origem do backend)
    ├── Dockerfile          # node build → nginx
    └── nginx.conf          # SPA fallback + proxy /api → api:8080
```
