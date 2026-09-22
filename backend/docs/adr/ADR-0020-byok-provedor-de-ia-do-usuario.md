# ADR-0020 — Provedor de IA do usuário (BYOK), presets e cifragem da chave

**Status:** Aceito (2026-09-21) · **Complementa:** [ADR-0019](ADR-0019-gap-analysis-curriculo-vaga-ia.md)

## Contexto

O gap-analysis (ADR-0019) chama um LLM. Duas alternativas: o servidor mantém uma chave global (operador paga por todas as análises) ou cada usuário traz a própria chave (BYOK, *bring your own key*).

A chave global acopla o custo variável da IA ao operador e exige controle de abuso/limites. BYOK transfere custo e responsabilidade ao usuário, encaixa no paywall por permissão (ADR-0013) e é comum em ferramentas de produtividade.

BYOK, porém, traz dois riscos de segurança: guardar um segredo de terceiro e permitir que o usuário informe uma URL arbitrária (SSRF — apontar para localhost, rede interna ou endpoint de metadados da nuvem).

## Decisão

- **BYOK puro:** não há chave/provedor padrão no servidor. Sem provedor configurado pelo usuário, a análise retorna `400`. O servidor nunca usa credencial própria para IA.
- **Provedores pré-definidos (sem URL livre):** `CatalogoProvedoresIa` mapeia um enum `ProvedorIa` (`OpenAi, Gemini, OpenRouter, Groq, Ollama`) para uma `BaseUrl` fixa definida no servidor. **A URL nunca vem do usuário** — elimina SSRF. Provedores locais (Ollama) só existem se o operador definir `Ia:Presets:OllamaBaseUrl`.
- **Configuração por usuário:** entidade `ConfiguracaoIa` (`Provedor`, `Modelo`, `ApiKeyCriptografada`, `Ativo`), única por usuário. Endpoints `GET/PUT /api/ia/configuracao` e `POST /api/ia/testar`, todos sob `verify_user`.
- **Cifragem em repouso:** `ICriptografiaService` com **AES-256-GCM**; a chave de 256 bits é derivada (SHA-256) do segredo `Seguranca:ChaveCripto`. O valor gravado é `nonce(12) || tag(16) || ciphertext` em Base64. A chave em claro **nunca** é devolvida pela API (apenas mascarada: `••••••<4 últimos>`) nem logada.
- **Escolha do AES-GCM em vez de Data Protection:** sem key ring em volume, sem estado adicional; sobrevive a restart e encaixa no padrão de segredos por env já usado (`JWT_KEY`). Trocar `Seguranca:ChaveCripto` invalida as chaves gravadas — o usuário reconfigura (mesmo tradeoff do JWT).
- **Teste de conexão:** `POST /api/ia/testar` faz uma chamada mínima ao provedor e devolve um resultado (`ok`/`mensagem`), permitindo validar a chave antes de salvar.

## Consequências

- O operador não paga por análise; não há chave de IA de terceiros no servidor.
- Onboarding exige um passo extra (configurar provedor) para usar a compatibilidade.
- Segredo derivado de env: rotação de `Seguranca:ChaveCripto` exige que todos reconfigurem a chave de IA.
- `Seguranca:ChaveCripto` é um segredo de produção obrigatório; o default de dev existe apenas para não quebrar o ambiente local.
- Adicionar um provedor OpenAI-compatible novo = adicionar uma entrada no catálogo, sem tocar no analisador.
- A UI avisa que currículo e descrição saem para o provedor escolhido; o usuário responde pela chave e pelos custos na conta dele.
