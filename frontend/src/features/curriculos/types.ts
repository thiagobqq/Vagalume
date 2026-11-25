export type CategoriaAtsDto = {
  categoria: string
  nota: number
  diagnostico: string
  recomendacoes: string[]
}

export type AlertaAtsDto = {
  severidade: string
  problema: string
  comoCorrigir: string
}

export type AnaliseAtsDto = {
  curriculoId: string
  curriculoNome: string
  score: number
  resumo: string
  categorias: CategoriaAtsDto[]
  alertas: AlertaAtsDto[]
  pontosFortes: string[]
  sugestoes: string[]
  provedor: string
  modelo: string
  criadoEm: string
  atualizadoEm?: string
}
