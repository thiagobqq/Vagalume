# ADR-0002 — Backend dita o contrato de API

**Status:** Aceito (2026-08-25)

## Contexto

O frontend já existe e usa MSW para mockar chamadas, mas os mocks são placeholders sem valor contratual. É preciso definir quem lidera o desenho dos endpoints e dos payloads.

## Decisão

O backend é a fonte da verdade do contrato: rotas, verbos, códigos HTTP e shapes de payload são definidos aqui e documentados via Swagger/OpenAPI. O frontend (e seus mocks MSW) se adapta ao que o backend expõe.

## Consequências

- Não existe "respeitar contrato legado" — a API nasce limpa, sem formas herdados de spike.
- O Swagger é documento canônico; mantê-lo correto (e coerente com o ADR-0011) é obrigação contínua.
- Mudanças de contrato passam a exigir atenção a breaking changes no frontend, já que ele é consumidor ativo desde o dia 1.
