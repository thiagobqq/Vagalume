# ADR-0008 — Currículos: múltiplos por usuário, ativo único, arquivo em volume

**Status:** Aceito (2026-08-25)

## Contexto

O Fluxo 1 pede upload de currículo (PDF) no onboarding; o Fluxo 3 (gap-analysis, fatia futura) compara "o currículo" com a vaga. Perguntas decididas:

- Um ou múltiplos currículos? → Múltiplos (pessoa adapta o CV por área), com **um marcado como ativo**.
- Onde guardar o arquivo? → **Volume de arquivos próprio** (`/app/uploads`), com metadados no banco — não `bytea` no SQL Server (bloat de banco/backup) e nunca dentro do volume de dados do próprio servidor SQL.
- Parse de texto? → Não na fatia 1; só armazenar. Parse é problema da fatia do gap-analysis.
- Exclusão? → Não implementar agora (ADR-0003).

## Decisão

- Entidade `Curriculo`: `Id`, `UsuarioId`, `NomeArquivoOriginal`, `CaminhoArmazenado`, `TamanhoBytes`, `Ativo` (bool), `CriadoEm`.
- **Upload novo vira o ativo automaticamente** (desativa o anterior na mesma transação). Existe um e apenas um ativo por usuário.
- Arquivo salvo em `/app/uploads/{usuarioId}/{guid}.pdf` — nome original preservado apenas como metadado.
- Validação: **PDF only** (content-type + extensão), **máx 10 MB** (`RequestSizeLimit`).
- Endpoints: upload (multipart), listar (com flag de ativo), download, marcar ativo — ver ADR-0012. Sem delete.
- Acesso sempre escopado ao usuário autenticado — currículo de outro usuário é 404.

## Consequências

- Múltiplas versões sem custo extra agora; trocar o ativo é PATCH barato.
- Gap-analysis futuro lê "o currículo ativo" — contrato já definido.
- Sem exclusão, volume só cresce — aceitável na fatia 1 (revisitado junto com delete/abandono).
- Backup do VPS precisa cobrir o volume `/app/uploads` além do banco — registrado no compose como volume nomeado.
