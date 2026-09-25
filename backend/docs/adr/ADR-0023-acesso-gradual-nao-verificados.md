# ADR-0023 — Acesso gradual para usuários não verificados

**Status:** Aceito (2026-09-25)

## Contexto

O ADR-0013 unificou busca de vagas e análise de IA sob a permissão `verify_user`, concedida manualmente enquanto não existe cobrança. Na prática, todo o valor do produto (currículos, inbox de vagas, IA/ATS) ficava atrás do mesmo gate: um usuário recém-cadastrado não conseguia experimentar nada sem depender do mantenedor.

Queremos reduzir o atrito de entrada: permitir que o usuário não verificado experimente o núcleo do produto e só encontre o gate ao tentar os recursos que têm custo/acesso restrito.

## Decisão

- **Liberado para qualquer usuário autenticado** (apenas `[Authorize]`, sem `verify_user`):
  - `/api/curriculos` — enviar, listar, baixar e ativar currículos.
  - `/api/vagas` — inbox, mudança de status e aplicar.
  - `/api/busca` — perfil, fontes, execução e histórico.
- **Fonte liberada sem verificação: apenas `Trampos`.** A regra vive em um único ponto, `AcessoFonte` (`Vagalume.Domain/Authorization/AcessoFonte.cs`): `RequerVerificacao(tipo) => tipo != FonteTipo.Trampos`.
- **Enforcement das demais fontes (Gupy, Programathor, InfoJobs, Catho, LinkedIn, RSS):**
  - `BuscaController.CriarFonte`/`AtualizarFonte` respondem **403** (envelope) para usuário não verificado.
  - `BuscaEnfileirador` só considera fonte ativa permitida ao usuário ao decidir se há configuração.
  - `ColetaVagasService` filtra as fontes ativas pela permissão antes de coletar — cobre busca agendada e usuário que teve `verify_user` revogado.
- **Continuam exclusivas de `verify_user`:** `/api/ia/*`, análise ATS (`/api/curriculos/{id}/analise-ats`) e compatibilidade (`/api/vagas/{id}/compatibilidade`), pois dependem de IA/BYOK.
- **Verificação continua manual**, feita pelo mantenedor via `/app/admin` enquanto o projeto está em desenvolvimento.
- **Frontend é UX, não segurança:** rotas `/app/vagas` e `/app/curriculos` e itens de nav deixam de exigir `verify_user`. Os controles bloqueados (aba de IA, botão de análise ATS, botão de compatibilidade, fontes restritas) exibem um **popup** ao tentar interagir, além de um cadeado discreto; não há aviso inline permanente. A decisão real permanece no backend.

## Consequências

- Usuário não verificado experimenta currículos + busca com Trampos sem depender do mantenedor; encontra o gate apenas em IA/ATS/compatibilidade e nas fontes restritas.
- Não há erro 403 silencioso na UI: os pontos bloqueados são apresentados como bloqueados, com caminho claro para pedir a verificação.
- A regra de fonte fica num único ponto (`AcessoFonte`), usado pela API e pelo pipeline, evitando divergência entre criação e execução.
- Quando o paywall entrar (ADR-0013, `Origem = Assinatura`), a mesma verificação de permissão passa a valer para assinantes **sem alterar controllers ou o pipeline**.
