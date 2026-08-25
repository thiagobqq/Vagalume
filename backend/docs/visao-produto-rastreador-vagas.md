# Visão do Produto — Analista de Busca de Emprego

## O problema que resolve

Quem está buscando emprego ativamente (aplicando em várias vagas por semana) enfrenta três dores concretas:

1. **Perde o controle.** Depois de 15-20 candidaturas, ninguém lembra em que empresa aplicou, quando, nem em que fase está. Planilha resolve isso de forma manual e chata — a pessoa esquece de atualizar e abandona.
2. **Não sabe se está aplicando certo.** Manda o mesmo currículo genérico pra tudo, sem saber onde está o gap real entre o que a vaga pede e o que ela tem.
3. **Perde o timing.** Devia fazer follow-up em X dias, mas esquece, ou não sabe o que escrever quando lembra.

O produto não substitui o julgamento da pessoa (não aplica sozinho, não decide por ela) — ele **organiza, extrai informação e sugere a ação certa na hora certa**, coisa que uma planilha nunca faz porque é passiva.

## Quem usa

Pessoa CLT ou PJ, no Brasil, buscando recolocação ou primeira vaga em uma área específica (o público inicial mais fácil de validar é tech, porque é mais digitalizado e mais aberto a pagar por ferramenta — mas o modelo serve pra qualquer área).

## Como a pessoa usa, do início ao fim

### Fluxo 1 — Cadastro e primeira vaga
1. Usuário cria conta (e-mail/senha ou Google).
2. Faz upload do currículo atual (PDF).
3. Vê uma tela vazia tipo kanban com colunas: *Aplicado → Triagem → Entrevista → Oferta → Recusado/Encerrado*.
4. Clica em "adicionar vaga", cola o link da vaga (LinkedIn, Gupy, Catho, etc.).
5. O sistema lê a página automaticamente e preenche: empresa, cargo, requisitos principais, local, modalidade (remoto/híbrido/presencial). Usuário confirma ou ajusta e salva.
6. A vaga aparece como um card na coluna "Aplicado".

### Fluxo 2 — Acompanhamento no dia a dia
1. Usuário recebe e-mail (ou vê no painel) quando uma vaga passa X dias sem atualização: *"Já se passaram 7 dias desde que você aplicou na [Empresa]. Considere um follow-up."*
2. Clica em "gerar follow-up" — o sistema escreve um rascunho de mensagem considerando a vaga, a empresa e quanto tempo passou. Usuário edita e copia pra usar no LinkedIn/e-mail.
3. Conforme o processo avança, o usuário arrasta o card entre as colunas (ou atualiza o status manualmente).

### Fluxo 3 — Comparação com a vaga (o diferencial pago)
1. Ao salvar uma vaga, o sistema compara os requisitos extraídos com o currículo que o usuário subiu.
2. Mostra um painel: "essa vaga pede 3 anos de experiência com X, seu currículo menciona 1 ano" — gaps específicos, não um score genérico.
3. Sugere ajustes pontuais (não reescreve o currículo inteiro — aponta o que faltou).

### Fluxo 4 — Entrevista marcada
1. Quando o usuário move um card pra "Entrevista", o sistema oferece gerar perguntas prováveis com base na vaga salva (cargo, empresa, requisitos) — prep rápido e contextual.

### Fluxo 5 — Visão do funil (o que só existe com o tempo)
1. Depois de algumas semanas de uso, o usuário acessa um painel com métricas do próprio funil: quantas candidaturas viraram entrevista, tempo médio de resposta por tipo de vaga, quais candidaturas pararam na triagem.
2. Isso ajuda a pessoa perceber padrões que sozinha não enxergaria (ex: "vagas que você aplicou via indicação convertem 3x mais que via site — foque nisso").

## O que o sistema NÃO faz (importante deixar claro pro usuário)
- Não aplica em vagas automaticamente — o usuário sempre aplica ele mesmo, o sistema só registra.
- Não garante emprego nem promete "mais entrevistas" como as ferramentas de auto-apply prometem.
- Não substitui a revisão humana do currículo — aponta gaps, não reescreve tudo sozinho.

## Estrutura de cobrança

| Camada | O que inclui |
|---|---|
| **Grátis** | Kanban manual, cadastro de vaga por link com extração automática, lembretes de follow-up básicos |
| **Pago** | Comparação de gap currículo x vaga, geração de follow-up por IA, prep de entrevista contextual, painel de métricas do funil |

A camada grátis já resolve a dor de organização (suficiente pra atrair e reter uso). A camada paga entrega os insights que exigem processamento de IA e que uma planilha jamais teria — é aí que mora a disposição de pagar.

## Resumindo em uma frase
Um painel que centraliza sua busca de emprego, extrai e organiza informação de vaga automaticamente, e te diz — com dados do seu próprio histórico — onde ajustar pra ter mais resultado, sem nunca aplicar por você.
