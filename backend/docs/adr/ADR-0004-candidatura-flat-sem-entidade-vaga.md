# ADR-0004 — Candidatura flat: sem entidade Vaga na fatia 1

**Status:** Aceito (2026-08-25) · **Revisto por:** [ADR-0014](ADR-0014-vaga-e-inbox-de-descoberta.md)

## Contexto

No produto, o usuário "adiciona uma vaga", mas o que ele realmente registra é **a candidatura dele** àquela vaga. Modelar `Vaga` como entidade global normalizada (compartilhada entre usuários) só faz sentido se houver recurso que compare vagas entre usuários — e nada no roadmap atual exige isso. O gap-analysis (Fluxo 3) compara requisitos da *candidatura* com o currículo do usuário; os requisitos podem morar no registro pessoal.

Foi levantada a hipótese de criar a tabela `Vaga` agora e usá-la depois; foi decidido **não** criar (entidade morta no schema = custo sem benefício).

## Decisão

Não existe tabela `Vaga` na fatia 1. A entidade `Candidatura` é flat e carrega ela mesma os dados da vaga como campos de texto:

- `Empresa` (texto, obrigatório)
- `Cargo` (texto, obrigatório)
- `Link` (URL, opcional — indicação pode não ter link)
- `Origem` (enum, obrigatório — ADR-0007)
- `Observacoes` (texto livre, opcional)

Quando a extração automática por URL chegar, aí se avalia normalizar (e será por ADR novo).

## Consequências

- Schema mínimo e simples; nenhuma consulta de join para o caso de uso principal.
- Se dois usuários aplicarem para a mesma vaga, haverá dois registros independentes — aceitável, pois não há feature cross-usuário no roadmap.
- Duplicação de dados da vaga entre candidaturas é irrelevante em volume (dezenas de candidaturas por usuário, não milhões).
- Requisitos extraídos e modalidade (remoto/híbrido/presencial) só entram no modelo quando a extração existir — a Candidatura ganhará campos/entidade relacionada naquela fatia.
