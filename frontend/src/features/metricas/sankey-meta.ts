export type SankeyNodeMeta = {
  label: string
  shortHint: string
  color: string
  soft: string
  column: number
  orderInColumn: number
}

export const SANKEY_NODE_META: Record<string, SankeyNodeMeta> = {
  Aplicado: {
    label: "Aplicado",
    shortHint: "Total de candidaturas",
    color: "var(--st-applied)",
    soft: "var(--st-applied-soft)",
    column: 0,
    orderInColumn: 0,
  },
  Triagem: {
    label: "Triagem",
    shortHint: "Avançaram para triagem",
    color: "var(--st-triage)",
    soft: "var(--st-triage-soft)",
    column: 1,
    orderInColumn: 0,
  },
  Entrevista: {
    label: "Entrevista",
    shortHint: "Chegaram em entrevista",
    color: "var(--st-interview)",
    soft: "var(--st-interview-soft)",
    column: 2,
    orderInColumn: 0,
  },
  Oferta: {
    label: "Oferta",
    shortHint: "Receberam oferta",
    color: "var(--st-offer)",
    soft: "var(--st-offer-soft)",
    column: 3,
    orderInColumn: 0,
  },
  EmAndamento: {
    label: "Em processo",
    shortHint: "Ativas em triagem/entrevista/oferta",
    color: "var(--crayon-green)",
    soft: "var(--crayon-green-soft)",
    column: 4,
    orderInColumn: 0,
  },
  Aguardando: {
    label: "Aguardando",
    shortHint: "Ainda sem resposta em Aplicado",
    color: "var(--crayon-orange)",
    soft: "var(--crayon-orange-soft)",
    column: 4,
    orderInColumn: 1,
  },
  Encerrado: {
    label: "Encerrado",
    shortHint: "Processos encerrados",
    color: "var(--st-closed)",
    soft: "var(--st-closed-soft)",
    column: 4,
    orderInColumn: 2,
  },
}
