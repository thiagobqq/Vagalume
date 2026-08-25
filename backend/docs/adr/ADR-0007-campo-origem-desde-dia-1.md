# ADR-0007 — Campo Origem da candidatura desde o dia 1

**Status:** Aceito (2026-08-25)

## Contexto

O Fluxo 5 do produto promete insights como *"vagas que você aplicou via indicação convertem 3x mais que via site"*. Isso só é possível se a origem da candidatura existir **desde a primeira candidatura registrada** — retro-preencher é impossível (o usuário não lembra), e o insight só aparece com histórico acumulado.

## Decisão

A `Candidatura` tem campo obrigatório **`Origem`**, como enum string:

`LinkedIn`, `Gupy`, `Catho`, `Indicacao`, `SiteEmpresa`, `Outro`

- Persistido como string no banco (legível em consultas e dashboards; novo valor não exige migration de tipo).
- Validado na API — valor fora da lista é 400.
- Lista versionável: adicionar origem nova é evolutivo; remover exige cuidado com dados existentes.

## Consequências

- Custo na fatia 1: um campo a mais no form e no DTO. Retorno: a matéria-prima do diferencial de insights (Fluxo 5) acumula desde o dia 1.
- Se a extração por URL chegar, a origem pode ser pré-preenchida a partir do domínio do link (linkedin.com → LinkedIn) — o campo já estará lá.
