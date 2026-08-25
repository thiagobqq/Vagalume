# ADR-0011 — Envelope de resposta consistente `GenericResponseData<T>`

**Status:** Aceito (2026-08-25)

## Contexto

O base usa um envelope `{ Success, Message, Data }`, mas de forma inconsistente: alguns endpoints retornam string crua (`Ok(response.Message)`), outros retornam o envelope, outros misturam envelope com status HTTP sem relação clara. Como o backend dita o contrato (ADR-0002), a inconsistência não pode se propagar.

## Decisão

Todos os endpoints retornam o envelope, com **status HTTP sempre correto e coerente com o envelope**:

- `200` — sucesso com dados (GET, PATCH).
- `201` — criação (POST).
- `400` — erro de validação/negócio (`Success = false`, mensagem em `Message`).
- `401` — não autenticado / refresh inválido.
- `404` — recurso inexistente **ou** recurso de outro usuário.
- `413` — upload acima de 10 MB.
- `202` reservado para operações assíncronas futuras (não usado na fatia 1).

Regras adicionais:

- `Data` é tipado por endpoint (`GenericResponseData<T>`); nunca objeto solto, nunca string crua.
- Erros de validação de model binding retornam o envelope com `Message` agregando os erros (padrão `[ApiController]` customizado), não `ValidationProblemDetails` default.

## Consequências

- Frontend tem uma única forma de interpretar resposta: olhar status + `Success` + `Message`/`Data`.
- Contrato previsível no Swagger; wrappers de fetch no frontend ficam triviais.
- Exige disciplina: cada endpoint novo segue o padrão (lint/review cuidam disso).
