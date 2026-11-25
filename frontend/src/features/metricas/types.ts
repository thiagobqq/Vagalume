export type EtapaFunilDto = {
  status: string
  atuais: number
  passaram: number
  taxaSobreTotal: number
}

export type FluxoSankeyDto = {
  origem: string
  destino: string
  quantidade: number
  percentualTotal: number
  percentualOrigem: number
}

export type TransicaoHistoricoDto = {
  origem: string
  destino: string
  quantidade: number
  candidaturasUnicas: number
  ehRetorno: boolean
  tempoMedioHoras: number | null
}

export type MetricasOrigemDto = {
  origem: string
  total: number
  diretasEncerradas: number
  avancaram: number
  entrevistas: number
  ofertas: number
  taxaAvanco: number
}

export type CandidaturaFluxoDto = {
  id: string
  empresa: string
  cargo: string
  origem: string
  status: string
  caminho: string[]
  destinoFinalSankey: string | null
  criadoEm: string
  atualizadoEm: string
}

export type InsightFunilDto = {
  tipo: "positivo" | "atencao" | "info" | string
  titulo: string
  descricao: string
}

export type MetricasFunilDto = {
  totalCandidaturas: number
  aguardandoResposta: number
  emAndamento: number
  diretasEncerradas: number
  avancaramParaTriagem: number
  avancaramDeAplicado: number
  chegaramEntrevista: number
  chegaramOferta: number
  totalEncerradas: number
  totalTransicoes: number
  totalRetornos: number
  tempoMedioPrimeiraRespostaDias: number | null
  etapas: EtapaFunilDto[]
  fluxosSankey: FluxoSankeyDto[]
  transicoes: TransicaoHistoricoDto[]
  porOrigem: MetricasOrigemDto[]
  candidaturas: CandidaturaFluxoDto[]
  insights?: InsightFunilDto[]
}

export type PeriodoFiltro = "all" | "30d" | "90d" | "180d" | "365d" | "custom"

export type SelecaoSankey =
  | { tipo: "fluxo"; origem: string; destino: string }
  | { tipo: "no"; id: string }
  | null
