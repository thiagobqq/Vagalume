# ADR-0016 — Fontes brasileiras e scraping (complementa o ADR-0015)

**Status:** Aceito (2026-09-21)

## Contexto

O ADR-0015 escolheu boards de ATS internacionais (Greenhouse, Lever, Ashby) por serem APIs públicas e de baixo risco. Na prática, esses boards são de empresas majoritariamente americanas e não cobrem o mercado brasileiro, que é o público do produto. As fontes relevantes no Brasil são portais (Gupy, Catho, InfoJobs, Trampos) e o Programathor (tech). A maioria não tem API empresarial aberta, mas expõe dados por APIs públicas de portal ou HTML server-rendered.

## Decisão

- **Fontes:** `Gupy` (API pública do portal), `Trampos` (API JSON v2), `Programathor`, `InfoJobs` e `Catho` (scraping HTML). A fonte `Rss` genérica é mantida para feeds de empresas/portais.
- **Removidos** `Greenhouse`, `Lever`, `Ashby` do enum e do front (dados antigos limpos por migration, já que o módulo é novo).
- **Contrato keyword-driven:** `IFonteVagas.BuscarAsync(FonteBusca, PerfilBusca, CancellationToken)`. Portais buscam pelas palavras-chave/tecnologias do perfil; RSS ignora o perfil.
- **`FonteBusca` flexível:** `Identificador` e `NomeEmpresa` passam a opcionais. Gupy aceita slug de empresa opcional (vazio = portal inteiro); RSS exige URL do feed; fontes de scraping não usam identificador.
- **Scraping com AngleSharp** (dependência nova), isolado por provider. Parâmetros conservadores (`Busca:Scraping`): `DelayMs`, `MaxPaginas`, `MaxItens`, `MaxDetalhes`, `MaxTermos`.
- **User-Agent de navegador** (`Scraping:UserAgent`) e `Accept-Language: pt-BR` nas requisições HTML: portais com WAF (Catho retorna 403 para UA de bot) exigem aparência de navegador. As APIs JSON usam o User-Agent do bot do projeto.
- **Descrição:** Gupy (API) e Trampos (API de detalhe) trazem descrição; InfoJobs busca a página de detalhe; Programathor usa as tags/nível da listagem (a página de detalhe responde 500); Catho usa os dados da listagem (detalhe é renderizado no cliente).
- **`Origem` ao aplicar:** Gupy → `Gupy`; Catho → `Catho`; Trampos/Programathor/InfoJobs → `Outro`.

## Consequências

- O produto passa a cobrir o mercado brasileiro, com a Gupy (dominante) como fonte principal.
- Mais um pacote para manter (AngleSharp) e superfície de manutenção (HTML muda); mitigado por providers isolados e erro por fonte já existente no `ExecucaoBusca`.
- Risco/ToS assumido ao scrapear: Catho tem `robots.txt` restritivo e WAF; por isso rate-limit conservador e UA de navegador. Se bloquear, a falha fica isolada àquela fonte (métrica por fonte registra o erro).
- Trampos tem pouco volume de tecnologia (~10 vagas em ~223); o provider varre as páginas e prioriza categorias `ti`/`dados` para não gastar requisições com irrelevantes.
- Scraping em volume pode exigir proxy/cache em fatia futura. Portais com login obrigatório (LinkedIn/Indeed) permanecem fora do escopo.
- Complementa o ADR-0015, que permanece válido no pipeline, dedup, agendamento e notificação.
