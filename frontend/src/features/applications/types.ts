export type Status = "aplicado" | "triagem" | "entrevista" | "oferta" | "recusado"

export type Application = {
  id: string
  empresa: string
  cargo: string
  link: string
  status: Status
  dataAplicacao: string // ISO
  requisitos?: string[]
  observacoes?: string
  followUpNotified?: boolean
}

export const STATUS_LABEL: Record<Status, string> = {
  aplicado: "Aplicado",
  triagem: "Triagem",
  entrevista: "Entrevista",
  oferta: "Oferta",
  recusado: "Recusado",
}

export const STATUS_COLOR: Record<Status, string> = {
  aplicado: "yellow",
  triagem: "blue",
  entrevista: "purple",
  oferta: "green",
  recusado: "pink",
}

export const STATUS_ORDER: Status[] = ["aplicado", "triagem", "entrevista", "oferta", "recusado"]
