# ADR-0022 — Análise ATS do currículo (somente IA, por currículo)

**Status:** Aceito (2026-09-25)

## Contexto

O produto já oferece gap-analysis currículo × vaga (ADR-0019), mas o candidato também precisa saber se o próprio currículo é **legível e bem ranqueado por um ATS (Applicant Tracking System)** — problema anterior e independente da vaga. O mercado-alvo é o brasileiro (Gupy, Kenoby, Solides, Vagas.com, Catho, entre outros).

A análise deve:

- exigir a mesma permissão de usuário verificado (`verify_user`) dos demais módulos premium;
- usar a configuração de IA do próprio usuário (BYOK, ADR-0020), sem chave no servidor;
- partir do PDF já enviado (extração de texto existente, `IExtratorTextoPdf`);
- ser acionável por um botão **“analisar”** na lista de currículos, por currículo.

Foi avaliado um template de prompt público (persona de professor de inglês para RH/entrevistas), mas ele não é um analisador de ATS e foi descartado.

## Decisão

- **Nova feature “Análise ATS” por currículo**, exposta em `GET/POST /api/curriculos/{id}/analise-ats`, sob `[Authorize(Policy = Permissoes.VerifyUser)]`.
- **Análise somente por IA** (sem checagens determinísticas de layout): um prompt pt-BR especializado em ATS brasileiro avalia 6 categorias fixas — palavras-chave/competências, formatação e leitura por ATS, dados de contato, experiência e resultados, formação e cursos, idiomas e habilidades — e retorna JSON estrito com `score` (0–100), `resumo`, `categorias[]` (nota + diagnóstico + recomendações), `alertas[]` (severidade `alta|media|baixa`), `pontosFortes[]` e `sugestoes[]`.
- **Persistência por currículo**: entidade `AnaliseAts`, uma por `(UsuarioId, CurriculoId)`, atualizada por upsert na regeração. Reabrir a análise não gasta tokens.
- **Sem dependência da vaga**: a análise é do currículo isolado. A compatibilidade currículo × vaga continua sendo o ADR-0019.
- **Reuso do transporte de IA**: extrai-se `ClienteChatIa` (OpenAI-compatible chat completions, JSON mode, tratamento de 401/403/timeout) compartilhado entre `AnalisadorCompatibilidadeIa` e `AnalisadorAtsIa`; as abstrações de domínio permanecem separadas (`IAnalisadorAts` vs `IAnalisadorCompatibilidade`).
- **Frontend**: botão “analisar” por item de currículo, abrindo um diálogo que carrega a análise salva e gera automaticamente quando ainda não existe.

## Consequências

- O custo de tokens permanece do usuário (BYOK), consistente com o ADR-0019/0020; o botão só aparece para quem já acessa `/app/curriculos` (rota protegida por `verify_user`).
- Métricas de layout que um ATS faz de forma determinística (parsing de colunas, tabelas, imagens) ficam a cargo do LLM, com risco de imprecisão. Se surgir necessidade, uma evolução é complementar com checagens heurísticas no backend — o contrato do DTO já acomoda `alertas` de severidade.
- A análise acompanha o currículo específico (não “o ativo”), então trocar o currículo ativo não invalida análises antigas — cada arquivo tem seu próprio resultado.
- Novo schema (`AnalisesAts`) requer migration; segue o padrão de `AnaliseCompatibilidade` (JSON via `JsonList`, cascade no usuário, `NoAction` no currículo).
- Complementa ADR-0013 (permissões/paywall), ADR-0019 e ADR-0020, que permanecem válidos.
