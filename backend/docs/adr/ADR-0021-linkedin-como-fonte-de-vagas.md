# ADR-0021 — LinkedIn como fonte de vagas (revisa o ADR-0016)

**Status:** Aceito (2026-09-22)

## Contexto

O produto é um rastreador de vagas para o mercado brasileiro e o LinkedIn é o principal canal de vagas de tecnologia. O ADR-0016 havia deixado LinkedIn (e Indeed) fora do escopo por exigirem login. Porém, o LinkedIn expõe uma busca pública ("guest") que funciona sem autenticação:

- `GET https://www.linkedin.com/jobs-guest/jobs/api/seeMoreJobPostings/search?keywords=...&geoId=...&start=N` retorna HTML com ~10 cards de vaga.
- `GET https://www.linkedin.com/jobs-guest/jobs/api/jobPosting/{id}` retorna a página com a descrição.

A alternativa autenticada (API interna Voyager) entrega JSON estruturado e mais atributos, mas exige cookie de sessão (`li_at`/`JSESSIONID`), não tem API oficial self-serve e tem alto risco de banir a conta do usuário — justamente a conta usada para se candidatar. Login server-side é inviável na prática (CAPTCHA/checkpoint).

## Decisão

- **Adicionar a fonte `LinkedIn` (`FonteTipo.LinkedIn`)** com scraping **guest, sem autenticação**, no mesmo padrão dos demais portais (`ScraperFonteBase` + AngleSharp).
- **Keyword-driven:** a busca usa as palavras-chave/tecnologias do perfil (`TermosDeBusca`), com paginação por `start` (10 itens/página) até `Busca:Scraping:MaxPaginas`.
- **Localização:** default Brasil via `geoId=106057199`. O campo `Identificador` da fonte (ou `PerfilBusca.Localizacao`) sobrepõe: `geoId:NNN`/numérico usa geoId direto; texto livre usa `location=`.
- **Descrição:** busca o endpoint de detalhe `/jobPosting/{id}` para até `Busca:Scraping:MaxDetalhes` vagas; falha de detalhe é opcional e não descarta a vaga.
- **Resiliência:** `try/catch` por página; após 3 erros consecutivos a fonte encerra sem derrubar a execução. Cada requisição respeita `Busca:Scraping:DelayMs`.
- **Origem ao aplicar:** `FonteTipo.LinkedIn` → `OrigemCandidatura.LinkedIn` (o valor já existia desde o ADR-0007).
- **Sem migration:** `FonteTipo` é persistido como string (o valor novo não altera o schema).

## Consequências

- O produto passa a cobrir o principal canal de vagas de desenvolvimento, sem exigir credenciais do usuário nem armazenar cookies sensíveis.
- Risco assumido de ToS do LinkedIn e de throttling por IP: a busca guest é limitada e pode responder 429/999, sobretudo em IPs de datacenter (Docker/cloud). A falha fica isolada à fonte (métrica por fonte em `ExecucaoBusca`), então não afeta as demais.
- Volume por execução tende a ser menor que o de uma conta autenticada. Se virar gargalo, a evolução natural é cookie **por usuário** (BYOK-style, cifrado com o AES-GCM já existente) ou provedor de scraping/proxy — nunca uma conta compartilhada do app, nem a conta pessoal de candidaturas.
- Parsing de HTML é frágil a mudanças de layout; mitigado por provider isolado e seletores centralizados.
- Complementa o ADR-0015/0016, que permanecem válidos no pipeline, dedup, agendamento e notificação.
