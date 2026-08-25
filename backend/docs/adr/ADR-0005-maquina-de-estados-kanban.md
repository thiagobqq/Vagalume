# ADR-0005 — Máquina de estados do kanban: transições livres, terminal único

**Status:** Aceito (2026-08-25)

## Contexto

O kanban do produto tem colunas *Aplicado → Triagem → Entrevista → Oferta → Recusado/Encerrado*. Decisões pendentes: (a) transições livres ou impostas, (b) quantos status terminais, (c) se existe coluna antes de "Aplicado", (d) se a ordem dos cards dentro da coluna importa.

## Decisão

- **Status:** `Aplicado`, `Triagem`, `Entrevista`, `Oferta`, `Encerrado`.
- **Terminal único:** "Recusado/Encerrado" é um só status, renomeado para **Encerrado**. Motivos distintos (recusa da empresa, desistência do candidato, vaga preenchida) ficam como observação do evento de encerramento (ADR-0006), não como status separados.
- **Transições livres:** qualquer status pode ir para qualquer status, inclusive reabrir um `Encerrado`. A API não impõe ordem — o usuário é a autoridade sobre o próprio processo.
- **Nascimento do card:** toda candidatura nasce em `Aplicado`. Não existe coluna "salvei mas ainda não apliquei" na fatia 1 (reavaliar se doer).
- **Ordem dentro da coluna:** não importa na fatia 1 — cards ordenados por data de candidatura. Reordenação manual (campo posição + endpoint) só entra se houver demanda.

## Consequências

- API de mudança de status trivial de implementar e testar (um endpoint, sem grafo de transições válidas para validar).
- Métricas futuras do funil dependem de distinguir *como* algo encerrou — coberto pela observação do evento, não pelo status; se a observação não for preenchida, a métrica perde granularidade (aceito).
- Transições "estranhas" (Oferta → Aplicado) são permitidas por design: processos reais retrocedem.
