# ADR-0019 — Gap-analysis currículo × vaga com IA

**Status:** Aceito (2026-09-21) · **Complementado por:** [ADR-0020](ADR-0020-byok-provedor-de-ia-do-usuario.md) (provedor de IA do usuário)

## Contexto

O Fluxo 3 da visão do produto é o diferencial pago: comparar o currículo do candidato com os requisitos de uma vaga e apontar lacunas específicas ("a vaga pede 3 anos de X, seu currículo menciona 1"). O ADR-0003 havia adiado o gap-analysis explicitamente; o ADR-0008 já tinha deixado o contrato pronto ("gap-analysis futuro lê o currículo ativo") e adiado o parse do PDF para esta fatia.

Com a entidade `Vaga` (ADR-0014) e a inbox de descoberta em produção, existem vagas com `Descricao` para comparar. Falta decidir: como extrair texto do PDF, como avaliar aderência, onde o usuário vê o resultado e como cobrar/limitar o acesso.

A avaliação de aderência por regras (o `FiltroVagaService` do ADR-0015) é binária e serve para filtrar a coleta, mas não produz a análise qualitativa (gaps + sugestões) que é o valor pago.

## Decisão

- **Escopo:** a análise compara o **Currículo ativo** (ADR-0008) com uma `Vaga` da inbox. Não é aplicada a `Candidatura` (o kanban permanece intocado).
- **Extração de texto do PDF:** `IExtratorTextoPdf` com **PdfPig** (UglyToad.PdfPig, managed, sem dependência nativa). PDF escaneado (sem camada de texto) resulta em texto vazio e a análise é recusada com `400`, orientando o usuário.
- **Avaliação por IA:** `IAnalisadorCompatibilidade` envia o texto do currículo + os campos da vaga (título, empresa, local, modalidade, nível e descrição sem HTML) a um LLM via API **OpenAI-compatible** (`POST {baseUrl}/chat/completions`), pedindo JSON estrito: `score` (0–100), `resumo`, `requisitos[{requisito, atendido, evidencia, gap}]` e `sugestoes[]`. O provedor é configurado pelo próprio usuário (ADR-0020). O parse tolera JSON em bloco cercado (```json) como fallback.
- **Persistência e cache:** entidade `AnaliseCompatibilidade` única por `(UsuarioId, VagaId)`, guardando `CurriculoId`, provedor/modelo usados e o resultado. `POST` gera/regera (upsert); `GET` devolve a existente sem chamar a IA. Se o currículo ativo mudou desde a análise, a resposta marca `desatualizada=true`.
- **Processamento síncrono:** o `POST` aguarda o LLM (timeout configurável, default 50s), dentro do `proxy_read_timeout 90s` do nginx. Erros do provedor → `502`; timeout → `504`; sem currículo/PDF sem texto/IA não configurada → `400`.
- **Acesso:** todo o módulo (configuração e análise) atrás da permissão `gap_analysis.acesso` (ADR-0013), preparada para o paywall.
- **UI:** botão "compatibilidade" no card da inbox de vagas, abrindo um dialog com score, resumo, requisitos (atendido/gap) e sugestões.

## Consequências

- Implementa o gap-analysis adiado no ADR-0003 e completa o contrato do ADR-0008.
- Custo e latência da IA ficam com o usuário (BYOK, ADR-0020); o operador não paga por análise.
- Privacidade: o texto do currículo e a descrição da vaga saem para o provedor escolhido pelo usuário — avisado na UI.
- `response_format: json_object` não é universal entre provedores compatíveis; o parsing com fallback mitiga. Provedores que não seguirem o formato retornam `502`.
- Sem fila/worker: chamadas longas concorrentes dependem do `IHttpClientFactory` padrão. Se virar gargalo, migrar para o padrão assíncrono do ADR-0017.
- Sem testes automatizados no repo (ADR-0015); extrator, parser e mapeamento são testáveis por design.
